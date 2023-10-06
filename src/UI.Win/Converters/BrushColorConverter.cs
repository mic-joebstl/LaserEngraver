using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace LaserEngraver.UI.Win.Converters
{
	public class BrushColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is SolidColorBrush brush ? brush.Color : value;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is Color color ? new SolidColorBrush(color) : value;
	}
}
