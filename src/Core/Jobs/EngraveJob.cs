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
					.ThenBy(x => x.X);

				foreach (var point in points)
				{
					yield return point;
				}
			}
			else
			{
				var unhandledPoints = new HashSet<IEngravePoint>(relevantPoints);

				IEngravePoint? GetLeftmostNeighbor(IEngravePoint? context, IEnumerable<IEngravePoint>? points)
				{
					if (context is null || points is null)
					{
						return null;
					}

					var list = points.OrderBy(p => p.X).ToList();
					var other = list.Find(p => p.X == context.X - 1);
					if (other != null)
					{
						var io = list.IndexOf(other);
						for (var i = io - 1; i >= 0; i--)
						{
							var current = list[i];
							if (current.X != other.X - 1)
							{
								break;
							}
							other = current;
						}
						return other;
					}

					return null;
				}

				bool TryGetNextPoint(IEngravePoint? currentPoint, [MaybeNullWhen(false)] out IEngravePoint nextPoint)
				{
					////The following declarative is equal to the following imperative code, but much slower
					//nextPoint = unhandledPoints?
					//	.Where(other => other != currentPoint)
					//	.OrderBy(other =>
					//	{
					//		var x = other.X - (currentPoint?.X ?? 0);
					//		//var xSqr = x * x;

					//		var y = other.Y - (currentPoint?.Y ?? 0);
					//		//var ySqr = y * y;

					//		var sqrDistance = x * x + y * y;
					//		return sqrDistance < 2 ? 2 : sqrDistance;
					//	})
					//	.ThenBy(other => other.X > currentPoint?.X ? 0 : 1)
					//	.ThenBy(other => other.Y == currentPoint?.Y ? 0 : other.Y > currentPoint?.Y ? 1 : 2)
					//	.FirstOrDefault();

					nextPoint = null;
					var nextPointWeight = long.MaxValue;
					foreach(var point in unhandledPoints) 
					{
						var weightXDistance = point.X > currentPoint?.X ? 0 : 1;
						var weightYDistance = point.Y == currentPoint?.Y ? 0 : point.Y > currentPoint?.Y ? 1 : 2;
						long weightDistance = 0;
						{
							var x = point.X - (currentPoint?.X ?? 0);
							var y = point.Y - (currentPoint?.Y ?? 0);
							var sqrDistance = x * x + y * y;
							weightDistance =  sqrDistance < 2 ? 2 : sqrDistance;
						}

						long weight = (weightDistance << 2) + (weightXDistance << 1) + weightYDistance;
						if (weight < nextPointWeight)
						{
							nextPointWeight = weight;
							nextPoint = point;
						}
					}

					if (nextPoint != null)
					{
						var p = nextPoint;
						var sameLinePoints = unhandledPoints?.Where(other => other.Y == p.Y);
						nextPoint = GetLeftmostNeighbor(p, sameLinePoints) ?? p;
						return true;
					}

					return false;
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
						yield return pointGroup.OrderBy(p => p.X).ToArray();
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
				yield return pointGroup.OrderBy(p => p.X).ToArray();
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
				await device.Engrave(powerMilliwatt, duration, pointGroup.Length, cancellationToken);

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
