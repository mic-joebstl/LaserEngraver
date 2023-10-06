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
		private Rectangle _rectangle;
		private System.Drawing.Bitmap? _originalBitmap;
		private System.Drawing.Bitmap? _scaledBitmap;
		private BitmapImage? _image;
		private MemoryStream? _imageStream;
		private List<BurnTarget> _targets;
		private Point _position;
		private Size _size;
		private System.Drawing.RectangleF? _boundingRect;
		private bool _requiresRenderUpdate;
		private bool _resizing = false;

		public BurnArea()
		{
			_rectangle = new Rectangle();
			_rectangle.DataContext = this;
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

		public Shape Shape => _rectangle;

		public void RenderImage()
		{
			if (!_requiresRenderUpdate)
				return;

			_renderSync.Wait();
			try
			{
				if (!_requiresRenderUpdate)
					return;

				_image = new BitmapImage();
				_imageStream?.Dispose();
				_image.BeginInit();
				var ms = _imageStream = new MemoryStream();

				if (_scaledBitmap is null || _originalBitmap is null)
				{
					_targets.Clear();
				}

				var width = (int)(Size.Width < 1 ? 1 : Size.Width);
				var height = (int)(Size.Height < 1 ? 1 : Size.Height);
				if (_scaledBitmap != null && (width != _scaledBitmap.Width || height != _scaledBitmap.Height))
				{
					ScaleBitmap();
				}

				var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
				int bitsPerPixel = ((int)format & 0xff00) >> 8;
				int bytesPerPixel = (bitsPerPixel + 7) / 8;
				int stride = 4 * ((width * bytesPerPixel + 3) / 4);
				var imageBuffer = new byte[width * height * bytesPerPixel];

				Parallel.For(0, _targets.Count, i =>
				{
					var target = _targets[i];
					var byteIndex = target.Y * width * bytesPerPixel + target.X * bytesPerPixel;

					imageBuffer[byteIndex] = 0xff;
					imageBuffer[byteIndex + 1] = 0xff;
					imageBuffer[byteIndex + 2] = 0xff;
					imageBuffer[byteIndex + 3] = (byte)(0xff * target.FillRatio);
				});

				GCHandle pinnedArray = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);
				try
				{
					IntPtr pointer = pinnedArray.AddrOfPinnedObject();
					var bitmap = new System.Drawing.Bitmap(width, height, stride, format, pointer);
					bitmap.Save(ms, ImageFormat.Png);
					ms.Position = 0;
					_image.StreamSource = ms;
					_image.EndInit();

				}
				finally
				{
					pinnedArray.Free();
				}
				_rectangle.Fill = new ImageBrush(_image);
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
			var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
			var scaledBitmap = new System.Drawing.Bitmap((int)width, (int)height);
			var graph = System.Drawing.Graphics.FromImage(scaledBitmap);

			graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
			graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			//graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			graph.FillRectangle(brush, new System.Drawing.RectangleF(0, 0, (float)width, (float)height));
			graph.DrawImage(originalBitmap, 0, 0, width, height);
			_scaledBitmap = scaledBitmap;

			_targets.Clear();
			for (int x = 0; x < (int)width; x++)
			{
				for (int y = 0; y < (int)height; y++)
				{
					var pixel = scaledBitmap.GetPixel(x, y);
					var value = (0xff - pixel.R + 0xff - pixel.G + 0xff - pixel.B) / 3d;
					value *= pixel.A / 0xff;
					var target = new BurnTarget
					{
						FillRatio = value / 0xff,
						X = x,
						Y = y
					};
					_targets.Add(target);
				}
			}
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

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion

		private class BurnTarget
		{
			private byte _fillRatio;

			public BurnTarget()
			{
				_fillRatio = 0xff;
			}

			public int X { get; set; }
			public int Y { get; set; }

			public double FillRatio
			{
				get => _fillRatio / (double)0xff;
				set
				{
					value = value < 0 ? 0 : value > 1 ? 1 : value;
					_fillRatio = (byte)(value * 0xff);
				}
			}
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
