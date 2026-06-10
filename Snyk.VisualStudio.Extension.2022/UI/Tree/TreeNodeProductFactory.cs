using System;

namespace Snyk.VisualStudio.Extension.UI.Tree
{
    public static class TreeNodeProductFactory
    {
        public static IssueTreeNode GetIssueTreeNode(string product, TreeNode parent)
        {
            return product switch
            {
                Product.Code => new CodeTreeNode(parent),
                Product.Oss => new OssTreeNode(parent),
                Product.Iac => new IacTreeNode(parent),
                Product.Secrets => new SecretsTreeNode(parent),
                _ => throw new NotImplementedException()
            };
        }
        public static FileTreeNode GetFileTreeNode(string product, TreeNode parent)
        {
            return product switch
            {
                Product.Code => new CodeFileTreeNode(parent),
                Product.Oss => new OssFileTreeNode(parent),
                Product.Iac => new IacFileTreeNode(parent),
                Product.Secrets => new SecretsFileTreeNode(parent),
                _ => throw new NotImplementedException()
            };
        }
    }
}
