using LaserEngraver.Core.Configurations;
using LaserEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserEngraver.Core.Jobs
{
	public interface IEngravePoint
	{
		public int X { get; set; }
		public int Y { get; set; }
		public bool IsVisited { get; set; }
		public byte Intensity { get; set; }
	}

	public sealed class EngraveJob : Job, IPausableJob
	{
		private bool _signalPause;
		private Point _pointTranslation;
		private DeviceConfiguration _deviceConfiguration;
		private BurnConfiguration _burnConfiguration;
		private IEnumerable<IEngravePoint> _source;
		private IEngravePoint? _currentPoint;

		public EngraveJob(DeviceConfiguration deviceConfiguration, BurnConfiguration burnConfiguration, Point pointTranslation, IEnumerable<IEngravePoint> source)
		{
			_pointTranslation = pointTranslation;
			_deviceConfiguration = deviceConfiguration;
			_burnConfiguration = burnConfiguration;
			_source = source;
		}

		public override string Title => Resources.Localization.Texts.EngraveJobTitle;

		public void Pause()
		{
			_signalPause = true;
		}

		private IEnumerable<IEngravePoint> GetSortedPoints()
		{
			var relevantPoints = _source.Where(x => !x.IsVisited && x.Intensity > 0);
			if (_burnConfiguration.PlottingMode == BurnPlottingMode.Raster)
			{
				var points = relevantPoints
					.OrderBy(x => x.Y)
					.ThenBy(x => x.Y % 2 == 0 ? x.X : x.X * -1);

				int? currentX = null;
				int? currentY = null;
				var pointGroup = new List<IEngravePoint>();
				foreach (var point in points)
				{
					if (currentY != point.Y || currentX is null || point.Y % 2 == 0 ? currentX + 1 != point.X : currentX - 1 != point.X)
					{
						if (pointGroup.Count > 0)
						{
							foreach(var groupPoint in pointGroup.OrderBy(p => p.X))
							{
								yield return groupPoint;
							}
						}
						pointGroup.Clear();
					}

					currentX = point.X;
					currentY = point.Y;

					pointGroup.Add(point);
				}
				if (pointGroup.Count > 0)
				{
					foreach (var groupPoint in pointGroup.OrderBy(p => p.X))
					{
						yield return groupPoint;
					}
				}
			}
			else
			{
				var unhandledPoints = new HashSet<IEngravePoint>(relevantPoints);

				int clusterSize = 1;
				int clusterSizeSqr = 1;
				{
					int? minX = null;
					int? minY = null;
					int? maxX = null;
					int? maxY = null;
					foreach (var point in unhandledPoints)
					{
						if (minX is null || minX > point.X)
							minX = point.X;
						if (maxX is null || maxX < point.X)
							maxX = point.X;
						if (minY is null || minY > point.Y)
							minY = point.Y;
						if (maxY is null || maxY < point.Y)
							maxY = point.Y;
					}

					int width = minX is not null && maxX is not null ? maxX.Value - minX.Value : 1;
					int height = minY is not null && maxY is not null ? maxY.Value - minY.Value : 1;
					int diameter = (int)Math.Sqrt(width * width + height * height);

					clusterSize = diameter / 20;
					clusterSize = clusterSize == 0 ? 1 : clusterSize;
					int log = Math.Clamp((int)Math.Log2(clusterSize), 1, 16) - 1;

					clusterSize = 2 << log;
					clusterSizeSqr = clusterSize * clusterSize;
				}

				bool TryGetNextPoint(IEngravePoint? currentPoint, [MaybeNullWhen(false)] out IEngravePoint nextPoint)
				{
					nextPoint = unhandledPoints?
						.Where(other => other != currentPoint)
						.OrderBy(other =>
						{
							var x = other.X - (currentPoint?.X ?? 0);
							var y = other.Y - (currentPoint?.Y ?? 0);
							var sqrDistance = x * x + y * y;
							var trim = sqrDistance % clusterSizeSqr;
							return sqrDistance - trim;
						})
						.ThenBy(other => other.Y)
						.ThenBy(other => other.X > (currentPoint?.X ?? 0) ? 0 : 1)
						.ThenBy(other => other.X - (currentPoint?.X ?? 0))
						.FirstOrDefault();

					return nextPoint != null;
				}

				while (TryGetNextPoint(_currentPoint, out var nextPoint))
				{
					if (unhandledPoints.Remove(nextPoint))
					{
						yield return _currentPoint = nextPoint;
					}
				}
			}
		}

		private IEnumerable<IEngravePoint[]> GetPointGroups()
		{
			int? currentX = null;
			int? currentY = null;
			byte? currentIntensity = null;
			var pointGroup = new List<IEngravePoint>();
			foreach (var point in GetSortedPoints())
			{
				if (currentY != point.Y || currentX is null || currentX + 1 != point.X || currentIntensity != point.Intensity)
				{
					if (pointGroup.Count > 0)
					{
						yield return pointGroup.ToArray();
					}
					pointGroup.Clear();
				}

				currentX = point.X;
				currentY = point.Y;
				currentIntensity = point.Intensity;

				pointGroup.Add(point);
			}
			if (pointGroup.Count > 0)
			{
				yield return pointGroup.ToArray();
			}
		}

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			_signalPause = false;
			var duration = _burnConfiguration.Duration;

#if DEBUG
			var delayMilliseconds = 5d;
			var sw = new Stopwatch();
			sw.Start();
			long i = 0;
			var mockDevice = device as MockDevice;
#endif

			foreach (var pointGroup in GetPointGroups())
			{
				var startingPoint = pointGroup[0];

				var intensityFactor = startingPoint.Intensity / 255d;
				var powerMilliwatt = (ushort)(_deviceConfiguration.MaximumPowerMilliwatts * intensityFactor);

				var imageStartingPoint = new Point { X = startingPoint.X, Y = startingPoint.Y };
				imageStartingPoint.Offset(_pointTranslation);

				await device.MoveAbsoluteAsync(imageStartingPoint, cancellationToken);
				await device.Engrave(powerMilliwatt, duration, imageStartingPoint, pointGroup.Length, cancellationToken);

				foreach (var point in pointGroup)
				{
					point.IsVisited = true;
				}

				if (_signalPause)
				{
					Status = JobStatus.Paused;
					return;
				}

#if DEBUG
				if (mockDevice is not null)
				{
					var targetTime = i++ * delayMilliseconds;
					var elapsedTime = sw.ElapsedMilliseconds;
					var currentDelay = targetTime - elapsedTime;
					if (currentDelay > 0)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(currentDelay), cancellationToken);
					}
				}
#endif
			}

#if DEBUG
			sw.Stop();
#endif
		}
	}
}
