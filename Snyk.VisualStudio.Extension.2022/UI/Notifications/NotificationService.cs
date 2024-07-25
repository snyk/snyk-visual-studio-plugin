namespace Snyk.VisualStudio.Extension.UI.Notifications
{
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Snyk VS notification service.
    /// </summary>
    public class NotificationService
    {
        private const int MaxErrorLength = 300;
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
            else if (message.Length > MaxErrorLength)
            {
                message = message.Substring(0, MaxErrorLength) + "...";
            }

            this.infoBarService.ShowErrorInfoBar(message);
        }
    }
}
