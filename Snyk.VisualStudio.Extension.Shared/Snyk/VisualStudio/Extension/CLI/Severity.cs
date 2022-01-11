namespace Snyk.VisualStudio.Extension.CLI
{
    /// <summary>
    /// Severity class.
    /// </summary>
    public class Severity
    {
        /// <summary>
        /// Critical severity string.
        /// </summary>
        public const string Critical = "critical";

        /// <summary>
        /// High severity string.
        /// </summary>
        public const string High = "high";

        /// <summary>
        /// Medium severity string.
        /// </summary>
        public const string Medium = "medium";

        /// <summary>
        /// Low severity string.
        /// </summary>
        public const string Low = "low";

        /// <summary>
        /// Transform string severity representation to int representation.
        /// Crititlca => 3.
        /// High => 2.
        /// Medium => 1.
        /// Low => 0.
        /// </summary>
        /// <param name="severity">Severity as string.</param>
        /// <returns>Severity as int.</returns>
        public static int ToInt(string severity)
        {
            int intSeverityRepresentation;

            switch (severity)
            {
                case Severity.Critical:
                    intSeverityRepresentation = 3;
                    break;
                case Severity.High:
                    intSeverityRepresentation = 2;
                    break;
                case Severity.Medium:
                    intSeverityRepresentation = 1;
                    break;
                case Severity.Low:
                    intSeverityRepresentation = 0;
                    break;
                default:
                    intSeverityRepresentation = -1;
                    break;
            }

            return intSeverityRepresentation;
        }

        /// <summary>
        /// Transform int severity representation to string representation.
        /// 3 => Crititlca.
        /// 2 => High.
        /// 1 => Medium.
        /// 0 => Low.
        /// </summary>
        /// <param name="severity">Severity as int.</param>
        /// <returns>Severity as string.</returns>
        public static string FromInt(int severity)
        {
            string severityRepresentation;

            switch (severity)
            {
                case 3:
                    severityRepresentation = Severity.Critical;
                    break;
                case 2:
                    severityRepresentation = Severity.High;
                    break;
                case 1:
                    severityRepresentation = Severity.Medium;
                    break;
                case 0:
                    severityRepresentation = Severity.Low;
                    break;
                default:
                    severityRepresentation = Severity.Low;
                    break;
            }

            return severityRepresentation;
        }
    }
}
