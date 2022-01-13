namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykGeneralSettingsUserControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnykGeneralSettingsUserControl));
            this.customEndpointTextBox = new System.Windows.Forms.TextBox();
            this.customEndpointLabel = new System.Windows.Forms.Label();
            this.organizationLabel = new System.Windows.Forms.Label();
            this.organizationTextBox = new System.Windows.Forms.TextBox();
            this.tokenLabel = new System.Windows.Forms.Label();
            this.tokenTextBox = new System.Windows.Forms.TextBox();
            this.ignoreUnknownCACheckBox = new System.Windows.Forms.CheckBox();
            this.authenticateButton = new System.Windows.Forms.Button();
            this.authProgressBar = new System.Windows.Forms.ProgressBar();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.usageAnalyticsCheckBox = new System.Windows.Forms.CheckBox();
            this.ossEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.codeSecurityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.codeQualityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.generalSettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.productSelectionGroupBox = new System.Windows.Forms.GroupBox();
            this.snykCodeQualityInfoLabel = new System.Windows.Forms.Label();
            this.snykCodeSecurityInfoLabel = new System.Windows.Forms.Label();
            this.ossInfoLabel = new System.Windows.Forms.Label();
            this.checkAgainLinkLabel = new System.Windows.Forms.LinkLabel();
            this.snykCodeSettingsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.snykCodeDisabledInfoLabel = new System.Windows.Forms.Label();
            this.userExperienceGroupBox = new System.Windows.Forms.GroupBox();
            this.ossInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.snykCodeSecurityInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.snykCodeQualityInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.generalSettingsGroupBox.SuspendLayout();
            this.productSelectionGroupBox.SuspendLayout();
            this.userExperienceGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // customEndpointTextBox
            // 
            this.customEndpointTextBox.Location = new System.Drawing.Point(200, 165);
            this.customEndpointTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.customEndpointTextBox.Name = "customEndpointTextBox";
            this.customEndpointTextBox.Size = new System.Drawing.Size(836, 31);
            this.customEndpointTextBox.TabIndex = 0;
            this.customEndpointTextBox.TextChanged += new System.EventHandler(this.CustomEndpointTextBox_TextChanged);
            this.customEndpointTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.CustomEndpointTextBox_Validating);
            // 
            // customEndpointLabel
            // 
            this.customEndpointLabel.AutoSize = true;
            this.customEndpointLabel.Location = new System.Drawing.Point(8, 171);
            this.customEndpointLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.customEndpointLabel.Name = "customEndpointLabel";
            this.customEndpointLabel.Size = new System.Drawing.Size(180, 25);
            this.customEndpointLabel.TabIndex = 1;
            this.customEndpointLabel.Text = "Custom endpoint:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(8, 260);
            this.organizationLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(140, 25);
            this.organizationLabel.TabIndex = 2;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Location = new System.Drawing.Point(200, 258);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(836, 31);
            this.organizationTextBox.TabIndex = 3;
            this.organizationTextBox.TextChanged += new System.EventHandler(this.OrganizationTextBox_TextChanged);
            // 
            // tokenLabel
            // 
            this.tokenLabel.AutoSize = true;
            this.tokenLabel.Location = new System.Drawing.Point(8, 110);
            this.tokenLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.tokenLabel.Name = "tokenLabel";
            this.tokenLabel.Size = new System.Drawing.Size(78, 25);
            this.tokenLabel.TabIndex = 4;
            this.tokenLabel.Text = "Token:";
            // 
            // tokenTextBox
            // 
            this.tokenTextBox.Location = new System.Drawing.Point(200, 104);
            this.tokenTextBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.PasswordChar = '*';
            this.tokenTextBox.Size = new System.Drawing.Size(836, 31);
            this.tokenTextBox.TabIndex = 5;
            this.tokenTextBox.TextChanged += new System.EventHandler(this.TokenTextBox_TextChanged);
            this.tokenTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.TokenTextBox_Validating);
            // 
            // ignoreUnknownCACheckBox
            // 
            this.ignoreUnknownCACheckBox.AutoSize = true;
            this.ignoreUnknownCACheckBox.Location = new System.Drawing.Point(200, 206);
            this.ignoreUnknownCACheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ignoreUnknownCACheckBox.Name = "ignoreUnknownCACheckBox";
            this.ignoreUnknownCACheckBox.Size = new System.Drawing.Size(231, 29);
            this.ignoreUnknownCACheckBox.TabIndex = 6;
            this.ignoreUnknownCACheckBox.Text = "Ignore unknown CA";
            this.ignoreUnknownCACheckBox.UseVisualStyleBackColor = true;
            this.ignoreUnknownCACheckBox.CheckedChanged += new System.EventHandler(this.IgnoreUnknownCACheckBox_CheckedChanged);
            // 
            // authenticateButton
            // 
            this.authenticateButton.Location = new System.Drawing.Point(200, 38);
            this.authenticateButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.authenticateButton.Name = "authenticateButton";
            this.authenticateButton.Size = new System.Drawing.Size(386, 38);
            this.authenticateButton.TabIndex = 7;
            this.authenticateButton.Text = "Connect Visual Studio to Snyk.io";
            this.authenticateButton.UseVisualStyleBackColor = true;
            this.authenticateButton.Click += new System.EventHandler(this.AuthenticateButton_Click);
            // 
            // authProgressBar
            // 
            this.authProgressBar.Location = new System.Drawing.Point(200, 146);
            this.authProgressBar.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.authProgressBar.MarqueeAnimationSpeed = 10;
            this.authProgressBar.Name = "authProgressBar";
            this.authProgressBar.Size = new System.Drawing.Size(840, 10);
            this.authProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.authProgressBar.TabIndex = 8;
            this.authProgressBar.Visible = false;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // usageAnalyticsCheckBox
            // 
            this.usageAnalyticsCheckBox.AutoSize = true;
            this.usageAnalyticsCheckBox.Checked = true;
            this.usageAnalyticsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.usageAnalyticsCheckBox.Location = new System.Drawing.Point(24, 58);
            this.usageAnalyticsCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.usageAnalyticsCheckBox.Name = "usageAnalyticsCheckBox";
            this.usageAnalyticsCheckBox.Size = new System.Drawing.Size(250, 29);
            this.usageAnalyticsCheckBox.TabIndex = 9;
            this.usageAnalyticsCheckBox.Text = "Send usage analytics";
            this.usageAnalyticsCheckBox.UseVisualStyleBackColor = true;
            this.usageAnalyticsCheckBox.CheckedChanged += new System.EventHandler(this.UsageAnalyticsCheckBox_CheckedChanged);
            // 
            // ossEnabledCheckBox
            // 
            this.ossEnabledCheckBox.AutoSize = true;
            this.ossEnabledCheckBox.Checked = true;
            this.ossEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ossEnabledCheckBox.Location = new System.Drawing.Point(24, 58);
            this.ossEnabledCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ossEnabledCheckBox.Name = "ossEnabledCheckBox";
            this.ossEnabledCheckBox.Size = new System.Drawing.Size(362, 29);
            this.ossEnabledCheckBox.TabIndex = 11;
            this.ossEnabledCheckBox.Text = "Snyk Open Source vulnerabilities";
            this.ossEnabledCheckBox.UseVisualStyleBackColor = true;
            this.ossEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OssEnabledCheckBox_CheckedChanged);
            // 
            // codeSecurityEnabledCheckBox
            // 
            this.codeSecurityEnabledCheckBox.AutoSize = true;
            this.codeSecurityEnabledCheckBox.Checked = true;
            this.codeSecurityEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.codeSecurityEnabledCheckBox.Location = new System.Drawing.Point(24, 104);
            this.codeSecurityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.codeSecurityEnabledCheckBox.Name = "codeSecurityEnabledCheckBox";
            this.codeSecurityEnabledCheckBox.Size = new System.Drawing.Size(371, 29);
            this.codeSecurityEnabledCheckBox.TabIndex = 12;
            this.codeSecurityEnabledCheckBox.Text = "Snyk Code Security vulnerabilities";
            this.codeSecurityEnabledCheckBox.UseVisualStyleBackColor = true;
            this.codeSecurityEnabledCheckBox.CheckedChanged += new System.EventHandler(this.CodeSecurityEnabledCheckBox_CheckedChanged);
            // 
            // codeQualityEnabledCheckBox
            // 
            this.codeQualityEnabledCheckBox.AutoSize = true;
            this.codeQualityEnabledCheckBox.Checked = true;
            this.codeQualityEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.codeQualityEnabledCheckBox.Location = new System.Drawing.Point(484, 104);
            this.codeQualityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.codeQualityEnabledCheckBox.Name = "codeQualityEnabledCheckBox";
            this.codeQualityEnabledCheckBox.Size = new System.Drawing.Size(290, 29);
            this.codeQualityEnabledCheckBox.TabIndex = 13;
            this.codeQualityEnabledCheckBox.Text = "Snyk Code Quality issues";
            this.codeQualityEnabledCheckBox.UseVisualStyleBackColor = true;
            this.codeQualityEnabledCheckBox.CheckedChanged += new System.EventHandler(this.CodeQualityEnabledCheckBox_CheckedChanged);
            // 
            // generalSettingsGroupBox
            // 
            this.generalSettingsGroupBox.Controls.Add(this.tokenLabel);
            this.generalSettingsGroupBox.Controls.Add(this.tokenTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.authProgressBar);
            this.generalSettingsGroupBox.Controls.Add(this.authenticateButton);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointLabel);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationLabel);
            this.generalSettingsGroupBox.Controls.Add(this.ignoreUnknownCACheckBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationTextBox);
            this.generalSettingsGroupBox.Location = new System.Drawing.Point(20, 19);
            this.generalSettingsGroupBox.Margin = new System.Windows.Forms.Padding(16, 15, 16, 15);
            this.generalSettingsGroupBox.Name = "generalSettingsGroupBox";
            this.generalSettingsGroupBox.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.generalSettingsGroupBox.Size = new System.Drawing.Size(1550, 327);
            this.generalSettingsGroupBox.TabIndex = 17;
            this.generalSettingsGroupBox.TabStop = false;
            this.generalSettingsGroupBox.Text = "General Settings";
            // 
            // productSelectionGroupBox
            // 
            this.productSelectionGroupBox.Controls.Add(this.snykCodeQualityInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeSecurityInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.ossInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.checkAgainLinkLabel);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeSettingsLinkLabel);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeDisabledInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.codeQualityEnabledCheckBox);
            this.productSelectionGroupBox.Controls.Add(this.ossEnabledCheckBox);
            this.productSelectionGroupBox.Controls.Add(this.codeSecurityEnabledCheckBox);
            this.productSelectionGroupBox.Location = new System.Drawing.Point(20, 385);
            this.productSelectionGroupBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.productSelectionGroupBox.Name = "productSelectionGroupBox";
            this.productSelectionGroupBox.Padding = new System.Windows.Forms.Padding(16, 15, 16, 15);
            this.productSelectionGroupBox.Size = new System.Drawing.Size(1550, 250);
            this.productSelectionGroupBox.TabIndex = 18;
            this.productSelectionGroupBox.TabStop = false;
            this.productSelectionGroupBox.Text = "Product Selection";
            // 
            // snykCodeQualityInfoLabel
            // 
            this.snykCodeQualityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeQualityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeQualityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeQualityInfoLabel.Image")));
            this.snykCodeQualityInfoLabel.Location = new System.Drawing.Point(768, 96);
            this.snykCodeQualityInfoLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.snykCodeQualityInfoLabel.Name = "snykCodeQualityInfoLabel";
            this.snykCodeQualityInfoLabel.Size = new System.Drawing.Size(40, 38);
            this.snykCodeQualityInfoLabel.TabIndex = 20;
            this.snykCodeQualityInfoLabel.Text = "   ";
            this.snykCodeQualityInfoToolTip.SetToolTip(this.snykCodeQualityInfoLabel, "Find and fix code quality issues in your application code in real time");
            // 
            // snykCodeSecurityInfoLabel
            // 
            this.snykCodeSecurityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeSecurityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeSecurityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeSecurityInfoLabel.Image")));
            this.snykCodeSecurityInfoLabel.Location = new System.Drawing.Point(390, 96);
            this.snykCodeSecurityInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.snykCodeSecurityInfoLabel.Name = "snykCodeSecurityInfoLabel";
            this.snykCodeSecurityInfoLabel.Size = new System.Drawing.Size(40, 38);
            this.snykCodeSecurityInfoLabel.TabIndex = 20;
            this.snykCodeSecurityInfoLabel.Text = "    ";
            this.snykCodeSecurityInfoToolTip.SetToolTip(this.snykCodeSecurityInfoLabel, "Find and fix vulnerabilities in your application code in real time");
            // 
            // ossInfoLabel
            // 
            this.ossInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.ossInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ossInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("ossInfoLabel.Image")));
            this.ossInfoLabel.Location = new System.Drawing.Point(380, 50);
            this.ossInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ossInfoLabel.MaximumSize = new System.Drawing.Size(32, 31);
            this.ossInfoLabel.MinimumSize = new System.Drawing.Size(32, 31);
            this.ossInfoLabel.Name = "ossInfoLabel";
            this.ossInfoLabel.Size = new System.Drawing.Size(32, 31);
            this.ossInfoLabel.TabIndex = 20;
            this.ossInfoLabel.Text = "   ";
            this.ossInfoToolTip.SetToolTip(this.ossInfoLabel, "Find and automatically fix open source vulnerabilities");
            // 
            // checkAgainLinkLabel
            // 
            this.checkAgainLinkLabel.AutoSize = true;
            this.checkAgainLinkLabel.Location = new System.Drawing.Point(316, 188);
            this.checkAgainLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.checkAgainLinkLabel.Name = "checkAgainLinkLabel";
            this.checkAgainLinkLabel.Size = new System.Drawing.Size(132, 25);
            this.checkAgainLinkLabel.TabIndex = 16;
            this.checkAgainLinkLabel.TabStop = true;
            this.checkAgainLinkLabel.Text = "Check again";
            this.checkAgainLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CheckAgainLinkLabel_LinkClicked);
            // 
            // snykCodeSettingsLinkLabel
            // 
            this.snykCodeSettingsLinkLabel.AutoSize = true;
            this.snykCodeSettingsLinkLabel.Location = new System.Drawing.Point(18, 188);
            this.snykCodeSettingsLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.snykCodeSettingsLinkLabel.Name = "snykCodeSettingsLinkLabel";
            this.snykCodeSettingsLinkLabel.Size = new System.Drawing.Size(291, 25);
            this.snykCodeSettingsLinkLabel.TabIndex = 15;
            this.snykCodeSettingsLinkLabel.TabStop = true;
            this.snykCodeSettingsLinkLabel.Text = "Snyk > Settings > Snyk Code";
            this.snykCodeSettingsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SnykCodeSettingsLinkLabel_LinkClicked);
            // 
            // snykCodeDisabledInfoLabel
            // 
            this.snykCodeDisabledInfoLabel.AutoSize = true;
            this.snykCodeDisabledInfoLabel.Location = new System.Drawing.Point(18, 154);
            this.snykCodeDisabledInfoLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.snykCodeDisabledInfoLabel.Name = "snykCodeDisabledInfoLabel";
            this.snykCodeDisabledInfoLabel.Size = new System.Drawing.Size(578, 25);
            this.snykCodeDisabledInfoLabel.TabIndex = 14;
            this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled by your organisation\'s configuration:";
            // 
            // userExperienceGroupBox
            // 
            this.userExperienceGroupBox.Controls.Add(this.usageAnalyticsCheckBox);
            this.userExperienceGroupBox.Location = new System.Drawing.Point(20, 673);
            this.userExperienceGroupBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userExperienceGroupBox.Name = "userExperienceGroupBox";
            this.userExperienceGroupBox.Padding = new System.Windows.Forms.Padding(16, 15, 16, 15);
            this.userExperienceGroupBox.Size = new System.Drawing.Size(1550, 115);
            this.userExperienceGroupBox.TabIndex = 19;
            this.userExperienceGroupBox.TabStop = false;
            this.userExperienceGroupBox.Text = "User experience";
            // 
            // ossInfoToolTip
            // 
            this.ossInfoToolTip.IsBalloon = true;
            this.ossInfoToolTip.ShowAlways = true;
            // 
            // snykCodeSecurityInfoToolTip
            // 
            this.snykCodeSecurityInfoToolTip.IsBalloon = true;
            this.snykCodeSecurityInfoToolTip.ShowAlways = true;
            // 
            // snykCodeQualityInfoToolTip
            // 
            this.snykCodeQualityInfoToolTip.IsBalloon = true;
            this.snykCodeQualityInfoToolTip.ShowAlways = true;
            // 
            // SnykGeneralSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.userExperienceGroupBox);
            this.Controls.Add(this.productSelectionGroupBox);
            this.Controls.Add(this.generalSettingsGroupBox);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(1590, 1442);
            this.Name = "SnykGeneralSettingsUserControl";
            this.Size = new System.Drawing.Size(1590, 1442);
            this.Load += new System.EventHandler(this.SnykGeneralSettingsUserControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.generalSettingsGroupBox.ResumeLayout(false);
            this.generalSettingsGroupBox.PerformLayout();
            this.productSelectionGroupBox.ResumeLayout(false);
            this.productSelectionGroupBox.PerformLayout();
            this.userExperienceGroupBox.ResumeLayout(false);
            this.userExperienceGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox customEndpointTextBox;
        private System.Windows.Forms.Label customEndpointLabel;
        private System.Windows.Forms.Label organizationLabel;
        private System.Windows.Forms.TextBox organizationTextBox;
        private System.Windows.Forms.Label tokenLabel;
        private System.Windows.Forms.TextBox tokenTextBox;
        private System.Windows.Forms.CheckBox ignoreUnknownCACheckBox;
        private System.Windows.Forms.Button authenticateButton;
        private System.Windows.Forms.ProgressBar authProgressBar;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.CheckBox usageAnalyticsCheckBox;
        private System.Windows.Forms.CheckBox codeQualityEnabledCheckBox;
        private System.Windows.Forms.CheckBox codeSecurityEnabledCheckBox;
        private System.Windows.Forms.CheckBox ossEnabledCheckBox;
        private System.Windows.Forms.GroupBox generalSettingsGroupBox;
        private System.Windows.Forms.GroupBox userExperienceGroupBox;
        private System.Windows.Forms.GroupBox productSelectionGroupBox;
        private System.Windows.Forms.LinkLabel checkAgainLinkLabel;
        private System.Windows.Forms.LinkLabel snykCodeSettingsLinkLabel;
        private System.Windows.Forms.Label snykCodeDisabledInfoLabel;
        private System.Windows.Forms.Label ossInfoLabel;
        private System.Windows.Forms.Label snykCodeSecurityInfoLabel;
        private System.Windows.Forms.ToolTip ossInfoToolTip;
        private System.Windows.Forms.ToolTip snykCodeSecurityInfoToolTip;
        private System.Windows.Forms.Label snykCodeQualityInfoLabel;
        private System.Windows.Forms.ToolTip snykCodeQualityInfoToolTip;
    }
}
