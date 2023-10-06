using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserEngraver.Core.Devices.Serial
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
		InitEngrave = 0x14,
		Stop = 0x16,
		HomeTopLeft = 0x17,
		Pause = 0x18,
		Continue = 0x19,
		HomeCenter = 0x1A,
		Discrete = 0x1B,
		NonDiscrete = 0x1C,
	}

	public enum EngraveDirection : byte
	{
		Default = RightToLeft,
		RightToLeft = 0,
		LeftToRight = 1,
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

	public class InitEngraveCommand : MoveCommand
	{
		public InitEngraveCommand() { }

		public InitEngraveCommand(short x, short y)
			: base(x, y)
		{
			X = x;
			Y = y;
		}

		public override EngraverCommandType Type => EngraverCommandType.InitEngrave;
	}

	public class EngraveCommand : EngraverCommand
	{
		public override EngraverCommandType Type => EngraverCommandType.Engrave;

		public EngraveCommand(ushort powerMilliwatt, byte duration)
		{
			PowerMilliwatt = powerMilliwatt;
			Duration = duration;
		}

		public ushort PowerMilliwatt { get; set; }
		public byte Duration { get; set; }
		public byte[]? Data { get; set; }

		protected override byte[] BuildArguments()
		{
			using (var ms = new System.IO.MemoryStream())
			{
				var data = Data ?? new byte[0];

				ms.WriteByte(0);
				ms.WriteByte(Duration);
				ms.WriteByte((byte)(PowerMilliwatt >> 8));
				ms.WriteByte((byte)PowerMilliwatt);

				ms.WriteByte(0);
				ms.WriteByte((byte)EngraveDirection.Default);
				ms.Write(data, 0, data.Length);

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
