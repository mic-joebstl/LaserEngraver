using LaserPathEngraver.Core.Configurations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LaserPathEngraver.UI.Win.Configuration
{
	public class UserConfiguration
	{
		public CultureInfo Culture { get; set; } = new CultureInfo("en");
		public bool ShowHelp { get; set; } = false;
		public bool AutoCenterView { get; set; } = true;
		public bool PreserveAspectRatio { get; set; } = true;
		public Unit Unit { get; set; } = Unit.cm;
		public Theme Theme { get; set; } = Theme.Dark;

	}

	public class Theme : INotifyPropertyChanged
	{
		private SolidColorBrush _foreground;
		private SolidColorBrush _canvasBackground;
		private SolidColorBrush _sectionBackground;
		private SolidColorBrush _burnTargetBackground;

		public Theme()
		{
			_foreground = SystemColors.ControlTextBrush;
			_canvasBackground = SystemColors.ControlLightLightBrush;
			_sectionBackground = SystemColors.ControlLightBrush;
			_burnTargetBackground = SystemColors.ControlLightLightBrush;
		}

		public SolidColorBrush Foreground
		{
			get => _foreground;
			set
			{
				if (_foreground != value)
				{
					_foreground = value;
					OnPropertyChanged(nameof(Foreground));
				}
			}
		}

		public SolidColorBrush CanvasBackground
		{
			get => _canvasBackground;
			set
			{
				if (_canvasBackground != value)
				{
					_canvasBackground = value;
					OnPropertyChanged(nameof(CanvasBackground));
				}
			}
		}

		public SolidColorBrush SectionBackground
		{
			get => _sectionBackground;
			set
			{
				if (_sectionBackground != value)
				{
					_sectionBackground = value;
					OnPropertyChanged(nameof(SectionBackground));
				}
			}
		}

		public SolidColorBrush BurnTargetBackground
		{
			get => _burnTargetBackground;
			set
			{
				if (_burnTargetBackground != value)
				{
					_burnTargetBackground = value;
					OnPropertyChanged(nameof(BurnTargetBackground));
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public static Theme Default => Dark;

		public static Theme Light => new()
		{
			Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)),
			CanvasBackground = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd)),
			SectionBackground = new SolidColorBrush(Color.FromRgb(0xbb, 0xbb, 0xbb)),
			BurnTargetBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff)),
		};

		public static Theme Dark => new()
		{
			Foreground = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd)),
			CanvasBackground = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)),
			SectionBackground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)),
			BurnTargetBackground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)),
		};

		public override bool Equals(object? obj) => obj is Theme theme
			&& theme.Foreground.Color.Equals(Foreground.Color)
			&& theme.CanvasBackground.Color.Equals(CanvasBackground.Color)
			&& theme.SectionBackground.Color.Equals(SectionBackground.Color);

		public override int GetHashCode() =>
			Foreground.Color.GetHashCode()
			^ CanvasBackground.Color.GetHashCode()
			^ SectionBackground.Color.GetHashCode();
	}
}
