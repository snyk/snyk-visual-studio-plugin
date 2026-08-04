namespace Snyk.VisualStudio.Extension.Service
{
    public interface IWorkspaceTrustService
    {
        void AddFolderToTrusted(string absoluteFolderPath);
    }
}
