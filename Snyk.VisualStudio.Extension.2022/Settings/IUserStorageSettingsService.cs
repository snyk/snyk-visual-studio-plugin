namespace Snyk.VisualStudio.Extension.Settings
{
    using System.Collections.Generic;

    public interface IUserStorageSettingsService
    {
        ISet<string> TrustedFolders { get; set; }
    }
}
