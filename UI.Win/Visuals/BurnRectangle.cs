using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace LaserPathEngraver.UI.Win.Visuals
{
	public class BurnRectangle : IVisual
	{
		private System.Windows.Shapes.Rectangle _rectangle;
		private Point _position;
		private Size _size;

		public BurnRectangle()
		{
			_rectangle = new System.Windows.Shapes.Rectangle();
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

		public Shape Shape => _rectangle;
	}
}
