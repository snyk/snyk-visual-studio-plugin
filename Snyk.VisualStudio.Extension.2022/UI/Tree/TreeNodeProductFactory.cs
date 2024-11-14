using System;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public static class TreeNodeProductFactory
    {
        public static IssueTreeNode GetIssueTreeNode(string product)
        {
            return product switch
            {
                Product.Code => new CodeTreeNode(),
                Product.Oss => new OssTreeNode(),
                Product.Iac => new IacTreeNode(),
                _ => throw new NotImplementedException()
            };
        }
        public static FileTreeNode GetFileTreeNode(string product)
        {
            return product switch
            {
                Product.Code => new CodeFileTreeNode(),
                Product.Oss => new OssFileTreeNode(),
                Product.Iac => new IacFileTreeNode(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
