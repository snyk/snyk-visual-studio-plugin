using Iteratively;

namespace Snyk.Analytics
{
    using System;

    /// <summary>
    /// Analysis result (success or error).
    /// </summary>
    public enum AnalyticsAnalysisResult
    {
        Success,
        Error
    }

    public static class AnalyticsAnalysisResultExtensions
    {
        public static string ToAmplitudeString(this AnalyticsAnalysisResult result)
        {
            switch (result)
            {
                case AnalyticsAnalysisResult.Success:
                    return "Success";
                case AnalyticsAnalysisResult.Error:
                    return "Error";
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        public static AnalysisIsReady.Result ToAnalysisIsReadyEnum(this AnalyticsAnalysisResult analysisResult)
        {
            switch (analysisResult)
            {
                case AnalyticsAnalysisResult.Success:
                    return AnalysisIsReady.Result.Success;
                case AnalyticsAnalysisResult.Error:
                    return AnalysisIsReady.Result.Error;
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisResult), analysisResult, null);
            }
        }
    }
}