namespace Snyk.VisualStudio.Extension.Shared.UI
{
    using System.Text;
    using System.Windows.Media;
    using CLI;

    /// <summary>
    /// Filter vulnerabilities by severity.
    /// </summary>
    public class SeverityFilter
    {
        /// <summary>
        /// Critical severity title.
        /// </summary>
        public const string CriticalTitle = "Critical severity";

        /// <summary>
        /// High severity title.
        /// </summary>
        public const string HighTitle = "High severity";

        /// <summary>
        /// Medium severity title.
        /// </summary>
        public const string MediumTitle = "Medium severity";

        /// <summary>
        /// Low severity title.
        /// </summary>
        public const string LowTitle = "Low severity";

        /// <summary>
        /// Crititcal severity filter.
        /// </summary>
        public static readonly string Critical = string.Format(FilterTemplate, Severity.Critical);

        /// <summary>
        /// High severity filter.
        /// </summary>
        public static readonly string High = string.Format(FilterTemplate, Severity.High);

        /// <summary>
        /// Medium severity filter.
        /// </summary>
        public static readonly string Medium = string.Format(FilterTemplate, Severity.Medium);

        /// <summary>
        /// Low severity filter.
        /// </summary>
        public static readonly string Low = string.Format(FilterTemplate, Severity.Low);

        private const string FilterTemplate = "severity:\"{0}\"";

        private const string CriticalColorHex = "#9d2a23";
        private const string HighColorHex = "#C75450";
        private const string MediumColorHex = "#EDA200";
        private const string LowColorHex = "#6E6E6E";

        /// <summary>
        /// Initializes a new instance of the <see cref="SeverityFilter"/> class.
        /// Calculate is critical, high, medium and low severity exists in filter string.
        /// </summary>
        /// <param name="filterString">Filter string.</param>
        protected SeverityFilter(string filterString)
        {
            this.FilterString = filterString;

            this.ContainsCritical = filterString.Contains(SeverityFilter.Critical);
            this.ContainsHigh = filterString.Contains(SeverityFilter.High);
            this.ContainsMedium = filterString.Contains(SeverityFilter.Medium);
            this.ContainsLow = filterString.Contains(SeverityFilter.Low);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeverityFilter"/> class.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="hexColor">Color in hex format.</param>
        protected SeverityFilter(string title, string hexColor)
            : this(title, (Color)ColorConverter.ConvertFromString(hexColor)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeverityFilter"/> class.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="color">Color of severity.</param>
        protected SeverityFilter(string title, Color color)
        {
            this.Title = title;
            this.Color = color;
        }

        /// <summary>
        /// Gets empty severity.
        /// </summary>
        public static SeverityFilter Empty => new SeverityFilter(string.Empty, Colors.Transparent);

        /// <summary>
        /// Gets or sets a value indicating whether Title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether initial filter string.
        /// </summary>
        public string FilterString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether crititcal severity filter exists.
        /// </summary>
        public bool ContainsCritical { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether high severity filter exists.
        /// </summary>
        public bool ContainsHigh { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether medium severity filter exists.
        /// </summary>
        public bool ContainsMedium { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether low severity filter exists.
        /// </summary>
        public bool ContainsLow { get; set; }

        /// <summary>
        /// Remove from query string severity information.
        /// </summary>
        /// <returns>Result query string.</returns>
        public string GetOnlyQueryString()
        {
            StringBuilder stringBuilder = new StringBuilder(this.FilterString);

            if (this.ContainsCritical)
            {
                stringBuilder.Replace(SeverityFilter.Critical, string.Empty);
            }

            if (this.ContainsHigh)
            {
                stringBuilder.Replace(SeverityFilter.High, string.Empty);
            }

            if (this.ContainsMedium)
            {
                stringBuilder.Replace(SeverityFilter.Medium, string.Empty);
            }

            if (this.ContainsLow)
            {
                stringBuilder.Replace(SeverityFilter.Low, string.Empty);
            }

            return stringBuilder.ToString().Trim();
        }

        /// <summary>
        /// Check is severity string is critical or high or medium or low.
        /// </summary>
        /// <param name="severity">Source severity string.</param>
        /// <returns>Bool value.</returns>
        public bool IsVulnerabilityIncluded(string severity)
        {
            bool isIncluded = true;

            if (this.ContainsCritical || this.ContainsHigh || this.ContainsMedium || this.ContainsLow)
            {
                switch (severity)
                {
                    case Severity.Critical:
                        isIncluded = this.ContainsCritical;
                        break;
                    case Severity.High:
                        isIncluded = this.ContainsHigh;
                        break;
                    case Severity.Medium:
                        isIncluded = this.ContainsMedium;
                        break;
                    case Severity.Low:
                        isIncluded = this.ContainsLow;
                        break;
                    default:
                        isIncluded = false;
                        break;
                }
            }

            return isIncluded;
        }

        /// <summary>
        /// Create new Filter by query string.
        /// </summary>
        /// <param name="queryString">Query string.</param>
        /// <returns><see cref="SeverityFilter"/>.</returns>
        public static SeverityFilter ByQueryString(string queryString) => new SeverityFilter(queryString);

        /// <summary>
        /// Create new Filter by severity name.
        /// </summary>
        /// <param name="severity">Severity name.</param>
        /// <returns><see cref="SeverityFilter"/>.</returns>
        public static SeverityFilter ByName(string severity)
        {
            SeverityFilter severityFilter;

            switch (severity)
            {
                case Severity.Critical:
                    severityFilter = new SeverityFilter(CriticalTitle, CriticalColorHex);
                    break;
                case Severity.High:
                    severityFilter = new SeverityFilter(HighTitle, HighColorHex);
                    break;
                case Severity.Medium:
                    severityFilter = new SeverityFilter(MediumTitle, MediumColorHex);
                    break;
                case Severity.Low:
                    severityFilter = new SeverityFilter(LowTitle, LowColorHex);
                    break;
                default:
                    severityFilter = Empty;
                    break;
            }

            return severityFilter;
        }
    }
}
