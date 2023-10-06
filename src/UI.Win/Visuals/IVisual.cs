using LaserEngraver.UI.Win.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace LaserEngraver.UI.Win.Visuals
{
	public interface IVisual
	{
		Point Position { get; set; }
		Size Size { get; set; }
		FrameworkElement Element { get; }

		void ApplyTheme(Theme theme);
	}
}
