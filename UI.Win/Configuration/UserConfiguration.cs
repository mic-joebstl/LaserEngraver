using LaserPathEngraver.Core.Configurations;
using System;
using System.Collections.Concurrent;
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
		public Unit Unit { get; set; } = Unit.Cm;
		public Theme Theme { get; set; } = Theme.Dark;
		public Theme? CustomTheme { get; set; }

	}

	public class Theme : INotifyPropertyChanged
	{
		private SolidColorBrush _foreground;
		private SolidColorBrush _errorMessgaeForeground;
		private SolidColorBrush _canvasBackground;
		private SolidColorBrush _sectionBackground;
		private SolidColorBrush _burnTargetBackground;
		private Color _burnVisitedColor;
		private Color _burnStartColor;
		private Color _burnEndColor;
		private ConcurrentDictionary<byte, Color> _burnGradientMap;

		public Theme()
		{
			_foreground = SystemColors.ControlTextBrush;
			_errorMessgaeForeground = SystemColors.ControlTextBrush;
			_canvasBackground = SystemColors.ControlLightLightBrush;
			_sectionBackground = SystemColors.ControlLightBrush;
			_burnTargetBackground = SystemColors.ControlLightLightBrush;
			_burnVisitedColor = Color.FromRgb(0xff, 0xff, 0xff);
			_burnStartColor = Color.FromRgb(0xff, 0xff, 0xff);
			_burnEndColor = Color.FromRgb(0x00, 0x00, 0x00);
			_burnGradientMap = new ConcurrentDictionary<byte, Color>(4, 0xff);
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

		public SolidColorBrush ErrorMessageForeground
		{
			get => _errorMessgaeForeground;
			set
			{
				if (_errorMessgaeForeground != value)
				{
					_errorMessgaeForeground = value;
					OnPropertyChanged(nameof(ErrorMessageForeground));
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

		public Color BurnVisitedColor
		{
			get => _burnVisitedColor;
			set
			{
				if (_burnVisitedColor != value)
				{
					_burnVisitedColor = value;
					OnPropertyChanged(nameof(BurnVisitedColor));
				}
			}
		}

		public Color BurnStartColor
		{
			get => _burnStartColor;
			set
			{
				if (_burnStartColor != value)
				{
					_burnStartColor = value;
					_burnGradientMap.Clear();
					OnPropertyChanged(nameof(BurnStartColor));
				}
			}
		}

		public Color BurnEndColor
		{
			get => _burnEndColor;
			set
			{
				if (_burnEndColor != value)
				{
					_burnEndColor = value;
					_burnGradientMap.Clear();
					OnPropertyChanged(nameof(BurnEndColor));
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public static Theme Default => Light;

		public static Theme Light => new()
		{
			Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)),
			ErrorMessageForeground = new SolidColorBrush(Color.FromRgb(0x75, 0x00, 0x10)),
			CanvasBackground = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd)),
			SectionBackground = new SolidColorBrush(Color.FromRgb(0xbb, 0xbb, 0xbb)),
			BurnTargetBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff)),
			BurnVisitedColor = Color.FromArgb(0xff, 0xa3, 0x50, 0x00),
			BurnStartColor = Color.FromArgb(0x00, 0x00, 0x00, 0x00),
			BurnEndColor = Color.FromArgb(0xff, 0x00, 0x00, 0x00),
		};

		public static Theme Dark => new()
		{
			Foreground = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd)),
			ErrorMessageForeground = new SolidColorBrush(Color.FromRgb(0xfd, 0xc9, 0xc2)),
			CanvasBackground = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)),
			SectionBackground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)),
			BurnTargetBackground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)),
			BurnVisitedColor = Color.FromArgb(0xff, 0xff, 0xf5, 0xd9),
			BurnStartColor = Color.FromArgb(0x00, 0xff, 0xff, 0xff),
			BurnEndColor = Color.FromArgb(0xff, 0xff, 0xff, 0xff),
		};

		public override bool Equals(object? obj) => obj is Theme theme
			&& theme.Foreground.Color.Equals(Foreground.Color)
			&& theme.ErrorMessageForeground.Color.Equals(ErrorMessageForeground.Color)
			&& theme.CanvasBackground.Color.Equals(CanvasBackground.Color)
			&& theme.SectionBackground.Color.Equals(SectionBackground.Color)
			&& theme.BurnTargetBackground.Color.Equals(BurnTargetBackground.Color)
			&& theme.BurnVisitedColor.Equals(BurnVisitedColor)
			&& theme.BurnStartColor.Equals(BurnStartColor)
			&& theme.BurnEndColor.Equals(BurnEndColor);

		public override int GetHashCode() =>
			Foreground.Color.GetHashCode()
			^ ErrorMessageForeground.Color.GetHashCode()
			^ CanvasBackground.Color.GetHashCode()
			^ SectionBackground.Color.GetHashCode()
			^ BurnTargetBackground.Color.GetHashCode()
			^ BurnVisitedColor.GetHashCode()
			^ BurnStartColor.GetHashCode()
			^ BurnEndColor.GetHashCode();

		public Color GetBurnGradientColor(byte value)
			=> _burnGradientMap.GetOrAdd(value, CalculateBurnGradientColor);

		private Color CalculateBurnGradientColor(byte value)
		{
			var aMin = BurnStartColor.A;
			var aMax = BurnEndColor.A;

			var rMin = BurnStartColor.R;
			var rMax = BurnEndColor.R;

			var gMin = BurnStartColor.G;
			var gMax = BurnEndColor.G;

			var bMin = BurnStartColor.B;
			var bMax = BurnEndColor.B;

			return Color.FromArgb
			(
				a: (byte)(aMin + ((aMax - aMin) * value / 255d)),
				r: (byte)(rMin + ((rMax - rMin) * value / 255d)),
				g: (byte)(gMin + ((gMax - gMin) * value / 255d)),
				b: (byte)(bMin + ((bMax - bMin) * value / 255d))
			);
		}
	}
}
