using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Snyk.VisualStudio.Extension.Service;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Theme
{
    public class SnykVsThemeService
    {
        public event EventHandler<SnykVsThemeChangedEventArgs> ThemeChanged;

        private readonly ISnykServiceProvider serviceProvider;        

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

    public class ThemeInfo
    {
        private readonly string _themeName;
        private readonly string _themeColor;
        private readonly string _themeSize;
        private readonly string _themeFileName;

        public ThemeInfo(string name, string fileName, string color, string size)
        {
            _themeName = name;
            _themeFileName = fileName;
            _themeColor = color;
            _themeSize = size;
        }

        public string ThemeFileName
        {
            get { return _themeFileName; }
        }

        public string ThemeName
        {
            get { return _themeName; }
        }

        public string ThemeColor
        {
            get { return _themeColor; }
        }

        public string ThemeSize
        {
            get { return _themeSize; }
        }

        public static ThemeInfo Current
        {
            get
            {
                var fileName = new StringBuilder(260);
                var color = new StringBuilder(260);
                var size = new StringBuilder(260);
                int hResult = GetCurrentThemeName(fileName, fileName.Capacity, color, color.Capacity, size, size.Capacity);
                
                if (hResult < 0)
                {
                    throw Marshal.GetExceptionForHR(hResult);
                }                    

                string themeName = Path.GetFileNameWithoutExtension(fileName.ToString());

                return new ThemeInfo(themeName, fileName.ToString(), color.ToString(), size.ToString());
            }
        }

        [DllImport("uxtheme", CharSet = CharSet.Auto)]
        private static extern int GetCurrentThemeName(
            StringBuilder pszThemeFileName, 
            int dwMaxNameChars, 
            StringBuilder pszColorBuff, 
            int cchMaxColorChars, 
            StringBuilder pszSizeBuff, 
            int cchMaxSizeChars);
    }
}
