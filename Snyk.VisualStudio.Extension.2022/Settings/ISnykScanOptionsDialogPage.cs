﻿using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykScanOptionsDialogPage
{
    void Initialize(ISnykServiceProvider provider);
    SnykScanOptionsUserControl SnykScanOptionsUserControl { get; }
}