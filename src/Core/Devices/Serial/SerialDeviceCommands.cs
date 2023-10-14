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
		SettingsUpdate = 0x28,
	}

	public enum EngraveDirection : byte
	{
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

	public interface IDeviceSettings
	{
		byte StandbyBrightness { get; set; }
		ushort LineDelayMilliseconds { get; set; }
		ushort MaximumPowerMilliwatts { get; set; }
		byte SubdivisionCount { get; set; }
		byte XAxisCommutationCompensation { get; set; }
		byte YAxisCommutationCompensation { get; set; }
		ushort StepCount { get; set; }
		ushort SpeedUpperLimit { get; set; }
		ushort SpeedLowerLimit { get; set; }
		ushort PositioningSpeed { get; set; }
	}

	public class SettingsUpdateCommand : EngraverCommand, IDeviceSettings
	{
		public override EngraverCommandType Type => EngraverCommandType.SettingsUpdate;

		public SettingsUpdateCommand(IDeviceSettings settings)
		{
			StandbyBrightness = settings.StandbyBrightness;
			LineDelayMilliseconds = settings.LineDelayMilliseconds;
			MaximumPowerMilliwatts = settings.MaximumPowerMilliwatts;
			SubdivisionCount = settings.SubdivisionCount;
			XAxisCommutationCompensation = settings.XAxisCommutationCompensation;
			YAxisCommutationCompensation = settings.YAxisCommutationCompensation;
			StepCount = settings.StepCount;
			SpeedUpperLimit = settings.SpeedUpperLimit;
			SpeedLowerLimit = settings.SpeedLowerLimit;
			PositioningSpeed = settings.PositioningSpeed;
		}

		public byte StandbyBrightness { get; set; }
		public ushort LineDelayMilliseconds { get; set; }
		public ushort MaximumPowerMilliwatts { get; set; }
		public byte SubdivisionCount { get; set; }
		public byte XAxisCommutationCompensation { get; set; }
		public byte YAxisCommutationCompensation { get; set; }
		public ushort StepCount { get; set; }
		public ushort SpeedUpperLimit { get; set; }
		public ushort SpeedLowerLimit { get; set; }
		public ushort PositioningSpeed { get; set; }

		protected override byte[] BuildArguments()
		{
			return new byte[]
			{
				StandbyBrightness,
				(byte)(LineDelayMilliseconds >> 8),
				(byte)LineDelayMilliseconds,
				(byte)(MaximumPowerMilliwatts >> 8),
				(byte)MaximumPowerMilliwatts,
				SubdivisionCount,
				XAxisCommutationCompensation,
				YAxisCommutationCompensation,
				(byte)(StepCount >> 8),
				(byte)StepCount,
				(byte)(SpeedUpperLimit >> 8),
				(byte)SpeedUpperLimit,
				(byte)(SpeedLowerLimit >> 8),
				(byte)SpeedLowerLimit,
				(byte)(PositioningSpeed >> 8),
				(byte)PositioningSpeed,
			};
		}
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
		public EngraveDirection Direction { get; set; }

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
				ms.WriteByte((byte)Direction);
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
