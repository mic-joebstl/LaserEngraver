using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Jobs
{
	[Flags]
	public enum JobStatus : byte
	{
		None = 0,
		Running = 1,
		Paused = 2,
		Cancelled = 4,
		Failed = 8,
		Done = 12
	}

	public class JobStatusChangedEventArgs : EventArgs
	{
		internal JobStatusChangedEventArgs(JobStatus status)
		{
			Status = status;
		}

		public JobStatus Status { get; private set; }
	}

	public delegate void JobStatusChangedEventHandler(Job sender, JobStatusChangedEventArgs jobStatusChangedEventArgs);

	public abstract class Job : INotifyPropertyChanged, IDisposable
	{
		private JobStatus _status;
		private Stopwatch _stopWatch;
		private Exception? _exception;
		private CancellationTokenSource _cts;

		public event PropertyChangedEventHandler? PropertyChanged;

		public event JobStatusChangedEventHandler? StatusChanged;

		public Job()
		{
			_stopWatch = new Stopwatch();
			_cts = new CancellationTokenSource();
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

		public TimeSpan ElapsedDuration => _stopWatch.Elapsed;

		public Exception? Exception => _exception;

		public abstract string Title { get; }

		public void Cancel()
		{
			_cts.Cancel();
		}

		internal async Task ExecuteAsync(Device device, CancellationToken cancellationToken)
		{
			_stopWatch.Start();
			try
			{
				using (var lts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken))
				{
					try
					{
						Status = JobStatus.Running;
						await ExecuteCoreAsync(device, lts.Token);
						if (Status != JobStatus.Paused)
						{
							Status = JobStatus.Done;
							Dispose();
						}
					}
					catch (TaskCanceledException)
					{
						Status = JobStatus.Cancelled;
						Dispose();
					}
					catch (Exception ex)
					{
						_exception = ex;
						Status = JobStatus.Failed;
						Dispose();
						throw;
					}
				}
			}
			finally
			{
				_stopWatch.Stop();
			}
		}

		protected abstract Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken);

		public void Dispose()
		{
			_cts.Dispose();
		}
	}

	public sealed class HomingJob : Job
	{
		public HomingJob()
		{ }

		public override string Title => Resources.Localization.Texts.HomingJobTitle;

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			await device.HomingAsync(cancellationToken);
		}
	}
}
