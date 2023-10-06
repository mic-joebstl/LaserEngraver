﻿using System;
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
using LaserPathEngraver.Core.Devices;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using LaserPathEngraver.Core.Jobs;
using System.Windows.Input;

namespace LaserPathEngraver.UI.Win
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private string? _errorMessage;
		private IWritableOptions<UserConfiguration> _userConfiguration;
		private Space _space;
		private DeviceDispatcherService _deviceDispatcher;
		private bool _enableVisualEffects;
		private Effect? _dropShadowEffect;
		private System.Windows.Point _mouseLastPos;
		private System.Windows.Point _mouseLastDownPos;
		private DispatcherTimer _jobElapsedTimer;

		public MainWindowViewModel(IWritableOptions<UserConfiguration> userConfiguration, Space space, DeviceDispatcherService deviceDispatcher)
		{
			_userConfiguration = userConfiguration;
			_deviceDispatcher = deviceDispatcher;
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

			ConnectCommand = new AsyncRelayCommand(
				cancelableExecute: async (CancellationToken cancellationToken) =>
				{
					try
					{
						ErrorMessage = null;
						if (_deviceDispatcher.DeviceStatus == DeviceStatus.Disconnected)
						{
							await _deviceDispatcher.Connect(cancellationToken);
							await _deviceDispatcher.ExecuteJob(new HomingJob(), cancellationToken);
						}
						else if (_deviceDispatcher.DeviceStatus == DeviceStatus.Ready)
						{
							await _deviceDispatcher.Disconnect(cancellationToken);
						}
					}
					catch (Exception ex)
					{
						ErrorMessage = ex.Message;
					}
				},
				canExecute: () => IsEditable
			);

			var cancelCommand = new RelayCommand(
				execute: () =>
				{
					try
					{
						ErrorMessage = null;
						_deviceDispatcher.CancelJob();
					}
					catch (Exception ex)
					{
						ErrorMessage = ex.Message;
					}
				},
				canExecute: () => _deviceDispatcher.JobStatus == JobStatus.Running
			);
			CancelCommand = cancelCommand;

			_deviceDispatcher.DeviceStatusChanged += (Device sender, DeviceStatusChangedEventArgs args) =>
			{
				RaisePropertyChanged(nameof(ConnectCommandText));
				RaisePropertyChanged(nameof(DeviceStatusText));
				RaisePropertyChanged(nameof(IsEditable));
			};

			_deviceDispatcher.JobStatusChanged += (object sender, JobStatusChangedEventArgs args) =>
			{
				RaisePropertyChanged(nameof(JobStatusText));
				cancelCommand.NotifyCanExecuteChanged();
			};

			_jobElapsedTimer = new DispatcherTimer(DispatcherPriority.Render);
			_jobElapsedTimer.Interval = TimeSpan.FromSeconds(0.1);
			_jobElapsedTimer.Tick += (object? sender, EventArgs e) => RaisePropertyChanged(nameof(JobElapsedDuration));
			_jobElapsedTimer.Start();
		}

		public string? ErrorMessage
		{
			get => _errorMessage;
			set
			{
				_errorMessage = value;
				RaisePropertyChanged(nameof(ErrorMessage));
			}
		}

		public DeviceDispatcherService DeviceDispatcher => _deviceDispatcher;

		public TimeSpan JobElapsedDuration => DeviceDispatcher.JobElapsedDuration;

		public Space Space => _space;

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

		public string ConnectCommandText =>
			DeviceDispatcher.DeviceStatus == DeviceStatus.Disconnected ? Resources.Localization.Texts.ConnectButtonText :
			DeviceDispatcher.DeviceStatus == DeviceStatus.Connecting ? Resources.Localization.Texts.ConnectButtonText :
			Resources.Localization.Texts.DisconnectButtonText;

		public string? DeviceStatusText =>
			DeviceDispatcher.DeviceStatus == DeviceStatus.Disconnected ? Resources.Localization.Texts.DeviceStatusDisconnectedText :
			DeviceDispatcher.DeviceStatus == DeviceStatus.Connecting ? Resources.Localization.Texts.DeviceStatusConnectingText :
			DeviceDispatcher.DeviceStatus == DeviceStatus.Ready ? Resources.Localization.Texts.DeviceStatusReadyText :
			DeviceDispatcher.DeviceStatus == DeviceStatus.Executing ? Resources.Localization.Texts.DeviceStatusExecutingText :
			DeviceDispatcher.DeviceStatus == DeviceStatus.Disconnecting ? Resources.Localization.Texts.DeviceStatusDisconnectingText :
			null;

		public string? JobStatusText =>
			DeviceDispatcher.JobStatus == JobStatus.None ? Resources.Localization.Texts.JobStatusNoneText :
			DeviceDispatcher.JobStatus == JobStatus.Running ? String.Format(Resources.Localization.Texts.JobStatusRunningFormatText, DeviceDispatcher.JobTitle) :
			DeviceDispatcher.JobStatus == JobStatus.Paused ? String.Format(Resources.Localization.Texts.JobStatusPausedFormatText, DeviceDispatcher.JobTitle) :
			DeviceDispatcher.JobStatus == JobStatus.Cancelled ? String.Format(Resources.Localization.Texts.JobStatusStoppedFormatText, DeviceDispatcher.JobTitle) :
			DeviceDispatcher.JobStatus == JobStatus.Done ? String.Format(Resources.Localization.Texts.JobStatusDoneFormatText, DeviceDispatcher.JobTitle) :
			null;

		public ICommand ConnectCommand { get; private set; }

		public ICommand CancelCommand { get; private set; }

		public Cursor CanvasCursor
		{
			get
			{
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
				{
					return Cursors.SizeAll;
				}
				else if (IsEditable)
				{
					var boundingRect = Space.CanvasBoundingRect;
					var topLeft = Space.SpacePositionToScreenPosition(new System.Windows.Point(boundingRect.Left, boundingRect.Top));
					var bottomRight = Space.SpacePositionToScreenPosition(new System.Windows.Point(boundingRect.Right, boundingRect.Bottom));
					var pos = _mouseLastPos;
					if (pos.X >= topLeft.X && pos.X <= bottomRight.X && pos.Y >= topLeft.Y && pos.Y <= bottomRight.Y)
					{
						return Cursors.Hand;
					}
				}
				return Cursors.Arrow;
			}
		}

		public bool IsEditable => DeviceDispatcher.DeviceStatus == DeviceStatus.Disconnected || DeviceDispatcher.DeviceStatus == DeviceStatus.Ready;

		private void OnSpaceMouseDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
				{
					e.MouseDevice.Capture(_space.Canvas);
				}
				else if (e.Source is FrameworkElement element && element.DataContext is BurnArea)
				{
					if (IsEditable)
					{
						e.MouseDevice.Capture(element);
					}
				}
				_mouseLastDownPos = _mouseLastPos = e.GetPosition(_space.Canvas);
			}
		}

		private void OnSpaceMouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				var captured = e.MouseDevice.Captured;
				if (captured != null)
				{
					Vector delta = e.GetPosition(_space.Canvas) - _mouseLastPos;

					if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
					{
						_space.AutoCenterView = false;
						_space.OffsetX += delta.X / _space.Scale;
						_space.OffsetY += delta.Y / _space.Scale;
					}
					else if (captured is FrameworkElement element && element.DataContext is BurnArea burnArea)
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
			RaisePropertyChanged(nameof(CanvasCursor));
		}

		private void OnSpaceMouseUp(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (DeviceDispatcher.DeviceStatus == DeviceStatus.Ready && _mouseLastDownPos == e.GetPosition(_space.Canvas))
			{
				ErrorMessage = null;
				var spacePosition = Space.ScreenPositionToSpacePosition(_mouseLastDownPos);
				var boundingRect = Space.CanvasBoundingRect;
				spacePosition = new System.Windows.Point
				{
					X = spacePosition.X < boundingRect.Left ? boundingRect.Left : spacePosition.X > boundingRect.Right ? boundingRect.Right : spacePosition.X,
					Y = spacePosition.Y < boundingRect.Top ? boundingRect.Top : spacePosition.Y > boundingRect.Bottom ? boundingRect.Bottom : spacePosition.Y,
				};

				Dispatcher.CurrentDispatcher.BeginInvoke(async () => 
				{
					if (DeviceDispatcher.DeviceStatus == DeviceStatus.Ready)
					{
						try
						{
							

							await _deviceDispatcher.ExecuteJob(
								new MoveAbsoluteJob(new System.Drawing.Point { X = (int)spacePosition.X, Y = (int)spacePosition.Y }),
								CancellationToken.None
							);
						}
						catch (Exception ex)
						{
							ErrorMessage = ex.Message;
						}
					}
				});
			}

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
			RaisePropertyChanged(nameof(CanvasCursor));
		}

		public void OnSpaceKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			RaisePropertyChanged(nameof(CanvasCursor));
		}

		public void OnSpaceDragOver(object sender, DragEventArgs e)
		{
			if (!TryGetFirstSupportedFilePath(e, out _))
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;
			}
		}

		public void OnSpaceDrop(object sender, DragEventArgs e)
		{
			if (TryGetFirstSupportedFilePath(e, out var filePath))
			{
				_space.LoadBitmap(filePath);
			}
		}

		private bool TryGetFirstSupportedFilePath(DragEventArgs e, out string filePath)
		{
			if (IsEditable && e.Data.GetDataPresent(DataFormats.FileDrop, true))
			{
				string[]? filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				if (filenames != null)
				{
					foreach (var path in filenames)
					{
						var extension = System.IO.Path.GetExtension(path);
						if (extension != null && (new[] { ".bmp", ".gif", ".exif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" }).Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
						{
							filePath = path;
							return true;
						}
					}
				}
			}
			filePath = "";
			return false;
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion
	}
}
