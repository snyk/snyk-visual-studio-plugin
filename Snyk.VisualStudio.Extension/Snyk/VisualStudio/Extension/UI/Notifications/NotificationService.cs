namespace Snyk.VisualStudio.Extension.UI.Notifications
{
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Snyk VS notification service.
    /// </summary>
    public class NotificationService
    {
        private VsInfoBarService infoBarService;

        private NotificationService(ISnykServiceProvider serviceProvider) => this.infoBarService = new VsInfoBarService(serviceProvider);

        /// <summary>
        /// Gets return single instance of <see cref="NotificationService"/>.
        /// </summary>
        public static NotificationService Instance { get; private set; }

        public static void Initialize(ISnykServiceProvider serviceProvider) => Instance = new NotificationService(serviceProvider);

        /// <summary>
        /// Show warning info bar with provided message.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void ShowWarningInfoBar(string message) => this.infoBarService.ShowWarningInfoBar(message);
    }
}
