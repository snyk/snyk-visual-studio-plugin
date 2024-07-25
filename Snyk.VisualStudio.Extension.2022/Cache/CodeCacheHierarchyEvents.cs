namespace Snyk.VisualStudio.Extension.Shared.Cache
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.Common;
    using Toolkit = Community.VisualStudio.Toolkit;

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
        public CodeCacheHierarchyEvents(IFileProvider fileProvider)
            : base()
        {
            this.fileProvider = fileProvider;
        }

        /// <inheritdoc/>
        protected override void OnFileSaved(string file) => this.fileProvider.AddChangedFile(file);

        /// <inheritdoc/>
        protected override void OnFileRename(Toolkit.AfterRenameProjectItemEventArgs eventArgs)
        {
            foreach (var item in eventArgs.ProjectItemRenames)
            {
                this.fileProvider.AddChangedFile(item.SolutionItem.FullPath);
            }
        }

        /// <inheritdoc/>
        protected override void OnFileRemove(Toolkit.AfterRemoveProjectItemEventArgs eventArgs)
        {
            foreach (var item in eventArgs.ProjectItemRemoves)
            {
                this.fileProvider.RemoveFile(item.RemovedItemName);
            }
        }

        /// <inheritdoc/>
        protected override void OnFileAdd(IEnumerable<Toolkit.SolutionItem> solutionItems)
        {
            foreach (var item in solutionItems)
            {
                this.fileProvider.AddChangedFile(item.FullPath);
            }
        }
    }
}
