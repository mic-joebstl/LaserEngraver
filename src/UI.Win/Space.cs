using LaserEngraver.Core.Configurations;
using LaserEngraver.Core.Devices;
using LaserEngraver.UI.Win.Configuration;
using LaserEngraver.UI.Win.Visuals;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LaserEngraver.UI.Win
{
	public class Space : INotifyPropertyChanged, IDisposable
	{
		#region Fields

		private IWritableOptions<DeviceConfiguration> _deviceConfiguration;
		private IWritableOptions<UserConfiguration> _userConfiguration;
		private IWritableOptions<BurnConfiguration> _burnConfiguration;

		private Canvas _canvas;
		private BurnRectangle _burnBoundsRectangle;
		private BurnRectangle _burnBitmapRectangle;
		private BurnArea _burnArea;
		private BurnTarget _burnTarget;

		private double _scale;
		private double _offsetX;
		private double _offsetY;
		private bool _autoCenterView;
		private bool _preserveAspectRatio;
		Stopwatch _renderStopwatch;
		private long _renderStopwatchIterations;
		private TimeSpan _renderStopwatchDuration;
		private double _renderRate;
		private TimeSpan _renderCanvasInterval;
		private TimeSpan _renderBitmapInterval;
		private DispatcherTimer _renderCanvasTimer;
		private BackgroundWorker _renderBitmapTimer;
		private double _canvasWidthDot;
		private double _canvasHeightDot;
		private double _deviceResolutionDpi;
		private double _screenResolutionDpi;
		private Dispatcher _windowDispatcher;

		#endregion

		#region Initialization

		public Space(IWritableOptions<DeviceConfiguration> deviceConfiguration, IWritableOptions<UserConfiguration> userConfiguration, IWritableOptions<BurnConfiguration> burnConfiguration, DeviceDispatcherService deviceDispatcher)
		{
			_userConfiguration = userConfiguration;
			_deviceConfiguration = deviceConfiguration;
			_burnConfiguration = burnConfiguration;
			_canvasHeightDot = deviceConfiguration.Value.HeightDots;
			_canvasWidthDot = deviceConfiguration.Value.WidthDots;
			_screenResolutionDpi = 96;
			_deviceResolutionDpi = deviceConfiguration.Value.DPI;
			_autoCenterView = userConfiguration.Value.AutoCenterView;
			_preserveAspectRatio = userConfiguration.Value.PreserveAspectRatio;
			_windowDispatcher = Dispatcher.CurrentDispatcher;

			_canvas = new Canvas();
			_canvas.Width = 0;
			_canvas.Height = 0;
			_canvas.HorizontalAlignment = HorizontalAlignment.Left;
			_canvas.VerticalAlignment = VerticalAlignment.Top;
			_canvas.Background = System.Windows.Media.Brushes.Transparent;

			_renderRate = 0;
			_scale = 1;
			_offsetX = 0;
			_offsetY = 0;
			_renderStopwatch = new Stopwatch();
			_renderCanvasInterval = TimeSpan.FromMilliseconds(8);
			_renderBitmapInterval = TimeSpan.FromMilliseconds(250);

			_renderCanvasTimer = new DispatcherTimer(DispatcherPriority.Render);
			_renderCanvasTimer.Interval = _renderCanvasInterval;
			_renderCanvasTimer.Tick += OnRenderCanvasTimer;
			_renderCanvasTimer.Start();

			_renderBitmapTimer = new BackgroundWorker();
			_renderBitmapTimer.DoWork += async (object? sender, DoWorkEventArgs e) =>
			{
				try
				{
					while (!_renderBitmapTimer.CancellationPending)
					{
						_burnArea?.RenderImage();
						await Task.Delay(_renderBitmapInterval);
					}
					e.Cancel = true;
				}
				catch (Exception)
				{
					//TODO MainWindow.ErrorMessage
				}
			};
			_renderBitmapTimer.RunWorkerAsync();

			_burnBoundsRectangle = new BurnRectangle();
			_burnBoundsRectangle.Size = new Size((double)_canvasWidthDot, (double)_canvasHeightDot);
			_burnBoundsRectangle.StrokeThickness = 2;
			_burnBoundsRectangle.StrokeDashArray = new DoubleCollection() { 5 };
			AddVisualToCanvas(_burnBoundsRectangle);

			_burnBitmapRectangle = new BurnRectangle();
			_burnBitmapRectangle.StrokeThickness = 1;
			_burnBitmapRectangle.StrokeDashArray = new DoubleCollection() { 2.5 };

			AddVisualToCanvas(_burnBitmapRectangle);

			_burnArea = new BurnArea();
			_burnArea.PropertyChanged += OnBurnAreaPropertyChanged;
			_burnArea.Size = new Size(0, 0);
			_burnArea.Position = new Point((double)_canvasWidthDot / 2, (double)_canvasHeightDot / 2);
			_burnArea.BoundingRect = CanvasBoundingRect;
			_burnArea.EngravingDuration = EngravingDuration;
			_burnArea.FixedDurationThreshold = FixedDurationThreshold;
			_burnArea.IsDurationVariable = IsDurationVariable;
			AddVisualToCanvas(_burnArea);

			_burnTarget = new BurnTarget();
			_burnTarget.Size = new Size(8, 8);
			_burnTarget.Element.Opacity = 0;
			deviceDispatcher.DevicePositionChanged += (Device sender, DevicePositionChangedEventArgs args) =>
			{
				_windowDispatcher.BeginInvoke(() =>
				{
					if (args.Position != null)
					{
						_burnTarget.Position = new Point(args.Position.Value.X, args.Position.Value.Y);
						_burnTarget.Element.Opacity = 0.7;
					}
					else
					{
						_burnTarget.Position = new Point(0, 0);
						_burnTarget.Element.Opacity = 0;
					}
				});
			};
			AddVisualToCanvas(_burnTarget);

			CenterRenderArea();
		}


		#endregion

		#region Properties

		public Canvas Canvas
		{
			get
			{
				return _canvas;
			}
		}

		public double ScreenResolutionDpi
		{
			get => _screenResolutionDpi;
			set
			{
				if (value != _screenResolutionDpi)
				{
					_screenResolutionDpi = value;
					RaisePropertyChanged(nameof(ScreenResolutionDpi));
					RaisePropertyChanged(nameof(ImageScale));
					RaisePropertyChanged(nameof(ImageScalePercent));
					RaisePropertyChanged(nameof(Scale));

				}
			}
		}

		public BurnArea BurnArea => _burnArea;

		public double ObservableWidth
		{
			get
			{
				return Canvas.ActualWidth;
			}
			set
			{
				Canvas.Width = value;
				RaisePropertyChanged(nameof(ObservableWidth));
			}
		}

		public double ObservableHeight
		{
			get
			{
				return Canvas.ActualHeight;
			}
			set
			{
				Canvas.Height = value;
				RaisePropertyChanged(nameof(ObservableHeight));
			}
		}

		public bool AutoCenterView
		{
			get => _autoCenterView;
			set
			{
				_userConfiguration.Update(config => config.AutoCenterView = value);
				_autoCenterView = value;
				RaisePropertyChanged(nameof(AutoCenterView));
			}
		}

		public double CanvasWidthDot
		{
			get
			{
				return _canvasWidthDot;
			}
			set
			{
				if (_canvasWidthDot != value)
				{
					_canvasWidthDot = value;
					_burnArea.BoundingRect = CanvasBoundingRect;
					RaisePropertyChanged(nameof(CanvasBoundingRect));
					RaisePropertyChanged(nameof(CanvasWidthDot));
				}
			}
		}

		public double CanvasHeightDot
		{
			get
			{
				return _canvasHeightDot;
			}
			set
			{
				if (_canvasHeightDot != value)
				{
					_canvasHeightDot = value;
					_burnArea.BoundingRect = CanvasBoundingRect;
					RaisePropertyChanged(nameof(CanvasBoundingRect));
					RaisePropertyChanged(nameof(CanvasHeightDot));
				}
			}
		}

		public System.Drawing.RectangleF CanvasBoundingRect => new System.Drawing.RectangleF(0, 0, (float)CanvasWidthDot, (float)CanvasHeightDot);

		public System.Drawing.RectangleF ImageBoundingRect => new System.Drawing.RectangleF((float)_burnArea.Position.X, (float)_burnArea.Position.Y, (float)ImageWidthDot, (float)ImageHeightDot);

		public double ImageWidthDot
		{
			get
			{
				return _burnArea.Size.Width;
			}
			set
			{
				var previousSize = _burnArea.Size;
				if (previousSize.Width != value)
				{
					if (PreserveAspectRatio)
					{
						var ratio = previousSize.Height > 0 ? previousSize.Width / previousSize.Height : 1;
						var width = value;
						var height = ratio > 0 ? value / ratio : previousSize.Height;

						_burnArea.Size = new Size(width, height);
						RaisePropertyChanged(nameof(ImageWidthDot));
						RaisePropertyChanged(nameof(ImageHeightDot));
						RaisePropertyChanged(nameof(ImageWidthCm));
						RaisePropertyChanged(nameof(ImageHeightCm));
						RaisePropertyChanged(nameof(ImageWidthIn));
						RaisePropertyChanged(nameof(ImageHeightIn));
						RaisePropertyChanged(nameof(ImageBoundingRect));
					}
					else
					{
						_burnArea.Size = new Size(value, _burnArea.Size.Height);
						RaisePropertyChanged(nameof(ImageWidthDot));
						RaisePropertyChanged(nameof(ImageHeightDot));
						RaisePropertyChanged(nameof(ImageWidthCm));
						RaisePropertyChanged(nameof(ImageHeightCm));
						RaisePropertyChanged(nameof(ImageWidthIn));
						RaisePropertyChanged(nameof(ImageHeightIn));
					}
				}
			}
		}

		public double ImageHeightDot
		{
			get
			{
				return _burnArea.Size.Height;
			}
			set
			{
				var previousSize = _burnArea.Size;
				if (previousSize.Width != value)
				{
					if (PreserveAspectRatio)
					{
						var ratio = previousSize.Height > 0 ? previousSize.Width / previousSize.Height : 1;
						var width = value * ratio;
						var height = value;

						_burnArea.Size = new Size(width, height);
						RaisePropertyChanged(nameof(ImageWidthDot));
						RaisePropertyChanged(nameof(ImageHeightDot));
						RaisePropertyChanged(nameof(ImageWidthCm));
						RaisePropertyChanged(nameof(ImageHeightCm));
						RaisePropertyChanged(nameof(ImageWidthIn));
						RaisePropertyChanged(nameof(ImageHeightIn));
						RaisePropertyChanged(nameof(ImageBoundingRect));
					}
					else
					{
						_burnArea.Size = new Size(_burnArea.Size.Width, value);
						RaisePropertyChanged(nameof(ImageHeightDot));
						RaisePropertyChanged(nameof(ImageHeightCm));
						RaisePropertyChanged(nameof(ImageBoundingRect));
					}
				}
			}
		}

		public double ImageWidthCm
		{
			get => ImageWidthDot / _deviceResolutionDpi * 2.54;
			set => ImageWidthDot = value / 2.54 * _deviceResolutionDpi;
		}

		public double ImageHeightCm
		{
			get => ImageHeightDot / _deviceResolutionDpi * 2.54;
			set => ImageHeightDot = value / 2.54 * _deviceResolutionDpi;
		}

		public double ImageWidthIn
		{
			get => ImageWidthDot / _deviceResolutionDpi;
			set => ImageWidthDot = value * _deviceResolutionDpi;
		}

		public double ImageHeightIn
		{
			get => ImageHeightDot / _deviceResolutionDpi;
			set => ImageHeightDot = value * _deviceResolutionDpi;
		}

		public bool PreserveAspectRatio
		{
			get => _preserveAspectRatio;
			set
			{
				_userConfiguration.Update(config => config.PreserveAspectRatio = value);
				_preserveAspectRatio = value;
				RaisePropertyChanged(nameof(PreserveAspectRatio));
			}
		}

		public double ResolutionDpi
		{
			get
			{
				return _deviceResolutionDpi;
			}
			set
			{
				if (_deviceResolutionDpi != value)
				{
					_deviceResolutionDpi = value;
					RaisePropertyChanged(nameof(ResolutionDpi));
				}
			}
		}

		public double RenderRate
		{
			get
			{
				return _renderRate > 999 ? 999 : _renderRate;
			}
		}

		public double Scale
		{
			get => _scale;
			set
			{
				if (_scale != value)
				{
					_scale = value;
					UpdateVisuals();
					RaisePropertyChanged(nameof(Scale));
					RaisePropertyChanged(nameof(ImageScale));
					RaisePropertyChanged(nameof(ImageScalePercent));
				}
			}
		}

		public double ImageScale
		{
			get
			{
				return Scale / _screenResolutionDpi * _deviceResolutionDpi;
			}
			set
			{
				if (value > 0)
				{
					Scale = value / _deviceResolutionDpi * _screenResolutionDpi;
				}
			}
		}

		public double ImageScalePercent
		{
			get => ImageScale * 100;
			set => ImageScale = value / 100;
		}

		public double OffsetX
		{
			get
			{
				return _offsetX;
			}
			set
			{
				if (_offsetX != value)
				{
					_offsetX = value;
					UpdateVisuals();
					RaisePropertyChanged(nameof(OffsetX));
				}
			}
		}

		public double OffsetY
		{
			get
			{
				return _offsetY;
			}
			set
			{
				if (_offsetY != value)
				{
					_offsetY = value;
					UpdateVisuals();
					RaisePropertyChanged(nameof(OffsetY));
				}
			}
		}

		public bool IsDurationVariable
		{
			get
			{
				return _burnConfiguration.Value.IntensityMode == BurnIntensityMode.Variable;
			}
			set
			{
				_burnConfiguration.Update(config => config.IntensityMode = value ? BurnIntensityMode.Variable : BurnIntensityMode.Fixed);
				_burnArea.IsDurationVariable = IsDurationVariable;
				RaisePropertyChanged(nameof(IsDurationVariable));
			}
		}

		public byte EngravingPower
		{
			get
			{
				return _burnConfiguration.Value.Power;
			}
			set
			{
				_burnConfiguration.Update(config => config.Power = value);
				RaisePropertyChanged(nameof(EngravingPower));
			}
		}

		public byte EngravingDuration
		{
			get
			{
				return _burnConfiguration.Value.Duration;
			}
			set
			{
				_burnConfiguration.Update(config => config.Duration = value);
				_burnArea.EngravingDuration = EngravingDuration;
				RaisePropertyChanged(nameof(EngravingDuration));
			}
		}

		public byte FixedDurationThreshold
		{
			get
			{
				return _burnConfiguration.Value.FixedIntensityThreshold;
			}
			set
			{
				_burnConfiguration.Update(config => config.FixedIntensityThreshold = value);
				_burnArea.FixedDurationThreshold = FixedDurationThreshold;
				RaisePropertyChanged(nameof(FixedDurationThreshold));
			}
		}

		public bool IsPlottingModeRasterEnabled
		{
			get
			{
				return _burnConfiguration.Value.PlottingMode == BurnPlottingMode.Raster;
			}
			set
			{
				_burnConfiguration.Update(config => config.PlottingMode = value ? BurnPlottingMode.Raster : BurnPlottingMode.RasterOptimized);
				RaisePropertyChanged(nameof(IsPlottingModeRasterEnabled));
				RaisePropertyChanged(nameof(IsPlottingModeRasterOptimizedEnabled));
			}
		}

		public bool IsPlottingModeRasterOptimizedEnabled
		{
			get
			{
				return _burnConfiguration.Value.PlottingMode == BurnPlottingMode.RasterOptimized;
			}
			set
			{
				_burnConfiguration.Update(config => config.PlottingMode = value ? BurnPlottingMode.RasterOptimized : BurnPlottingMode.Raster);
				RaisePropertyChanged(nameof(IsPlottingModeRasterEnabled));
				RaisePropertyChanged(nameof(IsPlottingModeRasterOptimizedEnabled));
			}
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion

		#region Methods

		public Point ScreenPositionToSpacePosition(Point position)
		{
			double x = (position.X - _canvas.ActualWidth / 2) / _scale - _offsetX;
			double y = (position.Y - _canvas.ActualHeight / 2) / _scale - _offsetY;
			return new Point(x, y);
		}

		public Point SpacePositionToScreenPosition(Point position)
		{
			double x = (position.X + _offsetX) * _scale + _canvas.ActualWidth / 2;
			double y = (position.Y + _offsetY) * _scale + _canvas.ActualHeight / 2;
			return new Point(x, y);
		}

		public IEnumerable<IVisual> GetVisuals()
		{
			yield return _burnBoundsRectangle;
			yield return _burnBitmapRectangle;
			yield return _burnArea;
			yield return _burnTarget;
		}

		private IEnumerable<IVisual> GetAutoCenterVisuals()
		{
			yield return _burnBoundsRectangle;
			yield return _burnBitmapRectangle;
			yield return _burnArea;
		}

		private void UpdateVisuals()
		{
			foreach (var visual in GetVisuals())
			{
				UpdateVisual(visual);
			}
		}

		private void UpdateVisual(IVisual visual)
		{
			var shape = visual.Element;
			if (visual is BurnTarget)
			{
				//BurnTarget has a fixed size
				shape.Width = visual.Size.Width;
				shape.Height = visual.Size.Height;

				var pointWidth = _scale;
				var position = SpacePositionToScreenPosition(new Point((int)visual.Position.X, (int)visual.Position.Y));
				Canvas.SetTop(shape, position.Y - (shape.Height / 2) + pointWidth / 2);
				Canvas.SetLeft(shape, position.X - (shape.Width / 2) + pointWidth / 2);
			}
			else
			{
				shape.Width = visual.Size.Width * _scale;
				shape.Height = visual.Size.Height * _scale;

				var position = SpacePositionToScreenPosition(new Point((int)visual.Position.X, (int)visual.Position.Y));
				Canvas.SetTop(shape, position.Y);
				Canvas.SetLeft(shape, position.X);
			}
		}

		private void AddVisualToCanvas(IVisual visual)
		{
			var shape = visual.Element;
			if (!Canvas.Children.Contains(shape))
			{
				Canvas.Children.Add(shape);
				UpdateVisual(visual);
			}
		}

		private void OnRenderCanvasTimer(object? sender, EventArgs e)
		{
			#region RenderInterval

			if (!_renderStopwatch.IsRunning)
			{
				_renderStopwatch.Start();
			}
			else
			{
				_renderStopwatchIterations++;
				_renderStopwatchDuration += _renderStopwatch.Elapsed;
				_renderStopwatch.Restart();

				if (_renderStopwatchDuration >= TimeSpan.FromSeconds(0.5))
				{
					_renderRate = _renderStopwatchIterations / _renderStopwatchDuration.TotalSeconds;
					_renderStopwatchDuration = TimeSpan.Zero;
					_renderStopwatchIterations = 0;
					RaisePropertyChanged(nameof(RenderRate));
				}
			}

			#endregion

			if (AutoCenterView)
			{
				CenterRenderArea(1 / _renderRate * 3);
			}

			UpdateVisuals();
		}

		private void OnBurnAreaPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BurnArea.Position))
			{
				_burnBitmapRectangle.Position = _burnArea.Position;
				RaisePropertyChanged(nameof(ImageBoundingRect));
			}
			if (e.PropertyName == nameof(BurnArea.Size))
			{
				RaisePropertyChanged(nameof(ImageHeightDot));
				RaisePropertyChanged(nameof(ImageWidthDot));
				RaisePropertyChanged(nameof(ImageHeightCm));
				RaisePropertyChanged(nameof(ImageWidthCm));
				RaisePropertyChanged(nameof(ImageHeightIn));
				RaisePropertyChanged(nameof(ImageWidthIn));
				RaisePropertyChanged(nameof(ImageBoundingRect));
				_burnBitmapRectangle.Size = _burnArea.Size;
			}
		}

		private void CenterRenderArea(double moveFactor = 1)
		{
			double offsetX = 0;
			double offsetY = 0;

			var visuals = GetAutoCenterVisuals().ToArray();
			if (visuals.Length > 0)
			{
				foreach (var visual in visuals)
				{
					offsetX -= visual.Position.X + visual.Size.Width / 2;
					offsetY -= visual.Position.Y + visual.Size.Height / 2;
				}
				offsetX /= visuals.Length;
				offsetY /= visuals.Length;

				var deltaX = offsetX - _offsetX;
				var deltaY = offsetY - _offsetY;

				if (deltaX != 0 || deltaY != 0)
				{
					moveFactor = moveFactor > 1 ? 1 : moveFactor < 0 ? 0 : moveFactor;

					var moveX = deltaX * moveFactor;
					var moveY = deltaY * moveFactor;

					var minValue = 0.1 / _scale;

					moveX = moveX > minValue || moveX < -minValue ? moveX : deltaX;
					moveY = moveY > minValue || moveY < -minValue ? moveY : deltaY;

					_offsetX += moveX;
					_offsetY += moveY;
				}
			}
		}

		public void LoadBitmap(string filePath)
		{
			_burnArea.LoadBitmap(filePath);
		}

		public void Dispose()
		{
			_renderBitmapTimer.Dispose();
			_renderCanvasTimer.Stop();
		}

		#endregion
	}
}
