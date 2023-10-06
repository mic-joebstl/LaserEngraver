using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace LaserEngraver.UI.Win.Rules
{
	public class DoubleValidationRule : ValidationRule
	{
		public DoubleValidationRule()
		{
			Minimum = double.MinValue;
			Maximum = double.MaxValue;
			BoundsExclusive = false;
		}

		public double Minimum { get; set; }
		public double Maximum { get; set; }
		public bool BoundsExclusive { get; set; }

		public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
		{
			double i;

			try
			{
				i = double.Parse((string)value, System.Globalization.NumberStyles.Any, cultureInfo);

				if (BoundsExclusive && i > Minimum && i < Maximum)
					return new ValidationResult(true, null);
				if (!BoundsExclusive && i >= Minimum && i <= Maximum)
					return new ValidationResult(true, null);
				else
					return new ValidationResult(false, "Value out of bounds.");
			}
			catch (Exception e)
			{
				return new ValidationResult(false, e.Message);
			}
		}

	}
}
