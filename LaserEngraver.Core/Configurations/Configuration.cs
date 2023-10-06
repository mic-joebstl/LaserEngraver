using System.ComponentModel;
using System.Globalization;

namespace LaserPathEngraver.Core.Configurations
{
	public class DeviceConfiguration
	{
		public double DPI { get; set; } = 510.54;
		public double WidthDots { get; set; } = 1608;
		public double HeightDots { get; set; } = 1608;
		public DeviceType Type { get; set; }
#if DEBUG
		//= DeviceType.Mock;
		= DeviceType.Serial;
#endif
		public short MaximumPowerMilliwatts { get; set; } = 1000;
		public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(15);
		public string? PortName { get; set; }
		public int? BaudRate { get; set; } = 115200;
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
		public byte Power { get; set; } = 0xff;
		public byte Duration { get; set; } = 0xff;
		public byte FixedIntensityThreshold { get; set; } = 0x7f;
		public BurnPlottingMode PlottingMode { get; set; } = BurnPlottingMode.RasterOptimized;
		public BurnIntensityMode IntensityMode { get; set; } = BurnIntensityMode.Variable;
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