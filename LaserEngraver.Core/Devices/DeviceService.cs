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

		public TimeSpan JobElapsedDuration => _currentJob?.ElapsedDuration ?? TimeSpan.Zero;

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
							_device.StatusChanged += OnDeviceStatusChanged;
						}
						else
						{
							throw new NotSupportedException("Devicetype " + _deviceConfiguration.Type + " not supported.");
						}
					}
				}
			}

			DeviceStatusChanged?.Invoke(_device, new DeviceStatusChangedEventArgs(DeviceStatus));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeviceStatus)));
			await _device.ConnectAsync(cancellationToken);
		}

		private void OnDeviceStatusChanged(Device sender, DeviceStatusChangedEventArgs args)
		{
			DeviceStatusChanged?.Invoke(sender, args);
		}

		private void OnJobStatusChanged(Job sender, JobStatusChangedEventArgs jobStatusChangedEventArgs)
		{
			JobStatusChanged?.Invoke(sender, jobStatusChangedEventArgs);
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
					}
				}

				if (device != null)
				{
					await device.DisconnectAsync(cancellationToken);
					lock (_syncRoot)
					{
						if (_device == device)
						{
							_device = null;
						}
						device.StatusChanged -= OnDeviceStatusChanged;
					}
					DeviceStatusChanged?.Invoke(device, new DeviceStatusChangedEventArgs(DeviceStatus));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeviceStatus)));
				}
			}
		}

		public async Task ExecuteJob(Job job, CancellationToken cancellationToken)
		{
			Device device;
			lock (_syncRoot)
			{
				if (_currentJob != null)
				{
					if (_currentJob.Status == JobStatus.Running)
					{
						_currentJob.Cancel();
					}
					else
					{
						_currentJob.Dispose();
					}
					_currentJob.StatusChanged -= OnJobStatusChanged;
				}
				_currentJob = job;
				_currentJob.StatusChanged += OnJobStatusChanged;
				device = _device ?? throw new InvalidOperationException("Device not connected");
			}


			await job.ExecuteAsync(device, cancellationToken);
		}
	}
}
