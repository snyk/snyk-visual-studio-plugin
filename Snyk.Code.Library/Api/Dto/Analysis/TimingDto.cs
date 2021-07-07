namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// Analysis result timing information.
    /// </summary>
    public class TimingDto
    {
        /// <summary>
        /// Gets or sets a value indicating anaylysis fetching code time.
        /// </summary>
        public int FetchingCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis.
        /// </summary>
        public int Analysis { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis queue.
        /// </summary>
        public int Queue { get; set; }
    }
}
