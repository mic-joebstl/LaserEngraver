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

	public interface IPausableJob
	{
		public void Pause();
	}

	public abstract class Job : IDisposable
	{
		private JobStatus _status;
		private Stopwatch _stopWatch;
		private Exception? _exception;
		private CancellationTokenSource _cts;

		public event JobStatusChangedEventHandler? StatusChanged;

		public Job()
		{
			_stopWatch = new Stopwatch();
			_cts = new CancellationTokenSource();
		}

		public JobStatus Status
		{
			get => _status;
			protected set
			{
				if (_status != value)
				{
					_status = value;
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
			if (Status == JobStatus.Paused)
			{
				Status = JobStatus.Cancelled;
				Dispose();
			}
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
					catch(UnexpectedDeviceDisconnectionException ex)
					{
						if (this is IPausableJob pausable)
						{
							pausable.Pause();
						}
						else
						{
							Status = JobStatus.Failed;
							Dispose();
						}
						_exception = ex;
						throw;
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

	public sealed class FramingJob : Job, IPausableJob
	{
		private Queue<Point> _frame;
		private TimeSpan _stepDelay;
		private bool _signalPause;

		public FramingJob(Rectangle frame, TimeSpan stepDelay)
		{
			_stepDelay = stepDelay;

			_frame = new Queue<Point>(4);
			_frame.Enqueue(new Point(frame.Left, frame.Top));
			_frame.Enqueue(new Point(frame.Right, frame.Top));
			_frame.Enqueue(new Point(frame.Right, frame.Bottom));
			_frame.Enqueue(new Point(frame.Left, frame.Bottom));
		}

		public override string Title => Resources.Localization.Texts.FramingJobTitle;

		public void Pause()
		{
			_signalPause = true;
		}

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			_signalPause = false;

			while (_frame.TryDequeue(out var point))
			{
				cancellationToken.ThrowIfCancellationRequested();
				await device.MoveAbsoluteAsync(point, cancellationToken);
				await Task.Delay(_stepDelay);
				_frame.Enqueue(point);

				if (_signalPause)
				{
					Status = JobStatus.Paused;
					return;
				}
			}
		}
	}
}
