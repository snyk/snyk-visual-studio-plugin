namespace Snyk.Code.Library.Api.Encoding
{
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Snyk Code API requests encoder.
    /// </summary>
    public class Encoder
    {
        /// <summary>
        /// Encodes payload to base64 and encrypts using Gzip compression.
        /// </summary>
        /// <param name="payload">content payload to be encoded</param>
        /// <returns>Encoded stream</returns>
        public static async Task<MemoryStream> EncodeAndCompressAsync(string payload)
        {
            var destinationPayload = new MemoryStream();

            using (var originalPayload = new MemoryStream(Encoding.UTF8.GetBytes(payload)))
            using (var compressor = new GZipStream(destinationPayload, CompressionMode.Compress, true))
            using (var base64Stream = new CryptoStream(compressor, new ToBase64Transform(), CryptoStreamMode.Write))
            {
                await originalPayload.CopyToAsync(base64Stream);
            }

            return destinationPayload;
        }
    }
}
