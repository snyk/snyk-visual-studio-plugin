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
            this.OrganizationInfoLink = new System.Windows.Forms.LinkLabel();
            this.OrgDescriptionText = new System.Windows.Forms.Label();
            this.additionalParamsInfoLabel = new System.Windows.Forms.Label();
            this.WebAccountSettingsLabel = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // additionalOptionsTextBox
            // 
            this.additionalOptionsTextBox.Location = new System.Drawing.Point(83, 35);
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
            this.label1.Location = new System.Drawing.Point(6, 18);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Additional Parameters:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(3, 320);
            this.organizationLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(80, 15);
            this.organizationLabel.TabIndex = 3;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Location = new System.Drawing.Point(83, 317);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(423, 20);
            this.organizationTextBox.TabIndex = 4;
            this.organizationTextBox.TextChanged += new System.EventHandler(this.OrganizationTextBox_TextChanged);
            // 
            // autoOrganizationCheckBox
            // 
            this.autoOrganizationCheckBox.AutoSize = true;
            this.autoOrganizationCheckBox.Location = new System.Drawing.Point(6, 176);
            this.autoOrganizationCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.autoOrganizationCheckBox.Name = "autoOrganizationCheckBox";
            this.autoOrganizationCheckBox.Size = new System.Drawing.Size(124, 19);
            this.autoOrganizationCheckBox.TabIndex = 8;
            this.autoOrganizationCheckBox.Text = "Auto organization";
            this.autoOrganizationCheckBox.UseVisualStyleBackColor = true;
            this.autoOrganizationCheckBox.CheckedChanged += new System.EventHandler(this.AutoOrganizationCheckBox_CheckedChanged);
            // 
            // autoOrganizationDescriptionLabel
            // 
            this.autoOrganizationDescriptionLabel.AutoSize = true;
            this.autoOrganizationDescriptionLabel.Location = new System.Drawing.Point(80, 197);
            this.autoOrganizationDescriptionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.autoOrganizationDescriptionLabel.Name = "autoOrganizationDescriptionLabel";
            this.autoOrganizationDescriptionLabel.Size = new System.Drawing.Size(426, 90);
            this.autoOrganizationDescriptionLabel.TabIndex = 9;
            this.autoOrganizationDescriptionLabel.Text = resources.GetString("autoOrganizationDescriptionLabel.Text");
            // 
            // OrganizationInfoLink
            // 
            this.OrganizationInfoLink.AutoSize = true;
            this.OrganizationInfoLink.Location = new System.Drawing.Point(80, 414);
            this.OrganizationInfoLink.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.OrganizationInfoLink.Name = "OrganizationInfoLink";
            this.OrganizationInfoLink.Size = new System.Drawing.Size(176, 15);
            this.OrganizationInfoLink.TabIndex = 5;
            this.OrganizationInfoLink.TabStop = true;
            this.OrganizationInfoLink.Text = "Learn more about organization";
            this.OrganizationInfoLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OrganizationInfoLink_LinkClicked);
            // 
            // OrgDescriptionText
            // 
            this.OrgDescriptionText.AutoSize = true;
            this.OrgDescriptionText.Location = new System.Drawing.Point(80, 339);
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
            this.additionalParamsInfoLabel.Location = new System.Drawing.Point(80, 155);
            this.additionalParamsInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.additionalParamsInfoLabel.Name = "additionalParamsInfoLabel";
            this.additionalParamsInfoLabel.Size = new System.Drawing.Size(418, 15);
            this.additionalParamsInfoLabel.TabIndex = 7;
            this.additionalParamsInfoLabel.Text = "Additional parameters used when calling the CLI, e.g. `-d` or `--exclude=bin`";
            // 
            // WebAccountSettingsLabel
            // 
            this.WebAccountSettingsLabel.AutoSize = true;
            this.WebAccountSettingsLabel.Location = new System.Drawing.Point(80, 287);
            this.WebAccountSettingsLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.WebAccountSettingsLabel.Name = "WebAccountSettingsLabel";
            this.WebAccountSettingsLabel.Size = new System.Drawing.Size(123, 15);
            this.WebAccountSettingsLabel.TabIndex = 10;
            this.WebAccountSettingsLabel.TabStop = true;
            this.WebAccountSettingsLabel.Text = "Web account settings";
            this.WebAccountSettingsLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebAccountSettingsLink_LinkClicked);
            // 
            // SnykSolutionOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.WebAccountSettingsLabel);
            this.Controls.Add(this.additionalParamsInfoLabel);
            this.Controls.Add(this.OrgDescriptionText);
            this.Controls.Add(this.OrganizationInfoLink);
            this.Controls.Add(this.autoOrganizationDescriptionLabel);
            this.Controls.Add(this.autoOrganizationCheckBox);
            this.Controls.Add(this.organizationTextBox);
            this.Controls.Add(this.organizationLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.additionalOptionsTextBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SnykSolutionOptionsUserControl";
            this.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Size = new System.Drawing.Size(4501, 1014);
            this.Load += new System.EventHandler(this.SnykProjectOptionsUserControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.LinkLabel OrganizationInfoLink;
        private System.Windows.Forms.Label OrgDescriptionText;
        private System.Windows.Forms.LinkLabel WebAccountSettingsLabel;
    }
}
