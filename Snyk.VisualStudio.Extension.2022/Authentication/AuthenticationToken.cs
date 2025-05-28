using System;

namespace Snyk.VisualStudio.Extension.Authentication
{
    /// <summary>
    /// Util for AuthenticationToken strings.
    /// </summary>
    public class AuthenticationToken
    {
        public static readonly AuthenticationToken EmptyToken = new(AuthenticationType.Token, string.Empty);

        private string value;

        public AuthenticationToken(AuthenticationType type, string value)
        {
            this.value = value;
            Type = type;
        }

        public AuthenticationType Type { get; }

        public override string ToString()
        {
            return this.value;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(this.value))
            {
                return false;
            }

            switch (Type)
            {
                case AuthenticationType.Token:
                    return Guid.TryParse(this.value, out _);
                case AuthenticationType.Pat:
                    // PAT tokens can be validated by a backend call, which is not necessary in this context.
                    return true;
                case AuthenticationType.OAuth:
                    {
                        var tokenState = GetTokenState(this.value);
                        return tokenState == OAuthTokenState.Valid;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static OAuthTokenState GetTokenState(string value)
        {
            var oauthToken = OAuthToken.FromJson(value);
            var expired = oauthToken?.IsExpired() == true;

            return (oauthToken, expired) switch
            {
                (null, _) => OAuthTokenState.Invalid,
                (_, true) => OAuthTokenState.Expired,
                (_, false) => OAuthTokenState.Valid
            };
        }

        private enum OAuthTokenState
        {
            Invalid,
            Expired,
            Valid
        }
    }
}