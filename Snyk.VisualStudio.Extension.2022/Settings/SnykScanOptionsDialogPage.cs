using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
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

    protected override IWin32Window Window => SnykScanOptionsUserControl;
    public SnykScanOptionsUserControl SnykScanOptionsUserControl
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
        // do nothing
    }

    protected override void OnClosed(EventArgs e)
    {
        // do nothing
    }

}