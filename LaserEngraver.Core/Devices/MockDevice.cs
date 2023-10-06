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
			lock (SyncRoot)
			{
				DemandState(DeviceStatus.Disconnected);
				Status = DeviceStatus.Connecting;
			}
			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			Status = DeviceStatus.Ready;
		}

		public override async Task DisconnectAsync(CancellationToken cancellationToken)
		{
			lock (SyncRoot)
			{
				DemandState(DeviceStatus.Ready);
				Status = DeviceStatus.Disconnecting;
			}
			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			Status = DeviceStatus.Disconnected;
		}

		public override async Task HomingAsync(CancellationToken cancellationToken)
		{
			lock (SyncRoot)
			{
				DemandState(DeviceStatus.Ready);
				Status = DeviceStatus.Executing;
			}
			await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			Position = new System.Drawing.Point(0, 0);
			Status = DeviceStatus.Ready;
		}

		public override async Task MoveRelativeAsync(Point vector, CancellationToken cancellationToken)
		{
			lock (SyncRoot)
			{
				DemandState(DeviceStatus.Ready);
				Status = DeviceStatus.Executing;
			}

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

			Status = DeviceStatus.Ready;
		}

		public override async Task MoveAbsoluteAsync(Point position, CancellationToken cancellationToken)
		{
			lock (SyncRoot)
			{
				DemandState(DeviceStatus.Ready);
				Status = DeviceStatus.Executing;
			}

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

			Status = DeviceStatus.Ready;
		}
	}
}
