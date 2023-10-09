using LaserEngraver.Core.Configurations;
using LaserEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;

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
		private Size _burnAreaSize;
		private DeviceConfiguration _deviceConfiguration;
		private BurnConfiguration _burnConfiguration;
		private IEnumerable<IEngravePoint> _source;
		private IEngravePoint? _currentPoint;

		public EngraveJob(DeviceConfiguration deviceConfiguration, BurnConfiguration burnConfiguration, Size burnAreaSize, Point pointTranslation, IEnumerable<IEngravePoint> source)
		{
			_pointTranslation = pointTranslation;
			_burnAreaSize = burnAreaSize;
			_deviceConfiguration = deviceConfiguration;
			_burnConfiguration = burnConfiguration;
			_source = source;
		}

		public override string Title => Resources.Localization.Texts.EngraveJobTitle;

		public void Pause()
		{
			_signalPause = true;
		}

		private EngravePointContext<T>? GetLeftmostNeighbor<T>(IEnumerable<EngravePointContext<T>> points, EngravePointContext<T>? currentPoint, CancellationToken cancellationToken)
		{
			if (currentPoint is null || points is null)
			{
				return null;
			}

			var list = points.OrderBy(p => p.Position.X).ToList();
			var other = list.Find(p => p.Position.X == currentPoint.Position.X - 1);
			if (other != null)
			{
				var io = list.IndexOf(other);
				for (var i = io - 1; i >= 0; i--)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var current = list[i];
					if (current.Position.X != other.Position.X - 1)
					{
						break;
					}
					other = current;
				}
				return other;
			}

			return null;
		}

		private bool TryGetNextPoint<T>(IEnumerable<EngravePointContext<T>> points, EngravePointContext? currentPoint, CancellationToken cancellationToken, [MaybeNullWhen(false)] out EngravePointContext<T> nextPoint)
		{
			nextPoint = null;

			if (currentPoint is null)
			{
				foreach (var point in points)
				{
					if (nextPoint is null || nextPoint.Position.Y > point.Position.Y || nextPoint.Position.X > point.Position.X && nextPoint.Position.Y == point.Position.Y)
					{
						nextPoint = point;
					}
				}
				return nextPoint is not null;
			}

			var nextPointWeight = long.MaxValue;
			foreach (var point in points)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var weightXDistance = point.Position.X > currentPoint?.Position.X ? 0 : 1;
				var weightYDistance = point.Position.Y == currentPoint?.Position.Y ? 0 : point.Position.Y > currentPoint?.Position.Y ? 1 : 2;
				long weightDistance = 0;
				if (currentPoint is not null)
				{
					var x = point.Position.X - currentPoint.Position.X;
					var y = point.Position.Y - currentPoint.Position.Y;
					var sqrDistance = x * x + y * y;
					weightDistance = sqrDistance < 2 ? 2 : sqrDistance;
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
				var sameLinePoints = points.Where(other => other.Position.Y == p.Position.Y);
				nextPoint = GetLeftmostNeighbor(sameLinePoints, p, cancellationToken) ?? p;
				return true;
			}

			return false;
		}

		private IEnumerable<IGrouping<Point, IEngravePoint>> GetClusters(IEnumerable<IEngravePoint> points, CancellationToken cancellationToken)
		{
			var totalPx = _burnAreaSize.Width * _burnAreaSize.Height;
			if (totalPx <= 0)
			{
				yield break;
			}

			var pendingPx = points.Count();
			if (pendingPx == 0)
			{
				yield break;
			}

			var avgPxPerCluster = 64;
			var clusterSizePx = avgPxPerCluster * totalPx / pendingPx;
			var clusters = points
				.GroupBy(point =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					var trimX = point.X % clusterSizePx;
					var trimY = point.Y % clusterSizePx;
					return new Point(point.X - trimX, point.Y - trimY);
				})
				.OrderBy(point => point.Key.X)
				.ThenBy(point => point.Key.Y);

			foreach (var cluster in clusters)
			{
				yield return cluster;
			}
		}

		private IEnumerable<IEngravePoint> GetSortedPoints(CancellationToken cancellationToken)
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
				var clusters = new HashSet<EngravePointContext<IGrouping<Point, IEngravePoint>>>(GetClusters(relevantPoints, cancellationToken).Select(EngravePointContext.Create));
				var currentCluster = default(EngravePointContext);
				while (TryGetNextPoint(clusters, currentCluster, cancellationToken, out var nextCluster))
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (!clusters.Remove(nextCluster))
					{
						continue;
					}

					var cluster = nextCluster.Context;
					if (cluster is null)
					{
						continue;
					}

					currentCluster = nextCluster;

					var unhandledPoints = new HashSet<EngravePointContext<IEngravePoint>>(cluster.Select(EngravePointContext.Create));
					while (TryGetNextPoint(unhandledPoints, _currentPoint is null ? null : EngravePointContext.Create(_currentPoint), cancellationToken, out var nextPoint))
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (unhandledPoints.Remove(nextPoint))
						{
							_currentPoint = nextPoint?.Context;
							if (_currentPoint is not null)
							{
								yield return _currentPoint;
							}
						}
					}
				}
			}
		}

		private IEnumerable<IEngravePoint[]> GetPointGroups(CancellationToken cancellationToken)
		{
			int? currentX = null;
			int? currentY = null;
			byte? currentIntensity = null;
			var pointGroup = new List<IEngravePoint>();
			foreach (var point in GetSortedPoints(cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();
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
#if DEBUG
			var delayMilliseconds = 5d;
			var sw = new Stopwatch();
			sw.Start();
			long i = 0;
			var mockDevice = device as MockDevice;
#endif
			try
			{
				_signalPause = false;
				var powerFactor = _burnConfiguration.Power / 255d;
				var power = (ushort)(_deviceConfiguration.MaximumPowerMilliwatts * powerFactor);

				foreach (var pointGroup in GetPointGroups(cancellationToken))
				{
					var startingPoint = pointGroup[0];
					var intensityFactor = startingPoint.Intensity / 255d;
					var duration = (byte)(_burnConfiguration.Duration * intensityFactor);

					var imageStartingPoint = new Point { X = startingPoint.X, Y = startingPoint.Y };
					imageStartingPoint.Offset(_pointTranslation);

					await device.MoveAbsoluteAsync(imageStartingPoint, cancellationToken);
					await device.Engrave(power, duration, pointGroup.Length, cancellationToken);

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
			}
			finally
			{
				if (device is Devices.Serial.SerialDevice serialDevice)
				{
					serialDevice.SignalEngravingCompleted();
				}
			}

#if DEBUG
			sw.Stop();
#endif
		}

		private class EngravePointContext
		{
			public Point Position { get; set; }

			public static EngravePointContext<IEngravePoint> Create(IEngravePoint context)
			{
				return new EngravePointContext<IEngravePoint>
				{
					Context = context,
					Position = new Point(context.X, context.Y)
				};
			}

			public static EngravePointContext<IGrouping<Point, IEngravePoint>> Create(IGrouping<Point, IEngravePoint> context)
			{
				return new EngravePointContext<IGrouping<Point, IEngravePoint>>
				{
					Context = context,
					Position = new Point(context.Key.X, context.Key.Y)
				};
			}
		}
		private class EngravePointContext<T> : EngravePointContext
		{
			public T? Context { get; set; }
		}
	}
}
