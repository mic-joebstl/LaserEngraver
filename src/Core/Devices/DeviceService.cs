using LaserEngraver.Core.Configurations;
using LaserEngraver.Core.Jobs;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace LaserEngraver.Core.Devices
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

		public event DevicePositionChangedEventHandler? DevicePositionChanged;

		public event JobStatusChangedEventHandler? JobStatusChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		public DeviceStatus DeviceStatus => _device?.Status ?? DeviceStatus.Disconnected;

		public Point? DevicePosition => _device?.Position;

		private Job? CurrentJob
		{
			get => _currentJob;
			set
			{
				lock (_syncRoot)
				{
					if (_currentJob != null && _currentJob != value)
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
					_currentJob = value;
					OnJobStatusChanged(this, new JobStatusChangedEventArgs(JobStatus));
					if (_currentJob != null)
					{
						_currentJob.StatusChanged += OnJobStatusChanged;
					}
				}
			}
		}

		public JobStatus JobStatus => CurrentJob?.Status ?? JobStatus.None;

		public string? JobTitle => CurrentJob?.Title;

		public TimeSpan JobElapsedDuration => CurrentJob?.ElapsedDuration ?? TimeSpan.Zero;

		public bool IsJobPausable => JobStatus == JobStatus.Running && CurrentJob is IPausableJob;

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
							_device.PositionChanged += OnDevicePositionChanged;
						}
						else if (_deviceConfiguration.Type == DeviceType.Serial)
						{
							_device = new Serial.SerialDevice(_deviceConfiguration);
							_device.StatusChanged += OnDeviceStatusChanged;
							_device.PositionChanged += OnDevicePositionChanged;
						}
						else
						{
							throw new NotSupportedException("Devicetype " + _deviceConfiguration.Type + " not supported.");
						}
					}
				}
			}

			DeviceStatusChanged?.Invoke(_device, new DeviceStatusChangedEventArgs(DeviceStatus));
			DevicePositionChanged?.Invoke(_device, new DevicePositionChangedEventArgs(DevicePosition));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeviceStatus)));

			using (var ctsTimeout = new CancellationTokenSource(_deviceConfiguration.ConnectionTimeout))
			using (var ctsCombined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token))
			{
				await _device.ConnectAsync(ctsCombined.Token);
			}
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
							CurrentJob = null;
						}
						device.StatusChanged -= OnDeviceStatusChanged;
					}
					DevicePositionChanged?.Invoke(device, new DevicePositionChangedEventArgs(DevicePosition));
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
				CurrentJob = job;
				device = _device ?? throw new InvalidOperationException("Device not connected");
			}

			await job.ExecuteAsync(device, cancellationToken);
		}

		public async Task ContinueJob(CancellationToken cancellationToken)
		{
			var job = CurrentJob;
			if (job != null && job.Status == JobStatus.Paused)
			{
				await ExecuteJob(job, cancellationToken);
			}
		}

		public void CancelJob()
		{
			CurrentJob?.Cancel();
		}

		public void PauseJob()
		{
			if (CurrentJob is IPausableJob pausableJob)
			{
				pausableJob.Pause();
			}
		}

		private void OnDevicePositionChanged(Device sender, DevicePositionChangedEventArgs args)
		{
			DevicePositionChanged?.Invoke(sender, args);
		}

		private void OnDeviceStatusChanged(Device sender, DeviceStatusChangedEventArgs args)
		{
			DeviceStatusChanged?.Invoke(sender, args);
		}

		private void OnJobStatusChanged(object sender, JobStatusChangedEventArgs jobStatusChangedEventArgs)
		{
			JobStatusChanged?.Invoke(sender, jobStatusChangedEventArgs);
		}
	}
}
