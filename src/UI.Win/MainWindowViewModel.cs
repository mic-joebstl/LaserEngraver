﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Effects;
using System.Collections;
using System.Windows.Threading;
using LaserEngraver.Core.Configurations;
using System.Linq;
using LaserEngraver.UI.Win.Visuals;
using LaserEngraver.Core.Devices;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using LaserEngraver.Core.Jobs;
using System.Windows.Input;
using LaserEngraver.UI.Win.Configuration;
using System.Reactive.Linq;

namespace LaserEngraver.UI.Win
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private string? _errorMessage;
		private IWritableOptions<UserConfiguration> _userConfiguration;
		private IWritableOptions<DeviceConfiguration> _deviceConfiguration;
		private IWritableOptions<BurnConfiguration> _burnConfiguration;
		private Space _space;
		private Dispatcher _windowDispatcher;
		private DeviceDispatcherService _deviceDispatcher;
		private bool _enableVisualEffects;
		private DropShadowEffect? _dropShadowEffect;
		private System.Windows.Point _mouseLastPos;
		private System.Windows.Point _mouseLastDownPos;
		private DispatcherTimer _jobElapsedTimer;
		private Theme _theme;
		private Theme? _customTheme;
		private DeviceStatus? _debouncedStatus;

		public MainWindowViewModel(IWritableOptions<UserConfiguration> userConfiguration, IWritableOptions<DeviceConfiguration> deviceConfiguration, IWritableOptions<BurnConfiguration> burnConfiguration, Space space, DeviceDispatcherService deviceDispatcher, Dispatcher windowDispatcher)
		{
			_userConfiguration = userConfiguration;
			_deviceConfiguration = deviceConfiguration;
			_burnConfiguration = burnConfiguration;
			_deviceDispatcher = deviceDispatcher;
			_windowDispatcher = windowDispatcher;
			_space = space;
			_space.Canvas.PreviewMouseDown += OnSpaceMouseDown;
			_space.Canvas.PreviewMouseUp += OnSpaceMouseUp;
			_space.Canvas.PreviewMouseMove += OnSpaceMouseMove;
			_space.Canvas.PreviewMouseWheel += OnSpaceMouseWheel;
			_enableVisualEffects = false;
			_dropShadowEffect = new DropShadowEffect()
			{
				ShadowDepth = 0,
				BlurRadius = 20,
				RenderingBias = RenderingBias.Performance
			};
			_theme = userConfiguration.Value.Theme;
			_customTheme = userConfiguration.Value.CustomTheme;
			_customTheme =
				_customTheme is not null && !_customTheme.Equals(Theme.Dark) && !_customTheme.Equals(Theme.Light) ? _customTheme : 
				!_theme.Equals(Theme.Dark) && !_theme.Equals(Theme.Light) ? _theme :
				null;

			_jobElapsedTimer = new DispatcherTimer(DispatcherPriority.Render);
			_jobElapsedTimer.Interval = TimeSpan.FromSeconds(0.1);
			_jobElapsedTimer.Tick += (object? sender, EventArgs e) => RaisePropertyChanged(nameof(JobElapsedDuration));
			_jobElapsedTimer.Start();

			InitializeThemeBindings();
			InitializeCommands();
		}

#if DEBUG
		public bool IsDebugModeEnabled => true;
#else
		public bool IsDebugModeEnabled => false;
#endif


		public Theme Theme => _theme;

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
				return _userConfiguration.Value.Unit == Unit.Cm;
			}
			set
			{
				_userConfiguration.Update(config => config.Unit = value ? Unit.Cm : Unit.Px);
				RaisePropertyChanged(nameof(ShowUnitPx));
				RaisePropertyChanged(nameof(ShowUnitCm));
				RaisePropertyChanged(nameof(ShowUnitIn));
			}
		}

		public bool ShowUnitPx
		{
			get
			{
				return _userConfiguration.Value.Unit == Unit.Px;
			}
			set
			{
				_userConfiguration.Update(config => config.Unit = value ? Unit.Px : Unit.Cm);
				RaisePropertyChanged(nameof(ShowUnitPx));
				RaisePropertyChanged(nameof(ShowUnitCm));
				RaisePropertyChanged(nameof(ShowUnitIn));
			}
		}

		public bool ShowUnitIn
		{
			get
			{
				return _userConfiguration.Value.Unit == Unit.In;
			}
			set
			{
				_userConfiguration.Update(config => config.Unit = value ? Unit.In : Unit.Cm);
				RaisePropertyChanged(nameof(ShowUnitPx));
				RaisePropertyChanged(nameof(ShowUnitCm));
				RaisePropertyChanged(nameof(ShowUnitIn));
			}
		}

		public bool HasCustomTheme => _customTheme != null;

		public bool ShowCustomTheme
		{
			get
			{
				return _userConfiguration.Value?.Theme.Equals(_customTheme) ?? false;
			}
			set
			{
				_theme = _customTheme ?? Theme.Default;
				_userConfiguration.Update(UpdateUserConfigTheme);
				RaisePropertyChanged(nameof(Theme));
				RaisePropertyChanged(nameof(ShowCustomTheme));
				RaisePropertyChanged(nameof(ShowDarkTheme));
				RaisePropertyChanged(nameof(ShowLightTheme));
			}
		}

		public bool ShowDarkTheme
		{
			get
			{
				return _userConfiguration.Value?.Theme.Equals(Theme.Dark) ?? false;
			}
			set
			{
				_theme = Theme.Dark;
				_userConfiguration.Update(UpdateUserConfigTheme);
				RaisePropertyChanged(nameof(Theme));
				RaisePropertyChanged(nameof(ShowCustomTheme));
				RaisePropertyChanged(nameof(ShowDarkTheme));
				RaisePropertyChanged(nameof(ShowLightTheme));
			}
		}

		public bool ShowLightTheme
		{
			get
			{
				return _userConfiguration.Value?.Theme.Equals(Theme.Light) ?? false;
			}
			set
			{
				_theme = Theme.Light;
				_userConfiguration.Update(UpdateUserConfigTheme);
				RaisePropertyChanged(nameof(Theme));
				RaisePropertyChanged(nameof(ShowCustomTheme));
				RaisePropertyChanged(nameof(ShowDarkTheme));
				RaisePropertyChanged(nameof(ShowLightTheme));
			}
		}

		public DropShadowEffect? DropShadowEffect
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
			DeviceDispatcher.DeviceStatus == DeviceStatus.Disconnected || DeviceDispatcher.DeviceStatus == DeviceStatus.Connecting ?
			Resources.Localization.Texts.ConnectButtonText :
			Resources.Localization.Texts.DisconnectButtonText;

		public string StartCommandText =>
			DeviceDispatcher.IsJobPausable ? Resources.Localization.Texts.PauseButtonText :
			DeviceDispatcher.JobStatus == JobStatus.Paused ? Resources.Localization.Texts.ContinueButtonText :
			Resources.Localization.Texts.StartButtonText;

		public string? DeviceStatusText =>
			_debouncedStatus == DeviceStatus.Disconnected ? Resources.Localization.Texts.DeviceStatusDisconnectedText :
			_debouncedStatus == DeviceStatus.Connecting ? Resources.Localization.Texts.DeviceStatusConnectingText :
			_debouncedStatus == DeviceStatus.Ready ? Resources.Localization.Texts.DeviceStatusReadyText :
			_debouncedStatus == DeviceStatus.Executing ? Resources.Localization.Texts.DeviceStatusExecutingText :
			_debouncedStatus == DeviceStatus.Disconnecting ? Resources.Localization.Texts.DeviceStatusDisconnectingText :
			null;

		public string? JobStatusText =>
			DeviceDispatcher.JobStatus == JobStatus.None ? Resources.Localization.Texts.JobStatusNoneText :
			DeviceDispatcher.JobStatus == JobStatus.Running ? String.Format(Resources.Localization.Texts.JobStatusRunningFormatText, DeviceDispatcher.JobTitle) :
			DeviceDispatcher.JobStatus == JobStatus.Paused ? String.Format(Resources.Localization.Texts.JobStatusPausedFormatText, DeviceDispatcher.JobTitle) :
			DeviceDispatcher.JobStatus == JobStatus.Cancelled ? String.Format(Resources.Localization.Texts.JobStatusStoppedFormatText, DeviceDispatcher.JobTitle) :
			DeviceDispatcher.JobStatus == JobStatus.Done ? String.Format(Resources.Localization.Texts.JobStatusDoneFormatText, DeviceDispatcher.JobTitle) :
			null;

		public ICommand? ConnectCommand { get; private set; }

		public ICommand? StartCommand { get; private set; }

		public ICommand? CancelCommand { get; private set; }

		public ICommand? FramingCommand { get; private set; }

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

		public bool IsEditable =>
			(
				DeviceDispatcher.DeviceStatus == DeviceStatus.Disconnected ||
				DeviceDispatcher.DeviceStatus == DeviceStatus.Ready
			)
			&& DeviceDispatcher.JobStatus != JobStatus.Running
			&& DeviceDispatcher.JobStatus != JobStatus.Paused;

		private void OnSpaceMouseDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				_mouseLastDownPos = _mouseLastPos = e.GetPosition(_space.Canvas);

				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
				{
					e.MouseDevice.Capture(_space.Canvas);
				}
				else if (IsEditable)
				{
					if (e.Source is FrameworkElement element && element.DataContext is BurnArea)
					{
						e.MouseDevice.Capture(element);
					}
					else
					{
						var boundingRect = Space.ImageBoundingRect;
						var topLeft = Space.SpacePositionToScreenPosition(new System.Windows.Point(boundingRect.Left, boundingRect.Top));
						var bottomRight = Space.SpacePositionToScreenPosition(new System.Windows.Point(boundingRect.Right, boundingRect.Bottom));
						var pos = _mouseLastPos;
						if (pos.X >= topLeft.X && pos.X <= bottomRight.X && pos.Y >= topLeft.Y && pos.Y <= bottomRight.Y)
						{
							e.MouseDevice.Capture(Space.BurnArea.Element);
						}
					}
				}
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

				_windowDispatcher.BeginInvoke(async () =>
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
				_space.ImageScale = Math.Round(_space.ImageScale + (double)e.Delta / 1000, 4);
			}
		}

		public void OnSpaceKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
		{
			if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
			{
				double? scale = null;
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.OemPlus) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Add))
				{
					scale = _space.ImageScalePercent + _space.ImageScalePercent / 10;
				}
				else if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.OemMinus) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Subtract))
				{
					scale = _space.ImageScalePercent - _space.ImageScalePercent / 10;
				}
				else if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D0) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.NumPad0))
				{
					scale = 100;
				}

				if (scale.HasValue)
				{
					scale = scale - scale % 5;
					_space.ImageScalePercent = scale.Value;
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

		private void InitializeCommands()
		{
			var connectCommand = new AsyncRelayCommand(
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
			ConnectCommand = connectCommand;

			var startCommand = new RelayCommand(
				execute: () =>
				{
					try
					{
						ErrorMessage = null;

						if (_deviceDispatcher.IsJobPausable)
						{
							_deviceDispatcher.PauseJob();
						}
						else if (_deviceDispatcher.JobStatus == JobStatus.Paused)
						{
							_windowDispatcher.BeginInvoke(async () => await _deviceDispatcher.ContinueJob(CancellationToken.None));
						}
						else if (_deviceDispatcher.DeviceStatus == DeviceStatus.Ready && DeviceDispatcher.JobStatus != JobStatus.Running && DeviceDispatcher.JobStatus != JobStatus.Paused)
						{
							var th = new Thread(async () =>
							{
								try
								{
									ErrorMessage = null;
									var burnArea = Space.BurnArea;
									var points = burnArea.Points;
									foreach (var point in points)
									{
										point.IsVisited = false;
									}
									var burnAreaPosition = new System.Drawing.Point
									{
										X = (int)burnArea.Position.X,
										Y = (int)burnArea.Position.Y
									};
									var burnAreaSize = new System.Drawing.Size
									{
										Width = (int)burnArea.Size.Width,
										Height = (int)burnArea.Size.Height
									};
									var job = new EngraveJob(_deviceConfiguration.Value, _burnConfiguration.Value, burnAreaSize, burnAreaPosition, points);
									await _deviceDispatcher.ExecuteJob(job, CancellationToken.None);
								}
								catch (Exception ex)
								{
									ErrorMessage = ex.Message;
								}
							});
							th.Start();
						}
					}
					catch (Exception ex)
					{
						ErrorMessage = ex.Message;
					}
				},
				canExecute: () =>
					_deviceDispatcher.JobStatus == JobStatus.Paused ||
					_deviceDispatcher.IsJobPausable ||
					_deviceDispatcher.DeviceStatus == DeviceStatus.Ready &&
					DeviceDispatcher.JobStatus != JobStatus.Running &&
					DeviceDispatcher.JobStatus != JobStatus.Paused &&
					Space.BurnArea.Points.Any()

			);
			StartCommand = startCommand;

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
				canExecute: () => _deviceDispatcher.JobStatus == JobStatus.Running || _deviceDispatcher.JobStatus == JobStatus.Paused
			);
			CancelCommand = cancelCommand;

			var framingCommand = new AsyncRelayCommand(
				cancelableExecute: async (CancellationToken cancellationToken) =>
				{
					try
					{
						ErrorMessage = null;
						var frameF = Space.ImageBoundingRect;
						var frame = new System.Drawing.Rectangle((int)frameF.X, (int)frameF.Y, (int)frameF.Width, (int)frameF.Height);
						if (frame.Width > 0 && frame.Height > 0)
						{
							var stepDelay = TimeSpan.FromSeconds(0.5);
							await _deviceDispatcher.ExecuteJob(new FramingJob(frame, stepDelay), cancellationToken);
						}
					}
					catch (Exception ex)
					{
						ErrorMessage = ex.Message;
					}
				},
				canExecute: () =>
				{
					if (!IsEditable || DeviceDispatcher.DeviceStatus != DeviceStatus.Ready)
					{
						return false;
					}
					var frame = Space.ImageBoundingRect;
					return frame.Width > 0 && frame.Height > 0;
				}
			);
			FramingCommand = framingCommand;

			_debouncedStatus = DeviceStatus.Disconnected;
			Observable
				.FromEvent<DeviceStatusChangedEventHandler, DeviceStatusChangedEventArgs>(
					handler => (sender, args) => handler(args),
					handler => _deviceDispatcher.DeviceStatusChanged += handler,
					handler => _deviceDispatcher.DeviceStatusChanged -= handler
				)
				.Throttle(TimeSpan.FromMilliseconds(80))
				.Subscribe((args) =>
				{
					_windowDispatcher.BeginInvoke(() =>
					{
						_debouncedStatus = _deviceDispatcher.DeviceStatus;
						RaisePropertyChanged(nameof(DeviceStatusText));
					});
				});

			_deviceDispatcher.DeviceStatusChanged += (Device sender, DeviceStatusChangedEventArgs args) =>
			{
				_windowDispatcher.BeginInvoke(() =>
				{
					RaisePropertyChanged(nameof(ConnectCommandText));
					RaisePropertyChanged(nameof(StartCommandText));
					RaisePropertyChanged(nameof(IsEditable));
					if (_debouncedStatus < _deviceDispatcher.DeviceStatus)
					{
						_debouncedStatus = _deviceDispatcher.DeviceStatus;
						RaisePropertyChanged(nameof(DeviceStatusText));
					}
					connectCommand.NotifyCanExecuteChanged();
					startCommand.NotifyCanExecuteChanged();
					framingCommand.NotifyCanExecuteChanged();
				});
			};

			_deviceDispatcher.JobStatusChanged += (object sender, JobStatusChangedEventArgs args) =>
			{
				_windowDispatcher.BeginInvoke(() =>
				{
					RaisePropertyChanged(nameof(JobStatusText));
					RaisePropertyChanged(nameof(StartCommandText));
					RaisePropertyChanged(nameof(IsEditable));
					connectCommand.NotifyCanExecuteChanged();
					startCommand.NotifyCanExecuteChanged();
					cancelCommand.NotifyCanExecuteChanged();
					framingCommand.NotifyCanExecuteChanged();
				});
			};

			Space.PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
			{
				_windowDispatcher.BeginInvoke(() =>
				{
					if (e.PropertyName == nameof(Space.ImageBoundingRect))
					{
						framingCommand.NotifyCanExecuteChanged();
					}
				});
			};
			Space.BurnArea.PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
			{
				_windowDispatcher.BeginInvoke(() =>
				{
					if (e.PropertyName == nameof(Space.BurnArea.Points))
					{
						startCommand.NotifyCanExecuteChanged();
					}
				});
			};
		}

		private void InitializeThemeBindings()
		{
			_theme.PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
			{
				_userConfiguration.Update(UpdateUserConfigTheme);
				RaisePropertyChanged(nameof(Theme));
			};

			void OnThemeChanged()
			{
				_dropShadowEffect.Color = _theme.Foreground.Color;
				RaisePropertyChanged(nameof(DropShadowEffect));

				foreach (var visual in Space.GetVisuals())
				{
					visual.ApplyTheme(_theme);
				}
			}

			PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
			{
				if (e.PropertyName == nameof(Theme))
				{
					OnThemeChanged();
				}
			};

			OnThemeChanged();
		}

		private void UpdateUserConfigTheme(UserConfiguration config)
		{
			config.Theme = _theme;
			config.CustomTheme = _customTheme;
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion
	}
}
