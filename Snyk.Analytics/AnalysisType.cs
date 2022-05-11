using System;

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
    }
}