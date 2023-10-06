using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace LaserPathEngraver.UI.Win.Visuals
{
	public class BurnTarget : IVisual
	{
		private System.Windows.Shapes.Ellipse _circle;
		private Point _position;
		private Size _size;

		public BurnTarget()
		{
			_circle = new System.Windows.Shapes.Ellipse();
		}

		public Point Position
		{
			get => _position;
			set
			{
				if (_position != value)
				{
					_position = value;
				}
			}
		}

		public Size Size
		{
			get => _size;
			set
			{
				if (_size != value)
				{
					_size = value;
				}
			}
		}

		public Shape Shape => _circle;
	}
}
