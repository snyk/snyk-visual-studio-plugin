using Snyk.VisualStudio.Extension.Util;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

namespace Snyk.VisualStudio.Extension
{   
    public class SnykAuthenticationService
    {
        private const string LoginUrl = "http://snyk.io/login?token={0}&from=VisualStudioSnykExtension";

        private const string VerifyApiTokenUrl = "https://snyk.io/api/v1/verify/callback";

        public string RequestApiToken()
        {
            string apiToken = GenerateNewApiToken();

            OpenAuthenticationPageInBrowser(apiToken);

            var verifyResponse = SendApiTokenVerifyRequest(apiToken);
           
            if (verifyResponse.isSuccessful)
            {
                return verifyResponse.token;
            }
            else
            {
                throw new AuthenticationException("Can't authenticate Snyk CLI and get API token.");
            }
        }

        public static SnykAuthenticationService NewInstance() => new SnykAuthenticationService();

        private SnykVerifyResponse SendApiTokenVerifyRequest(string apiToken)
        {            
            var httpWebResponse = SendVerifyPostRequest(apiToken);                        

            return GetVerifyResponse(httpWebResponse);
        }

        private HttpWebResponse SendVerifyPostRequest(string apiToken)
        {
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(VerifyApiTokenUrl);

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                SnykVerifyRequest snykVerifyRequest = new SnykVerifyRequest()
                {
                    token = apiToken
                };

                string json = Json.Serialize(snykVerifyRequest);

                streamWriter.Write(json);
            }

            return (HttpWebResponse) httpWebRequest.GetResponse();
        }

        private SnykVerifyResponse GetVerifyResponse(HttpWebResponse httpWebResponse)
        {
            using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                string responseJson = streamReader.ReadToEnd();

                return (SnykVerifyResponse)Json.Deserialize(responseJson, typeof(SnykVerifyResponse));
            }
        }

        private string GenerateNewApiToken() => Guid.NewGuid().ToString();

        private void OpenAuthenticationPageInBrowser(string apiToken) => System.Diagnostics.Process.Start(String.Format(LoginUrl, apiToken));                

        public class AuthenticationException : Exception
        {
            public AuthenticationException(string message) : base(message) { }
        }

        [DataContract]
        internal class SnykVerifyRequest
        {
            [DataMember]
            internal string token;
        }

        [DataContract]
        internal class SnykVerifyResponse
        {
            [DataMember(Name = "ok")]
            internal bool isSuccessful;

            [DataMember(Name = "api")]
            internal string token;
        }
    }
}
