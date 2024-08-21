using System.ComponentModel;

namespace Snyk.Common.Authentication
{
    public enum AuthenticationType
    {
        [Description("Token authentication")]
        Token,
        [Description("OAuth2 authentication")]
        OAuth,
    }
}
