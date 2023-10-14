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
		private Point _position;
		private Size _size;
		private PointAnimationStoryBoard _positionStoryBoard;

		public BurnTarget()
		{
			_circle = new System.Windows.Shapes.Ellipse();
			_positionStoryBoard = new PointAnimationStoryBoard { Limit = 2 };
		}

		public Point Position
		{
			get => _positionStoryBoard.CurrentAnimation.Current;
			set
			{
				if (_position != value)
				{
					_positionStoryBoard.Enqueue(new PointAnimation
					{
						From = _position,
						To = value,
						Duration = TimeSpan.FromSeconds(.25),
						AnimationTimingFunction = AnimationTimingFunction.EaseInOut
					});
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

		private enum AnimationTimingFunction 
		{
			Linear = 0,
			EaseInOut = 1
		}
		private class PointAnimation
		{
			private DateTime _startTime;
			private bool _hasStarted;
			private bool _hasFinished;

			public Point From { get; set; }
			public Point To { get; set; }
			public TimeSpan Duration { get; set; }
			public bool HasFinished
			{
				get
				{
					if (!_hasStarted)
					{
						return false;
					}
					if (_hasFinished)
					{
						return _hasFinished;
					}

					var now = DateTime.UtcNow;
					var duration = Duration.TotalMilliseconds;
					var t = (now - _startTime).TotalMilliseconds;
					if (t > duration)
					{
						_hasFinished = true;
					}

					return _hasFinished;
				}
			}
			public AnimationTimingFunction AnimationTimingFunction { get; set; }
			public Point Current
			{
				get
				{
					if (!_hasStarted)
					{
						return From;
					}
					if (HasFinished)
					{
						return To;
					}

					var duration = Duration.TotalMilliseconds;
					var t = (DateTime.UtcNow - _startTime).TotalMilliseconds;
					t = AnimationTimingFunction == AnimationTimingFunction.EaseInOut ? t * EaseInOut(t / duration) : t;

					var vector = To - From;
					var vectorT = vector / duration;

					return From + vectorT * t;
				}
			}

			public bool TryStart()
			{
				if(_hasStarted)
				{
					return false;
				}
				_hasStarted = true;
				_startTime = DateTime.UtcNow;

				return true;
			}

			private static double EaseInOut(double progress)
				=> progress < 0.5 ? 4 * progress * progress * progress : 1 - Math.Pow(-2 * progress + 2, 3) / 2;
		}

		private class PointAnimationStoryBoard
		{
			private readonly object _sync = new object();
			private readonly Queue<PointAnimation> _queue = new Queue<PointAnimation>();
			private PointAnimation _currentAnimation = new PointAnimation();

			public int Limit { get; set; }
			public PointAnimation CurrentAnimation
			{
				get
				{
					lock (_sync)
					{
						EnsureLimit();
						if (_queue.TryPeek(out var current))
						{
							current.TryStart();
							return _currentAnimation = current;
						}
						return _currentAnimation;
					}
				}
			}
			public void Enqueue(PointAnimation animation)
			{
				lock (_sync)
				{
					_queue.Enqueue(animation);
					EnsureLimit();
				}
			}

			private void EnsureLimit()
			{
				var limit = Limit;
				while (_queue.TryPeek(out var current))
				{
					if (current.HasFinished)
					{
						_queue.Dequeue();
					}
					else
					{
						break;
					}
				}
				while (_queue.Count > limit && _queue.TryDequeue(out _)) ;
			}
		}
	}
}
