namespace Snyk.VisualStudio.Extension.Theme
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Service;

    /// <summary>
    /// Add support for light and dark Visual Studio themes.
    /// </summary>
    public class SnykVsThemeService
    {
        /// <summary>
        /// Event handler for VS theme changed.
        /// </summary>
        public event EventHandler<SnykVsThemeChangedEventArgs> ThemeChanged;

        private readonly ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVsThemeService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider implementation.</param>
        public SnykVsThemeService(ISnykServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <summary>
        /// Initialize service async.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task InitializeAsync()
        {
            IVsShell vsShell = await this.serviceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;

            try
            {
                uint cookie = 0;

                int result = vsShell.AdviseBroadcastMessages(new VsBroadcastMessageEvents(this), out cookie);

                bool advised = (result == VSConstants.S_OK);
            }
            catch (COMException comException)
            {
                this.serviceProvider.ActivityLogger.LogError(comException.Message);
            }
            catch (InvalidComObjectException comObjectException)
            {
                this.serviceProvider.ActivityLogger.LogError(comObjectException.Message);
            }
        }

        /// <summary>
        /// Fire event if theme changed.
        /// </summary>
        public void OnThemeChanged() => this.ThemeChanged?.Invoke(this, new SnykVsThemeChangedEventArgs());
    }

    

    public class SnykVsThemeChangedEventArgs : EventArgs { }    
}
