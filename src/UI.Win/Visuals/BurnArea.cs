using LaserEngraver.Core.Configurations;
using LaserEngraver.Core.Jobs;
using LaserEngraver.UI.Win.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.Media3D.Converters;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LaserEngraver.UI.Win.Visuals
{
	public class BurnArea : IVisual, INotifyPropertyChanged
	{
		private SemaphoreSlim _renderSync;
		private Theme? _theme;
		private Image _image;
		private System.Drawing.Bitmap? _originalBitmap;
		private System.Drawing.Bitmap? _scaledBitmap;
		private WriteableBitmap? _bitmapImage;
		private MemoryStream? _imageStream;
		private List<BurnTarget> _targets;
		private Point _position;
		private Size _size;
		private System.Drawing.RectangleF? _boundingRect;
		private bool _requiresRenderUpdate;
		private bool _requiresPartialRenderUpdate;
		private bool _requiresTargetUpdate;
		private DateTime _targetUpdateRequestUtcDate = DateTime.MinValue;
		private TimeSpan _targetUpdateDebounceTime = TimeSpan.FromMilliseconds(320);
		private bool _resizing = false;
		private byte _engravingDuration = 0xff;
		private byte _fixedDurationThreshold = 0xff;
		private bool _isDurationVariable = false;
		private Dispatcher _windowDispatcher;

		public BurnArea()
		{
			_image = new Image();
			_image.SnapsToDevicePixels = true;
			_image.DataContext = this;
			_targets = new List<BurnTarget>(0);
			_renderSync = new SemaphoreSlim(1, 1);
			_requiresRenderUpdate = true;
			_windowDispatcher = Dispatcher.CurrentDispatcher;
		}

		public Point Position
		{
			get => _position;
			set
			{
				if (_position == value)
					return;

				lock (_renderSync)
				{
					if (_resizing || _position == value)
						return;

					_position = value;

					ResizeToBoundingRect();
					RaisePropertyChanged(nameof(Position));
				}
			}
		}

		public Size Size
		{
			get => _size;
			set
			{
				if (_size == value)
					return;

				lock (_renderSync)
				{
					if (_resizing || _size == value)
						return;

					_size = value;
					_requiresRenderUpdate = true;
					_requiresPartialRenderUpdate = false;

					ResizeToBoundingRect();
					RaisePropertyChanged(nameof(Size));
				}
			}
		}

		public bool IsDurationVariable
		{
			get => _isDurationVariable;
			set
			{
				if (_isDurationVariable != value)
				{
					_isDurationVariable = value;
					RequiresTargetUpdate = true;
					RaisePropertyChanged(nameof(IsDurationVariable));
				}
			}
		}

		public byte EngravingDuration
		{
			get => _engravingDuration;
			set
			{
				if (_engravingDuration != value)
				{
					_engravingDuration = value;
					RequiresTargetUpdate = true;
					RaisePropertyChanged(nameof(EngravingDuration));
				}
			}
		}

		public byte FixedDurationThreshold
		{
			get => _fixedDurationThreshold;
			set
			{
				if (_fixedDurationThreshold != value)
				{
					_fixedDurationThreshold = value;
					RequiresTargetUpdate = true;
					RaisePropertyChanged(nameof(FixedDurationThreshold));
				}
			}
		}

		public System.Drawing.RectangleF? BoundingRect
		{
			get => _boundingRect;
			set
			{
				if (_boundingRect != value)
				{
					_boundingRect = value;
					RaisePropertyChanged(nameof(BoundingRect));
				}
			}

		}

		public FrameworkElement Element => _image;

		public IEnumerable<IEngravePoint> Points
		{
			get
			{
				var enumerator = _targets.GetEnumerator();
				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}
		}

		private bool RequiresTargetUpdate
		{
			get => _requiresTargetUpdate && DateTime.UtcNow - _targetUpdateRequestUtcDate > _targetUpdateDebounceTime;
			set 
			{
				lock (_renderSync)
				{
					if (_requiresTargetUpdate = value)
					{
						_requiresPartialRenderUpdate = false;
						_targetUpdateRequestUtcDate = DateTime.UtcNow;
					}
				}
			}
		}
		
		public void RenderImage()
		{
			var requiresRenderUpdate = false;
			var requiresPartialRenderUpdate = false;
			var requiresTargetUpdate = false;
			var theme = default(Theme);

			lock (_renderSync)
			{
				requiresRenderUpdate = _requiresRenderUpdate;
				_requiresRenderUpdate = false;

				requiresPartialRenderUpdate = !requiresRenderUpdate && _requiresPartialRenderUpdate;
				_requiresPartialRenderUpdate = false;

				requiresTargetUpdate = RequiresTargetUpdate;
				RequiresTargetUpdate = false;
				theme = _theme;
			}

			if (!requiresRenderUpdate && !requiresPartialRenderUpdate && !requiresTargetUpdate)
				return;

			_renderSync.Wait();

			try
			{
				var targets = _targets;
				var originalBitmap = _originalBitmap;

				if (originalBitmap != null)
				{
					var width = (int)(Size.Width < 1 ? 1 : Size.Width);
					var height = (int)(Size.Height < 1 ? 1 : Size.Height);
					var scaledBitmap = _scaledBitmap;
					if (originalBitmap != null && requiresTargetUpdate || scaledBitmap == null || (width != scaledBitmap.Width || height != scaledBitmap.Height))
					{
						scaledBitmap = ScaleBitmap();
						targets = _targets;
					}
					if (scaledBitmap != null)
					{
						width = scaledBitmap.Width;
						height = scaledBitmap.Height;
						_size = new Size(width, height);

						var defaultColor = theme is null ? Color.FromArgb(0, 0, 0, 0) : (Color?)null;
						var writeableBitmap = _bitmapImage;
						var requiresPartialUpdate = requiresPartialRenderUpdate && writeableBitmap is not null;
						var areaWidth = 0;
						var areaHeight = 0;
						var areaTargets = default(List<BurnTarget>);
						var areaOffset = default((int X, int Y));

						if (requiresPartialUpdate)
						{
							_requiresPartialRenderUpdate = false;
							var topLeft = default((int X, int Y)?);
							var bottomRight = default((int X, int Y)?);

							foreach (var target in targets.Where(t => !t.IsDrawn))
							{
								if (topLeft is null)
								{
									topLeft = (target.X, target.Y);
								}
								if (topLeft.Value.X > target.X)
								{
									topLeft = (target.X, topLeft.Value.Y);
								}
								if (topLeft.Value.Y > target.Y)
								{
									topLeft = (topLeft.Value.X, target.Y);
								}

								if (bottomRight is null)
								{
									bottomRight = (target.X, target.Y);
								}
								if (bottomRight.Value.X < target.X)
								{
									bottomRight = (target.X, bottomRight.Value.Y);
								}
								if (bottomRight.Value.Y < target.Y)
								{
									bottomRight = (bottomRight.Value.X, target.Y);
								}
							}

							if (topLeft is not null && bottomRight is not null)
							{
								areaTargets = targets
									.Where(t =>
										t.X >= topLeft.Value.X && t.X <= bottomRight.Value.X &&
										t.Y >= topLeft.Value.Y && t.Y <= bottomRight.Value.Y)
									.ToList();
								areaWidth = bottomRight.Value.X - topLeft.Value.X + 1;
								areaHeight = bottomRight.Value.Y - topLeft.Value.Y + 1;
								areaOffset = (topLeft.Value.X, topLeft.Value.Y);
							}
						}
						else
						{
							areaTargets = targets;
							areaWidth = width;
							areaHeight = height;
						}

						if (areaWidth > 0 && areaHeight > 0)
						{
							var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
							int bitsPerPixel = ((int)format & 0xff00) >> 8;
							int bytesPerPixel = (bitsPerPixel + 7) / 8;
							int stride = 4 * ((areaWidth * bytesPerPixel + 3) / 4);
							var imageBuffer = new byte[areaWidth * areaHeight * bytesPerPixel];

							Parallel.For(0, areaTargets.Count, i =>
							{
								var target = areaTargets[i];
								var byteIndex = (target.Y - areaOffset.Y) * areaWidth * bytesPerPixel + (target.X - areaOffset.X) * bytesPerPixel;
								var fillValue = target.Intensity;
								var color = target.IsVisited ?
									theme.GetBurnVisitedColor(fillValue)
									: defaultColor ?? theme.GetBurnGradientColor(fillValue);

								imageBuffer[byteIndex + 3] = color.A;
								imageBuffer[byteIndex + 2] = color.R;
								imageBuffer[byteIndex + 1] = color.G;
								imageBuffer[byteIndex + 0] = color.B;

								target.IsDrawn = true;
							});

							if (requiresPartialUpdate)
							{
								_windowDispatcher.Invoke(() =>
								{
									var rect = new Int32Rect(0, 0, areaWidth, areaHeight);
									writeableBitmap.WritePixels(rect, imageBuffer, stride, areaOffset.X, areaOffset.Y);
								});
							}
							else
							{
								GCHandle pinnedArray = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);
								try
								{
									IntPtr pointer = pinnedArray.AddrOfPinnedObject();
									var bitmap = new System.Drawing.Bitmap(width, height, stride, format, pointer);
									var ms = new MemoryStream();
									bitmap.Save(ms, ImageFormat.Png);
									ms.Position = 0;

									_windowDispatcher.BeginInvoke(() =>
									{
										Interlocked.Exchange(ref _imageStream, ms)?.Dispose();

										var tempImage = new BitmapImage();
										tempImage.BeginInit();
										tempImage.StreamSource = ms;
										tempImage.EndInit();

										_bitmapImage = new WriteableBitmap(tempImage);
										_image.Source = _bitmapImage;
									});
								}
								finally
								{
									pinnedArray.Free();
								}
							}
						}
					}
				}
			}
			finally
			{
				_renderSync.Release();
			}
		}

		public void LoadBitmap(string filePath)
		{
			_originalBitmap = new System.Drawing.Bitmap(filePath);
			Size = new Size
			{
				Width = _originalBitmap.Width,
				Height = _originalBitmap.Height
			};

			var boundingRectObj = BoundingRect;
			if (boundingRectObj.HasValue)
			{
				var boundingRect = boundingRectObj.Value;
				Position = new Point
				{
					X = (boundingRect.Width - Size.Width) / 2,
					Y = (boundingRect.Height - Size.Height) / 2
				};
			}

			RequiresTargetUpdate = true;
			RenderImage();
		}

		private System.Drawing.Bitmap? ScaleBitmap()
		{
			var originalBitmap = _originalBitmap;
			if (originalBitmap is null)
			{
				return _scaledBitmap = null;
			}

			var width = (int)(Size.Width < 1 ? 1 : Size.Width);
			var height = (int)(Size.Height < 1 ? 1 : Size.Height);
			var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Transparent);
			var scaledBitmap = new System.Drawing.Bitmap(width, height);
			var graph = System.Drawing.Graphics.FromImage(scaledBitmap);

			graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			graph.FillRectangle(brush, new System.Drawing.RectangleF(0, 0, width, height));
			graph.DrawImage(originalBitmap, 0, 0, width, height);

			Interlocked.Exchange(ref _targets, new List<BurnTarget>(0));
			var targets = new List<BurnTarget>(scaledBitmap.Width * scaledBitmap.Height);

			var isDurationVariable = IsDurationVariable;
			var maxDuration = EngravingDuration;
			var minDuration = isDurationVariable ? 0 : FixedDurationThreshold;
			var intensityFactor = (double)maxDuration / 0xff;

			for (int x = 0; x < scaledBitmap.Width; x++)
			{
				for (int y = 0; y < scaledBitmap.Height; y++)
				{
					var pixel = scaledBitmap.GetPixel(x, y);
					var value = (0xff - pixel.R + 0xff - pixel.G + 0xff - pixel.B) / 3d;
					value *= pixel.A / 0xff;
					if (value < minDuration)
					{
						value = 0;
					}
					else if (isDurationVariable)
					{
						value *= intensityFactor;
					}
					else
					{
						value = maxDuration;
					}

					var target = new BurnTarget(this)
					{
						Intensity = (byte)value,
						X = x,
						Y = y
					};
					targets.Add(target);
				}
			}
			Interlocked.Exchange(ref _scaledBitmap, scaledBitmap);
			Interlocked.Exchange(ref _targets, targets);

			_windowDispatcher.BeginInvoke(() => RaisePropertyChanged(nameof(Points)));
			return scaledBitmap;
		}

		private void ResizeToBoundingRect()
		{
			if (_resizing)
				return;

			using (new Resizing(this))
			{
				if (_resizing)
					return;

				var boundingRectObj = BoundingRect;
				if (boundingRectObj is null)
					return;
				var boundingRect = boundingRectObj.Value;

				var position = Position;
				var size = Size;
				var ratio = size.Height != 0 ? size.Width / size.Height : 1;
				ratio = ratio > 0 ? ratio : 1;

				size.Height = size.Height > boundingRect.Height ? boundingRect.Height : size.Height;
				size.Width = size.Height * ratio;
				size.Width = size.Width > boundingRect.Width ? boundingRect.Width : size.Width;
				size.Height = size.Width / ratio;

				if (position.X + size.Width > boundingRect.Width)
				{
					position.X = boundingRect.Width - size.Width;
				}
				if (position.X < boundingRect.X)
				{
					position.X = 0;
				}

				if (position.Y + size.Height > boundingRect.Height)
				{
					position.Y = boundingRect.Height - size.Height;
				}
				if (position.Y < boundingRect.Y)
				{
					position.Y = 0;
				}

				Position = position;
				Size = size;
			}
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion

		#region IVisual Methods

		public void ApplyTheme(Theme theme)
		{
			if (_theme != theme)
			{
				lock(_renderSync)
				{
					_theme = theme;
					_requiresRenderUpdate = true;
					_requiresPartialRenderUpdate = false;
				}
			}
		}

		#endregion

		private class BurnTarget : IEngravePoint
		{
			private BurnArea _owner;
			private bool _isVisited;

			public BurnTarget(BurnArea owner)
			{
				_owner = owner;
			}

			public int X { get; set; }
			public int Y { get; set; }
			public bool IsVisited
			{
				get => _isVisited;
				set
				{
					if (_isVisited != value)
					{
						_isVisited = value;
						IsDrawn = false;
						_owner._requiresPartialRenderUpdate = true;
					}
				}
			}
			public bool IsDrawn { get; set; }
			public byte Intensity { get; set; } = 0xff;
		}

		private class Resizing : IDisposable
		{
			private BurnArea _owner;
			private Size _originalSize;
			private Point _originalPosition;
			public Resizing(BurnArea owner)
			{
				_owner = owner;
				_owner._resizing = true;
				_owner._renderSync.Wait();
				_originalPosition = owner.Position;
				_originalSize = owner.Size;
			}

			public void Dispose()
			{
				if ((int)_owner.Size.Width != (int)_originalSize.Width || (int)_owner.Size.Height != (int)_originalSize.Height)
				{
					_owner.RaisePropertyChanged(nameof(_owner.Size));
				}
				if ((int)_owner.Position.X != (int)_originalPosition.X || (int)_owner.Position.Y != (int)_originalPosition.Y)
				{
					_owner.RaisePropertyChanged(nameof(_owner.Position));
				}

				_owner._resizing = false;
				_owner._renderSync.Release();
			}
		}
	}
}
