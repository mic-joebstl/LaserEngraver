using LaserEngraver.UI.Win.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			_positionStoryBoard = new PointAnimationStoryBoard { Limit = 10 };
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
						To = value
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
			EaseIn = 1,
			EaseOut = 2,
			EaseInOut = 3,
		}
		private class PointAnimation
		{
			private bool _hasStarted;
			private bool _hasFinished;
			private Stopwatch _stopWatch = new Stopwatch();

			public Point From { get; set; }
			public Point To { get; set; }
			public double SpeedFactor { get; set; }
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

					var duration = Duration.TotalMilliseconds * SpeedFactor;
					var t = _stopWatch.ElapsedMilliseconds;
					if (t >= duration)
					{
						_hasFinished = true;
						_stopWatch.Stop();
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

					var duration = Duration.TotalMilliseconds * SpeedFactor;
					var t = (double)_stopWatch.ElapsedMilliseconds;
					t =
						AnimationTimingFunction == AnimationTimingFunction.EaseInOut ? t * EaseInOut(t / duration) :
						AnimationTimingFunction == AnimationTimingFunction.EaseIn ? t * EaseIn(t / duration) :
						AnimationTimingFunction == AnimationTimingFunction.EaseOut ? t * EaseOut(t / duration) :
						t;

					var vector = To - From;
					var vectorT = vector / duration;

					return From + vectorT * t;
				}
			}

			public bool TryStart()
			{
				if (_hasStarted)
				{
					return false;
				}
				_hasStarted = true;
				_stopWatch.Start();

				return true;
			}

			private static double EaseIn(double progress)
				=> progress < 0 ? 0 : progress > 1 ? 1 :
				   1 - EaseOut(1 - progress);

			private static double EaseOut(double progress)
				=> progress < 0 ? 0 : progress > 1 ? 1 :
				   progress * progress * progress;

			private static double EaseInOut(double progress)
				=> progress < 0 ? 0 : progress > 1 ? 1 :
				   progress < 0.5 ? 4 * progress * progress * progress : 1 - Math.Pow(-2 * progress + 2, 3) / 2;
		}

		private class PointAnimationStoryBoard
		{
			private readonly object _sync = new object();
			private readonly Queue<PointAnimation> _queue = new Queue<PointAnimation>();
			private PointAnimation _currentAnimation = new PointAnimation();
			private PointAnimation _lastAnimation;

			public PointAnimationStoryBoard()
			{
				_lastAnimation = _currentAnimation;
				_lastAnimation.TryStart();
			}

			public int Limit { get; set; }
			public PointAnimation CurrentAnimation
			{
				get
				{
					lock (_sync)
					{
						EnsureCurrent();
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
					var length = (animation.To - animation.From).Length;
					animation.Duration = (TimeSpan.FromSeconds(0.5) / 1600) * length;

					if (_lastAnimation.HasFinished || animation.Duration > TimeSpan.FromMilliseconds(10) || _lastAnimation.Duration > TimeSpan.FromMilliseconds(10))
					{
						_queue.Enqueue(animation);
						_lastAnimation = animation;
					}
					else
					{
						_lastAnimation.To = animation.To;
					}

					EnsureCurrent();
				}
			}

			private void EnsureCurrent()
			{
				var count = _queue.Count;
				var limit = Limit;
				var i = 0;
				foreach (var animation in _queue)
				{
					i++;
					var x = count > limit ? limit : count;
					animation.SpeedFactor = Math.Clamp(Math.Round(Math.Cos(x * (Math.PI / (2 * limit))), 2), 0, 1);
					animation.AnimationTimingFunction =
						count == 1 ? AnimationTimingFunction.EaseInOut :
						i == count ? AnimationTimingFunction.EaseOut :
						AnimationTimingFunction.Linear;
				}

				while (_queue.TryPeek(out var current) && _queue.Count > 1)
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
			}
		}
	}
}
