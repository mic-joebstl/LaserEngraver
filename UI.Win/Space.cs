using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.UI.Win.Visuals;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LaserPathEngraver.UI.Win
{
	public class Space : INotifyPropertyChanged
	{
		#region Fields

		private IWritableOptions<DeviceConfiguration> _deviceConfiguration;
		private IWritableOptions<UserConfiguration> _userConfiguration;

		private Canvas _canvas;
		private BurnRectangle _burnBoundsRectangle;
		private BurnRectangle _burnBitmapRectangle;
		private BurnArea _burnArea;

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
		private DispatcherTimer _renderBitmapTimer;
		private double _canvasWidthDot;
		private double _canvasHeightDot;
		private double _resolutionDpi;

		#endregion

		#region Initialization

		public Space(IWritableOptions<DeviceConfiguration> deviceConfiguration, IWritableOptions<UserConfiguration> userConfiguration)
		{
			_userConfiguration = userConfiguration;
			_deviceConfiguration = deviceConfiguration;
			_canvasHeightDot = deviceConfiguration.Value.HeightDots;
			_canvasWidthDot = deviceConfiguration.Value.WidthDots;
			_resolutionDpi = deviceConfiguration.Value.DPI;
			_autoCenterView = userConfiguration.Value.AutoCenterView;
			_preserveAspectRatio = userConfiguration.Value.PreserveAspectRatio;

			_canvas = new Canvas();
			_canvas.Width = 0;
			_canvas.Height = 0;
			_canvas.HorizontalAlignment = HorizontalAlignment.Left;
			_canvas.VerticalAlignment = VerticalAlignment.Top;
			_canvas.Cursor = System.Windows.Input.Cursors.Hand;
			_canvas.Background = System.Windows.Media.Brushes.Transparent;

			_renderRate = 0;
			_scale = 1;
			_offsetX = 0;
			_offsetY = 0;
			_renderStopwatch = new Stopwatch();
			_renderCanvasInterval = TimeSpan.FromMilliseconds(8);
			_renderBitmapInterval = TimeSpan.FromMilliseconds(32);

			_renderCanvasTimer = new DispatcherTimer(DispatcherPriority.Render);
			_renderCanvasTimer.Interval = _renderCanvasInterval;
			_renderCanvasTimer.Tick += OnRenderCanvasTimer;
			_renderCanvasTimer.Start();

			_renderBitmapTimer = new DispatcherTimer(DispatcherPriority.Render);
			_renderBitmapTimer.Interval = _renderBitmapInterval;
			_renderBitmapTimer.Tick += OnRenderBitmapTimer;
			_renderBitmapTimer.Start();

			_burnBoundsRectangle = new BurnRectangle();
			_burnBoundsRectangle.Size = new Size((double)_canvasWidthDot, (double)_canvasHeightDot);
			_burnBoundsRectangle.Shape.Stroke = System.Windows.Media.Brushes.White;
			_burnBoundsRectangle.Shape.StrokeThickness = 2;
			_burnBoundsRectangle.Shape.StrokeDashArray = new DoubleCollection() { 5 };
			AddVisualToCanvas(_burnBoundsRectangle);

			_burnBitmapRectangle = new BurnRectangle();
			_burnBitmapRectangle.Shape.Stroke = System.Windows.Media.Brushes.White;
			_burnBitmapRectangle.Shape.StrokeThickness = 1;
			_burnBitmapRectangle.Shape.StrokeDashArray = new DoubleCollection() { 2.5 };

			AddVisualToCanvas(_burnBitmapRectangle);

			_burnArea = new BurnArea();
			_burnArea.PropertyChanged += OnBurnAreaPropertyChanged;
			_burnArea.Size = new Size(0, 0);
			_burnArea.Position = new Point((double)_canvasWidthDot / 2, (double)_canvasHeightDot / 2);
			_burnArea.BoundingRect = new System.Drawing.RectangleF(0, 0, (float)CanvasWidthDot, (float)CanvasHeightDot);
			AddVisualToCanvas(_burnArea);
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
					_burnArea.BoundingRect = new System.Drawing.RectangleF(0, 0, (float)CanvasWidthDot, (float)CanvasHeightDot);
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
					_burnArea.BoundingRect = new System.Drawing.RectangleF(0, 0, (float)CanvasWidthDot, (float)CanvasHeightDot);
					RaisePropertyChanged(nameof(CanvasHeightDot));
				}
			}
		}

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
					}
					else
					{
						_burnArea.Size = new Size(value, _burnArea.Size.Height);
						RaisePropertyChanged(nameof(ImageWidthDot));
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
					}
					else
					{
						_burnArea.Size = new Size(_burnArea.Size.Width, value);
						RaisePropertyChanged(nameof(ImageHeightDot));
					}
				}
			}
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
				return _resolutionDpi;
			}
			set
			{
				if (_resolutionDpi != value)
				{
					_resolutionDpi = value;
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
			get
			{
				return _scale;
			}
			set
			{
				if (value > 0)
				{
					_scale = value;

					UpdateVisuals();
					RaisePropertyChanged(nameof(Scale));
					RaisePropertyChanged(nameof(ScalePercent));
				}
			}
		}

		public double ScalePercent
		{
			get => Scale * 100;
			set => Scale = value / 100;
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

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion

		#region Methods

		private Point ScreenPositionToSpacePosition(Point position)
		{
			double x = (position.X - _canvas.ActualWidth / 2) / _scale - _offsetX;
			double y = (position.Y - _canvas.ActualHeight / 2) / _scale - _offsetY;
			return new Point(x, y);
		}

		private Point SpacePositionToScreenPosition(Point position)
		{
			double x = (position.X + _offsetX) * _scale + _canvas.ActualWidth / 2;
			double y = (position.Y + _offsetY) * _scale + _canvas.ActualHeight / 2;
			return new Point(x, y);
		}

		private IEnumerable<IVisual> GetVisuals()
		{
			yield return _burnBoundsRectangle;
			yield return _burnBitmapRectangle;
			yield return _burnArea;
		}

		private void UpdateVisuals(bool updatePositions = true, bool updateBitmap = true)
		{
			foreach (var visual in GetVisuals())
			{
				UpdateVisual(visual, updatePositions, updateBitmap);
			}
		}

		private void UpdateVisual(IVisual visual, bool updatePositions = true, bool updateBitmap = true)
		{
			if (updatePositions)
			{
				var shape = visual.Shape;
				shape.Width = visual.Size.Width * _scale;
				shape.Height = visual.Size.Height * _scale;

				var position = SpacePositionToScreenPosition(visual.Position);
				Canvas.SetTop(shape, position.Y);
				Canvas.SetLeft(shape, position.X);
			}

			if (updateBitmap)
			{
				if (visual is BurnArea burnarea)
				{
					burnarea.RenderImage();
				}
			}
		}

		private void AddVisualToCanvas(IVisual visual)
		{
			var shape = visual.Shape;
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
				double offsetX = 0;
				double offsetY = 0;

				var visuals = GetVisuals().ToArray();
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
						var moveFactor = 1 / _renderRate * 3;
						moveFactor = moveFactor > 1 ? 1 : moveFactor;

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

			UpdateVisuals(updatePositions: true, updateBitmap: false);
		}

		private void OnRenderBitmapTimer(object? sender, EventArgs e)
		{
			UpdateVisuals(updatePositions: false, updateBitmap: true);
		}

		private void OnBurnAreaPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BurnArea.Position))
			{
				_burnBitmapRectangle.Position = _burnArea.Position;
			}
			if (e.PropertyName == nameof(BurnArea.Size))
			{
				RaisePropertyChanged(nameof(ImageHeightDot));
				RaisePropertyChanged(nameof(ImageWidthDot));
				_burnBitmapRectangle.Size = _burnArea.Size;
			}
		}

		public void LoadBitmap(string filePath)
		{
			_burnArea.LoadBitmap(filePath);
		}

		#endregion
	}
}
