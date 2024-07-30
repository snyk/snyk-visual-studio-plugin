using System.ComponentModel;

namespace Snyk.Common.Authentication
{
    public enum AuthenticationType
    {
        [Description("Token Authentication")]
        Token,
        [Description("OAuth2 Authentication")]
        OAuth,
    }
}
