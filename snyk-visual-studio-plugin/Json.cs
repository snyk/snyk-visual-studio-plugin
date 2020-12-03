using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Snyk.VisualStudio.Extension.Util
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
    }
}
