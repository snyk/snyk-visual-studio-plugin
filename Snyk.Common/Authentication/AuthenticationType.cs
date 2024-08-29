using System.ComponentModel;

namespace Snyk.Common.Authentication
{
    public enum AuthenticationType
    {
        [Description("OAuth2 authentication")]
        OAuth,
        [Description("Token authentication")]
        Token,
    }
}
