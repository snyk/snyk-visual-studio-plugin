namespace Snyk.VisualStudio.Extension.UI
{
    public interface ISnykProgressBarManager
    {
        void Update(int value);

        void Hide();

        void Show();

        void Show(string title);

        void ShowIndeterminate(string title);

        void SetTitle(string title);

        void HideAll();
    }
}
