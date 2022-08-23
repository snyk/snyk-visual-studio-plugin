namespace Snyk.Code.Library.Api.Encoding
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;

    /// <summary>
    /// Snyk Code API requests encoder.
    /// </summary>
    public class Encoder
    {
        private static readonly ILogger Logger = LogManager.ForContext<Encoder>();

        /// <summary>
        /// Encodes payload to base64 and encrypts using Gzip compression.
        /// </summary>
        /// <param name="payload">content payload to be encoded</param>
        /// <returns>Encoded stream</returns>
        public static async Task<MemoryStream> EncodeAndCompressAsync(string payload)
        {
            try
            {
                var destinationPayload = new MemoryStream();

                using (var originalPayload = new MemoryStream(Encoding.UTF8.GetBytes(payload)))
                using (var compressor = new GZipStream(destinationPayload, CompressionMode.Compress, true))
                using (var base64Stream = new CryptoStream(compressor, new ToBase64Transform(), CryptoStreamMode.Write))
                {
                    await Task.Run(() => originalPayload.CopyTo(base64Stream));

                    // Some rare errors about multiple async readers might be related to the next line.
                    // Removing it for now and using the blocking copy wrapped in a Task.Run()
                    // await originalPayload.CopyToAsync(base64Stream);
                }

                return destinationPayload;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error when trying to compress and encrypt payload");
                throw;
            }
        }
    }
}
