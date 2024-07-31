﻿using System;
using Snyk.Common.Authentication;

namespace Snyk.Code.Library.Tests.SnykCode
{
    /// <summary>
    /// Test settings.
    /// </summary>
    public class TestSettings
    {
        /// <summary>
        /// SnykCode development API URL.
        /// </summary>
        public const string SnykCodeApiUrl = "https://deeproxy.snyk.io/";

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
                        ApiToken = new AuthenticationToken(AuthenticationType.Token, Environment.GetEnvironmentVariable("TEST_API_TOKEN")),
                    };
                }

                return instance;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Api token.
        /// </summary>
        public AuthenticationToken ApiToken { get; set; }
    }
}
