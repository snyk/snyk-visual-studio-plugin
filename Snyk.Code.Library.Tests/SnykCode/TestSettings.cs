namespace Snyk.Code.Library.Tests.SnykCode
{    
    using System;   

    /// <summary>
    /// Test settings.
    /// </summary>
    public class TestSettings
    {
        public const string SnykCodeApiUrl = "https://deeproxy.dev.snyk.io/";

        /// <summary>
        /// Gets or sets a value indicating whether Api token.
        /// </summary>
        public string ApiToken { get; set; }

        private static TestSettings instance;

        /// <summary>
        /// Gets a value indicating whether Settings instance. If settings not loaded it will load it first from settings.json.
        /// </summary>
        /// <returns><see cref="TestSettings"/> instance.</returns>
        public static TestSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TestSettings
                    {
                        ApiToken = Environment.GetEnvironmentVariable("TEST_API_TOKEN"),
                    };                     
                }

                return instance;
            }
        }
    }
}
