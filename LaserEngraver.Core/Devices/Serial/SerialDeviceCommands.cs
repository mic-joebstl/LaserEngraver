using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Devices.Serial
{
	public enum EngraverCommandType : byte
	{
		None = 0,
		Move = 0x01,
		FanOn = 0x04,
		FanOff = 0x05,
		Reset = 0x06,
		Connect = 0x0A,
		Engrave = 0x09,
		Stop = 0x16,
		HomeTopLeft = 0x17,
		Pause = 0x18,
		Continue = 0x19,
		HomeCenter = 0x1A,
		Discrete = 0x1B,
		NonDiscrete = 0x1C,
	}

	public abstract class EngraverCommand
	{
		public abstract EngraverCommandType Type { get; }

		public virtual byte[] Build()
		{
			byte[] args = BuildArguments() ?? new byte[0];
			var length = (ushort)(3 + args.Length);

			using (var ms = new System.IO.MemoryStream())
			{
				ms.WriteByte((byte)Type);
				ms.WriteByte((byte)(length >> 8));
				ms.WriteByte((byte)(length));
				ms.Write(args, 0, args.Length);

				return ms.ToArray();
			}
		}

		protected virtual byte[] BuildArguments() => new byte[1];
	}

	public class SimpleEngraverCommand : EngraverCommand
	{
		private EngraverCommandType _Type;

		public SimpleEngraverCommand(EngraverCommandType type)
		{
			_Type = type;
		}

		public override EngraverCommandType Type => _Type;
	}

	public class MoveCommand : EngraverCommand
	{
		public MoveCommand() { }

		public MoveCommand(short x, short y)
			: this()
		{
			X = x;
			Y = y;
		}

		public override EngraverCommandType Type => EngraverCommandType.Move;

		public short X { get; set; }
		public short Y { get; set; }

		protected override byte[] BuildArguments()
		{
			return new byte[]
			{
				(byte)(X >> 8),
				(byte)X,
				(byte)(Y >> 8),
				(byte)Y
			};
		}
	}

	public class EngraveCommand : EngraverCommand
	{
		public override EngraverCommandType Type => EngraverCommandType.Engrave;

		public EngraveCommand(byte power, byte duration)
		{
			Power = power;
			Duration = duration;
		}

		public byte Power { get; set; }
		public byte Duration { get; set; }
		public byte[]? Data { get; set; }

		protected override byte[] BuildArguments()
		{
			using (var ms = new System.IO.MemoryStream())
			{
				ms.WriteByte(0);
				ms.WriteByte(Duration);
				ms.WriteByte(Power);
				ms.WriteByte(0);

				ms.WriteByte(0);    //Byte 1 Delta Y
				ms.WriteByte(0);    //Byte 2 Delta Y
				ms.Write(Data ?? new byte[0], 0, Data?.Length ?? 0);

				return ms.ToArray();
			}
		}
	}

	public class CustomEngraverCommand : EngraverCommand
	{
		public CustomEngraverCommand() { }

		public byte[]? Data { get; set; }

		public override EngraverCommandType Type => EngraverCommandType.None;

		public override byte[] Build() => Data ?? new byte[0];
	}
}
