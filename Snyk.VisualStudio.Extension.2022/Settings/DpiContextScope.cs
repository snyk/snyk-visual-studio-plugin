using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Snyk.VisualStudio.Extension.Settings
{
    internal static class DpiContextScope
    {
        // Win10+
        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        // Values from WinUser.h
        private static readonly IntPtr PER_MONITOR_AWARE_V2 = new IntPtr(-4);
        private static readonly IntPtr SYSTEM_AWARE = new IntPtr(-2);
        private static readonly IntPtr UNAWARE_GDISCALED = new IntPtr(-5);

        public static IDisposable EnterUnawareGdiScaled()
        {
            var old = SetThreadDpiAwarenessContext(UNAWARE_GDISCALED);
            return new Restore(old);
        }

        public static IDisposable EnterSystemAware()
        {
            var old = SetThreadDpiAwarenessContext(SYSTEM_AWARE);
            return new Restore(old);
        }

        public static IDisposable EnterPerMonitorV2()
        {
            var old = SetThreadDpiAwarenessContext(PER_MONITOR_AWARE_V2);
            return new Restore(old);
        }

        private sealed class Restore : IDisposable
        {
            private readonly IntPtr _old;
            public Restore(IntPtr old) => _old = old;
            public void Dispose() => SetThreadDpiAwarenessContext(_old);
        }
    }
}
