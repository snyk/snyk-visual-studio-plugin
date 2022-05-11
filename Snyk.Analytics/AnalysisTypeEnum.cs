using System;

namespace Snyk.Analytics
{
    public enum AnalysisTypeEnum
    {
        SnykOpenSource,
        SnykCodeSecurity,
        SnykCodeQuality,
    }

    public static class AnalysisTypeEnumExtensions
    {
        public static string ToAmplitudeString(this AnalysisTypeEnum analysisType)
        {
            switch (analysisType)
            {
                case AnalysisTypeEnum.SnykOpenSource:
                    return "Snyk Open Source";
                case AnalysisTypeEnum.SnykCodeSecurity:
                    return "Snyk Code Security";
                case AnalysisTypeEnum.SnykCodeQuality:
                    return "Snyk Code Quality";
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisType));
            }
        }
    }
}