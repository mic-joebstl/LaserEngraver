using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
			if (_configuration.PlottingMode == BurnPlottingMode.Rasterized)
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

				IEnumerable<IEngravePoint> HandleCluster(IEngravePoint point)
				{
					if (unhandledPoints.Remove(point))
					{
						yield return point;

						var neighbor = unhandledPoints.FirstOrDefault(other =>
						{
							var x = other.X - point.X;
							var y = other.Y - point.Y;
							return x * x + y * y <= 2;
						});

						if (neighbor is not null)
						{
							foreach (var p in HandleCluster(neighbor))
							{
								yield return p;
							}
						}
					}
				}

				while (unhandledPoints.Count > 0)
				{
					_currentPoint = _currentPoint is null ? unhandledPoints.First() :
						unhandledPoints
							//the exact vector length is not necessary to find the nearest point, just a comparable absolute weighting value
							.OrderBy(p =>
							{
								var x = p.X - _currentPoint.X;
								var y = p.Y - _currentPoint.Y;
								return x * x + y * y;
							})
							.First();

					foreach (var p in HandleCluster(_currentPoint))
					{
						yield return p;
						_currentPoint = p;
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
					mockDevice is not null && (
						i == 0 || previousPoint == null || (
							Math.Abs(imagePoint.X - previousPoint.Value.X) > 1 ||
							Math.Abs(imagePoint.Y - previousPoint.Value.Y) > 1
						)
					);
				if (moveImmediate)
				{
					mockDevice.MoveAbsoluteImmediate(imagePoint);
				}
				else
				{
					await device.MoveAbsoluteAsync(imagePoint, cancellationToken);
				}
#else
				await device.MoveAbsoluteAsync(imagePoint, cancellationToken);
#endif

				await device.Engrave(point.Intensity, duration, cancellationToken);

				point.IsVisited = true;

#if DEBUG
				var targetTime = i++ * delayMilliseconds;
				var elapsedTime = sw.ElapsedMilliseconds;
				var currentDelay = targetTime - elapsedTime;
				if (currentDelay > 0)
				{
					await Task.Delay(TimeSpan.FromMilliseconds(currentDelay), cancellationToken);
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
