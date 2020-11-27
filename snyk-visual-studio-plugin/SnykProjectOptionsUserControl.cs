using System;
using System.Windows.Forms;

namespace snyk_visual_studio_plugin
{
    public partial class SnykProjectOptionsUserControl : UserControl
    {       
        internal SnykProjectOptionsDialogPage projectOptionsPage;

        private SnykProjectSettingsService projectSettingsService;

        public SnykProjectOptionsUserControl(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.projectSettingsService = new SnykProjectSettingsService(serviceProvider);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            bool isProjectOpened = projectSettingsService.IsProjectOpened();

            additionalOptionsTextBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = projectSettingsService.GetAdditionalOptions();

                if (!String.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptionsTextBox.Text = additionalOptions;
                } else
                {
                    additionalOptionsTextBox.Text = "";
                }
            }
            catch (Exception exception)
            {
                additionalOptionsTextBox.Text = "";
            }
        }

        public void Initialize()
        {            
            additionalOptionsTextBox.Text = "";

            bool isProjectOpened = projectSettingsService.IsProjectOpened();

            additionalOptionsTextBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = projectSettingsService.GetAdditionalOptions();

                if (!String.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptionsTextBox.Text = additionalOptions;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void additionalOptionsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (projectSettingsService.IsProjectOpened())
            {
                projectSettingsService.saveAdditionalOptions(additionalOptionsTextBox.Text.ToString());
            }
        }
    }
}
