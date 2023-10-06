using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace LaserEngraver.UI.Win.Rules
{
	public class DecimalValidationRule : ValidationRule
	{
		public DecimalValidationRule()
		{
			Minimum = decimal.MinValue;
			Maximum = decimal.MaxValue;
			BoundsExclusive = false;
		}

		public decimal Minimum { get; set; }
		public decimal Maximum { get; set; }
		public bool BoundsExclusive { get; set; }

		public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
		{
			decimal n;

			try
			{
				n = decimal.Parse((string)value, System.Globalization.NumberStyles.Any, cultureInfo);

				if (BoundsExclusive && n > Minimum && n < Maximum)
					return new ValidationResult(true, null);
				if (!BoundsExclusive && n >= Minimum && n <= Maximum)
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
