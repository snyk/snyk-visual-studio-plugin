namespace Snyk.Code.Library.Domain.Analysis
{
    /// <summary>
    /// Status of Analysis.
    /// </summary>
    public class AnalysisStatus
    {
        /// <summary>
        /// Your request is waiting in a queue to be processed.
        /// </summary>
        public const string Waiting = "WAITING";

        /// <summary>
        /// The analysis has just begun and it is currently cloning/fetching the git repository or checking missing files.
        /// </summary>
        public const string Fetching = "FETCHING";

        /// <summary>
        /// DeepCode is analyzing every file in the bundle to check for bugs and create suggestions.
        /// </summary>
        public const string Analyzing = "ANALYZING";

        /// <summary>
        /// DeepCode has finished analyzing the files but external linter tools are still computing.
        /// </summary>
        public const string DcDone = "DC_DONE";

        /// <summary>
        /// All analyses have been computed and are available.
        /// </summary>
        public const string COMPLETE = "COMPLETE";

        /// <summary>
        /// Something went wrong with the analysis. For uploaded bundles this occurs when attempting to analyze bundles with missing files. If caused by a transient error, further calls to this API will reset the analysis status and start from the "FETCHING" phase again.
        /// </summary>
        public const string Failed = "FAILED";
    }
}
