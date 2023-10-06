using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Devices
{
	public class MockDevice : Device
	{
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

				await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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
					await Task.Delay(Math.Abs(vector.X) * TimeSpan.FromMilliseconds(1) + Math.Abs(vector.Y) * TimeSpan.FromMilliseconds(1), cancellationToken);
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

				var sw = new Stopwatch();
				sw.Start();
				long i = 0;
				while (Position != null && (Position.Value.X != position.X || Position.Value.Y != position.Y))
				{
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

					if (++i > sw.ElapsedMilliseconds / 2)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(i - sw.ElapsedMilliseconds / 2), cancellationToken);

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
	}
}
