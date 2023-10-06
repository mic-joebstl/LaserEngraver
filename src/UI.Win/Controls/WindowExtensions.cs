using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace LaserEngraver.UI.Win.Controls
{
	public static class WindowExtensions
	{
		public static decimal GetDpiRatio(this Window window)
		{
			var dpi = GetDpi(window, DpiType.Effective);
			decimal ratio = 1;
			if (dpi > 96)
				ratio = (decimal)dpi / 96M;

			return ratio;
		}

		public static uint GetDpi(this Window window, DpiType dpiType)
		{
			var hwnd = new WindowInteropHelper(window).Handle;
			return GetDpi(hwnd, dpiType);
		}

		private static uint GetDpi(IntPtr hwnd, DpiType dpiType)
		{
			var screen = Screen.FromHandle(hwnd);
			var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
			var mon = MonitorFromPoint(pnt, 2 /*MONITOR_DEFAULTTONEAREST*/);

			Debug.WriteLine("monitor handle: " + mon);
			try
			{
				uint dpiX, dpiY;
				GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
				return dpiX;
			}
			catch
			{
				// fallback for Windows 7 and older - not 100% reliable
				Graphics graphics = Graphics.FromHwnd(hwnd);
				float dpiXX = graphics.DpiX;
				return Convert.ToUInt32(dpiXX);
			}
		}


		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

		[DllImport("Shcore.dll")]
		private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

		public enum DpiType
		{
			Effective = 0,
			Angular = 1,
			Raw = 2,
		}
	}
}
