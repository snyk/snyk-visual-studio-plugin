namespace Snyk.VisualStudio.Extension.UI
{
    public interface ISnykProgressBarManager
    {
        void UpdateProgressBar(int value);

        void HideProgressBar();

        void ShowProgressBar();

        void ShowProgressBar(string title);

        void ShowIndeterminateProgressBar(string title);

        void SetProgressBarTitle(string title);

        void HideAllControls(); // TODO: This method must be placed not here or interface must be renamed...
    }
}
