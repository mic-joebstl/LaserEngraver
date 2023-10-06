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

		public BurnArea()
		{
			_rectangle = new Rectangle();
			_targets = new List<BurnTarget>();
			_renderSync = new SemaphoreSlim(1, 1);
		}

		public Point Position
		{
			get => _position;
			set
			{
				if (_position != value)
				{
					_position = value;
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
					RaisePropertyChanged(nameof(Size));
				}
			}
		}

		public Shape Shape => _rectangle;

		public void RenderImage()
		{
			_renderSync.Wait();
			try
			{
				_image = new BitmapImage();
				_imageStream?.Dispose();
				_image.BeginInit();
				var ms = _imageStream = new MemoryStream();

				if (_scaledBitmap is null || _originalBitmap is null)
				{
					_targets.Clear();
					Size = new Size(0, 0);
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
			}
			finally
			{
				_renderSync.Release();
			}
		}

		public void LoadBitmap(string filePath, Size maxSize)
		{
			_originalBitmap = new System.Drawing.Bitmap(filePath);
			var ratio = (double)_originalBitmap.Width / _originalBitmap.Height;
			var height = _originalBitmap.Height > maxSize.Height ? maxSize.Height : _originalBitmap.Height;
			var width = height * ratio;
			width = width > maxSize.Width ? maxSize.Width : width;
			height = width / ratio;
			Size = new Size(width, height);
			Position = new Point
			{
				X = (maxSize.Width - width) / 2,
				Y = (maxSize.Height - height) / 2
			};

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

			var width = (float)Size.Width;
			var height = (float)Size.Height;
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
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
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
	}
}
