using System.Runtime.Serialization;

namespace Snyk.VisualStudio.Extension.CLI
{
    [DataContract]
    public class LatestReleaseInfo
    {
        [DataMember]
        private string url;

        [DataMember]
        private int id;

        [DataMember(Name = "tag_name")]
        private string tagName;

        [DataMember]
        private string name;

        public string Url
        {
            get
            {
                return url;
            }

            set
            {
                url = value;
            }
        }

        public int Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        public string TagName
        {
            get
            {
                return tagName;
            }

            set
            {
                tagName = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
    }
}
