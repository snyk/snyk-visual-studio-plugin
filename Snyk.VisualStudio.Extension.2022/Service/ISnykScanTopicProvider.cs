using System;
using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Service
{
    public interface ISnykScanTopicProvider
    {
        /// <summary>
        /// Cli scanning started event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningStarted;

        /// <summary>
        /// SnykCode scanning started event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningStarted;

        /// <summary>
        /// Scanning OSS finished event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningFinished;

        /// <summary>
        /// Scanning SnykCode finished event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningFinished;

        /// <summary>
        /// Cli Scanning update event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanningUpdate;

        /// <summary>
        /// SnykCode scanning update event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanningUpdate;

        /// <summary>
        /// Sli scan error event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> OssScanError;

        /// <summary>
        /// SnykCode scan error event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeScanError;

        /// <summary>
        /// SnykCode disabled event handler.
        /// </summary>
        public event EventHandler<SnykCodeScanEventArgs> SnykCodeDisabled;

        /// <summary>
        /// Scanning cancelled event handler.
        /// </summary>
        public event EventHandler<SnykOssScanEventArgs> ScanningCancelled;

        public void FireCodeScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> analysisResult);
        public void FireOssScanningUpdateEvent(IDictionary<string, IEnumerable<Issue>> analysisResult);
        public void FireOssScanningFinishedEvent();
        public void FireSnykCodeScanningFinishedEvent();
    }
}