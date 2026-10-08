using System;
using System.Collections.Generic;
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
                this.serviceProvider.SnykOptionsManager.Save(this.serviceProvider.Options, triggerSettingsChangedEvent: false, updateOverrideTracker: false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to add a folder to trusted.");
            }
        }

        public bool IsFolderTrusted(string absoluteFolderPath)
        {
            if (string.IsNullOrEmpty(absoluteFolderPath))
            {
                return false;
            }

            var folder = Normalize(absoluteFolderPath);
            foreach (var trusted in this.serviceProvider.Options.TrustedFolders ?? new HashSet<string>())
            {
                var trustedFolder = Normalize(trusted);
                if (folder.Equals(trustedFolder, StringComparison.OrdinalIgnoreCase)
                    || folder.StartsWith(trustedFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string path) =>
            Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
