using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

[Guid("EB160D8D-C73F-4907-A9CA-4D287FACA36B")]
[ComVisible(true)]
public class SnykExperimentalDialogPage : DialogPage, ISnykExperimentalDialogPage
{
    private SnykExperimentalUserControl snykExperimentalUserControl;
    private ISnykServiceProvider serviceProvider;
    private ISnykOptions snykOptions;

    public void Initialize(ISnykServiceProvider provider)
    {
        this.serviceProvider = provider;
        this.snykOptions = provider.Options;
    }
    protected override IWin32Window Window => SnykExperimentalUserControl;
    public SnykExperimentalUserControl SnykExperimentalUserControl
    {
        get
        {
            if (snykExperimentalUserControl == null)
            {
                snykExperimentalUserControl = new SnykExperimentalUserControl(serviceProvider);
            }
            return snykExperimentalUserControl;
        }
    }

    // This method is used when the user clicks "Ok"
    public override void SaveSettingsToStorage()
    {
        this.snykOptions.IgnoredIssuesEnabled = SnykExperimentalUserControl.OptionsMemento.IgnoredIssuesEnabled;
        this.snykOptions.OpenIssuesEnabled = SnykExperimentalUserControl.OptionsMemento.OpenIssuesEnabled;
        this.serviceProvider.SnykOptionsManager.Save(this.snykOptions);

        if (LanguageClientHelper.IsLanguageServerReady() && snykOptions.AutoScan)
            LanguageClientHelper.LanguageClientManager().InvokeWorkspaceScanAsync(SnykVSPackage.Instance.DisposalToken).FireAndForget();
    }

    protected override void OnClosed(EventArgs e)
    {
        // do nothing
    }
}