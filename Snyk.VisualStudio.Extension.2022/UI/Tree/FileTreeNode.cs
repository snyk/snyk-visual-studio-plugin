using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Tree;

public class FileTreeNode : TreeNode 
{
    public virtual string FolderName { get; set; }
    public virtual string FileName { get; set; }
    public virtual IEnumerable<Issue> IssueList { get; set; }
}