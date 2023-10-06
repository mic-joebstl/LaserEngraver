using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LaserPathEngraver.UI.Win.Converters
{
	public class LogScaleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Math.Log10((double)value);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> Math.Pow(10, (double)value);
	}

	public class ByteScaleConverter : IValueConverter
	{
		public double MinValue { get; set; }
		public double MaxValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> (byte)value / (double)0xff * (MaxValue - MinValue) + MinValue;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var dblValue = value is string strValue ?
				Double.Parse(strValue, culture.NumberFormat) :
				(double)value;
			return (byte)(1 / (MaxValue - MinValue) * dblValue * 0xff);
		}
	}
}
