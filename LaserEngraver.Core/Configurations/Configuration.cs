using System.ComponentModel;
using System.Globalization;

namespace LaserPathEngraver.Core.Configurations
{
	public class DeviceConfiguration
	{
		public double DPI { get; set; } = 254;
		public double WidthDots { get; set; } = 800;
		public double HeightDots { get; set; } = 800;
		public DeviceType Type { get; set; }
#if DEBUG
= DeviceType.Mock;
#endif
		public string? PortName { get; set; }
		public int? BaudRate { get; set; }
		public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(9);
		public TimeSpan ExecuionTimeout { get; set; } = TimeSpan.FromSeconds(3);
	}

	public enum DeviceType
	{
		None = 0,
		Mock = 1,
		Serial = 2
	}

	public class UserConfiguration
	{
		public CultureInfo Culture { get; set; } = new CultureInfo("en");
		public bool ShowHelp { get; set; } = false;
		public bool AutoCenterView { get; set; } = true;
		public bool PreserveAspectRatio { get; set; } = true;
		public Unit Unit { get; set; } = Unit.cm;
	}

	public enum Unit
	{
		px = 0,
		cm = 1
	}

	public class BurnConfiguration
	{
		public byte Intensity { get; set; } = 0xff;
		public byte Duration { get; set; } = 0xff;
		public byte FixedIntensityThreshold { get; set; } = 0x7f;
		public BurnPlottingMode PlottingMode { get; set; } = BurnPlottingMode.NearestNeighbor;
		public BurnIntensityMode IntensityMode { get; set; } = BurnIntensityMode.Variable;

	}

	public enum BurnIntensityMode
	{
		Fixed,
		Variable
	}

	public enum BurnPlottingMode
	{
		Rasterized,
		NearestNeighbor
	}
}