namespace Snyk.VisualStudio.Extension.Shared.Cache
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Shell.Interop;
    using Toolkit = Community.VisualStudio.Toolkit;

    /// <summary>
    /// Impleemnts <see cref="IVsHierarchyEvents"/> for SnykCode cache.
    /// </summary>
    public abstract class AbstractCacheHierarchyEvents
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCacheHierarchyEvents"/> class.
        /// </summary>
        public AbstractCacheHierarchyEvents()
        {
            Toolkit.VS.Events.DocumentEvents.Saved += this.OnFileSaved;

            Toolkit.VS.Events.ProjectItemsEvents.AfterAddProjectItems += this.OnFileAdd;
            Toolkit.VS.Events.ProjectItemsEvents.AfterRemoveProjectItems += this.OnFileRemove;
            Toolkit.VS.Events.ProjectItemsEvents.AfterRenameProjectItems += this.OnFileRename;
        }

        /// <summary>
        /// Handle if file content (text) changed.
        /// </summary>
        /// <param name="file">Full path to file.</param>
        protected abstract void OnFileSaved(string file);

        /// <summary>
        /// Handle file rename (delete or add).
        /// </summary>
        /// <param name="eventArgs">AfterRenameProjectItem event args.</param>
        protected abstract void OnFileRename(Toolkit.AfterRenameProjectItemEventArgs eventArgs);

        /// <summary>
        /// Handle file deleted event.
        /// </summary>
        /// <param name="eventArgs">AfterRemoveProjectItem event args.</param>
        protected abstract void OnFileRemove(Toolkit.AfterRemoveProjectItemEventArgs eventArgs);

        /// <summary>
        /// Handle file add event.
        /// </summary>
        /// <param name="solutionItems">Solution items list..</param>
        protected abstract void OnFileAdd(IEnumerable<Toolkit.SolutionItem> solutionItems);
    }
}
