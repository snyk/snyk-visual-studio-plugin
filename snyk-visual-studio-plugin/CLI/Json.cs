using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Snyk.VisualStudio.Extension.CLI
{
    class Json
    {
        public static object Deserialize(string sourceJson, Type resultType)
        {
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceJson));
            var jsonSerializer = new DataContractJsonSerializer(resultType);

            var result = jsonSerializer.ReadObject(memoryStream);

            memoryStream.Close();

            return result;
        }

        public static string Serialize(object source)
        {
            var memoryStream = new MemoryStream();
            var jsonSerializer = new DataContractJsonSerializer(source.GetType());
            jsonSerializer.WriteObject(memoryStream, source);
            memoryStream.Position = 0;

            var streamReader = new StreamReader(memoryStream);

            return streamReader.ReadToEnd();
        }
    }
}
