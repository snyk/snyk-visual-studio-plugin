namespace Snyk.VisualStudio.Extension.Shared.Model
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
        /// Critical => 4.
        /// High => 3.
        /// Medium => 2.
        /// Low => 1.
        /// </summary>
        /// <param name="severity">Severity as string.</param>
        /// <returns>Severity as int.</returns>
        public static int ToInt(string severity)
        {
            int intSeverityRepresentation;

            switch (severity)
            {
                case Severity.Critical:
                    intSeverityRepresentation = 4;
                    break;
                case Severity.High:
                    intSeverityRepresentation = 3;
                    break;
                case Severity.Medium:
                    intSeverityRepresentation = 2;
                    break;
                case Severity.Low:
                    intSeverityRepresentation = 1;
                    break;
                default:
                    intSeverityRepresentation = 0;
                    break;
            }

            return intSeverityRepresentation;
        }

        /// <summary>
        /// Transform int severity representation to string representation.
        /// 3 => High.
        /// 2 => Medium.
        /// 1 => Low.
        /// </summary>
        /// <param name="severity">Severity as int.</param>
        /// <returns>Severity as string.</returns>
        public static string FromInt(int severity)
        {
            string severityRepresentation;

            switch (severity)
            {
                case 3:
                    severityRepresentation = Severity.High;
                    break;
                case 2:
                    severityRepresentation = Severity.Medium;
                    break;
                case 1:
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
