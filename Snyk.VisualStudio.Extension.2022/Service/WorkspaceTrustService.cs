﻿using System;
using System.IO;
using Serilog;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Service
{
    public class WorkspaceTrustService : IWorkspaceTrustService
    {
        private static readonly ILogger Logger = LogManager.ForContext<WorkspaceTrustService>();

        private readonly ISnykServiceProvider serviceProvider;

        public WorkspaceTrustService(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void AddFolderToTrusted(string absoluteFolderPath)
        {
            if (!Path.IsPathRooted(absoluteFolderPath))
            {
                throw new ArgumentException("Trusted folder path provided is not absolute.");
            }

            if (!Directory.Exists(absoluteFolderPath))
            {
                throw new ArgumentException("Trusted folder doesn't exist.");
            }

            try
            {
                var trustedFolders = this.serviceProvider.Options.TrustedFolders;
                trustedFolders.Add(absoluteFolderPath);
                this.serviceProvider.Options.TrustedFolders = trustedFolders;
                this.serviceProvider.SnykOptionsManager.Save(this.serviceProvider.Options, false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to add a folder to trusted.");
            }
        }

        public bool IsFolderTrusted(string absoluteFolderPath)
        {
            if (string.IsNullOrEmpty(absoluteFolderPath))
                return true;
            var trustedFolders = this.serviceProvider.Options.TrustedFolders;

            foreach (var trustedFolder in trustedFolders)
            {
                if (this.IsSubFolderOrEqual(trustedFolder, absoluteFolderPath))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Verify if subfolder is rooted at parent path.
        /// </summary>
        /// <param name="parentPath">Parent path to check against.</param>
        /// <param name="childPath">Subfolder path to verify.</param>
        /// <returns>Returns true if childPath is subfolder of parentPath, or equal to it.</returns>
        private bool IsSubFolderOrEqual(string parentPath, string childPath)
        {
            var parentUri = new Uri(parentPath);
            if (new Uri(childPath).Equals(parentUri))
            {
                return true;
            }

            var childUri = new DirectoryInfo(childPath).Parent;
            while (childUri != null)
            {
                if (new Uri(childUri.FullName).Equals(parentUri))
                {
                    return true;
                }

                childUri = childUri.Parent;
            }

            return false;
        }
    }
}
