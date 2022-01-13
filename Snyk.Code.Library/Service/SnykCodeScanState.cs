namespace Snyk.Code.Library.Service
{
    public enum SnykCodeScanState
    {
        /// <summary>
        /// This state (step) contains prepare code to upload, cache initialization, upload files for analysis.
        /// </summary>
        Preparing,

        /// <summary>
        /// This state (step) contains snykcode analysis results (response from code service waiting, analysing, etc).
        /// </summary>
        Analysing,
    }
}
