using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace snyk_visual_studio_plugin
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
