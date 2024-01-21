using LaserEngraver.Core.Devices.Serial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserEngraver.Core.Devices
{
	public class MockDevice : Device
	{
		public TimeSpan TimeSpanPerPoint { get; set; } = TimeSpan.FromTicks(TimeSpan.FromSeconds(2).Ticks / 1600);

		public override async Task ConnectAsync(CancellationToken cancellationToken)
		{
			using (var tx = StatusTransition(DeviceStatus.Disconnected, DeviceStatus.Connecting, DeviceStatus.Ready))
			{
				tx.Open();

				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

				tx.Commit();
			}
		}

		public override async Task DisconnectAsync(CancellationToken cancellationToken)
		{
			using (var tx = StatusTransition(DeviceStatus.Ready, DeviceStatus.Disconnecting, DeviceStatus.Disconnected))
			{
				tx.Open();

				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

				tx.Commit();
			}
		}

		public override async Task HomingAsync(CancellationToken cancellationToken)
		{

			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
				Position = new System.Drawing.Point(0, 0);
			}
		}

		public override async Task MoveRelativeAsync(Point vector, CancellationToken cancellationToken)
		{
			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				var position = Position;
				if (position != null)
				{
					var length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
					await Task.Delay(TimeSpanPerPoint * length, cancellationToken);
					Position = new Point
					{
						X = position.Value.X + vector.X,
						Y = position.Value.Y + vector.Y
					};
				}
			}
		}

		public override async Task MoveAbsoluteAsync(Point position, CancellationToken cancellationToken)
		{
			using(var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				var delayMilliseconds = TimeSpanPerPoint.TotalMilliseconds;
				var sw = new Stopwatch();
				sw.Start();
				long i = 0;
				while (Position != null && (Position.Value.X != position.X || Position.Value.Y != position.Y))
				{
					cancellationToken.ThrowIfCancellationRequested();

					var vector = new Point
					{
						X = position.X - Position.Value.X,
						Y = position.Y - Position.Value.Y
					};
					var unitVector = new Point
					{
						X = vector.X != 0 ? vector.X / Math.Abs(vector.X) : 0,
						Y = vector.Y != 0 ? vector.Y / Math.Abs(vector.Y) : 0
					};

					var targetTime = i++ * delayMilliseconds;
					var elapsedTime = sw.ElapsedMilliseconds;
					var currentDelay = targetTime - elapsedTime;
					if (i == 0 || currentDelay > 0)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(currentDelay));
					}

					Position = new Point
					{
						X = Position.Value.X + unitVector.X,
						Y = Position.Value.Y + unitVector.Y,
					};
				}
				sw.Stop();
			}
		}

		public void MoveAbsoluteImmediate(Point position)
		{
			Position = position;
		}

		public override async Task Engrave(ushort powerMilliwatt, byte duration, int length, CancellationToken cancellationToken)
		{
			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();
				await Task.Delay(TimeSpanPerPoint * length);
				var direction = EngraveDirection.RightToLeft;
				var position = Position;
				if (position != null)
				{
					if (direction == EngraveDirection.LeftToRight)
					{
						Position = new Point(position.Value.X + length - 1, position.Value.Y + 1);
					}
					else
					{
						Position = new Point(position.Value.X + length, position.Value.Y);
					}
				}
				await Task.Delay(TimeSpan.Zero);
			}
		}
	}
}
