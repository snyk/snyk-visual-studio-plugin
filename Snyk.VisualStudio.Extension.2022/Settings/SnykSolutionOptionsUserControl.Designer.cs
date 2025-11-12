namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykSolutionOptionsUserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnykSolutionOptionsUserControl));
            this.additionalOptionsTextBox = new System.Windows.Forms.TextBox();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.organizationLabel = new System.Windows.Forms.Label();
            this.organizationTextBox = new System.Windows.Forms.TextBox();
            this.autoOrganizationCheckBox = new System.Windows.Forms.CheckBox();
            this.autoOrganizationDescriptionLabel = new System.Windows.Forms.Label();
            this.OrgDescriptionText = new System.Windows.Forms.Label();
            this.additionalParamsInfoLabel = new System.Windows.Forms.Label();
            this.WebAccountSettingsLabel = new System.Windows.Forms.LinkLabel();
            this.folderConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.folderConfigurationGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // additionalOptionsTextBox
            // 
            this.additionalOptionsTextBox.Location = new System.Drawing.Point(127, 44);
            this.additionalOptionsTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.additionalOptionsTextBox.Multiline = true;
            this.additionalOptionsTextBox.Name = "additionalOptionsTextBox";
            this.additionalOptionsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.additionalOptionsTextBox.Size = new System.Drawing.Size(423, 118);
            this.additionalOptionsTextBox.TabIndex = 0;
            this.additionalOptionsTextBox.TextChanged += new System.EventHandler(this.AdditionalOptionsTextBox_TextChanged);
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 27);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Additional Parameters:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(9, 329);
            this.organizationLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(80, 15);
            this.organizationLabel.TabIndex = 3;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Location = new System.Drawing.Point(127, 329);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(418, 20);
            this.organizationTextBox.TabIndex = 4;
            this.organizationTextBox.TextChanged += new System.EventHandler(this.OrganizationTextBox_TextChanged);
            // 
            // autoOrganizationCheckBox
            // 
            this.autoOrganizationCheckBox.AutoSize = true;
            this.autoOrganizationCheckBox.Location = new System.Drawing.Point(12, 185);
            this.autoOrganizationCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.autoOrganizationCheckBox.Name = "autoOrganizationCheckBox";
            this.autoOrganizationCheckBox.Size = new System.Drawing.Size(124, 19);
            this.autoOrganizationCheckBox.TabIndex = 8;
            this.autoOrganizationCheckBox.Text = "Auto-select organization";
            this.autoOrganizationCheckBox.UseVisualStyleBackColor = true;
            this.autoOrganizationCheckBox.CheckedChanged += new System.EventHandler(this.AutoOrganizationCheckBox_CheckedChanged);
            // 
            // autoOrganizationDescriptionLabel
            // 
            this.autoOrganizationDescriptionLabel.AutoSize = true;
            this.autoOrganizationDescriptionLabel.Location = new System.Drawing.Point(127, 206);
            this.autoOrganizationDescriptionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.autoOrganizationDescriptionLabel.Name = "autoOrganizationDescriptionLabel";
            this.autoOrganizationDescriptionLabel.Size = new System.Drawing.Size(426, 90);
            this.autoOrganizationDescriptionLabel.TabIndex = 9;
            this.autoOrganizationDescriptionLabel.Text = resources.GetString("autoOrganizationDescriptionLabel.Text");
            // 
            // OrgDescriptionText
            // 
            this.OrgDescriptionText.AutoSize = true;
            this.OrgDescriptionText.Location = new System.Drawing.Point(127, 350);
            this.OrgDescriptionText.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.OrgDescriptionText.Name = "OrgDescriptionText";
            this.OrgDescriptionText.Size = new System.Drawing.Size(375, 75);
            this.OrgDescriptionText.TabIndex = 6;
            this.OrgDescriptionText.Text = resources.GetString("OrgDescriptionText.Text");
            // 
            // additionalParamsInfoLabel
            // 
            this.additionalParamsInfoLabel.AutoSize = true;
            this.additionalParamsInfoLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.additionalParamsInfoLabel.Location = new System.Drawing.Point(127, 164);
            this.additionalParamsInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.additionalParamsInfoLabel.Name = "additionalParamsInfoLabel";
            this.additionalParamsInfoLabel.Size = new System.Drawing.Size(418, 15);
            this.additionalParamsInfoLabel.TabIndex = 7;
            this.additionalParamsInfoLabel.Text = "Additional parameters used when calling the CLI, e.g. `-d` or `--exclude=bin`";
            // 
            // WebAccountSettingsLabel
            // 
            this.WebAccountSettingsLabel.AutoSize = true;
            this.WebAccountSettingsLabel.Location = new System.Drawing.Point(127, 296);
            this.WebAccountSettingsLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.WebAccountSettingsLabel.Name = "WebAccountSettingsLabel";
            this.WebAccountSettingsLabel.Size = new System.Drawing.Size(123, 15);
            this.WebAccountSettingsLabel.TabIndex = 10;
            this.WebAccountSettingsLabel.TabStop = true;
            this.WebAccountSettingsLabel.Text = "Web account settings";
            this.WebAccountSettingsLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebAccountSettingsLink_LinkClicked);
            // 
            // folderConfigurationGroupBox
            // 
            this.folderConfigurationGroupBox.Controls.Add(this.WebAccountSettingsLabel);
            this.folderConfigurationGroupBox.Controls.Add(this.additionalParamsInfoLabel);
            this.folderConfigurationGroupBox.Controls.Add(this.OrgDescriptionText);
            this.folderConfigurationGroupBox.Controls.Add(this.autoOrganizationDescriptionLabel);
            this.folderConfigurationGroupBox.Controls.Add(this.autoOrganizationCheckBox);
            this.folderConfigurationGroupBox.Controls.Add(this.organizationTextBox);
            this.folderConfigurationGroupBox.Controls.Add(this.organizationLabel);
            this.folderConfigurationGroupBox.Controls.Add(this.label1);
            this.folderConfigurationGroupBox.Controls.Add(this.additionalOptionsTextBox);
            this.folderConfigurationGroupBox.Location = new System.Drawing.Point(10, 10);
            this.folderConfigurationGroupBox.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.folderConfigurationGroupBox.Name = "folderConfigurationGroupBox";
            this.folderConfigurationGroupBox.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.folderConfigurationGroupBox.Size = new System.Drawing.Size(747, 427);
            this.folderConfigurationGroupBox.TabIndex = 0;
            this.folderConfigurationGroupBox.TabStop = false;
            this.folderConfigurationGroupBox.Text = "Folder Configuration";
            // 
            // SnykSolutionOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.folderConfigurationGroupBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SnykSolutionOptionsUserControl";
            this.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Size = new System.Drawing.Size(4501, 1014);
            this.Load += new System.EventHandler(this.SnykProjectOptionsUserControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.folderConfigurationGroupBox.ResumeLayout(false);
            this.folderConfigurationGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox additionalOptionsTextBox;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label additionalParamsInfoLabel;
        private System.Windows.Forms.Label organizationLabel;
        private System.Windows.Forms.TextBox organizationTextBox;
        private System.Windows.Forms.CheckBox autoOrganizationCheckBox;
        private System.Windows.Forms.Label autoOrganizationDescriptionLabel;
        private System.Windows.Forms.Label OrgDescriptionText;
        private System.Windows.Forms.LinkLabel WebAccountSettingsLabel;
        private System.Windows.Forms.GroupBox folderConfigurationGroupBox;
    }
}
