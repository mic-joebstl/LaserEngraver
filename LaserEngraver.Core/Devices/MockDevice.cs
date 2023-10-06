using System;
using System.Collections.Generic;
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

			var oldPosition = Position;
			if (oldPosition != null)
			{
				var vector = new Point
				{
					X = position.X - oldPosition.Value.X,
					Y = position.Y - oldPosition.Value.Y
				};
				await Task.Delay(Math.Abs(vector.X) * TimeSpan.FromMilliseconds(1) + Math.Abs(vector.Y) * TimeSpan.FromMilliseconds(1), cancellationToken);
				Position = position;
			}

			Status = DeviceStatus.Ready;
		}
	}
}
