using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Jobs
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
		private BurnConfiguration _configuration;
		private IEnumerable<IEngravePoint> _source;
		private IEngravePoint? _currentPoint;

		public EngraveJob(BurnConfiguration configuration, Point pointTranslation, IEnumerable<IEngravePoint> source)
		{
			_pointTranslation = pointTranslation;
			_configuration = configuration;
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
			if (_configuration.PlottingMode == BurnPlottingMode.Raster)
			{
				var points = relevantPoints
					.OrderBy(x => x.Y)
					.ThenBy(x => x.Y % 2 == 0 ? x.X : x.X * -1);
				foreach(var point in points)
				{
					yield return point;
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

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			_signalPause = false;
			var duration = _configuration.Duration;

#if DEBUG
			var delayMilliseconds = 5d;
			var sw = new Stopwatch();
			sw.Start();
			long i = 0;
			var mockDevice = device as MockDevice;
			Point? previousPoint = default;
#endif

			foreach (var point in GetSortedPoints())
			{
				var imagePoint = new Point { X = point.X, Y = point.Y };
				imagePoint.Offset(_pointTranslation);

#if DEBUG
				var moveImmediate =
					mockDevice is not null &&
					previousPoint != null &&
					Math.Abs(imagePoint.X - previousPoint.Value.X) <= 1 &&
					Math.Abs(imagePoint.Y - previousPoint.Value.Y) <= 1;

				if (moveImmediate)
				{
					mockDevice.MoveAbsoluteImmediate(imagePoint);
				}
				else
				{
					sw.Stop();
					await device.MoveAbsoluteAsync(imagePoint, cancellationToken);
					sw.Start();
				}
#else
				await device.MoveAbsoluteAsync(imagePoint, cancellationToken);
#endif

				await device.Engrave(point.Intensity, duration, cancellationToken);

				point.IsVisited = true;

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

				if (_signalPause)
				{
					Status = JobStatus.Paused;
					return;
				}
			}

#if DEBUG
			sw.Stop();
#endif
		}
	}
}
