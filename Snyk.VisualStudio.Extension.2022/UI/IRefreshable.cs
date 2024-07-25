namespace Snyk.VisualStudio.Extension.UI
{
    /// <summary>
    /// Common interface for call refresh on component.
    /// </summary>
    public interface IRefreshable
    {
        /// <summary>
        /// Refresh (rerender) component UI.
        /// </summary>
        void Refresh();
    }
}
