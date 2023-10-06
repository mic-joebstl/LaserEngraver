using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserEngraver.Core.Devices.Serial
{
	public static class SerialPortExtensions
	{
		public async static Task ReadAsync(this SerialPort serialPort, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var bytesToRead = count;
			var temp = new byte[count];

			while (bytesToRead > 0)
			{
				var readBytes = await serialPort.BaseStream.ReadAsync(temp, 0, bytesToRead, cancellationToken);
				Array.Copy(temp, 0, buffer, offset + count - bytesToRead, readBytes);
				bytesToRead -= readBytes;
			}
		}

		public async static Task<byte[]> ReadAsync(this SerialPort serialPort, int count, CancellationToken cancellationToken)
		{
			var buffer = new byte[count];
			await serialPort.ReadAsync(buffer, 0, count, cancellationToken);
			return buffer;
		}

		public async static Task WriteAsync(this SerialPort serialPort, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await serialPort.BaseStream.WriteAsync(buffer, offset, count, cancellationToken);
			await serialPort.BaseStream.FlushAsync(cancellationToken);
		}
	}
}
