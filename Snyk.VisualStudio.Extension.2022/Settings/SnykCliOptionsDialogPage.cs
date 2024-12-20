using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

[Guid("D1D89CEB-7691-4A67-8579-4C3DDE776982")]
[ComVisible(true)]
public class SnykCliOptionsDialogPage : DialogPage, ISnykCliOptionsDialogPage
{
    private SnykCliOptionsUserControl snykCliOptionsUserControl;
    private ISnykServiceProvider serviceProvider;
    private ISnykOptions snykOptions;

    public void Initialize(ISnykServiceProvider provider)
    {
        this.serviceProvider = provider;
        this.snykOptions = provider.Options;
    }
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
        this.serviceProvider.SnykOptionsManager.Save(this.snykOptions);
    }

    protected override void OnClosed(EventArgs e)
    {
        // do nothing
    }

    private void HandleCliDownload()
    {
        var memento = SnykCliOptionsUserControl.OptionsMemento;

        this.snykOptions.CliDownloadUrl = memento.CliDownloadUrl;

        var binariesAutoUpdateChanged = this.snykOptions.BinariesAutoUpdate != memento.BinariesAutoUpdate;
        var releaseChannelChanged = this.snykOptions.CliReleaseChannel != memento.CliReleaseChannel;
        var cliCustomPathChanged = this.snykOptions.CliCustomPath != memento.CliCustomPath;

        this.snykOptions.CurrentCliVersion = memento.CurrentCliVersion;
        this.snykOptions.BinariesAutoUpdate = memento.BinariesAutoUpdate;
        this.snykOptions.CliReleaseChannel = memento.CliReleaseChannel;
        this.snykOptions.CliCustomPath = memento.CliCustomPath;

        var hasChanges = binariesAutoUpdateChanged || releaseChannelChanged || cliCustomPathChanged;
        if (!hasChanges)
        {
            return;
        }

        serviceProvider.TasksService.CancelTasks();

        if (memento.BinariesAutoUpdate)
        {
            // DownloadStarted event stops language server and DownloadFinished starts it automatically
            this.serviceProvider.TasksService.Download();
        }
        else
        {
            LanguageClientHelper.LanguageClientManager().RestartServerAsync().FireAndForget();
        }
    }
}