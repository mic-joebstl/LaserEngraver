﻿using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.Core.Jobs;
using LaserPathEngraver.UI.Win.Configuration;
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

namespace LaserPathEngraver.UI.Win.Visuals
{
	public class BurnArea : IVisual, INotifyPropertyChanged
	{
		private SemaphoreSlim _renderSync;
		private Theme? _theme;
		private Image _image;
		private System.Drawing.Bitmap? _originalBitmap;
		private System.Drawing.Bitmap? _scaledBitmap;
		private BitmapImage? _bitmapImage;
		private MemoryStream? _imageStream;
		private List<BurnTarget> _targets;
		private Point _position;
		private Size _size;
		private System.Drawing.RectangleF? _boundingRect;
		private bool _requiresRenderUpdate;
		private bool _requiresTargetUpdate;
		private DateTime _targetUpdateRequestUtcDate = DateTime.MinValue;
		private TimeSpan _targetUpdateDebounceTime = TimeSpan.FromMilliseconds(320);
		private bool _resizing = false;
		private byte _engraverPower = 0xff;
		private byte _fixedPowerThreshold = 0xff;
		private bool _isPowerVariable = false;

		public BurnArea()
		{
			_image = new Image();
			_image.DataContext = this;
			_targets = new List<BurnTarget>();
			_renderSync = new SemaphoreSlim(1, 1);
			_requiresRenderUpdate = true;
		}

		public Point Position
		{
			get => _position;
			set
			{
				if (_position != value)
				{
					_position = value;
					if (_resizing)
						return;
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
				if (_size != value)
				{
					_size = value;
					_requiresRenderUpdate = true;
					if (_resizing)
						return;
					ResizeToBoundingRect();
					RaisePropertyChanged(nameof(Size));
				}
			}
		}

		public bool IsPowerVariable
		{
			get => _isPowerVariable;
			set
			{
				if (_isPowerVariable != value)
				{
					_isPowerVariable = value;
					RequestTargetUpdate();
					RaisePropertyChanged(nameof(IsPowerVariable));
				}
			}
		}

		public byte EngravingPower
		{
			get => _engraverPower;
			set 
			{
				if(_engraverPower != value)
				{
					_engraverPower = value;
					RequestTargetUpdate();
					RaisePropertyChanged(nameof(EngravingPower));
				}
			}
		}

		public byte FixedPowerThreshold
		{
			get => _fixedPowerThreshold;
			set
			{
				if (_fixedPowerThreshold != value)
				{
					_fixedPowerThreshold = value;
					RequestTargetUpdate();
					RaisePropertyChanged(nameof(FixedPowerThreshold));
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

		public void RenderImage()
		{
			if (!_requiresRenderUpdate && !RequiresTargetUpdate())
				return;

			_renderSync.Wait();
			try
			{
				if (!_requiresRenderUpdate && !RequiresTargetUpdate())
					return;

				if (_originalBitmap is null)
				{
					_targets.Clear();
				}

				if (_originalBitmap != null)
				{
					_bitmapImage = new BitmapImage();
					_imageStream?.Dispose();
					_bitmapImage.BeginInit();
					var ms = _imageStream = new MemoryStream();

					var width = (int)(Size.Width < 1 ? 1 : Size.Width);
					var height = (int)(Size.Height < 1 ? 1 : Size.Height);
					if (_originalBitmap != null && RequiresTargetUpdate() || _scaledBitmap != null && (width != _scaledBitmap.Width || height != _scaledBitmap.Height))
					{
						ScaleBitmap();
					}
					if (_scaledBitmap != null)
					{
						width = _scaledBitmap.Width;
						height = _scaledBitmap.Height;
						_size = new Size(width, height);

						var theme = _theme;
						var defaultColor = theme is null ? Color.FromArgb(0, 0, 0, 0) : (Color?)null;
						var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
						int bitsPerPixel = ((int)format & 0xff00) >> 8;
						int bytesPerPixel = (bitsPerPixel + 7) / 8;
						int stride = 4 * ((width * bytesPerPixel + 3) / 4);
						var imageBuffer = new byte[width * height * bytesPerPixel];

						Parallel.For(0, _targets.Count, i =>
						{
							var target = _targets[i];
							var byteIndex = target.Y * width * bytesPerPixel + target.X * bytesPerPixel;
							var fillValue = target.Intensity;
							var color = target.IsVisited ? 
								theme.GetBurnVisitedColor(fillValue)
								: defaultColor ?? theme.GetBurnGradientColor(fillValue);

							imageBuffer[byteIndex + 3] = color.A;
							imageBuffer[byteIndex + 2] = color.R;
							imageBuffer[byteIndex + 1] = color.G;
							imageBuffer[byteIndex + 0] = color.B;
						});

						GCHandle pinnedArray = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);
						try
						{
							IntPtr pointer = pinnedArray.AddrOfPinnedObject();
							var bitmap = new System.Drawing.Bitmap(width, height, stride, format, pointer);
							bitmap.Save(ms, ImageFormat.Png);
							ms.Position = 0;
							_bitmapImage.StreamSource = ms;
							_bitmapImage.EndInit();
						}
						finally
						{
							pinnedArray.Free();
						}
						_image.SnapsToDevicePixels = true;
						_image.Source = _bitmapImage;
					}
				}

				_requiresRenderUpdate = false;
			}
			finally
			{
				_renderSync.Release();
			}
		}

		public void LoadBitmap(string filePath)
		{
			_requiresRenderUpdate = true;
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

			ScaleBitmap();
		}

		private void ScaleBitmap()
		{
			var originalBitmap = _originalBitmap;
			if (originalBitmap is null)
			{
				_scaledBitmap = null;
				return;
			}

			var width = Size.Width < 1 ? 1 : (float)Size.Width;
			var height = Size.Height < 1 ? 1 : (float)Size.Height;
			var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Transparent);
			var scaledBitmap = new System.Drawing.Bitmap((int)width, (int)height);
			var graph = System.Drawing.Graphics.FromImage(scaledBitmap);

			graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			graph.FillRectangle(brush, new System.Drawing.RectangleF(0, 0, (float)width, (float)height));
			graph.DrawImage(originalBitmap, 0, 0, width, height);
			_scaledBitmap = scaledBitmap;

			_targets.Clear();

			var isPowerVariable = IsPowerVariable;
			var maxPower = EngravingPower;
			var minPower = isPowerVariable ? 0 : FixedPowerThreshold;
			var intensityFactor = (double)maxPower / 0xff;

			for (int x = 0; x < _scaledBitmap.Width; x++)
			{
				for (int y = 0; y < _scaledBitmap.Height; y++)
				{
					var pixel = scaledBitmap.GetPixel(x, y);
					var value = (0xff - pixel.R + 0xff - pixel.G + 0xff - pixel.B) / 3d;
					value *= pixel.A / 0xff;
					if (value < minPower)
					{
						value = 0;
					}
					else if(isPowerVariable)
					{
						value *= intensityFactor;
					}
					else
					{
						value = maxPower;
					}

					var target = new BurnTarget(this)
					{
						Intensity = (byte)value,
						X = x,
						Y = y
					};
					_targets.Add(target);
				}
			}
			_requiresTargetUpdate = false;
			RaisePropertyChanged(nameof(Points));
		}

		private void ResizeToBoundingRect()
		{
			if (_resizing)
				return;

			using (new Resizing(this))
			{
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

		private void RequestTargetUpdate()
		{
			_requiresTargetUpdate = true;
			_targetUpdateRequestUtcDate = DateTime.UtcNow;
		}

		private bool RequiresTargetUpdate()
		{
			return _requiresTargetUpdate && DateTime.UtcNow - _targetUpdateRequestUtcDate > _targetUpdateDebounceTime;
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
				_theme = theme;
				_requiresRenderUpdate = true;
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
						_owner._requiresRenderUpdate = true;
					}
				}
			}
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
