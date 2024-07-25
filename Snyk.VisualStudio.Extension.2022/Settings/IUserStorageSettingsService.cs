namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System.Collections.Generic;

    public interface IUserStorageSettingsService
    {
        ISet<string> TrustedFolders { get; set; }
    }
}
