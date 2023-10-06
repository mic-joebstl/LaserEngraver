using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LaserPathEngraver.UI.Win.Visuals
{
	public class BurnArea : IVisual
	{
		private Rectangle _rectangle;
		private BitmapImage? _image;
		private MemoryStream? _imageStream;
		private Point _position;
		private Size _size;

		public BurnArea()
		{
			_rectangle = new Rectangle();
		}

		public Point Position
		{
			get => _position;
			set
			{
				if (_position != value)
				{
					_position = value;
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
				}
			}
		}

		public Shape Shape => _rectangle;

		public void RenderImage()
		{
			_image = new BitmapImage();
			_imageStream?.Dispose();
			_image.BeginInit();
			var ms = _imageStream = new MemoryStream();

			var width = (int)Size.Width;
			var height = (int)Size.Height;
			var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
			int bitsPerPixel = ((int)format & 0xff00) >> 8;
			int bytesPerPixel = (bitsPerPixel + 7) / 8;
			int stride = 4 * ((width * bytesPerPixel + 3) / 4);
			var imageBuffer = new byte[width * height * bytesPerPixel];
			var randy = new Random();
			int i = 0;
			for (int r = 0; r < height; r++)
			{
				for (int c = 0; c < width; c++)
				{
					imageBuffer[i++] = 0xff;
					imageBuffer[i++] = 0xff;
					imageBuffer[i++] = 0xff;
					//imageBuffer[i++] = (byte)(1d / 800 * r * 0xff);
					imageBuffer[i++] = (byte)randy.Next(0, 0xff);
				}
			}
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

		private class BurnTarget
		{
			private byte _fillRatio;
			private Point _position;

			public BurnTarget()
			{
				_fillRatio = 0xff;
			}

			public Point Position
			{
				get => _position;
				set
				{
					if (_position != value)
					{
						_position = value;
					}
				}
			}

			public double FillRatio
			{
				get => _fillRatio / 0xff;
				set
				{
					value = value < 0 ? 0 : value > 1 ? 1 : value;
					_fillRatio = (byte)(value * 0xff);
				}
			}
		}
	}
}
