namespace Snyk.VisualStudio.Extension.CLI
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents latest CLI release information.
    /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether Url.
        /// </summary>
        public string Url
        {
            get
            {
                return this.url;
            }

            set
            {
                this.url = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Id.
        /// </summary>
        public int Id
        {
            get
            {
                return this.id;
            }

            set
            {
                this.id = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tag name.
        /// </summary>
        public string TagName
        {
            get
            {
                return this.tagName;
            }

            set
            {
                this.tagName = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
            }
        }
    }
}