using LaserEngraver.UI.Win.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace LaserEngraver.UI.Win.Visuals
{
	public class BurnTarget : IVisual
	{
		private System.Windows.Shapes.Ellipse _circle;
		private Point _positionPrevious;
		private Point _position;
		private Size _size;

		private TimeSpan _animationTime = TimeSpan.FromMilliseconds(250);
		private DateTime _updateTime = DateTime.UtcNow;

		public BurnTarget()
		{
			_circle = new System.Windows.Shapes.Ellipse();
		}

		public Point Position
		{
			get
			{
				var now = DateTime.UtcNow;
				var duration = _animationTime.TotalMilliseconds;
				var t = (now - _updateTime).TotalMilliseconds;
				if (t > duration)
				{
					return _position;
				}
				var tFactor = t / duration;
				var tSmoothed = t * (tFactor < 0.5 ? 4 * tFactor * tFactor * tFactor : 1 - Math.Pow(-2 * tFactor + 2, 3) / 2);

				var vector = _position - _positionPrevious;
				var vectorT = vector / duration;

				return _positionPrevious + vectorT * tSmoothed;
			}
			set
			{
				if (_position != value)
				{
					_positionPrevious = _position;
					_position = value;
					_updateTime = DateTime.UtcNow;
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

		public FrameworkElement Element => _circle;

		#region IVisual Methods

		public void ApplyTheme(Theme theme)
		{
			_circle.Stroke = theme.BurnTargetBackground;
			_circle.Fill = theme.Foreground;
			_circle.Effect = new DropShadowEffect()
			{
				Color = theme.BurnTargetBackground.Color,
				ShadowDepth = 0,
				BlurRadius = 20,
				RenderingBias = RenderingBias.Performance
			};
		}

		#endregion
	}
}
