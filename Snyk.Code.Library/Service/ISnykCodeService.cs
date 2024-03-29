﻿namespace Snyk.Code.Library.Service
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;

    /// <summary>
    /// Contains high level busines logic for SnykCode APIs.
    /// </summary>
    public interface ISnykCodeService
    {
        /// <summary>
        /// Gets or sets scan event handler.
        /// </summary>
        EventHandler<SnykCodeEventArgs> ScanEventHandler { get; set; }

        /// <summary>
        /// Scan source code provided for code vulnerabilities.
        /// </summary>
        /// <param name="fileProvider">Provider for files to scan.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> token to cancel request.</param>
        /// <returns><see cref="AnalysisResult"/> object.</returns>
        Task<AnalysisResult> ScanAsync(IFileProvider fileProvider, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get SnykCode error message from exception.
        /// </summary>
        /// <param name="e">Source exception.</param>
        /// <returns>String exception message.</returns>
        string GetSnykCodeErrorMessage(Exception e);

        /// <summary>
        /// Clean variables and cache.
        /// </summary>
        void Clean();
    }
}
