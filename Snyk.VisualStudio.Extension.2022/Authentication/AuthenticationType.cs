using System.ComponentModel;

namespace Snyk.VisualStudio.Extension.Authentication
{
    public enum AuthenticationType
    {
        [Description("OAuth2 authentication")]
        OAuth,
        [Description("Token authentication")]
        Token,
    }
}
