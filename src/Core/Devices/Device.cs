using LaserEngraver.Core.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LaserEngraver.Core.Devices
{
	public abstract class Device
	{
		private readonly object _syncRoot = new object();
		private DeviceStatus _status;
		private Point? _position;

		public event DeviceStatusChangedEventHandler? StatusChanged;

		public event DevicePositionChangedEventHandler? PositionChanged;

		protected object SyncRoot => _syncRoot;

		public DeviceStatus Status
		{
			get => _status;
			protected set
			{
				if (value != _status)
				{
					var triggerUpdate = false;
					lock (_syncRoot)
					{
						if (value != _status)
						{
							_status = value;
							triggerUpdate = true;
						}
					}
					if (triggerUpdate)
					{
						StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(value));
					}
				}
			}
		}

		public Point? Position
		{
			get => _position;
			protected set
			{
				if (value != _position)
				{
					_position = value;
					PositionChanged?.Invoke(this, new DevicePositionChangedEventArgs(value));
				}
			}
		}

		protected void DemandState(DeviceStatus status)
		{
			if (Status != status)
			{
				lock (_syncRoot)
				{
					if (Status != status)
					{
						throw new InvalidOperationException("Operation requires device of status " + status);
					}
				}
			}
		}

		public abstract Task ConnectAsync(CancellationToken cancellationToken);
		public abstract Task DisconnectAsync(CancellationToken cancellationToken);
		public abstract Task HomingAsync(CancellationToken cancellationToken);
		public abstract Task MoveRelativeAsync(Point vector, CancellationToken cancellationToken);
		public abstract Task MoveAbsoluteAsync(Point position, CancellationToken cancellationToken);
		public abstract Task Engrave(ushort powerMilliwatt, byte duration, CancellationToken cancellationToken);
		public abstract Task Engrave(ushort powerMilliwatt, byte duration, Point startingPoint, int length, CancellationToken cancellationToken);

		protected IDeviceStatusIntermediateTransition StatusIntermediateTransition(DeviceStatus sourceStatus, DeviceStatus intermediateStatus)
		{
			return new DeviceStatusTransition(this, sourceStatus, intermediateStatus, sourceStatus);
		}

		protected IDeviceStatusTransition StatusTransition(DeviceStatus sourceStatus, DeviceStatus openStatus, DeviceStatus targetStatus)
		{
			return new DeviceStatusTransition(this, sourceStatus, openStatus, targetStatus);
		}

		protected interface IDeviceStatusIntermediateTransition: IDisposable
		{
			void Open();
		}

		protected interface IDeviceStatusTransition : IDeviceStatusIntermediateTransition
		{
			void Commit();
		}

		private class DeviceStatusTransition : IDeviceStatusTransition
		{
			private Device _owner;
			private DeviceStatus _sourceStatus;
			private DeviceStatus _openStatus;
			private DeviceStatus _targetStatus;
			private bool _commited;

			public DeviceStatusTransition(Device owner, DeviceStatus sourceStatus, DeviceStatus openStatus, DeviceStatus targetStatus)
			{
				_owner = owner;
				_sourceStatus = sourceStatus;
				_openStatus = openStatus;
				_targetStatus = targetStatus;
			}

			public void Dispose()
			{
				if (!_commited)
				{
					_owner.Status = _sourceStatus;
				}
			}

			public void Open()
			{
				lock (_owner.SyncRoot)
				{
					_owner.DemandState(_sourceStatus);
					_owner.Status = _openStatus;
				}
			}

			public void Commit()
			{
				_owner.Status = _targetStatus;
				_commited = true;
			}
		}
	}

	public enum DeviceStatus
	{
		Disconnected = 0,
		Connecting = 1,
		Ready = 2,
		Executing = 3,
		Disconnecting = 4
	}

	public class DeviceStatusChangedEventArgs : EventArgs
	{
		internal DeviceStatusChangedEventArgs(DeviceStatus status)
		{
			Status = status;
		}

		public DeviceStatus Status { get; private set; }
	}

	public delegate void DeviceStatusChangedEventHandler(Device sender, DeviceStatusChangedEventArgs args);

	public class DevicePositionChangedEventArgs : EventArgs
	{
		internal DevicePositionChangedEventArgs(Point? position)
		{
			Position = position;
		}

		public Point? Position { get; private set; }
	}

	public delegate void DevicePositionChangedEventHandler(Device sender, DevicePositionChangedEventArgs args);

	public class UnexpectedDeviceDisconnectionException : Exception
	{
		public UnexpectedDeviceDisconnectionException()
			: base(Resources.Localization.Texts.UnexpectedDeviceDisconnectedExceptionMessage)
		{ }
	}

}
