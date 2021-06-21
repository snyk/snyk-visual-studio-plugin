namespace Snyk.Code.Library.Common
{
    using System.Text.Json;

    /// <summary>
    /// Json util for serialize and deserialize objects with Json serialization parameters.
    /// </summary>
    public static class Json
    {
        public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, SerializerOptions);

        public static string Serialize<T>(T sourceObj) => JsonSerializer.Serialize<T>(sourceObj, SerializerOptions);

        private static JsonSerializerOptions SerializerOptions => new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
}
