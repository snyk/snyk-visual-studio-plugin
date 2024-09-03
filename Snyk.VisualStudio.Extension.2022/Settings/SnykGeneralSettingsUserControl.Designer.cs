using System;
using System.Diagnostics;
using System.Windows.Forms;

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
            this.authType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.OrganizationInfoLink = new System.Windows.Forms.LinkLabel();
            this.OrgDescriptionText = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.resetCliPathToDefaultButton = new System.Windows.Forms.Button();
            this.CliPathBrowseButton = new System.Windows.Forms.Button();
            this.CliPathTextBox = new System.Windows.Forms.TextBox();
            this.CliPathLabel = new System.Windows.Forms.Label();
            this.ManageBinariesAutomaticallyCheckbox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
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
            this.customCliPathFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.ExecutablesGroupBox = new System.Windows.Forms.GroupBox();
            this.authMethodDescription = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.generalSettingsGroupBox.SuspendLayout();
            this.productSelectionGroupBox.SuspendLayout();
            this.userExperienceGroupBox.SuspendLayout();
            this.ExecutablesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // customEndpointTextBox
            // 
            this.customEndpointTextBox.Location = new System.Drawing.Point(129, 168);
            this.customEndpointTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.customEndpointTextBox.Name = "customEndpointTextBox";
            this.customEndpointTextBox.Size = new System.Drawing.Size(300, 20);
            this.customEndpointTextBox.TabIndex = 0;
            this.customEndpointTextBox.LostFocus += new System.EventHandler(this.CustomEndpointTextBox_LostFocus);
            this.customEndpointTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.CustomEndpointTextBox_Validating);
            // 
            // customEndpointLabel
            // 
            this.customEndpointLabel.AutoSize = true;
            this.customEndpointLabel.Location = new System.Drawing.Point(4, 171);
            this.customEndpointLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.customEndpointLabel.Name = "customEndpointLabel";
            this.customEndpointLabel.Size = new System.Drawing.Size(89, 13);
            this.customEndpointLabel.TabIndex = 1;
            this.customEndpointLabel.Text = "Custom endpoint:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(4, 217);
            this.organizationLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(69, 13);
            this.organizationLabel.TabIndex = 2;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Location = new System.Drawing.Point(129, 216);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(300, 20);
            this.organizationTextBox.TabIndex = 3;
            this.organizationTextBox.TextChanged += new System.EventHandler(this.OrganizationTextBox_TextChanged);
            // 
            // tokenLabel
            // 
            this.tokenLabel.AutoSize = true;
            this.tokenLabel.Location = new System.Drawing.Point(4, 139);
            this.tokenLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.tokenLabel.Name = "tokenLabel";
            this.tokenLabel.Size = new System.Drawing.Size(41, 13);
            this.tokenLabel.TabIndex = 4;
            this.tokenLabel.Text = "Token:";
            // 
            // tokenTextBox
            // 
            this.tokenTextBox.Location = new System.Drawing.Point(129, 136);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.PasswordChar = '*';
            this.tokenTextBox.Size = new System.Drawing.Size(300, 20);
            this.tokenTextBox.TabIndex = 5;
            this.tokenTextBox.TextChanged += new System.EventHandler(this.TokenTextBox_TextChanged);
            this.tokenTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.TokenTextBox_Validating);
            // 
            // ignoreUnknownCACheckBox
            // 
            this.ignoreUnknownCACheckBox.AutoSize = true;
            this.ignoreUnknownCACheckBox.Location = new System.Drawing.Point(129, 189);
            this.ignoreUnknownCACheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.ignoreUnknownCACheckBox.Name = "ignoreUnknownCACheckBox";
            this.ignoreUnknownCACheckBox.Size = new System.Drawing.Size(120, 17);
            this.ignoreUnknownCACheckBox.TabIndex = 6;
            this.ignoreUnknownCACheckBox.Text = "Ignore unknown CA";
            this.ignoreUnknownCACheckBox.UseVisualStyleBackColor = true;
            this.ignoreUnknownCACheckBox.CheckedChanged += new System.EventHandler(this.IgnoreUnknownCACheckBox_CheckedChanged);
            // 
            // authenticateButton
            // 
            this.authenticateButton.Location = new System.Drawing.Point(127, 104);
            this.authenticateButton.Name = "authenticateButton";
            this.authenticateButton.Size = new System.Drawing.Size(193, 26);
            this.authenticateButton.TabIndex = 7;
            this.authenticateButton.Text = "Connect Visual Studio to Snyk.io";
            this.authenticateButton.UseVisualStyleBackColor = true;
            this.authenticateButton.Click += new System.EventHandler(this.AuthenticateButton_Click);
            // 
            // authProgressBar
            // 
            this.authProgressBar.Location = new System.Drawing.Point(129, 158);
            this.authProgressBar.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.authProgressBar.MarqueeAnimationSpeed = 10;
            this.authProgressBar.Name = "authProgressBar";
            this.authProgressBar.Size = new System.Drawing.Size(300, 5);
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
            this.usageAnalyticsCheckBox.Location = new System.Drawing.Point(12, 30);
            this.usageAnalyticsCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.usageAnalyticsCheckBox.Name = "usageAnalyticsCheckBox";
            this.usageAnalyticsCheckBox.Size = new System.Drawing.Size(165, 17);
            this.usageAnalyticsCheckBox.TabIndex = 9;
            this.usageAnalyticsCheckBox.Text = "Send usage statistics to Snyk";
            this.usageAnalyticsCheckBox.UseVisualStyleBackColor = true;
            this.usageAnalyticsCheckBox.CheckedChanged += new System.EventHandler(this.UsageAnalyticsCheckBox_CheckedChanged);
            // 
            // ossEnabledCheckBox
            // 
            this.ossEnabledCheckBox.AutoSize = true;
            this.ossEnabledCheckBox.Checked = true;
            this.ossEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ossEnabledCheckBox.Location = new System.Drawing.Point(12, 30);
            this.ossEnabledCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.ossEnabledCheckBox.Name = "ossEnabledCheckBox";
            this.ossEnabledCheckBox.Size = new System.Drawing.Size(182, 17);
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
            this.codeSecurityEnabledCheckBox.Location = new System.Drawing.Point(12, 54);
            this.codeSecurityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.codeSecurityEnabledCheckBox.Name = "codeSecurityEnabledCheckBox";
            this.codeSecurityEnabledCheckBox.Size = new System.Drawing.Size(185, 17);
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
            this.codeQualityEnabledCheckBox.Location = new System.Drawing.Point(242, 54);
            this.codeQualityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.codeQualityEnabledCheckBox.Name = "codeQualityEnabledCheckBox";
            this.codeQualityEnabledCheckBox.Size = new System.Drawing.Size(145, 17);
            this.codeQualityEnabledCheckBox.TabIndex = 13;
            this.codeQualityEnabledCheckBox.Text = "Snyk Code Quality issues";
            this.codeQualityEnabledCheckBox.UseVisualStyleBackColor = true;
            this.codeQualityEnabledCheckBox.CheckedChanged += new System.EventHandler(this.CodeQualityEnabledCheckBox_CheckedChanged);
            // 
            // generalSettingsGroupBox
            // 
            this.generalSettingsGroupBox.Controls.Add(this.authMethodDescription);
            this.generalSettingsGroupBox.Controls.Add(this.authType);
            this.generalSettingsGroupBox.Controls.Add(this.label2);
            this.generalSettingsGroupBox.Controls.Add(this.OrganizationInfoLink);
            this.generalSettingsGroupBox.Controls.Add(this.OrgDescriptionText);
            this.generalSettingsGroupBox.Controls.Add(this.tokenLabel);
            this.generalSettingsGroupBox.Controls.Add(this.tokenTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.authProgressBar);
            this.generalSettingsGroupBox.Controls.Add(this.authenticateButton);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointLabel);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationLabel);
            this.generalSettingsGroupBox.Controls.Add(this.ignoreUnknownCACheckBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationTextBox);
            this.generalSettingsGroupBox.Location = new System.Drawing.Point(10, 10);
            this.generalSettingsGroupBox.Margin = new System.Windows.Forms.Padding(8);
            this.generalSettingsGroupBox.Name = "generalSettingsGroupBox";
            this.generalSettingsGroupBox.Padding = new System.Windows.Forms.Padding(2);
            this.generalSettingsGroupBox.Size = new System.Drawing.Size(560, 327);
            this.generalSettingsGroupBox.TabIndex = 17;
            this.generalSettingsGroupBox.TabStop = false;
            this.generalSettingsGroupBox.Text = "General Settings";
            // 
            // authType
            // 
            this.authType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.authType.FormattingEnabled = true;
            this.authType.Items.AddRange(new object[] {
            "OAuth",
            "Token"});
            this.authType.Location = new System.Drawing.Point(129, 26);
            this.authType.Name = "authType";
            this.authType.Size = new System.Drawing.Size(193, 21);
            this.authType.TabIndex = 13;
            this.authType.SelectionChangeCommitted += new System.EventHandler(this.authType_SelectionChangeCommitted);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 29);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = " Authentication Method:";
            // 
            // OrganizationInfoLink
            // 
            this.OrganizationInfoLink.AutoSize = true;
            this.OrganizationInfoLink.Location = new System.Drawing.Point(137, 287);
            this.OrganizationInfoLink.Name = "OrganizationInfoLink";
            this.OrganizationInfoLink.Size = new System.Drawing.Size(150, 13);
            this.OrganizationInfoLink.TabIndex = 11;
            this.OrganizationInfoLink.TabStop = true;
            this.OrganizationInfoLink.Text = "Learn more about organization";
            this.OrganizationInfoLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OrganizationInfoLink_LinkClicked);
            // 
            // OrgDescriptionText
            // 
            this.OrgDescriptionText.AutoSize = true;
            this.OrgDescriptionText.Location = new System.Drawing.Point(137, 238);
            this.OrgDescriptionText.Name = "OrgDescriptionText";
            this.OrgDescriptionText.Size = new System.Drawing.Size(376, 39);
            this.OrgDescriptionText.TabIndex = 10;
            this.OrgDescriptionText.Text = "Specify an organization slug name to run tests for that organization.\r\nIt must ma" +
    "tch the URL slug as displayed in the URL of your org in the Snyk UI:\r\nhttps://ap" +
    "p.snyk.io/org/[OrgSlugName]";
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(7, 106);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(513, 32);
            this.richTextBox1.TabIndex = 18;
            this.richTextBox1.Text = "Snyk will download, install and update the dependencies for you. If this option i" +
    "s disabled, make sure valid paths to the dependencies are provided.";
            // 
            // resetCliPathToDefaultButton
            // 
            this.resetCliPathToDefaultButton.Location = new System.Drawing.Point(208, 26);
            this.resetCliPathToDefaultButton.Name = "resetCliPathToDefaultButton";
            this.resetCliPathToDefaultButton.Size = new System.Drawing.Size(97, 23);
            this.resetCliPathToDefaultButton.TabIndex = 17;
            this.resetCliPathToDefaultButton.Text = "Reset to default";
            this.resetCliPathToDefaultButton.UseVisualStyleBackColor = true;
            this.resetCliPathToDefaultButton.Click += new System.EventHandler(this.ClearCliCustomPathButton_Click);
            // 
            // CliPathBrowseButton
            // 
            this.CliPathBrowseButton.Location = new System.Drawing.Point(127, 26);
            this.CliPathBrowseButton.Name = "CliPathBrowseButton";
            this.CliPathBrowseButton.Size = new System.Drawing.Size(75, 23);
            this.CliPathBrowseButton.TabIndex = 16;
            this.CliPathBrowseButton.Text = "Browse";
            this.CliPathBrowseButton.UseVisualStyleBackColor = true;
            this.CliPathBrowseButton.Click += new System.EventHandler(this.CliPathBrowseButton_Click);
            // 
            // CliPathTextBox
            // 
            this.CliPathTextBox.Location = new System.Drawing.Point(129, 51);
            this.CliPathTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.CliPathTextBox.Name = "CliPathTextBox";
            this.CliPathTextBox.ReadOnly = true;
            this.CliPathTextBox.Size = new System.Drawing.Size(300, 20);
            this.CliPathTextBox.TabIndex = 15;
            // 
            // CliPathLabel
            // 
            this.CliPathLabel.AutoSize = true;
            this.CliPathLabel.Location = new System.Drawing.Point(4, 31);
            this.CliPathLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.CliPathLabel.Name = "CliPathLabel";
            this.CliPathLabel.Size = new System.Drawing.Size(78, 13);
            this.CliPathLabel.TabIndex = 14;
            this.CliPathLabel.Text = "Snyk CLI Path:";
            // 
            // ManageBinariesAutomaticallyCheckbox
            // 
            this.ManageBinariesAutomaticallyCheckbox.AutoSize = true;
            this.ManageBinariesAutomaticallyCheckbox.Location = new System.Drawing.Point(12, 87);
            this.ManageBinariesAutomaticallyCheckbox.Margin = new System.Windows.Forms.Padding(2);
            this.ManageBinariesAutomaticallyCheckbox.Name = "ManageBinariesAutomaticallyCheckbox";
            this.ManageBinariesAutomaticallyCheckbox.Size = new System.Drawing.Size(15, 14);
            this.ManageBinariesAutomaticallyCheckbox.TabIndex = 13;
            this.ManageBinariesAutomaticallyCheckbox.UseVisualStyleBackColor = true;
            this.ManageBinariesAutomaticallyCheckbox.CheckedChanged += new System.EventHandler(this.ManageBinariesAutomaticallyCheckbox_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 87);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(253, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Update and install Snyk dependencies automatically";
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
            this.productSelectionGroupBox.Location = new System.Drawing.Point(10, 505);
            this.productSelectionGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.productSelectionGroupBox.Name = "productSelectionGroupBox";
            this.productSelectionGroupBox.Padding = new System.Windows.Forms.Padding(8);
            this.productSelectionGroupBox.Size = new System.Drawing.Size(560, 130);
            this.productSelectionGroupBox.TabIndex = 18;
            this.productSelectionGroupBox.TabStop = false;
            this.productSelectionGroupBox.Text = "Product Selection";
            // 
            // snykCodeQualityInfoLabel
            // 
            this.snykCodeQualityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeQualityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeQualityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeQualityInfoLabel.Image")));
            this.snykCodeQualityInfoLabel.Location = new System.Drawing.Point(384, 50);
            this.snykCodeQualityInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.snykCodeQualityInfoLabel.Name = "snykCodeQualityInfoLabel";
            this.snykCodeQualityInfoLabel.Size = new System.Drawing.Size(20, 20);
            this.snykCodeQualityInfoLabel.TabIndex = 20;
            this.snykCodeQualityInfoLabel.Text = "   ";
            this.snykCodeQualityInfoToolTip.SetToolTip(this.snykCodeQualityInfoLabel, "Find and fix code quality issues in your application code in real time");
            // 
            // snykCodeSecurityInfoLabel
            // 
            this.snykCodeSecurityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeSecurityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeSecurityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeSecurityInfoLabel.Image")));
            this.snykCodeSecurityInfoLabel.Location = new System.Drawing.Point(195, 50);
            this.snykCodeSecurityInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.snykCodeSecurityInfoLabel.Name = "snykCodeSecurityInfoLabel";
            this.snykCodeSecurityInfoLabel.Size = new System.Drawing.Size(20, 20);
            this.snykCodeSecurityInfoLabel.TabIndex = 20;
            this.snykCodeSecurityInfoLabel.Text = "    ";
            this.snykCodeSecurityInfoToolTip.SetToolTip(this.snykCodeSecurityInfoLabel, "Find and fix vulnerabilities in your application code in real time");
            // 
            // ossInfoLabel
            // 
            this.ossInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.ossInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ossInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("ossInfoLabel.Image")));
            this.ossInfoLabel.Location = new System.Drawing.Point(190, 26);
            this.ossInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ossInfoLabel.MaximumSize = new System.Drawing.Size(16, 16);
            this.ossInfoLabel.MinimumSize = new System.Drawing.Size(16, 16);
            this.ossInfoLabel.Name = "ossInfoLabel";
            this.ossInfoLabel.Size = new System.Drawing.Size(16, 16);
            this.ossInfoLabel.TabIndex = 20;
            this.ossInfoLabel.Text = "   ";
            this.ossInfoToolTip.SetToolTip(this.ossInfoLabel, "Find and automatically fix open source vulnerabilities");
            // 
            // checkAgainLinkLabel
            // 
            this.checkAgainLinkLabel.AutoSize = true;
            this.checkAgainLinkLabel.Location = new System.Drawing.Point(158, 98);
            this.checkAgainLinkLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.checkAgainLinkLabel.Name = "checkAgainLinkLabel";
            this.checkAgainLinkLabel.Size = new System.Drawing.Size(67, 13);
            this.checkAgainLinkLabel.TabIndex = 16;
            this.checkAgainLinkLabel.TabStop = true;
            this.checkAgainLinkLabel.Text = "Check again";
            this.checkAgainLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CheckAgainLinkLabel_LinkClicked);
            // 
            // snykCodeSettingsLinkLabel
            // 
            this.snykCodeSettingsLinkLabel.AutoSize = true;
            this.snykCodeSettingsLinkLabel.Location = new System.Drawing.Point(9, 98);
            this.snykCodeSettingsLinkLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.snykCodeSettingsLinkLabel.Name = "snykCodeSettingsLinkLabel";
            this.snykCodeSettingsLinkLabel.Size = new System.Drawing.Size(145, 13);
            this.snykCodeSettingsLinkLabel.TabIndex = 15;
            this.snykCodeSettingsLinkLabel.TabStop = true;
            this.snykCodeSettingsLinkLabel.Text = "Snyk > Settings > Snyk Code";
            this.snykCodeSettingsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SnykCodeSettingsLinkLabel_LinkClicked);
            // 
            // snykCodeDisabledInfoLabel
            // 
            this.snykCodeDisabledInfoLabel.AutoSize = true;
            this.snykCodeDisabledInfoLabel.Location = new System.Drawing.Point(9, 80);
            this.snykCodeDisabledInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.snykCodeDisabledInfoLabel.Name = "snykCodeDisabledInfoLabel";
            this.snykCodeDisabledInfoLabel.Size = new System.Drawing.Size(282, 13);
            this.snykCodeDisabledInfoLabel.TabIndex = 14;
            this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled by your organisation\'s configuration:";
            // 
            // userExperienceGroupBox
            // 
            this.userExperienceGroupBox.Controls.Add(this.usageAnalyticsCheckBox);
            this.userExperienceGroupBox.Location = new System.Drawing.Point(10, 643);
            this.userExperienceGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.userExperienceGroupBox.Name = "userExperienceGroupBox";
            this.userExperienceGroupBox.Padding = new System.Windows.Forms.Padding(8);
            this.userExperienceGroupBox.Size = new System.Drawing.Size(560, 60);
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
            // customCliPathFileDialog
            // 
            this.customCliPathFileDialog.SupportMultiDottedExtensions = true;
            // 
            // ExecutablesGroupBox
            // 
            this.ExecutablesGroupBox.Controls.Add(this.richTextBox1);
            this.ExecutablesGroupBox.Controls.Add(this.CliPathLabel);
            this.ExecutablesGroupBox.Controls.Add(this.resetCliPathToDefaultButton);
            this.ExecutablesGroupBox.Controls.Add(this.label1);
            this.ExecutablesGroupBox.Controls.Add(this.CliPathBrowseButton);
            this.ExecutablesGroupBox.Controls.Add(this.ManageBinariesAutomaticallyCheckbox);
            this.ExecutablesGroupBox.Controls.Add(this.CliPathTextBox);
            this.ExecutablesGroupBox.Location = new System.Drawing.Point(10, 348);
            this.ExecutablesGroupBox.Name = "ExecutablesGroupBox";
            this.ExecutablesGroupBox.Size = new System.Drawing.Size(560, 152);
            this.ExecutablesGroupBox.TabIndex = 19;
            this.ExecutablesGroupBox.TabStop = false;
            this.ExecutablesGroupBox.Text = "Executables Settings";
            // 
            // authMethodDescription
            // 
            this.authMethodDescription.BackColor = System.Drawing.SystemColors.Control;
            this.authMethodDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.authMethodDescription.Location = new System.Drawing.Point(127, 53);
            this.authMethodDescription.Name = "authMethodDescription";
            this.authMethodDescription.ReadOnly = true;
            this.authMethodDescription.Size = new System.Drawing.Size(428, 45);
            this.authMethodDescription.TabIndex = 19;
            this.authMethodDescription.Text = resources.GetString("authMethodDescription.Text");
            // 
            // SnykGeneralSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.ExecutablesGroupBox);
            this.Controls.Add(this.userExperienceGroupBox);
            this.Controls.Add(this.productSelectionGroupBox);
            this.Controls.Add(this.generalSettingsGroupBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(795, 750);
            this.Name = "SnykGeneralSettingsUserControl";
            this.Size = new System.Drawing.Size(795, 750);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.generalSettingsGroupBox.ResumeLayout(false);
            this.generalSettingsGroupBox.PerformLayout();
            this.productSelectionGroupBox.ResumeLayout(false);
            this.productSelectionGroupBox.PerformLayout();
            this.userExperienceGroupBox.ResumeLayout(false);
            this.userExperienceGroupBox.PerformLayout();
            this.ExecutablesGroupBox.ResumeLayout(false);
            this.ExecutablesGroupBox.PerformLayout();
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
        private LinkLabel OrganizationInfoLink;
        private Label OrgDescriptionText;
        private Label label1;
        private CheckBox ManageBinariesAutomaticallyCheckbox;
        private Label CliPathLabel;
        private TextBox CliPathTextBox;
        private Button CliPathBrowseButton;
        private OpenFileDialog customCliPathFileDialog;
        private Button resetCliPathToDefaultButton;
        private RichTextBox richTextBox1;
        private GroupBox ExecutablesGroupBox;
        private Label label2;
        private ComboBox authType;
        private RichTextBox authMethodDescription;
    }
}
