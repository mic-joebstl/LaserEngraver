using System;
using System.Collections.Generic;
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
	}
}
