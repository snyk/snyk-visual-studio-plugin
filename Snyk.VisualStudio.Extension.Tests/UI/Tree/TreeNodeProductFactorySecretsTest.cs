using Snyk.VisualStudio.Extension.UI.Tree;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Tree
{
    public class TreeNodeProductFactorySecretsTest
    {
        [Fact]
        public void GetIssueTreeNode_ReturnsSecretsTreeNode_ForSecretsProduct()
        {
            // Arrange
            var parent = new InfoTreeNode();

            // Act
            var node = TreeNodeProductFactory.GetIssueTreeNode(Product.Secrets, parent);

            // Assert
            Assert.IsType<SecretsTreeNode>(node);
        }

        [Fact]
        public void GetFileTreeNode_ReturnsSecretsFileTreeNode_ForSecretsProduct()
        {
            // Arrange
            var parent = new InfoTreeNode();

            // Act
            var node = TreeNodeProductFactory.GetFileTreeNode(Product.Secrets, parent);

            // Assert
            Assert.IsType<SecretsFileTreeNode>(node);
        }
    }
}
