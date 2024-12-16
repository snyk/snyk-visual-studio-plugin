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
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.ossEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.codeSecurityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.codeQualityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.generalSettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.authMethodDescription = new System.Windows.Forms.RichTextBox();
            this.authType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.OrganizationInfoLink = new System.Windows.Forms.LinkLabel();
            this.OrgDescriptionText = new System.Windows.Forms.Label();
            this.productSelectionGroupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbDelta = new System.Windows.Forms.ComboBox();
            this.ignoreGroupbox = new System.Windows.Forms.GroupBox();
            this.cbIgnoredIssues = new System.Windows.Forms.CheckBox();
            this.cbOpenIssues = new System.Windows.Forms.CheckBox();
            this.snykIacInfoLabel = new System.Windows.Forms.Label();
            this.iacEnabledCheckbox = new System.Windows.Forms.CheckBox();
            this.snykCodeQualityInfoLabel = new System.Windows.Forms.Label();
            this.snykCodeSecurityInfoLabel = new System.Windows.Forms.Label();
            this.ossInfoLabel = new System.Windows.Forms.Label();
            this.checkAgainLinkLabel = new System.Windows.Forms.LinkLabel();
            this.snykCodeSettingsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.snykCodeDisabledInfoLabel = new System.Windows.Forms.Label();
            this.userExperienceGroupBox = new System.Windows.Forms.GroupBox();
            this.autoScanCheckBox = new System.Windows.Forms.CheckBox();
            this.ossInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.snykCodeSecurityInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.snykCodeQualityInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.customCliPathFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.mainPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.generalSettingsGroupBox.SuspendLayout();
            this.productSelectionGroupBox.SuspendLayout();
            this.ignoreGroupbox.SuspendLayout();
            this.userExperienceGroupBox.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // customEndpointTextBox
            // 
            this.customEndpointTextBox.Location = new System.Drawing.Point(172, 197);
            this.customEndpointTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.customEndpointTextBox.Name = "customEndpointTextBox";
            this.customEndpointTextBox.Size = new System.Drawing.Size(399, 22);
            this.customEndpointTextBox.TabIndex = 0;
            this.customEndpointTextBox.LostFocus += new System.EventHandler(this.CustomEndpointTextBox_LostFocus);
            this.customEndpointTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.CustomEndpointTextBox_Validating);
            // 
            // customEndpointLabel
            // 
            this.customEndpointLabel.AutoSize = true;
            this.customEndpointLabel.Location = new System.Drawing.Point(5, 201);
            this.customEndpointLabel.Name = "customEndpointLabel";
            this.customEndpointLabel.Size = new System.Drawing.Size(110, 16);
            this.customEndpointLabel.TabIndex = 1;
            this.customEndpointLabel.Text = "Custom endpoint:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(5, 257);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(85, 16);
            this.organizationLabel.TabIndex = 2;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Location = new System.Drawing.Point(172, 256);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(399, 22);
            this.organizationTextBox.TabIndex = 3;
            // 
            // tokenLabel
            // 
            this.tokenLabel.AutoSize = true;
            this.tokenLabel.Location = new System.Drawing.Point(5, 161);
            this.tokenLabel.Name = "tokenLabel";
            this.tokenLabel.Size = new System.Drawing.Size(49, 16);
            this.tokenLabel.TabIndex = 4;
            this.tokenLabel.Text = "Token:";
            // 
            // tokenTextBox
            // 
            this.tokenTextBox.Location = new System.Drawing.Point(172, 158);
            this.tokenTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.PasswordChar = '*';
            this.tokenTextBox.Size = new System.Drawing.Size(399, 22);
            this.tokenTextBox.TabIndex = 5;
            this.tokenTextBox.TextChanged += new System.EventHandler(this.TokenTextBox_TextChanged);
            this.tokenTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.TokenTextBox_Validating);
            // 
            // ignoreUnknownCACheckBox
            // 
            this.ignoreUnknownCACheckBox.AutoSize = true;
            this.ignoreUnknownCACheckBox.Location = new System.Drawing.Point(172, 223);
            this.ignoreUnknownCACheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ignoreUnknownCACheckBox.Name = "ignoreUnknownCACheckBox";
            this.ignoreUnknownCACheckBox.Size = new System.Drawing.Size(143, 20);
            this.ignoreUnknownCACheckBox.TabIndex = 6;
            this.ignoreUnknownCACheckBox.Text = "Ignore unknown CA";
            this.ignoreUnknownCACheckBox.UseVisualStyleBackColor = true;
            this.ignoreUnknownCACheckBox.CheckedChanged += new System.EventHandler(this.IgnoreUnknownCACheckBox_CheckedChanged);
            // 
            // authenticateButton
            // 
            this.authenticateButton.Location = new System.Drawing.Point(169, 118);
            this.authenticateButton.Margin = new System.Windows.Forms.Padding(4);
            this.authenticateButton.Name = "authenticateButton";
            this.authenticateButton.Size = new System.Drawing.Size(257, 32);
            this.authenticateButton.TabIndex = 7;
            this.authenticateButton.Text = "Connect IDE to Snyk";
            this.authenticateButton.UseVisualStyleBackColor = true;
            this.authenticateButton.Click += new System.EventHandler(this.AuthenticateButton_Click);
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // ossEnabledCheckBox
            // 
            this.ossEnabledCheckBox.AutoSize = true;
            this.ossEnabledCheckBox.Checked = true;
            this.ossEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ossEnabledCheckBox.Location = new System.Drawing.Point(16, 37);
            this.ossEnabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ossEnabledCheckBox.Name = "ossEnabledCheckBox";
            this.ossEnabledCheckBox.Size = new System.Drawing.Size(141, 20);
            this.ossEnabledCheckBox.TabIndex = 11;
            this.ossEnabledCheckBox.Text = "Snyk Open Source";
            this.ossEnabledCheckBox.UseVisualStyleBackColor = true;
            this.ossEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OssEnabledCheckBox_CheckedChanged);
            // 
            // codeSecurityEnabledCheckBox
            // 
            this.codeSecurityEnabledCheckBox.AutoSize = true;
            this.codeSecurityEnabledCheckBox.Checked = true;
            this.codeSecurityEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.codeSecurityEnabledCheckBox.Location = new System.Drawing.Point(16, 95);
            this.codeSecurityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.codeSecurityEnabledCheckBox.Name = "codeSecurityEnabledCheckBox";
            this.codeSecurityEnabledCheckBox.Size = new System.Drawing.Size(146, 20);
            this.codeSecurityEnabledCheckBox.TabIndex = 12;
            this.codeSecurityEnabledCheckBox.Text = "Snyk Code Security";
            this.codeSecurityEnabledCheckBox.UseVisualStyleBackColor = true;
            this.codeSecurityEnabledCheckBox.CheckedChanged += new System.EventHandler(this.CodeSecurityEnabledCheckBox_CheckedChanged);
            // 
            // codeQualityEnabledCheckBox
            // 
            this.codeQualityEnabledCheckBox.AutoSize = true;
            this.codeQualityEnabledCheckBox.Checked = true;
            this.codeQualityEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.codeQualityEnabledCheckBox.Location = new System.Drawing.Point(256, 96);
            this.codeQualityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.codeQualityEnabledCheckBox.Name = "codeQualityEnabledCheckBox";
            this.codeQualityEnabledCheckBox.Size = new System.Drawing.Size(139, 20);
            this.codeQualityEnabledCheckBox.TabIndex = 13;
            this.codeQualityEnabledCheckBox.Text = "Snyk Code Quality";
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
            this.generalSettingsGroupBox.Controls.Add(this.authenticateButton);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointLabel);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationLabel);
            this.generalSettingsGroupBox.Controls.Add(this.ignoreUnknownCACheckBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationTextBox);
            this.generalSettingsGroupBox.Location = new System.Drawing.Point(29, 10);
            this.generalSettingsGroupBox.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.generalSettingsGroupBox.Name = "generalSettingsGroupBox";
            this.generalSettingsGroupBox.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.generalSettingsGroupBox.Size = new System.Drawing.Size(747, 402);
            this.generalSettingsGroupBox.TabIndex = 17;
            this.generalSettingsGroupBox.TabStop = false;
            this.generalSettingsGroupBox.Text = "General Settings";
            // 
            // authMethodDescription
            // 
            this.authMethodDescription.BackColor = System.Drawing.SystemColors.Control;
            this.authMethodDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.authMethodDescription.Location = new System.Drawing.Point(169, 65);
            this.authMethodDescription.Margin = new System.Windows.Forms.Padding(4);
            this.authMethodDescription.Name = "authMethodDescription";
            this.authMethodDescription.ReadOnly = true;
            this.authMethodDescription.Size = new System.Drawing.Size(571, 46);
            this.authMethodDescription.TabIndex = 19;
            this.authMethodDescription.Text = "Specifies whether to authenticate with OAuth2 or with an API token.\nNote: OAuth2 " +
    "authentication is recommended as it provides enhanced security.";
            // 
            // authType
            // 
            this.authType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.authType.FormattingEnabled = true;
            this.authType.Items.AddRange(new object[] {
            "OAuth",
            "Token"});
            this.authType.Location = new System.Drawing.Point(172, 32);
            this.authType.Margin = new System.Windows.Forms.Padding(4);
            this.authType.Name = "authType";
            this.authType.Size = new System.Drawing.Size(256, 24);
            this.authType.TabIndex = 13;
            this.authType.SelectionChangeCommitted += new System.EventHandler(this.authType_SelectionChangeCommitted);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(144, 16);
            this.label2.TabIndex = 12;
            this.label2.Text = " Authentication Method:";
            // 
            // OrganizationInfoLink
            // 
            this.OrganizationInfoLink.AutoSize = true;
            this.OrganizationInfoLink.Location = new System.Drawing.Point(183, 343);
            this.OrganizationInfoLink.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.OrganizationInfoLink.Name = "OrganizationInfoLink";
            this.OrganizationInfoLink.Size = new System.Drawing.Size(188, 16);
            this.OrganizationInfoLink.TabIndex = 11;
            this.OrganizationInfoLink.TabStop = true;
            this.OrganizationInfoLink.Text = "Learn more about organization";
            this.OrganizationInfoLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OrganizationInfoLink_LinkClicked);
            // 
            // OrgDescriptionText
            // 
            this.OrgDescriptionText.AutoSize = true;
            this.OrgDescriptionText.Location = new System.Drawing.Point(183, 283);
            this.OrgDescriptionText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.OrgDescriptionText.Name = "OrgDescriptionText";
            this.OrgDescriptionText.Size = new System.Drawing.Size(459, 48);
            this.OrgDescriptionText.TabIndex = 10;
            this.OrgDescriptionText.Text = "Specify an organization slug name to run tests for that organization.\r\nIt must ma" +
    "tch the URL slug as displayed in the URL of your org in the Snyk UI:\r\nhttps://ap" +
    "p.snyk.io/org/[OrgSlugName]";
            // 
            // productSelectionGroupBox
            // 
            this.productSelectionGroupBox.Controls.Add(this.label4);
            this.productSelectionGroupBox.Controls.Add(this.label3);
            this.productSelectionGroupBox.Controls.Add(this.cbDelta);
            this.productSelectionGroupBox.Controls.Add(this.ignoreGroupbox);
            this.productSelectionGroupBox.Controls.Add(this.snykIacInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.iacEnabledCheckbox);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeQualityInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeSecurityInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.ossInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.checkAgainLinkLabel);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeSettingsLinkLabel);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeDisabledInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.codeQualityEnabledCheckBox);
            this.productSelectionGroupBox.Controls.Add(this.ossEnabledCheckBox);
            this.productSelectionGroupBox.Controls.Add(this.codeSecurityEnabledCheckBox);
            this.productSelectionGroupBox.Location = new System.Drawing.Point(29, 424);
            this.productSelectionGroupBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.productSelectionGroupBox.Name = "productSelectionGroupBox";
            this.productSelectionGroupBox.Padding = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.productSelectionGroupBox.Size = new System.Drawing.Size(747, 242);
            this.productSelectionGroupBox.TabIndex = 18;
            this.productSelectionGroupBox.TabStop = false;
            this.productSelectionGroupBox.Text = "Issue view options";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 191);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(182, 16);
            this.label4.TabIndex = 26;
            this.label4.Text = "All Issues Vs Net New Issues:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 216);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(347, 16);
            this.label3.TabIndex = 24;
            this.label3.Text = "Specifies whether to see only net new issues or all issues.";
            // 
            // cbDelta
            // 
            this.cbDelta.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDelta.FormattingEnabled = true;
            this.cbDelta.Location = new System.Drawing.Point(235, 183);
            this.cbDelta.Margin = new System.Windows.Forms.Padding(4);
            this.cbDelta.Name = "cbDelta";
            this.cbDelta.Size = new System.Drawing.Size(160, 24);
            this.cbDelta.TabIndex = 25;
            this.cbDelta.SelectionChangeCommitted += new System.EventHandler(this.cbDelta_SelectionChangeCommitted);
            // 
            // ignoreGroupbox
            // 
            this.ignoreGroupbox.Controls.Add(this.cbIgnoredIssues);
            this.ignoreGroupbox.Controls.Add(this.cbOpenIssues);
            this.ignoreGroupbox.Location = new System.Drawing.Point(493, 15);
            this.ignoreGroupbox.Name = "ignoreGroupbox";
            this.ignoreGroupbox.Size = new System.Drawing.Size(240, 80);
            this.ignoreGroupbox.TabIndex = 23;
            this.ignoreGroupbox.TabStop = false;
            this.ignoreGroupbox.Text = "Show the following issues";
            // 
            // cbIgnoredIssues
            // 
            this.cbIgnoredIssues.AutoSize = true;
            this.cbIgnoredIssues.Checked = true;
            this.cbIgnoredIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbIgnoredIssues.Location = new System.Drawing.Point(21, 51);
            this.cbIgnoredIssues.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbIgnoredIssues.Name = "cbIgnoredIssues";
            this.cbIgnoredIssues.Size = new System.Drawing.Size(117, 20);
            this.cbIgnoredIssues.TabIndex = 25;
            this.cbIgnoredIssues.Text = "Ignored issues";
            this.cbIgnoredIssues.UseVisualStyleBackColor = true;
            this.cbIgnoredIssues.CheckedChanged += new System.EventHandler(this.cbIgnoredIssues_CheckedChanged);
            // 
            // cbOpenIssues
            // 
            this.cbOpenIssues.AutoSize = true;
            this.cbOpenIssues.Checked = true;
            this.cbOpenIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOpenIssues.Location = new System.Drawing.Point(21, 22);
            this.cbOpenIssues.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbOpenIssues.Name = "cbOpenIssues";
            this.cbOpenIssues.Size = new System.Drawing.Size(104, 20);
            this.cbOpenIssues.TabIndex = 24;
            this.cbOpenIssues.Text = "Open issues";
            this.cbOpenIssues.UseVisualStyleBackColor = true;
            this.cbOpenIssues.CheckedChanged += new System.EventHandler(this.cbOpenIssues_CheckedChanged);
            // 
            // snykIacInfoLabel
            // 
            this.snykIacInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykIacInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykIacInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykIacInfoLabel.Image")));
            this.snykIacInfoLabel.Location = new System.Drawing.Point(228, 66);
            this.snykIacInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.snykIacInfoLabel.MaximumSize = new System.Drawing.Size(21, 20);
            this.snykIacInfoLabel.MinimumSize = new System.Drawing.Size(21, 20);
            this.snykIacInfoLabel.Name = "snykIacInfoLabel";
            this.snykIacInfoLabel.Size = new System.Drawing.Size(21, 20);
            this.snykIacInfoLabel.TabIndex = 22;
            this.snykIacInfoLabel.Text = "   ";
            this.ossInfoToolTip.SetToolTip(this.snykIacInfoLabel, "Find and fix insecure configurations in Terraform and Kubernetes code");
            // 
            // iacEnabledCheckbox
            // 
            this.iacEnabledCheckbox.AutoSize = true;
            this.iacEnabledCheckbox.Checked = true;
            this.iacEnabledCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.iacEnabledCheckbox.Location = new System.Drawing.Point(16, 66);
            this.iacEnabledCheckbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.iacEnabledCheckbox.Name = "iacEnabledCheckbox";
            this.iacEnabledCheckbox.Size = new System.Drawing.Size(191, 20);
            this.iacEnabledCheckbox.TabIndex = 21;
            this.iacEnabledCheckbox.Text = "Snyk Infrastructure as Code";
            this.iacEnabledCheckbox.UseVisualStyleBackColor = true;
            this.iacEnabledCheckbox.CheckedChanged += new System.EventHandler(this.iacEnabledCheckbox_CheckedChanged);
            // 
            // snykCodeQualityInfoLabel
            // 
            this.snykCodeQualityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeQualityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeQualityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeQualityInfoLabel.Image")));
            this.snykCodeQualityInfoLabel.Location = new System.Drawing.Point(400, 92);
            this.snykCodeQualityInfoLabel.Name = "snykCodeQualityInfoLabel";
            this.snykCodeQualityInfoLabel.Size = new System.Drawing.Size(27, 25);
            this.snykCodeQualityInfoLabel.TabIndex = 20;
            this.snykCodeQualityInfoLabel.Text = "   ";
            this.snykCodeQualityInfoToolTip.SetToolTip(this.snykCodeQualityInfoLabel, "Find and fix code quality issues in your application code in real time");
            // 
            // snykCodeSecurityInfoLabel
            // 
            this.snykCodeSecurityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeSecurityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeSecurityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeSecurityInfoLabel.Image")));
            this.snykCodeSecurityInfoLabel.Location = new System.Drawing.Point(167, 91);
            this.snykCodeSecurityInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.snykCodeSecurityInfoLabel.Name = "snykCodeSecurityInfoLabel";
            this.snykCodeSecurityInfoLabel.Size = new System.Drawing.Size(27, 25);
            this.snykCodeSecurityInfoLabel.TabIndex = 20;
            this.snykCodeSecurityInfoLabel.Text = "    ";
            this.snykCodeSecurityInfoToolTip.SetToolTip(this.snykCodeSecurityInfoLabel, "Find and fix vulnerabilities in your application code in real time");
            // 
            // ossInfoLabel
            // 
            this.ossInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.ossInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ossInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("ossInfoLabel.Image")));
            this.ossInfoLabel.Location = new System.Drawing.Point(167, 37);
            this.ossInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ossInfoLabel.MaximumSize = new System.Drawing.Size(21, 20);
            this.ossInfoLabel.MinimumSize = new System.Drawing.Size(21, 20);
            this.ossInfoLabel.Name = "ossInfoLabel";
            this.ossInfoLabel.Size = new System.Drawing.Size(21, 20);
            this.ossInfoLabel.TabIndex = 20;
            this.ossInfoLabel.Text = "   ";
            this.ossInfoToolTip.SetToolTip(this.ossInfoLabel, "Find and automatically fix open source vulnerabilities");
            // 
            // checkAgainLinkLabel
            // 
            this.checkAgainLinkLabel.AutoSize = true;
            this.checkAgainLinkLabel.Location = new System.Drawing.Point(211, 156);
            this.checkAgainLinkLabel.Name = "checkAgainLinkLabel";
            this.checkAgainLinkLabel.Size = new System.Drawing.Size(82, 16);
            this.checkAgainLinkLabel.TabIndex = 16;
            this.checkAgainLinkLabel.TabStop = true;
            this.checkAgainLinkLabel.Text = "Check again";
            this.checkAgainLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CheckAgainLinkLabel_LinkClicked);
            // 
            // snykCodeSettingsLinkLabel
            // 
            this.snykCodeSettingsLinkLabel.AutoSize = true;
            this.snykCodeSettingsLinkLabel.Location = new System.Drawing.Point(12, 156);
            this.snykCodeSettingsLinkLabel.Name = "snykCodeSettingsLinkLabel";
            this.snykCodeSettingsLinkLabel.Size = new System.Drawing.Size(177, 16);
            this.snykCodeSettingsLinkLabel.TabIndex = 15;
            this.snykCodeSettingsLinkLabel.TabStop = true;
            this.snykCodeSettingsLinkLabel.Text = "Snyk > Settings > Snyk Code";
            this.snykCodeSettingsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SnykCodeSettingsLinkLabel_LinkClicked);
            // 
            // snykCodeDisabledInfoLabel
            // 
            this.snykCodeDisabledInfoLabel.AutoSize = true;
            this.snykCodeDisabledInfoLabel.Location = new System.Drawing.Point(12, 134);
            this.snykCodeDisabledInfoLabel.Name = "snykCodeDisabledInfoLabel";
            this.snykCodeDisabledInfoLabel.Size = new System.Drawing.Size(358, 16);
            this.snykCodeDisabledInfoLabel.TabIndex = 14;
            this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled by your organisation\'s configuration:";
            // 
            // userExperienceGroupBox
            // 
            this.userExperienceGroupBox.Controls.Add(this.autoScanCheckBox);
            this.userExperienceGroupBox.Location = new System.Drawing.Point(29, 670);
            this.userExperienceGroupBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.userExperienceGroupBox.Name = "userExperienceGroupBox";
            this.userExperienceGroupBox.Padding = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.userExperienceGroupBox.Size = new System.Drawing.Size(747, 64);
            this.userExperienceGroupBox.TabIndex = 19;
            this.userExperienceGroupBox.TabStop = false;
            this.userExperienceGroupBox.Text = "User experience";
            // 
            // autoScanCheckBox
            // 
            this.autoScanCheckBox.AutoSize = true;
            this.autoScanCheckBox.Checked = true;
            this.autoScanCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoScanCheckBox.Location = new System.Drawing.Point(16, 28);
            this.autoScanCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.autoScanCheckBox.Name = "autoScanCheckBox";
            this.autoScanCheckBox.Size = new System.Drawing.Size(266, 20);
            this.autoScanCheckBox.TabIndex = 10;
            this.autoScanCheckBox.Text = "Scan automatically on start-up and save";
            this.autoScanCheckBox.UseVisualStyleBackColor = true;
            this.autoScanCheckBox.CheckedChanged += new System.EventHandler(this.autoScanCheckBox_CheckedChanged);
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
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.generalSettingsGroupBox);
            this.mainPanel.Controls.Add(this.productSelectionGroupBox);
            this.mainPanel.Controls.Add(this.userExperienceGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1060, 923);
            this.mainPanel.TabIndex = 20;
            // 
            // SnykGeneralSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(1060, 923);
            this.Name = "SnykGeneralSettingsUserControl";
            this.Size = new System.Drawing.Size(1060, 923);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.generalSettingsGroupBox.ResumeLayout(false);
            this.generalSettingsGroupBox.PerformLayout();
            this.productSelectionGroupBox.ResumeLayout(false);
            this.productSelectionGroupBox.PerformLayout();
            this.ignoreGroupbox.ResumeLayout(false);
            this.ignoreGroupbox.PerformLayout();
            this.userExperienceGroupBox.ResumeLayout(false);
            this.userExperienceGroupBox.PerformLayout();
            this.mainPanel.ResumeLayout(false);
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
        private System.Windows.Forms.ErrorProvider errorProvider;
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
        private OpenFileDialog customCliPathFileDialog;
        private Label label2;
        private ComboBox authType;
        private RichTextBox authMethodDescription;
        private CheckBox autoScanCheckBox;
        private Label snykIacInfoLabel;
        private CheckBox iacEnabledCheckbox;
        private GroupBox ignoreGroupbox;
        private CheckBox cbIgnoredIssues;
        private CheckBox cbOpenIssues;
        private Label label3;
        private ComboBox cbDelta;
        private Label label4;
        private Panel mainPanel;
    }
}
