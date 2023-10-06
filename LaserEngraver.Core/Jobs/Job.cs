using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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

	public delegate void JobStatusChangedEventHandler(object sender, JobStatusChangedEventArgs jobStatusChangedEventArgs);

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
					catch (Exception ex) when (
						ex is OperationCanceledException ||
						ex is TaskCanceledException)
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

	public sealed class MoveAbsoluteJob : Job
	{
		private Point _position;

		public MoveAbsoluteJob(Point position)
		{
			_position = position;
		}

		public override string Title => Resources.Localization.Texts.MoveAbsoluteJobTitle;

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			await device.MoveAbsoluteAsync(_position, cancellationToken);
		}
	}

	public sealed class FramingJob : Job
	{
		private Rectangle _frame;
		private TimeSpan _stepDelay;

		public FramingJob(Rectangle frame, TimeSpan stepDelay)
		{
			_frame = frame;
			_stepDelay = stepDelay;
		}

		public override string Title => Resources.Localization.Texts.FramingJobTitle;

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			var queue = new Queue<Point>(4);
			queue.Enqueue(new Point(_frame.Left, _frame.Top));
			queue.Enqueue(new Point(_frame.Right, _frame.Top));
			queue.Enqueue(new Point(_frame.Right, _frame.Bottom));
			queue.Enqueue(new Point(_frame.Left, _frame.Bottom));
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var point = queue.Dequeue();
				await device.MoveAbsoluteAsync(point, cancellationToken);
				await Task.Delay(_stepDelay);
				queue.Enqueue(point);
			}
		}
	}
}
