namespace Snyk.Code.Library.Tests.SnykCode.Api
{
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Api.Encoding;
    using Xunit;

    public class EncoderTest
    {
        [Fact]
        public async Task Encoder_EncodeAndCompress_EncodesCorrectlyAsync()
        {
            var str = "Hello world!";

            byte[] encodedBytes = null;
            using (var encoder = await Encoder.EncodeAndCompressAsync(str))
            {
                encodedBytes = encoder.ToArray();
            }

            Assert.Equal(str, this.DecodeAndDecompress(encodedBytes));
        }

        private string DecodeAndDecompress(byte[] bytes)
        {
            using (var encodedPayload = new MemoryStream(bytes))
            using (var decodedPayload = new MemoryStream())
            using (var decompressor = new GZipStream(encodedPayload, CompressionMode.Decompress))
            using (var base64Stream = new CryptoStream(decodedPayload, new FromBase64Transform(), CryptoStreamMode.Write))
            {
                decompressor.CopyTo(base64Stream);
                return System.Text.Encoding.ASCII.GetString(decodedPayload.ToArray());
            }
        }
    }
}
