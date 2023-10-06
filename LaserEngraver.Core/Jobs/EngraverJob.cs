using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Jobs
{
	public abstract class Job : INotifyPropertyChanged
	{
		private JobStatus _status;
		private Device _device;

		public event PropertyChangedEventHandler? PropertyChanged;

		public event JobStatusChangedEventHandler? StatusChanged;

		public Job(Device device)
		{
			_device = device;
		}

		public JobStatus Status
		{
			get => _status;
			private set
			{
				if (_status != value)
				{
					_status = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
					StatusChanged?.Invoke(this, new JobStatusChangedEventArgs(value));
				}
			}
		}

		public abstract Task ExecuteAsync(CancellationToken cancellationToken);
	}

	public enum JobStatus
	{
		None,
		Running,
		Paused,
		Stopped,
		Done
	}

	public class JobStatusChangedEventArgs : EventArgs
	{
		internal JobStatusChangedEventArgs(JobStatus status)
		{
			Status = status;
		}

		public JobStatus Status { get; private set; }
	}

	public delegate void JobStatusChangedEventHandler(Job sender, JobStatusChangedEventArgs JobStatusChangedEventArgs);
}
