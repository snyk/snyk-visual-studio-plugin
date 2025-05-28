using System.ComponentModel;

namespace Snyk.VisualStudio.Extension.Authentication
{
    public enum AuthenticationType
    {
        [Description("OAuth2 (Recommended)")]
        OAuth,
        [Description("Personal Access Token")]
        Pat,
        [Description("API Token (Legacy)")]
        Token,
    }
}
