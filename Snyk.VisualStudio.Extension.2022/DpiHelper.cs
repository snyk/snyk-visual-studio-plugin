using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;

namespace Snyk.VisualStudio.Extension
{

    public static class DpiHelper
    {
        private const int DPI_HOSTING_BEHAVIOR_INVALID = -1;
        private const int DPI_HOSTING_BEHAVIOR_DEFAULT = 0;
        private const int DPI_HOSTING_BEHAVIOR_MIXED = 1;

        [DllImport("user32.dll")]
        private static extern int SetThreadDpiHostingBehavior(int value);

        public static void ShowDpiAwareDialog(Func<Window> createDialog)
        {
            // Save current behavior
            int old = SetThreadDpiHostingBehavior(DPI_HOSTING_BEHAVIOR_INVALID);

            try
            {
                // Allow mixed contexts so WebBrowser can be per‑monitor aware
                SetThreadDpiHostingBehavior(DPI_HOSTING_BEHAVIOR_MIXED);

                var dlg = createDialog();      // construct DialogWindow here
                dlg.ShowDialog();              // or Show()
            }
            finally
            {
                // Restore previous behavior
                if (old != DPI_HOSTING_BEHAVIOR_INVALID)
                    SetThreadDpiHostingBehavior(old);
            }
        }
    }

}
