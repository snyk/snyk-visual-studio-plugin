﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Utils
{
    /// <summary>
    /// Json util for serialize and deserialize objects with Json serialization parameters.
    /// </summary>
    public static class Json
    {
        /// <summary>
        /// Gets a value indicating whether with serialization options.
        /// For example it enable Camel case by default.
        /// </summary>
        private static JsonSerializerSettings SerializerOptions => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        /// <summary>
        /// Deserialize json to object depending on type.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="json">Json string for deserialization.</param>
        /// <returns>Result object.</returns>
        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, SerializerOptions);

        /// <summary>
        /// Serialize object to json.
        /// </summary>
        /// <typeparam name="T">Source type.</typeparam>
        /// <param name="sourceObj">Source object to serialize.</param>
        /// <returns>Result string.</returns>
        public static string Serialize<T>(T sourceObj) => JsonConvert.SerializeObject(sourceObj, SerializerOptions);
    }
}
