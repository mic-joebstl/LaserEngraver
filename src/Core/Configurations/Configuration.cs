using System.ComponentModel;
using System.Globalization;
using Serial = LaserEngraver.Core.Devices.Serial;

namespace LaserEngraver.Core.Configurations
{
	public class DeviceConfiguration : Serial.IDeviceSettings
	{
		public double DPI { get; set; } = 508;
		public double WidthDots { get; set; } = 1600;
		public double HeightDots { get; set; } = 1600;
		public DeviceType Type { get; set; }
#if DEBUG
		= DeviceType.Mock;
#else
		= DeviceType.Serial;
#endif
		public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(15);
		public string? PortName { get; set; }
		public int? BaudRate { get; set; } = 115200;

		#region ISerialDevice
		public byte StandbyBrightness { get; set; } = 70;
		public ushort LineDelayMilliseconds { get; set; } = 100;
		public ushort MaximumPowerMilliwatts { get; set; } = 1000;
		public byte SubdivisionCount { get; set; } = 4;
		public byte XAxisCommutationCompensation { get; set; } = 1;
		public byte YAxisCommutationCompensation { get; set; } = 1;
		public ushort StepCount { get; set; } = 200;
		public ushort SpeedUpperLimit { get; set; } = 870;
		public ushort SpeedLowerLimit { get; set; } = 600;
		public ushort PositioningSpeed { get; set; } = 5;
		#endregion
	}

	public enum DeviceType
	{
		None = 0,
		Mock = 1,
		Serial = 2
	}

	public enum Unit
	{
		Px = 0,
		Cm = 1,
		In = 2,
	}

	public class BurnConfiguration
	{
		public byte Power { get; set; } = 0x7f;
		public byte Duration { get; set; } = 0x7f;
		public byte FixedIntensityThreshold { get; set; } = 0x7f;
		public BurnPlottingMode PlottingMode { get; set; } = BurnPlottingMode.RasterOptimized;
		public BurnIntensityMode IntensityMode { get; set; } = BurnIntensityMode.Fixed;
	}

	public enum BurnIntensityMode
	{
		Fixed,
		Variable
	}

	public enum BurnPlottingMode
	{
		Raster,
		RasterOptimized
	}
}