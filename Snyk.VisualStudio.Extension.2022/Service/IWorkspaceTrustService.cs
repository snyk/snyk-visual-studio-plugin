namespace Snyk.VisualStudio.Extension.Service
{
    public interface IWorkspaceTrustService
    {
        bool IsFolderTrusted(string absoluteFolderPath);

        void AddFolderToTrusted(string absoluteFolderPath);
    }
}
