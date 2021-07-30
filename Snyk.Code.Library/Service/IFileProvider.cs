namespace Snyk.Code.Library.Service
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provide file path and content for solutions and projects.
    /// </summary>
    public interface IFileProvider
    {
        IDictionary<string, string> CreaateFileHashToContentDictionary(IList<string> files);

        Task InitializeAsync();

        IDictionary<string, string> CreateFilePathToHashDictionary();
    }
}
