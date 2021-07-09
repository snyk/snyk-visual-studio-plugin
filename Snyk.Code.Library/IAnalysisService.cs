namespace Snyk.Code.Library
{
    using System.Threading.Tasks;
    using Snyk.Code.Library.Domain;

    /// <summary>
    /// Contains logic related to SnykCode analysis logic.
    /// </summary>
    public interface IAnalysisService
    {
        /// <summary>
        /// Starts a new bundle analysis or checks its current status and available results.
        /// Returns the current analysis status, the relative progress (between 0 and 1) within the current status, the analysisURL that you can access on your browser to see the interactive analysis on DeepCode, and the analysisResults if available. 
        /// The status is defined as follows:
        /// WAITING: Your request is waiting in a queue to be processed.
        /// FETCHING: The analysis has just begun and it is currently cloning/fetching the git repository or checking missing files.
        /// ANALYZING: DeepCode is analyzing every file in the bundle to check for bugs and create suggestions.
        /// DC_DONE: DeepCode has finished analyzing the files but external linter tools are still computing.
        /// DONE: All analyses have been computed and are available.
        /// FAILED: Something went wrong with the analysis. For uploaded bundles this occurs when attempting to analyze bundles with missing files.If caused by a transient error, further calls to this API will reset the analysis status and start from the "FETCHING" phase again.
        /// The analysisResults object is only available in the "DONE" status.
        /// It contains all the suggestions and the relative positions.
        /// </summary>
        /// <param name="bundleId">Source bundle id to analysy.</param>
        /// <returns>Analysis results with suggestions and the relative positions.</returns>
        Task<AnalysisResult> GetAnalysisAsync(string bundleId);
    }
}
