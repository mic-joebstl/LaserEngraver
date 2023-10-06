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
		private Ellipse _ellipse;
		private double _fillRatio;
		private Point _position;
		private Size _size;

		public BurnTarget()
		{
			_ellipse = new Ellipse();
			_fillRatio = 1;
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

		public double FillRatio
		{
			get => _fillRatio;
			set
			{
				value = value < 0 ? 0 : value > 1 ? 1 : value;
				if (value != _fillRatio)
				{
					_fillRatio = value;
				}

			}
		}

		public Shape Shape => _ellipse;
	}
}
