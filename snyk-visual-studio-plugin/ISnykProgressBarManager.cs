namespace Snyk.VisualStudio.Extension.UI
{
    public interface ISnykProgressBarManager
    {
        void Update(int value);

        void Hide();

        void Show();

        void Show(string title);

        void SetTitle(string title);
    }
}
