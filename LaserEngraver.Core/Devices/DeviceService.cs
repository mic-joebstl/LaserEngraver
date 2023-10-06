using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.Core.Jobs;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Devices
{
	public class DeviceDispatcherService : INotifyPropertyChanged
	{
		private readonly object _syncRoot = new object();
		private DeviceConfiguration _deviceConfiguration;
		private Device? _device;
		private Job? _currentJob;

		public DeviceDispatcherService(IOptions<DeviceConfiguration> deviceConfiguration)
		{
			_deviceConfiguration = deviceConfiguration.Value;
		}

		public event DeviceStatusChangedEventHandler? DeviceStatusChanged;

		public event JobStatusChangedEventHandler? JobStatusChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		public DeviceStatus DeviceStatus => _device?.Status ?? DeviceStatus.Disconnected;

		public JobStatus JobStatus => _currentJob?.Status ?? JobStatus.None;

		public async Task Connect(CancellationToken cancellationToken)
		{
			if (_device == null)
			{
				lock (_syncRoot)
				{
					if (_device == null)
					{
						if (_deviceConfiguration.Type == DeviceType.None)
						{
							throw new InvalidOperationException("Invalid device-configuration");
						}
						else if (_deviceConfiguration.Type == DeviceType.Mock)
						{
							_device = new MockDevice();
							_device.StatusChanged += (Device device, DeviceStatusChangedEventArgs args) => DeviceStatusChanged?.Invoke(device, args);
						}
						else
						{
							throw new NotSupportedException("Devicetype " + _deviceConfiguration.Type + " not supported.");
						}
					}
				}
			}

			DeviceStatusChanged?.Invoke(_device, new DeviceStatusChangedEventArgs(_device.Status));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeviceStatus)));
			await _device.ConnectAsync(cancellationToken);
		}

		public async Task Disconnect(CancellationToken cancellationToken)
		{
			Device? device = null;
			if (_device != null)
			{
				lock (_syncRoot)
				{
					if (_device != null)
					{
						device = _device;
						_device = null;
					}
				}

				if (device != null)
				{
					await device.DisconnectAsync(cancellationToken);
					DeviceStatusChanged?.Invoke(_device, new DeviceStatusChangedEventArgs(_device.Status));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeviceStatus)));
				}
			}
		}
	}
}
