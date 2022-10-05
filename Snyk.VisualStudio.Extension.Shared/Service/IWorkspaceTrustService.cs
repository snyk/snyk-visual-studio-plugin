namespace Snyk.VisualStudio.Extension.Shared.Service
{
    public interface IWorkspaceTrustService
    {
        bool IsFolderTrusted(string absoluteFolderPath);

        void AddFolderToTrusted(string absoluteFolderPath);
    }
}
