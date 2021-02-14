using System.Runtime.Serialization;

namespace Snyk.VisualStudio.Extension.CLI
{
    [DataContract]
    public class CliError
    {
        [DataMember(Name = "ok")]
        private bool isSuccess;
        
        [DataMember(Name = "error")]
        private string message;

        [DataMember]
        private string path;

        public CliError() { }

        public CliError(string message)
        {
            this.Message = message;
        }

        public bool IsSuccess
        {
            get
            {
                return isSuccess;
            }

            set
            {
                isSuccess = value;
            }
        }

        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
            }
        }

        public string Path
        {
            get
            {
                return path;
            }

            set
            {
                path = value;
            }
        }
    }
}
