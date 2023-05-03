namespace Snyk.Common.Authentication;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OAuthToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("expiry")]
    public string Expiry { get; set; }

    public static OAuthToken FromJson(string token)
    {
        OAuthToken result = null;

        try
        {
            result = JsonSerializer.Deserialize<OAuthToken>(token);
        }
        catch (Exception ex)
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