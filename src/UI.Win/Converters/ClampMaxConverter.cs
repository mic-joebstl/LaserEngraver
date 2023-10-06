using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LaserEngraver.UI.Win.Converters
{
	public class ClampMaxConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is double doubleValue) ||
				!(parameter is string parameterText) ||
				!double.TryParse(parameterText, out var doubleParameter))
			{
				return Binding.DoNothing;
			}

			return doubleValue > doubleParameter ? doubleParameter : doubleValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new InvalidOperationException();
		}
	}
}
