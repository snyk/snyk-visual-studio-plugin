using System;

namespace Snyk.Common.Authentication
{
    public delegate string TokenRefresher();

    /// <summary>
    /// Util for AuthenticationToken strings.
    /// </summary>
    public class AuthenticationToken
    {
        public static readonly AuthenticationToken EmptyToken = new(AuthenticationType.Token, string.Empty);

        public TokenRefresher TokenRefresher;

        private string value;

        public AuthenticationToken(AuthenticationType type, string value)
        {
            this.value = value;
            Type = type;
            this.TokenRefresher = null;
        }

        public AuthenticationType Type { get; }

        public override string ToString()
        {
            // if possible and required, update the token before using it
            if (this.TokenRefresher != null && Type == AuthenticationType.OAuth)
            {
                var oauthToken = OAuthToken.FromJson(this.value);
                if (oauthToken != null)
                {
                    var expired = oauthToken.IsExpired();
                    if (expired)
                    {
                        this.value = this.TokenRefresher();
                    }
                }
            }

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
                case AuthenticationType.OAuth:
                    {
                        var tokenState = GetTokenState(this.value);
                        if (tokenState == OAuthTokenState.Expired)
                        {
                            this.value = this.TokenRefresher();
                            tokenState = GetTokenState(this.value);
                        }

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