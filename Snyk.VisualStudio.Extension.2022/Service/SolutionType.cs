namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// Solution type enum.
    /// </summary>
    public enum SolutionType
    {
        /// <summary>
        /// No open solution.
        /// </summary>
        NoOpenSolution,

        /// <summary>
        /// Solution with projects.
        /// </summary>
        Solution,

        /// <summary>
        /// Project.
        /// </summary>
        Project,

        /// <summary>
        /// Folder.
        /// </summary>
        Folder,
    }
}
