using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
namespace Snyk.Common.Authentication
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class OAuthToken
    {
        public string AccessToken { get; set; }

        public string TokenType { get; set; }

        public string RefreshToken { get; set; }

        public string Expiry { get; set; }

        public static OAuthToken FromJson(string token)
        {
            OAuthToken result = null;

            try
            {
                result = JsonConvert.DeserializeObject<OAuthToken>(token);
            }
            catch (Exception)
            {
                // nothing
            }

            return result;
        }

        public bool IsExpired()
        {
            var expiryDate = DateTime.Parse(Expiry).ToUniversalTime();
            expiryDate = expiryDate.AddSeconds(10);
            return expiryDate < DateTime.UtcNow;
        }
    }
}