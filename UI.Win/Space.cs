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
using System.Windows.Threading;

namespace LaserPathEngraver.UI.Win
{
	public class Space : INotifyPropertyChanged
	{
		#region Fields

		private Canvas _canvas;
		private DateTime _renderDateTime;
		private DateTime _renderCalculationsPerSecondDateTime;
		private List<double> _renderIntervalHistory;
		private double _scale;
		private double _offsetX;
		private double _offsetY;
		private double _calculationRate;
		private TimeSpan _realRenderInterval;
		private TimeSpan _previousRealRenderInterval;
		private DispatcherTimer _dispatcherTimer;
		private decimal _canvasWidthDot;
		private decimal _canvasHeightDot;
		private decimal _resolutionDpi;

		#endregion

		#region Initialization

		public Space()
		{
			_canvasHeightDot = 800;
			_canvasWidthDot = 800;
			_resolutionDpi = 25.4M;
			_canvas = new Canvas();
			_canvas.Width = 0;
			_canvas.Height = 0;
			_canvas.HorizontalAlignment = HorizontalAlignment.Left;
			_canvas.VerticalAlignment = VerticalAlignment.Top;
			_canvas.Cursor = System.Windows.Input.Cursors.Hand;
			_canvas.Background = System.Windows.Media.Brushes.Transparent;
			_renderDateTime = DateTime.Now;
			_renderCalculationsPerSecondDateTime = DateTime.Now;
			_renderIntervalHistory = new List<double>();
			_calculationRate = 0;
			_scale = 1;
			_offsetX = 0;
			_offsetY = 0;
			_realRenderInterval = TimeSpan.FromMilliseconds(1);
			_previousRealRenderInterval = TimeSpan.FromMilliseconds(1);
			_dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
			_dispatcherTimer.Interval = _realRenderInterval;
			_dispatcherTimer.Tick += OnRender;
			_dispatcherTimer.Start();
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

		public decimal CanvasWidthDot
		{
			get
			{
				return _canvasWidthDot;
			}
			set
			{
				if(_canvasWidthDot != value)
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

		public double CalculationRate
		{
			get
			{
				return _calculationRate;
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

					ScaleBodies();
					/*
					foreach (var body in _Bodies)
					{
						System.Windows.Controls.Canvas.SetTop(body.Visual, SpacePositionToScreenPosition(body.Position).Y);
						System.Windows.Controls.Canvas.SetLeft(body.Visual, SpacePositionToScreenPosition(body.Position).X);
					}
					*/
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

		public Point ScreenPositionToSpacePosition(Point position)
		{
			//double x = (position.X - _Canvas.ActualWidth / 2 - _OffsetX) / _Scale;
			//double y = (position.Y - _Canvas.ActualHeight / 2 - _OffsetY) / _Scale;
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

		private void OnBodyPositionChanged(object sender, EventArgs e)
		{
			/*
			var body = sender as IBody;
			if (body != null)
			{
				_canvas.Dispatcher.BeginInvoke((Action)(() =>
				{
					System.Windows.Controls.Canvas.SetTop(body.Visual, SpacePositionToScreenPosition(body.Position).Y);
					System.Windows.Controls.Canvas.SetLeft(body.Visual, SpacePositionToScreenPosition(body.Position).X);
				}), DispatcherPriority.Render);
			}
			*/
		}

		private void ScaleBodies()
		{
			/*
			foreach (var body in _Bodies)
			{
				if (body is Ball)
					ScaleBody(body);
				else
					throw new NotImplementedException();
			}
			*/
		}

		/*
		private void ScaleBody(IBody body)
		{
			if (body is Ball)
			{
				foreach (var b in _Bodies)
				{
					if (b == body)
					{
						b.Visual.Width = (b as Ball).Radius * 2 * _scale + 0.5;
						b.Visual.Height = (b as Ball).Radius * 2 * _scale + 0.5;
						break;
					}
				}
			}
			else
				throw new NotImplementedException();

		}

		private void AddBodyToCanvas(IBody body)
		{
			if (!Canvas.Children.Contains(body.Visual))
			{
				Canvas.Children.Add(body.Visual);
				System.Windows.Controls.Canvas.SetTop(body.Visual, SpacePositionToScreenPosition(body.Position).Y);
				System.Windows.Controls.Canvas.SetLeft(body.Visual, SpacePositionToScreenPosition(body.Position).X);
				body.PositionChanged += OnBodyPositionChanged;
			}
		}
		*/

		private void OnRender(object? sender, EventArgs e)
		{
			#region RenderInterval

			_previousRealRenderInterval = _realRenderInterval;
			_realRenderInterval = DateTime.Now - _renderDateTime;
			_renderDateTime = DateTime.Now;
			_renderIntervalHistory.Add(_realRenderInterval.TotalMilliseconds);

			if ((DateTime.Now - _renderCalculationsPerSecondDateTime).TotalMilliseconds >= 1000)
			{
				double sumOfCalculationsPerSecond = 0;
				foreach (double d in _renderIntervalHistory)
				{
					sumOfCalculationsPerSecond += 1000 / d;
				}
				_calculationRate = sumOfCalculationsPerSecond / _renderIntervalHistory.Count;
				_renderIntervalHistory.Clear();
				RaisePropertyChanged(nameof(CalculationRate));
				_renderCalculationsPerSecondDateTime = DateTime.Now;
			}

			#endregion

			/*
			double offsetX = 0;
			double offsetY = 0;

			double maxMass = 0;

			if (_EnableAutomaticCameraAdjustment && _Bodies.Count > 0)
			{
				maxMass = _Bodies.Max(b => b.Mass);
			}
			for (int i = 0; i < _Bodies.Count; i++)
			{
				_Bodies[i].Position = _Bodies[i].Position + _Bodies[i].Velocity * _realRenderInterval.TotalSeconds;
				_Bodies[i].GravitationalForce = new Vector(0, 0);

				//DetectCollisions();

				if (_EnableAutomaticCameraAdjustment)
				{
					offsetX += _Bodies[i].Position.X * (_Bodies[i].Mass / maxMass);
					offsetY += _Bodies[i].Position.Y * (_Bodies[i].Mass / maxMass);
				}

				if (_Bodies[i] is Ball)
				{
					if (_IsAudioVisualisationEnabled && (_VisualiseCapturedAudio || _VisualiseRenderedAudio))
					{
						double newRadius = 0;

						if (_VisualiseCapturedAudio && _VisualiseRenderedAudio)
							newRadius = (_Bodies[i] as Ball).DesiredRadius + (_Bodies[i] as Ball).DesiredRadius * (_AudioPeak.MasterPeaks[i % _AudioPeak.MasterPeaks.Count()]) * 5;
						else if (_VisualiseCapturedAudio)
							newRadius = (_Bodies[i] as Ball).DesiredRadius + (_Bodies[i] as Ball).DesiredRadius * (_AudioPeak.CapturingMasterPeaks[i % _AudioPeak.CapturingMasterPeaks.Count()]) * 5;
						else if (_VisualiseRenderedAudio)
							newRadius = (_Bodies[i] as Ball).DesiredRadius + (_Bodies[i] as Ball).DesiredRadius * (_AudioPeak.RenderingMasterPeaks[i % _AudioPeak.RenderingMasterPeaks.Count()]) * 3;

						double deltaRadius = newRadius - (_Bodies[i] as Ball).Radius;
						(_Bodies[i] as Ball).Radius = newRadius;
						_Bodies[i].Position = new Point(_Bodies[i].Position.X - deltaRadius, _Bodies[i].Position.Y - deltaRadius);
						_Bodies[i].Visual.Width = (_Bodies[i] as Ball).Radius * 2 * _scale + 0.5;
						_Bodies[i].Visual.Height = (_Bodies[i] as Ball).Radius * 2 * _scale + 0.5;
					}
				}
				else
				{
					throw new NotSupportedException();
				}
			}

			if (_EnableAutomaticCameraAdjustment && _Bodies.Count != 0)
			{
				offsetX /= _Bodies.Count;
				OffsetX += (-offsetX - OffsetX) * _realRenderInterval.TotalSeconds;
				offsetY /= _Bodies.Count;
				OffsetY += (-offsetY - OffsetY) * _realRenderInterval.TotalSeconds;
			}

			//Sets the forces of the bodies
			//for (int i = 0; i < _BodyPairList.Count; i++)
			Parallel.For(0, _BodyPairList.Count, (i) =>
			{
				bool fused = false;
				if (_IsCollisionEnabled)
				{
					if (_IsEnclosedArea)
					{

					}

					//DetectCollisions();

					if (Intersect(_BodyPairList[i].A, _BodyPairList[i].B) && !_BodyPairList[i].HaveContact)
					{
						_BodyPairList[i].HaveContact = true;

						SetVelocityAfterCollision(_BodyPairList[i].A, _BodyPairList[i].B);
					}
					else if (_BodyPairList[i].HaveContact)
					{
						if (Intersect(_BodyPairList[i].A, _BodyPairList[i].B))
						{
							AvoidOverlap(_BodyPairList[i].A, _BodyPairList[i].B);
							//AccumulateFriction(_BodyPairList[i].A, _BodyPairList[i].B, 1d, 0.3d);
						}
						else
						{
							_BodyPairList[i].HaveContact = false;
						}
					}
				}
				else if (_resolutionDpi)
				{
					if (Intersect(_BodyPairList[i].A, _BodyPairList[i].B))
					{
						_canvas.Dispatcher.BeginInvoke((Action<IBody, IBody>)((a, b) =>
						{
							if (Contains(a) && Contains(b))
							{
								Remove(a);
								Remove(b);
								Add(Fuse(a, b));
							}
						}), DispatcherPriority.Background, _BodyPairList[i].A, _BodyPairList[i].B);

						fused = true;
					}
				}

				if (_canvasHeightDot &&
					!fused &&
					!_BodyPairList[i].HaveContact)
				{
					SetGravitationForce(_BodyPairList[i].A, _BodyPairList[i].B);
				}
			});
			*/
		}

		#endregion
	}
}
