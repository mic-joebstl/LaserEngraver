using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace LaserPathEngraver.UI.Win.Converters
{
	[ValueConversion(typeof(bool), typeof(Visibility))]
	public sealed class BoolToVisibilityConverter : IValueConverter
	{
		public BoolToVisibilityConverter()
		{
			DefaultValue = Visibility.Visible;
			TrueValue = Visibility.Visible;
			FalseValue = Visibility.Collapsed;
		}

		public Visibility DefaultValue { get; set; }
		public Visibility TrueValue { get; set; }
		public Visibility FalseValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is bool swtch ? swtch ? TrueValue : FalseValue : DefaultValue;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> Equals(value, TrueValue);
	}
}
