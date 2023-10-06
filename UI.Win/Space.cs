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
		private BurnArea _burnableArea;
		private double _scale;
		private double _offsetX;
		private double _offsetY;
		private bool _autoCenterView;
		Stopwatch _renderStopwatch;
		private long _renderStopwatchIterations;
		private TimeSpan _renderStopwatchDuration;
		private double _renderRate;
		private TimeSpan _renderInterval;
		private DispatcherTimer _dispatcherTimer;
		private decimal _canvasWidthDot;
		private decimal _canvasHeightDot;
		private decimal _resolutionDpi;

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
			_renderInterval = TimeSpan.FromMilliseconds(8);
			
			_dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
			_dispatcherTimer.Interval = _renderInterval;
			_dispatcherTimer.Tick += OnRender;
			_dispatcherTimer.Start();

			_burnableArea = new BurnArea();
			_burnableArea.Size = new Size((double)_canvasWidthDot, (double)_canvasHeightDot);
			_burnableArea.Shape.Stroke = System.Windows.Media.Brushes.White;
			_burnableArea.Shape.StrokeThickness = 2;
			_burnableArea.Shape.StrokeDashArray = new DoubleCollection() { 5 };
			AddVisualToCanvas(_burnableArea);
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

		public decimal CanvasWidthDot
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
					RaisePropertyChanged(nameof(CanvasWidthDot));
				}
			}
		}

		public decimal CanvasHeightDot
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
					RaisePropertyChanged(nameof(CanvasHeightDot));
				}
			}
		}

		public decimal ResolutionDpi
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
				}
			}
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
					Render();
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
					Render();
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
			yield return _burnableArea;
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
			var shape = visual.Shape;
			shape.Width = visual.Size.Width * _scale;
			shape.Height = visual.Size.Height * _scale;

			var position = SpacePositionToScreenPosition(visual.Position);
			Canvas.SetTop(shape, position.Y);
			Canvas.SetLeft(shape, position.X);
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

		private void Render()
		{
			#region RenderInterval

			if (!_renderStopwatch.IsRunning)
			{
				_renderStopwatch.Start();
			}
			else
			{
				_renderInterval = _renderStopwatch.Elapsed;
				_renderStopwatchIterations++;
				_renderStopwatchDuration += _renderInterval;
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
						offsetX += visual.Position.X + visual.Size.Width / 2;
						offsetY += visual.Position.Y + visual.Size.Height / 2;
					}
					offsetX /= visuals.Length;
					offsetY /= visuals.Length;

					_offsetX += (-offsetX - _offsetX) * _renderInterval.TotalSeconds;
					_offsetY += (-offsetY - _offsetY) * _renderInterval.TotalSeconds;
				}
			}

			UpdateVisuals();
		}

		private void OnRender(object? sender, EventArgs e)
		{
			Render();
		}

		#endregion
	}
}
