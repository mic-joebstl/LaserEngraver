﻿using LaserPathEngraver.UI.Win.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace LaserPathEngraver.UI.Win.Visuals
{
	public interface IVisual
	{
		Point Position { get; set; }
		Size Size { get; set; }
		Shape Shape { get; }

		void ApplyTheme(Theme theme);
	}
}
