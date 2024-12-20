using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

[Guid("6A88ADAA-CA31-4146-8410-A340F8AA7E92")]
[ComVisible(true)]
public class SnykUserExperienceDialogPage : DialogPage, ISnykUserExperienceDialogPage
{
    private SnykUserExperienceUserControl snykUserExperienceUserControl;
    private ISnykServiceProvider serviceProvider;
    private ISnykOptions snykOptions;

    public void Initialize(ISnykServiceProvider provider)
    {
        this.serviceProvider = provider;
        this.snykOptions = provider.Options;
    }

    protected override IWin32Window Window => SnykUserExperienceUserControl;
    public SnykUserExperienceUserControl SnykUserExperienceUserControl
    {
        get
        {
            if (snykUserExperienceUserControl == null)
            {
                snykUserExperienceUserControl = new SnykUserExperienceUserControl(serviceProvider);
            }
            return snykUserExperienceUserControl;
        }
    }

    // This method is used when the user clicks "Ok"
    public override void SaveSettingsToStorage()
    {
        this.snykOptions.AutoScan = SnykUserExperienceUserControl.OptionsMemento.AutoScan;
        this.serviceProvider.SnykOptionsManager.Save(this.snykOptions);

        if (LanguageClientHelper.IsLanguageServerReady() && this.snykOptions.AutoScan)
            serviceProvider.LanguageClientManager.InvokeWorkspaceScanAsync(SnykVSPackage
                .Instance.DisposalToken).FireAndForget();
    }

    protected override void OnClosed(EventArgs e)
    {
        // do nothing
    }
}