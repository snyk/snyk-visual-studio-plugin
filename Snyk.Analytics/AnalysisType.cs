using System;
using Iteratively;

namespace Snyk.Analytics
{
    public enum AnalysisType
    {
        SnykOpenSource,
        SnykCodeSecurity,
        SnykCodeQuality,
    }

    public static class AnalysisTypeEnumExtensions
    {
        public static string ToAmplitudeString(this AnalysisType analysisType)
        {
            switch (analysisType)
            {
                case AnalysisType.SnykOpenSource:
                    return "Snyk Open Source";
                case AnalysisType.SnykCodeSecurity:
                    return "Snyk Code Security";
                case AnalysisType.SnykCodeQuality:
                    return "Snyk Code Quality";
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisType));
            }
        }

        public static AnalysisIsReady.AnalysisType ToAnalysisIsReadyEnum(this AnalysisType analysisTypeParam)
        {
            AnalysisIsReady.AnalysisType analysisType;
            switch (analysisTypeParam)
            {
                case AnalysisType.SnykOpenSource:
                    analysisType = AnalysisIsReady.AnalysisType.SnykOpenSource;
                    break;
                case AnalysisType.SnykCodeSecurity:
                    analysisType = AnalysisIsReady.AnalysisType.SnykCodeSecurity;
                    break;
                case AnalysisType.SnykCodeQuality:
                    analysisType = AnalysisIsReady.AnalysisType.SnykCodeQuality;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisTypeParam), analysisTypeParam, null);
            }

            return analysisType;
        }
    }
}