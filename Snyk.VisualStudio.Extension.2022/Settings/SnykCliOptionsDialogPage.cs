using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

[Guid("D1D89CEB-7691-4A67-8579-4C3DDE776982")]
[ComVisible(true)]
public class SnykCliOptionsDialogPage : DialogPage, ISnykCliOptionsDialogPage
{
    private SnykCliOptionsUserControl snykCliOptionsUserControl;
    private ISnykServiceProvider serviceProvider;

    public void Initialize(ISnykServiceProvider provider)
    {
        this.serviceProvider = provider;
        this.SnykOptions = provider.Options;
    }

    public ISnykOptions SnykOptions { get; set; }

    protected override IWin32Window Window => SnykCliOptionsUserControl;
    public SnykCliOptionsUserControl SnykCliOptionsUserControl
    {
        get
        {
            if (snykCliOptionsUserControl == null)
            {
                snykCliOptionsUserControl = new SnykCliOptionsUserControl(serviceProvider);
            }
            return snykCliOptionsUserControl;
        }
    }

    // This method is used when the user clicks "Ok"
    public override void SaveSettingsToStorage()
    {
        HandleCliDownload();
        this.SnykOptions.SaveSettings();
        this.SnykOptions.InvokeSettingsChangedEvent();
    }

    protected override void OnClosed(EventArgs e)
    {
        
    }

    private void HandleCliDownload()
    {
        var releaseChannel = SnykCliOptionsUserControl.GetReleaseChannel().Trim();
        var downloadUrl = SnykCliOptionsUserControl.GetCliDownloadUrl().Trim();
        var manageBinariesAutomatically = SnykCliOptionsUserControl.GetManageBinariesAutomatically();
        if (!manageBinariesAutomatically)
        {
            this.SnykOptions.CurrentCliVersion = string.Empty;
            this.SnykOptions.BinariesAutoUpdate = false;
            serviceProvider.TasksService.CancelDownloadTask();
            // Language Server restart will happen on DownloadCancelled Event.
            return;
        }
        if (this.SnykOptions.CliReleaseChannel != releaseChannel || this.SnykOptions.CliDownloadUrl != downloadUrl || this.SnykOptions.BinariesAutoUpdate != manageBinariesAutomatically)
        {
            this.SnykOptions.CliDownloadUrl = downloadUrl;
            this.SnykOptions.CliReleaseChannel = releaseChannel;
            this.SnykOptions.BinariesAutoUpdate = manageBinariesAutomatically;
            serviceProvider.TasksService.CancelDownloadTask();
            this.serviceProvider.TasksService.Download();
        }
    }
}