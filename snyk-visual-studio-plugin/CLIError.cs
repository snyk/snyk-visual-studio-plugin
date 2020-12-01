using System.Runtime.Serialization;

namespace Snyk.VisualStudio.Extension
{
    [DataContract]
    class CLIError
    {
        [DataMember(Name = "ok")]
        internal bool IsSuccess;
        
        [DataMember(Name = "error")]
        internal string Message;

        [DataMember]
        internal string Path;
    }
}
