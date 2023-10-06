using System.ComponentModel;
using System.Globalization;

namespace LaserPathEngraver.Core.Configurations
{
	public class DeviceConfiguration
	{
		public decimal DPI { get; set; }
		public decimal WidthDots { get; set; }
		public decimal HeightDots { get; set; }
	}

	public class UserConfiguration
	{
		public CultureInfo Culture { get; set; } = new CultureInfo("en");
		public bool ShowTutorial { get; set; }
	}
}