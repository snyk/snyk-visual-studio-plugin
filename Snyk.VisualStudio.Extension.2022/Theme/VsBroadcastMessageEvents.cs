namespace Snyk.VisualStudio.Extension.Shared.Theme
{
    using System;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Snyk implementation of IVsBroadcastMessageEvents.
    /// </summary>
    public class VsBroadcastMessageEvents : IVsBroadcastMessageEvents
    {
        private const uint WmSystemColorChange = 0x15;

        private readonly SnykVsThemeService vsThemeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsBroadcastMessageEvents"/> class.
        /// </summary>
        /// <param name="service">Snyk Visual Studio theme service</param>
        public VsBroadcastMessageEvents(SnykVsThemeService service) => this.vsThemeService = service;

        /// <summary>
        /// Handle broadcast theme message.
        /// </summary>
        /// <param name="messageCode">Message code.</param>
        /// <param name="wParam">wParam.</param>
        /// <param name="lParam">lParam.</param>
        /// <returns>VSConstants.S_OK.</returns>
        public int OnBroadcastMessage(uint messageCode, IntPtr wParam, IntPtr lParam)
        {
            if (messageCode == WmSystemColorChange)
            {
                this.vsThemeService.OnThemeChanged();
            }

            return VSConstants.S_OK;
        }
    }
}
