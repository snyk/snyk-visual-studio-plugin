namespace Snyk.Common.Service
{
    /// <summary>
    /// User for Snyk Analytics.
    /// </summary>
    public class SnykUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string Id { get; set; }

        private string idHash = string.Empty;
        public string IdHash
        {
            get
            {
                if (string.IsNullOrEmpty(idHash) && !string.IsNullOrEmpty(Id))
                {
                    idHash =  Sha256.ComputeHash(Id);
                }

                return idHash;
            }
        }
    }
}
