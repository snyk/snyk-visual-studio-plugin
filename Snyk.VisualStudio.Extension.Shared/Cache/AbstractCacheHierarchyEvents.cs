namespace Snyk.VisualStudio.Extension.Shared.Cache
{
    using System;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.Common;

    /// <summary>
    /// Impleemnts <see cref="IVsHierarchyEvents"/> for SnykCode cache.
    /// </summary>
    public abstract class AbstractCacheHierarchyEvents : IVsHierarchyEvents
    {
        protected IVsHierarchy vsHierarchy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCacheHierarchyEvents"/> class.
        /// </summary>
        /// <param name="vsHierarchy"><see cref="IVsHierarchy"/> instance.</param>
        public AbstractCacheHierarchyEvents(IVsHierarchy vsHierarchy) => this.vsHierarchy = vsHierarchy;

        /// <summary>
        /// Handle file add event.
        /// </summary>
        /// <param name="filePath">Full paht to file.</param>
        public abstract void OnFileAdded(string filePath);

        /// <summary>
        /// Handle file deleted event.
        /// </summary>
        /// <param name="filePath">Full paht to file.</param>
        public abstract void OnFileDeleted(string filePath);

        /// <summary>
        /// Handle file changed event.
        /// </summary>
        /// <param name="filePath">Full paht to file.</param>
        public abstract void OnFileChanged(string filePath);

        /// <summary>
        /// Add file to <see cref="IFileProvider"/> as new file.
        /// </summary>
        /// <param name="itemidParent">Identifier of the parent, or root node of the hierarchy in which the item is added.</param>
        /// <param name="itemidSiblingPrev">Identifier that indicates where the item is added in relation to other items (siblings) within the parent hierarchy (itemidParent).</param>
        /// <param name="itemidAdded">Identifier of the added item.</param>
        /// <returns><see cref="VSConstants"/> S_OK.</returns>
        public int OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
        {
            this.OnFileAdded(this.GetFilePath(itemidAdded));

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies clients when items are appended to the end of the hierarchy.
        /// </summary>
        /// <param name="itemidParent">Identifier of the parent or root node of the hierarchy to which the item is appended.</param>
        /// <returns><see cref="VSConstants"/> S_OK.</returns>
        public int OnItemsAppended(uint itemidParent) => VSConstants.S_OK;

        /// <summary>
        /// Add file to <see cref="IFileProvider"/> as deleted.
        /// </summary>
        /// <param name="itemid">Identifier of the deleted item. This is the same identifier assigned to the new item by the hierarchy when it is added to the hierarchy.</param>
        /// <returns><see cref="VSConstants"/> S_OK.</returns>
        public int OnItemDeleted(uint itemid)
        {
            this.OnFileDeleted(this.GetFilePath(itemid));

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Add file to <see cref="IFileProvider"/> as changed.
        /// </summary>
        /// <param name="itemid">Identifier of the item whose property has changed. For a list of itemid values, see VSITEMID.</param>
        /// <param name="propid">Identifier of the property of itemid. For a list of propid values, see __VSHPROPID.</param>
        /// <param name="flags">Not implemented.</param>
        /// <returns><see cref="VSConstants"/> S_OK.</returns>
        public int OnPropertyChanged(uint itemid, int propid, uint flags)
        {
            this.OnFileChanged(this.GetFilePath(itemid));

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies clients when changes are made to the item inventory of a hierarchy.
        /// </summary>
        /// <param name="itemidParent">Parent item identifier, or root, of the hierarchy whose item inventory has changed.</param>
        /// <returns><see cref="VSConstants"/> S_OK.</returns>
        public int OnInvalidateItems(uint itemidParent) => VSConstants.S_OK;

        /// <summary>
        /// Notifies clients when changes are made to icons.
        /// </summary>
        /// <param name="hicon">Icon handle.</param>
        /// <returns><see cref="VSConstants"/> S_OK.</returns>
        public int OnInvalidateIcon(IntPtr hicon) => VSConstants.S_OK;

        /// <summary>
        /// Get file paht by item id.
        /// </summary>
        /// <param name="itemid">Item id.</param>
        /// <returns>Full path to solution file.</returns>
        protected string GetFilePath(uint itemid)
        {
            object itemAddedExtObject;

            if (this.vsHierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out itemAddedExtObject) == VSConstants.S_OK)
            {
                var projectItem = itemAddedExtObject as ProjectItem;
                if (projectItem != null)
                {
                    return projectItem.FileNames[1];
                }
            }

            return null;
        }
    }
}
