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
using LaserPathEngraver.Core.Configurations;
using Microsoft.Extensions.Options;

namespace LaserPathEngraver.UI.Win
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Fields

		private IWritableOptions<UserConfiguration> _userConfiguration;
		private Space _space;
		private bool _enableVisualEffects;
		private Effect? _dropShadowEffect;
		private System.Windows.Point _mouseDown;
		private System.Windows.Point _mouseUp;
		private System.Windows.Point _mouseLastPos;

		#endregion

		#region Initialization

		public MainWindowViewModel(IWritableOptions<UserConfiguration> userConfiguration, Space space)
		{
			_userConfiguration = userConfiguration;
			_space = space;
			_space.Canvas.PreviewMouseDown += OnSpaceMouseDown;
			_space.Canvas.PreviewMouseUp += OnSpaceMouseUp;
			_space.Canvas.PreviewMouseMove += OnSpaceMouseMove;
			_space.Canvas.PreviewMouseWheel += OnSpaceMouseWheel;
			_enableVisualEffects = false;
			_dropShadowEffect = new DropShadowEffect()
			{
				ShadowDepth = 0,
				Color = Colors.White,
				BlurRadius = 20,
				RenderingBias = RenderingBias.Performance
			};
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
				return _userConfiguration.Value.ShowTutorial;
			}
			set
			{
				_userConfiguration.Update(config => config.ShowTutorial = value);
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

		private void OnSpaceMouseDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
				{
					e.MouseDevice.Capture(_space.Canvas);
				}
				_mouseDown = e.GetPosition(_space.Canvas);
			}
		}

		private void OnSpaceMouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
				{
					_space.AutoCenterView = false;
					Vector delta = e.GetPosition(_space.Canvas) - _mouseLastPos;
					_space.OffsetX += delta.X / _space.Scale;
					_space.OffsetY += delta.Y / _space.Scale;
				}

				_mouseUp = e.GetPosition(_space.Canvas);
			}
			_mouseLastPos = e.GetPosition(_space.Canvas);
		}

		private void OnSpaceMouseUp(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.MouseDevice.Captured == _space.Canvas)
			{
				e.MouseDevice.Capture(null);
			}
		}

		private void OnSpaceMouseWheel(object? sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
			{
				_space.Scale = Math.Round(_space.Scale + (double)e.Delta / 1000, 4);
			}
		}

		public void OnSpaceKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
		{
			if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
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
			if (System.Windows.Input.Keyboard.IsKeyUp(System.Windows.Input.Key.LeftCtrl))
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
