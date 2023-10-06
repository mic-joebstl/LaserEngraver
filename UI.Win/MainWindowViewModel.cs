using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.Collections;
using System.Windows.Threading;

namespace LaserPathEngraver.UI.Win
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Fields

		private Space _space;
		private ImageBrush _sphere;
		private double _newElementMass;
		private double _newElementSize;
		private double _randomMinimumSize;
		private double _randomMaximumSize;
		private bool _enableVisualEffects;
		private bool _showTutorial;
		private Effect? _dropShadowEffect;
		private System.Windows.Point _mouseDown;
		private System.Windows.Point _mouseUp;
		private System.Windows.Point _mouseLastPos;
		private DispatcherTimer _mouseDownTimer;

		#endregion

		#region Initialization

		public MainWindowViewModel()
		{
			_space = new Space();
			_space.Canvas.PreviewMouseDown += OnSpaceMouseDown;
			_space.Canvas.PreviewMouseUp += OnSpaceMouseUp;
			_space.Canvas.PreviewMouseMove += OnSpaceMouseMove;
			_space.Canvas.PreviewMouseWheel += OnSpaceMouseWheel;
			_sphere = new ImageBrush();
			_enableVisualEffects = false;
			_showTutorial = true;
			_dropShadowEffect = new DropShadowEffect()
			{
				ShadowDepth = 0,
				Color = Colors.White,
				BlurRadius = 20,
				RenderingBias = RenderingBias.Performance
			};
			_mouseDownTimer = new DispatcherTimer(DispatcherPriority.Input);
			_mouseDownTimer.Interval = TimeSpan.FromMilliseconds(10);
			_mouseDownTimer.Tick += OnSpaceMouseDown;
		}

		#endregion

		#region Properties

		public Space Space
		{
			get
			{
				return _space;
			}
		}

		public bool EnableVisualEffects
		{
			get
			{
				return _enableVisualEffects;
			}
			set
			{
				_enableVisualEffects = value;
				RaisePropertyChanged(nameof(EnableVisualEffects));
				RaisePropertyChanged(nameof(DropShadowEffect));
			}
		}

		public bool ShowTutorial
		{
			get
			{
				return _showTutorial;
			}
			set
			{
				_showTutorial = value;
				RaisePropertyChanged(nameof(ShowTutorial));
			}
		}

		public Effect? DropShadowEffect
		{
			get
			{
				if (_enableVisualEffects)
					return _dropShadowEffect;
				else
					return null;
			}
			set
			{
				_dropShadowEffect = value;
				RaisePropertyChanged(nameof(DropShadowEffect));
			}
		}

		#endregion

		#region Methods

		private void OnSpaceMouseDown(object? sender, EventArgs e)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
			{
				/*
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
					System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
				{
					System.Windows.Point newPos = Space.ScreenPositionToSpacePosition(System.Windows.Input.Mouse.GetPosition(_Space.Canvas));


					Ball newBall = new Ball(_NewElementSize / 2);
					newBall.Mass = _NewElementMass;
					newBall.Visual.Fill = System.Windows.Media.Brushes.White;
					//newBall.Visual.Fill = _Miahel;
					newBall.Position = new System.Windows.Point(newPos.X - _NewElementSize / 2, newPos.Y - _NewElementSize / 2);
					_Space.Add(newBall);
					RaisePropertyChanged("NumberOfElements");
				}
				else
					_MouseDownTimer.Stop();
				*/
			}), DispatcherPriority.Input);
		}

		private void OnSpaceMouseDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				/*
				_MouseDown = e.GetPosition(_Space.Canvas);
				_Line.X1 = _MouseDown.X;
				_Line.Y1 = _MouseDown.Y;
				_Line.X2 = _MouseDown.X;
				_Line.Y2 = _MouseDown.Y;
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift))
				{
					_Line.Visibility = Visibility.Hidden;
					_MouseDownTimer.Start();
				}
				else if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
				{
					e.MouseDevice.Capture(_Space.Canvas);
					_Line.Visibility = Visibility.Hidden;
				}
				else
					_Line.Visibility = Visibility.Visible;
				*/
			}
		}

		private void OnSpaceMouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
		{
			/*
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
					System.Windows.Input.Keyboard.IsKeyUp(System.Windows.Input.Key.LeftShift))
				{
					_Space.EnableAutomaticCameraAdjustment = false;
					Vector delta = e.GetPosition(_Space.Canvas) - _MouseLastPos;
					_Space.OffsetX += delta.X / _Space.Scale;
					_Space.OffsetY += delta.Y / _Space.Scale;
				}

				_MouseUp = e.GetPosition(_Space.Canvas);
				_Line.X2 = _MouseUp.X;
				_Line.Y2 = _MouseUp.Y;
			}
			else if (e.LeftButton == System.Windows.Input.MouseButtonState.Released && _Line.Visibility == Visibility.Visible)
				_Line.Visibility = Visibility.Hidden;
			_MouseLastPos = e.GetPosition(_Space.Canvas);
			*/
		}

		private void OnSpaceMouseUp(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			/*
			if (_Line.Visibility == Visibility.Visible)
			{
				System.Windows.Point _SpaceMouseUp = _Space.ScreenPositionToSpacePosition(_MouseUp);
				System.Windows.Point _SpaceMouseDown = _Space.ScreenPositionToSpacePosition(_MouseDown);

				_Line.Visibility = Visibility.Hidden;
				Ball newBall = new Ball(_NewElementSize / 2);
				newBall.Mass = _NewElementMass;
				newBall.Visual.Fill = System.Windows.Media.Brushes.White;
				//newBall.Visual.Fill = _Miahel;
				newBall.Position = new System.Windows.Point(_SpaceMouseUp.X - _NewElementSize / 2, _SpaceMouseUp.Y - _NewElementSize / 2);
				newBall.Velocity = ((_MouseDown - _MouseUp) / _Space.Scale);
				_Space.Add(newBall);
				RaisePropertyChanged("NumberOfElements");
			}
			if (e.MouseDevice.Captured == _Space.Canvas)
			{
				e.MouseDevice.Capture(null);
			}
			*/
		}

		private void OnSpaceMouseWheel(object? sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			/*
			if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
			{
				_Space.Scale = Math.Round(_Space.Scale + (double)e.Delta / 1000, 4);
			}
			*/
		}

		public void OnSpaceKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
		{
			if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
				System.Windows.Input.Keyboard.IsKeyUp(System.Windows.Input.Key.LeftShift))
			{
				_space.Canvas.Cursor = System.Windows.Input.Cursors.ScrollAll;

				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.OemPlus) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Add))
				{
					_space.Scale += _space.Scale / 10;
				}
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.OemMinus) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Subtract))
				{
					_space.Scale -= _space.Scale / 10;
				}
			}
			else
				_space.Canvas.Cursor = System.Windows.Input.Cursors.Hand;

		}

		public void OnSpaceKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (System.Windows.Input.Keyboard.IsKeyUp(System.Windows.Input.Key.LeftCtrl) ||
				System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift))
			{
				_space.Canvas.Cursor = System.Windows.Input.Cursors.Hand;
			}
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion
	}
}
