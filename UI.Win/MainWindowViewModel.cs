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
using System.Linq;
using LaserPathEngraver.UI.Win.Visuals;

namespace LaserPathEngraver.UI.Win
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Fields

		private IWritableOptions<UserConfiguration> _userConfiguration;
		private Space _space;
		private bool _enableVisualEffects;
		private Effect? _dropShadowEffect;
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

		public bool ShowHelp
		{
			get
			{
				return _userConfiguration.Value.ShowHelp;
			}
			set
			{
				_userConfiguration.Update(config => config.ShowHelp = value);
				RaisePropertyChanged(nameof(ShowHelp));
			}
		}

		public bool ShowUnitCm
		{
			get
			{
				return _userConfiguration.Value.Unit == Unit.cm;
			}
			set
			{
				_userConfiguration.Update(config => config.Unit = value ? Unit.cm : Unit.px);
				RaisePropertyChanged(nameof(ShowUnitCm));
				RaisePropertyChanged(nameof(ShowUnitPx));
			}
		}

		public bool ShowUnitPx
		{
			get
			{
				return _userConfiguration.Value.Unit == Unit.px;
			}
			set
			{
				_userConfiguration.Update(config => config.Unit = value ? Unit.px : Unit.cm);
				RaisePropertyChanged(nameof(ShowUnitCm));
				RaisePropertyChanged(nameof(ShowUnitPx));
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
				else if(e.Source is FrameworkElement element && element.DataContext is BurnArea burnArea)
				{
					e.MouseDevice.Capture(element);
				}
				_mouseLastPos = e.GetPosition(_space.Canvas);
			}
		}

		private void OnSpaceMouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				var captured = e.MouseDevice.Captured;
				if(captured != null) 
				{
					Vector delta = e.GetPosition(_space.Canvas) - _mouseLastPos;

					if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
					{
						_space.AutoCenterView = false;
						_space.OffsetX += delta.X / _space.Scale;
						_space.OffsetY += delta.Y / _space.Scale;
					}
					else if(captured is FrameworkElement element && element.DataContext is BurnArea burnArea)
					{
						burnArea.Position = new System.Windows.Point
						{
							X = burnArea.Position.X + delta.X / _space.Scale,
							Y = burnArea.Position.Y + delta.Y / _space.Scale
						};
					}
				}
			}
			_mouseLastPos = e.GetPosition(_space.Canvas);
		}

		private void OnSpaceMouseUp(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			e.MouseDevice.Capture(null);
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
				else if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.OemMinus) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Subtract))
				{
					_space.Scale -= _space.Scale / 10;
				}
				else if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D0) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.NumPad0))
				{
					_space.Scale = 1;
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

		public void OnSpaceDragOver(object sender, DragEventArgs e)
		{
			var dropEnabled = false;
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
			{
				string[]? filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				if (filenames != null)
				{
					foreach (var filePath in filenames)
					{
						var extension = System.IO.Path.GetExtension(filePath);
						if (extension != null && (new[] { ".bmp", ".gif", ".exif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" }).Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
						{
							dropEnabled = true;
							break;
						}
					}
				}
			}

			if (!dropEnabled)
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;
			}
		}

		public void OnSpaceDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
			{
				string[]? filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				if (filenames != null)
				{
					foreach (var filePath in filenames)
					{
						var extension = System.IO.Path.GetExtension(filePath);
						if (extension != null && (new[] { ".bmp", ".gif", ".exif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" }).Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
						{
							_space.LoadBitmap(filePath);
							break;
						}
					}
				}
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
