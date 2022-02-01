namespace Snyk.VisualStudio.Extension.Shared.Cache
{
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.Common;

    /// <summary>
    /// Impleemnts <see cref="IVsHierarchyEvents"/> for SnykCode cache.
    /// </summary>
    public class CodeCacheHierarchyEvents : AbstractCacheHierarchyEvents
    {
        private IFileProvider fileProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeCacheHierarchyEvents"/> class.
        /// </summary>
        /// <param name="vsHierarchy">VS Hierarchy instance.</param>
        /// <param name="fileProvider">File provider instance.</param>
        public CodeCacheHierarchyEvents(IVsHierarchy vsHierarchy, IFileProvider fileProvider)
            : base(vsHierarchy) => this.fileProvider = fileProvider;

        /// <inheritdoc/>
        public override void OnFileAdded(string filePath)
            => this.fileProvider.AddChangedFile(filePath);

        /// <inheritdoc/>
        public override void OnFileDeleted(string filePath)
            => this.fileProvider.RemoveFile(filePath);

        /// <inheritdoc/>
        public override void OnFileChanged(string filePath)
            => this.fileProvider.AddChangedFile(filePath);
    }
}
