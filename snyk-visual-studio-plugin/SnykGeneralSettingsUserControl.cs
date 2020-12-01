using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Net;

namespace snyk_visual_studio_plugin
{
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

    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        internal SnykGeneralOptionsDialogPage optionsDialogPage;

        public SnykGeneralSettingsUserControl()
        {
            InitializeComponent();
        }
        
        public void Initialize()
        {
            tokenTextBox.Text = optionsDialogPage.ApiToken;
            customEndpointTextBox.Text = optionsDialogPage.CustomEndpoint;
            organizationTextBox.Text = optionsDialogPage.Organization;
            ignoreUnknownCACheckBox.Checked = optionsDialogPage.IgnoreUnknownCA;
        }
        
        private void authenticateButton_Click(object sender, EventArgs e)
        {
            using (var webClient = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                webClient.Headers.Add("user-agent", "VisualStudioSnykExtension");

                string endpointUri = "http://snyk.io";
                Guid newToken = Guid.NewGuid();

                string token = newToken.ToString();

                string loginUri = endpointUri + "/login?token=" + token + "&from=VisualStudioSnykExtension";

                System.Diagnostics.Process.Start(loginUri);

                var httpWebRequest = (HttpWebRequest) WebRequest.Create("https://snyk.io/api/v1/verify/callback");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = GetSnykVerifyRequestJson(token);

                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                string jsonResponseStr = null;

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    jsonResponseStr = streamReader.ReadToEnd();
                }
                
                SnykVerifyResponse verifyResponse = GetSnykVerifyResponse(jsonResponseStr);

                if (verifyResponse.isSuccessful)
                {
                    tokenTextBox.Text = verifyResponse.token;
                } else
                {
                    MessageBox.Show("Error");
                }
            }
        }

        private void tokenTextBox_TextChanged(object sender, EventArgs e)
        {
            optionsDialogPage.ApiToken = tokenTextBox.Text;
        }

        private void customEndpointTextBox_TextChanged(object sender, EventArgs e)
        {
            optionsDialogPage.CustomEndpoint = customEndpointTextBox.Text;
        }

        private void organizationTextBox_TextChanged(object sender, EventArgs e)
        {
            optionsDialogPage.Organization = organizationTextBox.Text;
        }

        private void ignoreUnknownCACheckBox_CheckedChanged(object sender, EventArgs e)
        {
            optionsDialogPage.IgnoreUnknownCA = ignoreUnknownCACheckBox.Checked;
        }

        private string GetSnykVerifyRequestJson(string token)
        {
            SnykVerifyRequest snykVerifyRequest = new SnykVerifyRequest();
            snykVerifyRequest.token = token;

            var memoryStream = new MemoryStream();
            var jsonSerializer = new DataContractJsonSerializer(typeof(SnykVerifyRequest));
            jsonSerializer.WriteObject(memoryStream, snykVerifyRequest);
            memoryStream.Position = 0;

            var streamReader = new StreamReader(memoryStream);
            
            return streamReader.ReadToEnd();
        }

        private SnykVerifyResponse GetSnykVerifyResponse(string json)
        {
            var verifyResponse = new SnykVerifyResponse();
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var jsonSerializer = new DataContractJsonSerializer(verifyResponse.GetType());

            verifyResponse = jsonSerializer.ReadObject(memoryStream) as SnykVerifyResponse;

            memoryStream.Close();

            return verifyResponse;
        }
    }  
}
