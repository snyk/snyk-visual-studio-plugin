using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Services
{
    public class SnykVsThemeService
    {
        public event EventHandler<SnykVsThemeChangedEventArgs> ThemeChanged;

        private readonly ISnykServiceProvider serviceProvider;
        private bool advised;

        public SnykVsThemeService(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            IVsShell vsShell = await serviceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;

            try
            {
                uint cookie = 0;

                int result = vsShell.AdviseBroadcastMessages(new VsBroadcastMessageEvents(this), out cookie);

                bool advised = (result == VSConstants.S_OK);
            }
            catch (COMException comException)
            {
                serviceProvider.ActivityLogger.LogError(comException.Message);
            }
            catch (InvalidComObjectException comObjectException)
            {
                serviceProvider.ActivityLogger.LogError(comObjectException.Message);
            }
        }

        public void OnThemeChanged() => ThemeChanged?.Invoke(this, new SnykVsThemeChangedEventArgs());
    }

    class VsBroadcastMessageEvents : IVsBroadcastMessageEvents
    {
        private const uint WM_SYSCOLORCHANGE = 0x15;

        private SnykVsThemeService vsThemeService;

        public VsBroadcastMessageEvents(SnykVsThemeService service)
        {
            this.vsThemeService = service;
        }

        public int OnBroadcastMessage(uint messageCode, IntPtr wParam, IntPtr lParam)
        {           
            if (WM_SYSCOLORCHANGE == messageCode)
            {
                vsThemeService.OnThemeChanged();
            }

            return VSConstants.S_OK;
        }
    }

    public class SnykVsThemeChangedEventArgs : EventArgs { }
}
