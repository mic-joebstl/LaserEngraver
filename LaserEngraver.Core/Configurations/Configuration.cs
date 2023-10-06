using System.ComponentModel;
using System.Globalization;

namespace LaserPathEngraver.Core.Configurations
{
	public class DeviceConfiguration
	{
		public double DPI { get; set; }
		public double WidthDots { get; set; }
		public double HeightDots { get; set; }
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
}