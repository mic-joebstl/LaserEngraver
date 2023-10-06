﻿using LaserPathEngraver.Core.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Devices
{
	public abstract class Device : INotifyPropertyChanged
	{
		private readonly object _syncRoot = new object();
		private DeviceStatus _status;
		private Point? _position;

		public event PropertyChangedEventHandler? PropertyChanged;

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
					lock (_syncRoot)
					{
						if (value != _status)
						{
							_status = value;
							StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(value));
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
						}
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
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position)));
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
	}

	public enum DeviceStatus
	{
		Disconnected = 0,
		Connecting,
		Ready,
		Executing,
		Disconnecting
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

}