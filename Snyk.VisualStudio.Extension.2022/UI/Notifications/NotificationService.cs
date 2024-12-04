using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.UI.Notifications
{
    /// <summary>
    /// Snyk VS notification service.
    /// </summary>
    public class NotificationService
    {
        private const int MaxMsgLength = 300;
        private VsInfoBarService infoBarService;

        private NotificationService(ISnykServiceProvider serviceProvider) => this.infoBarService = new VsInfoBarService(serviceProvider);

        /// <summary>
        /// Gets return single instance of <see cref="NotificationService"/>.
        /// </summary>
        public static NotificationService Instance { get; private set; }

        public static void Initialize(ISnykServiceProvider serviceProvider) => Instance = new NotificationService(serviceProvider);

        /// <summary>
        /// Show error info bar with provided message.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void ShowErrorInfoBar(string message)
        {
            if (message.IsNullOrEmpty()) // Calling ShowErrorInfoBar with empty message will cause exception.
            {
                message = "Unknown error";
            }
            else if (message.Length > MaxMsgLength)
            {
                message = message.Substring(0, MaxMsgLength) + "...";
            }

            this.infoBarService.ShowErrorInfoBar(message);
        }

        /// <summary>
        /// Show update info bar with provided message.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void ShowInformationInfoBar(string message)
        {
            if (message.IsNullOrEmpty()) // Calling ShowErrorInfoBar with empty message will cause exception.
            {
                return;
            }
            
            if (message.Length > MaxMsgLength)
            {
                message = message.Substring(0, MaxMsgLength) + "...";
            }

            this.infoBarService.ShowInformationInfoBar(message);
        }
    }
}
