using System.Runtime.Serialization;

namespace Snyk.VisualStudio.Extension.CLI
{
    [DataContract]
    class CliError
    {
        [DataMember(Name = "ok")]
        internal bool IsSuccess;
        
        [DataMember(Name = "error")]
        internal string Message;

        [DataMember]
        internal string Path;
    }
}
