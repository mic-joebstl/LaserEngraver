using LaserPathEngraver.Core.Configurations;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace LaserPathEngraver.Core.Devices.Serial
{
	public class SerialDevice : Device
	{
		private SerialPort? _serialPort;
		private DeviceConfiguration _configuration;
		private bool _requiresReset;
		private SemaphoreSlim? _commandSync;

		public SerialDevice(DeviceConfiguration configuration)
		{
			_configuration = configuration;
		}

		private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (_commandSync.CurrentCount == 0)
			{
				_commandSync.Release();
			}

			var com = _serialPort;
			if (com != null && com.IsOpen)
			{
				var response = new byte[1];
				com.Read(response, 0, 1);

				if (response[0] != (byte)EngraverResponse.Completed)
				{
					throw new UnexpectedResponseException(response);
				}
			}
		}

		public override async Task ConnectAsync(CancellationToken cancellationToken)
		{
			using (var tx = StatusTransition(DeviceStatus.Disconnected, DeviceStatus.Connecting, DeviceStatus.Ready))
			{
				tx.Open();

				var com = _serialPort;
				if (com != null && com.IsOpen)
				{
					com.Dispose();
				}
				Interlocked.Exchange(ref _commandSync, new SemaphoreSlim(1, 1))?.Dispose();

				com = _serialPort = new SerialPort()
				{
					BaudRate = _configuration.BaudRate ?? throw new InvalidOperationException("Required configuration " + nameof(_configuration.BaudRate) + " not set"),
					PortName = GetPortName(),
				};
				com.DataReceived += OnDataReceived;
				com.Open();

				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Connect), cancellationToken);
				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.NonDiscrete), cancellationToken);

				tx.Commit();
			}
		}

		public override async Task DisconnectAsync(CancellationToken cancellationToken)
		{
			using (var tx = StatusTransition(DeviceStatus.Ready, DeviceStatus.Disconnecting, DeviceStatus.Disconnected))
			{
				tx.Open();

				try
				{
					await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Reset), cancellationToken);
					await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Stop), cancellationToken);

					_serialPort?.Close();
					_serialPort?.Dispose();
					_serialPort = null;

					tx.Commit();
				}
				catch (Exception)
				{
					Status = DeviceStatus.Disconnected;
					throw;
				}
			}
		}

		public override async Task HomingAsync(CancellationToken cancellationToken)
		{
			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Reset), cancellationToken);
				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.HomeCenter), cancellationToken);
				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Discrete), cancellationToken);

				Position = new Point((int)(_configuration.WidthDots / 2), (int)(_configuration.HeightDots / 2));
			}
		}

		public override async Task MoveRelativeAsync(Point vector, CancellationToken cancellationToken)
		{
			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				var position = Position;
				if (position != null)
				{
					var x = (short)vector.X;
					var y = (short)vector.Y;
					if (x != vector.X || y != vector.Y)
						throw new ArgumentOutOfRangeException(nameof(vector));

					if (_requiresReset)
					{
						await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Reset), cancellationToken);
					}
					await WriteCommand(new MoveCommand(x, y), cancellationToken);

					Position = new Point
					{
						X = position.Value.X + vector.X,
						Y = position.Value.Y + vector.Y
					};
				}
			}
		}

		public override async Task MoveAbsoluteAsync(Point position, CancellationToken cancellationToken)
		{
			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				while (Position != null && (Position.Value.X != position.X || Position.Value.Y != position.Y))
				{
					var vector = new PointF
					{
						X = position.X - Position.Value.X,
						Y = position.Y - Position.Value.Y
					};
					var vectorAbs = new PointF
					{
						X = Math.Abs(vector.X),
						Y = Math.Abs(vector.Y)
					};

					var x = (short)(
						vectorAbs.X >= 50 ? vector.X / vectorAbs.X * 50 :
						vector.X != 0 ? vector.X / Math.Abs(vector.X) : 0
					);
					var y = (short)(
						vectorAbs.Y >= 50 ? vector.Y / vectorAbs.Y * 50 :
						vector.Y != 0 ? vector.Y / Math.Abs(vector.Y) : 0
					);

					if (_requiresReset)
					{
						await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Reset), cancellationToken);
					}
					await WriteCommand(new MoveCommand(x, y), cancellationToken);

					Position = new Point
					{
						X = Position.Value.X + x,
						Y = Position.Value.Y + y,
					};
				}
			}
		}
		public override async Task Engrave(byte intensity, byte duration, CancellationToken cancellationToken)
		{
			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				var command = new EngraveCommand(intensity, duration)
				{
					Data = new byte[] { 128 }
				};
				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Reset), cancellationToken);
				await WriteCommand(command, cancellationToken);
				_requiresReset = true;
			}
		}
		public override async Task Engrave(byte intensity, byte duration, int length, CancellationToken cancellationToken)
		{
			if (length < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			using (var tx = StatusIntermediateTransition(DeviceStatus.Ready, DeviceStatus.Executing))
			{
				tx.Open();

				var bufferLength = (int)Math.Ceiling(length / 8d);
				var buffer = new byte[bufferLength];
				for (int ib = 0, il = 0; ib < bufferLength; ib++)
				{
					byte b = 0;
					for (byte i = 7; i >= 0 && il < length; i--, il++)
					{
						b += (byte)(1 << i);
					}
					buffer[ib] = b;
				}

				var command = new EngraveCommand(intensity, duration)
				{
					Data = buffer
				};

				await WriteCommand(new SimpleEngraverCommand(EngraverCommandType.Reset), cancellationToken);
				await WriteCommand(command, cancellationToken);

				var position = Position;
				if (position.HasValue)
				{
					Position = new Point(position.Value.X + length - 1, position.Value.Y);
				}

				_requiresReset = true;
			}
		}

		private async Task WriteCommand(EngraverCommand command, CancellationToken cancellationToken)
		{
			var com = _serialPort ?? throw new InvalidOperationException("Device not connected");

			try
			{
				await _commandSync?.WaitAsync(cancellationToken);

				var request = command.Build();
				com.Write(request, 0, request.Length);
			}
			catch (Exception)
			{
				if (_commandSync?.CurrentCount == 0)
				{
					_commandSync.Release();
				}
				throw;
			}
		}

		private string GetPortName()
		{
			var portName = _configuration.PortName;
			if (!String.IsNullOrEmpty(portName))
				return portName;

			var portNames = System.IO.Ports.SerialPort.GetPortNames().ToArray();
			if (portNames.Length == 0)
				throw new Exception(Resources.Localization.Texts.NoDeviceFoundException);
			if (portNames.Length == 1)
				return portNames[0];
			throw new Exception(String.Format(Resources.Localization.Texts.MoreThanOneDeviceFoundExceptionFormat, String.Join(Environment.NewLine, portNames)));
		}
	}

	public enum EngraverResponse : byte
	{
		Completed = 0x9
	}

	public class UnexpectedResponseException : Exception
	{
		public UnexpectedResponseException(byte[] buffer)
			: base(String.Format(Resources.Localization.Texts.UnexpectedResponseExceptionFormat, String.Join("", buffer.Take(512).Select(b => b.ToString("X")))))
		{ }
	}
}
