using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

[Guid("2AF9707D-A0A5-4D39-BC8C-6E23B4699F58")]
[ComVisible(true)]
public class SnykScanOptionsDialogPage : DialogPage, ISnykScanOptionsDialogPage
{
    private SnykScanOptionsUserControl snykScanOptionsUserControl;
    private ISnykServiceProvider serviceProvider;
    private ISnykOptions snykOptions;

    public void Initialize(ISnykServiceProvider provider)
    {
        this.serviceProvider = provider;
        this.snykOptions = provider.Options;
    }

    protected override IWin32Window Window => SnykCliOptionsUserControl;
    public SnykScanOptionsUserControl SnykCliOptionsUserControl
    {
        get
        {
            if (snykScanOptionsUserControl == null)
            {
                snykScanOptionsUserControl = new SnykScanOptionsUserControl(serviceProvider);
            }
            return snykScanOptionsUserControl;
        }
    }

    // This method is used when the user clicks "Ok"
    public override void SaveSettingsToStorage()
    {
        this.snykOptions.AutoScan = SnykCliOptionsUserControl.OptionsMemento.AutoScan;
        this.snykOptions.EnableDeltaFindings = SnykCliOptionsUserControl.OptionsMemento.EnableDeltaFindings;
        this.snykOptions.IgnoredIssuesEnabled = SnykCliOptionsUserControl.OptionsMemento.IgnoredIssuesEnabled;
        this.snykOptions.OpenIssuesEnabled = SnykCliOptionsUserControl.OptionsMemento.OpenIssuesEnabled;
        this.snykOptions.SnykCodeQualityEnabled = SnykCliOptionsUserControl.OptionsMemento.SnykCodeQualityEnabled;
        this.snykOptions.SnykCodeSecurityEnabled = SnykCliOptionsUserControl.OptionsMemento.SnykCodeSecurityEnabled;
        this.snykOptions.IacEnabled = SnykCliOptionsUserControl.OptionsMemento.IacEnabled;
        this.snykOptions.OssEnabled = SnykCliOptionsUserControl.OptionsMemento.OssEnabled;
        if (this.snykOptions.AutoScan)
            serviceProvider.LanguageClientManager.InvokeWorkspaceScanAsync(SnykVSPackage
                .Instance.DisposalToken).FireAndForget();

        this.serviceProvider.SnykOptionsManager.Save(this.snykOptions);
        this.snykOptions.InvokeSettingsChangedEvent();
    }

    protected override void OnClosed(EventArgs e)
    {
        // do nothing
    }

}