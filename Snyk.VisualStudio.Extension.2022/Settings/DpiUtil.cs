using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Snyk.VisualStudio.Extension.Settings
{
    public static class DpiUtil
    {
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        const int LOGPIXELSX = 88;

        public static double GetSystemScale()
        {
            var hdc = GetDC(IntPtr.Zero);
            try
            {
                int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                return dpiX / 96.0; // 1.25 for 120 DPI, 1.5 for 144 DPI, etc.
            }
            finally { ReleaseDC(IntPtr.Zero, hdc); }
        }
    }
}
