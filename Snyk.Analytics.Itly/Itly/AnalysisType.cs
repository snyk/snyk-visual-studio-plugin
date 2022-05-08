using System.Collections.Generic;

namespace Iteratively
{
    public enum AnalysisType
    {
        SnykAdvisor,
        SnykCodeQuality,
        SnykCodeSecurity,
        SnykOpenSource,
        SnykContainer,
        SnykInfrastructureAsCode
    }

    public static class AnalysisTypeExtensions
    {
        private static readonly Dictionary<AnalysisType, string> AnalysisTypeValues = new Dictionary<AnalysisType, string>
        {
            [AnalysisType.SnykAdvisor] = "Snyk Advisor",
            [AnalysisType.SnykCodeQuality] = "Snyk Code Quality",
            [AnalysisType.SnykCodeSecurity] = "Snyk Code Security",
            [AnalysisType.SnykOpenSource] = "Snyk Open Source",
            [AnalysisType.SnykContainer] = "Snyk Container",
            [AnalysisType.SnykInfrastructureAsCode] = "Snyk Infrastructure as Code"
        };

        public static string ToAmplitudeString(this AnalysisType type) => AnalysisTypeValues[type];
    }
}