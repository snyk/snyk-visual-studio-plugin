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
        this.serviceProvider.SnykOptionsManager.Save(this.SnykOptions);
        this.SnykOptions.InvokeSettingsChangedEvent();
    }

    protected override void OnClosed(EventArgs e)
    {
        
    }

    private void HandleCliDownload()
    {
        var memento = SnykCliOptionsUserControl.OptionsMemento;

        this.SnykOptions.CliDownloadUrl = memento.CliDownloadUrl;
        this.SnykOptions.CliCustomPath = memento.CliCustomPath;

        if (!memento.BinariesAutoUpdate)
        {
            HandleManualBinaries(memento);
            return;
        }

        // if auto-update is enabled, check if the release channel changed:
        if (this.SnykOptions.CliReleaseChannel != memento.CliReleaseChannel)
        {
            this.SnykOptions.CliReleaseChannel = memento.CliReleaseChannel;
            this.SnykOptions.BinariesAutoUpdate = true;
            serviceProvider.TasksService.CancelTasks();
            this.serviceProvider.TasksService.Download();
        }
    }

    private void HandleManualBinaries(ISnykOptions memento)
    {
        this.SnykOptions.CurrentCliVersion = string.Empty;
        this.SnykOptions.BinariesAutoUpdate = false;
        this.SnykOptions.CliReleaseChannel = memento.CliReleaseChannel;
        serviceProvider.TasksService.CancelTasks();
        LanguageClientHelper.LanguageClientManager().RestartServerAsync().FireAndForget();
    }
}