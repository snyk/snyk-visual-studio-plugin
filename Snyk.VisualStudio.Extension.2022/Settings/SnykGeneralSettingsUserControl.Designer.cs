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
            this.generalSettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.SnykRegionsLink = new System.Windows.Forms.LinkLabel();
            this.endpointDescriptionText = new System.Windows.Forms.RichTextBox();
            this.authMethodDescription = new System.Windows.Forms.RichTextBox();
            this.authType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ossInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.snykCodeSecurityInfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.customCliPathFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.mainPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.generalSettingsGroupBox.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // customEndpointTextBox
            // 
            this.customEndpointTextBox.Location = new System.Drawing.Point(169, 274);
            this.customEndpointTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.customEndpointTextBox.Name = "customEndpointTextBox";
            this.customEndpointTextBox.Size = new System.Drawing.Size(399, 20);
            this.customEndpointTextBox.TabIndex = 0;
            this.customEndpointTextBox.LostFocus += new System.EventHandler(this.CustomEndpointTextBox_LostFocus);
            this.customEndpointTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.CustomEndpointTextBox_Validating);
            // 
            // customEndpointLabel
            // 
            this.customEndpointLabel.AutoSize = true;
            this.customEndpointLabel.Location = new System.Drawing.Point(5, 276);
            this.customEndpointLabel.Name = "customEndpointLabel";
            this.customEndpointLabel.Size = new System.Drawing.Size(89, 13);
            this.customEndpointLabel.TabIndex = 1;
            this.customEndpointLabel.Text = "Custom endpoint:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(5, 331);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(69, 13);
            this.organizationLabel.TabIndex = 2;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Enabled = false;
            this.organizationTextBox.Location = new System.Drawing.Point(169, 331);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(399, 20);
            this.organizationTextBox.TabIndex = 3;
            this.organizationTextBox.Text = resources.GetString("organizationTextBox.Text");
            // 
            // tokenLabel
            // 
            this.tokenLabel.AutoSize = true;
            this.tokenLabel.Location = new System.Drawing.Point(5, 161);
            this.tokenLabel.Name = "tokenLabel";
            this.tokenLabel.Size = new System.Drawing.Size(41, 13);
            this.tokenLabel.TabIndex = 4;
            this.tokenLabel.Text = "Token:";
            // 
            // tokenTextBox
            // 
            this.tokenTextBox.Location = new System.Drawing.Point(169, 158);
            this.tokenTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.PasswordChar = '*';
            this.tokenTextBox.Size = new System.Drawing.Size(399, 20);
            this.tokenTextBox.TabIndex = 5;
            this.tokenTextBox.TextChanged += new System.EventHandler(this.TokenTextBox_TextChanged);
            this.tokenTextBox.Validating += new System.ComponentModel.CancelEventHandler(this.TokenTextBox_Validating);
            // 
            // ignoreUnknownCACheckBox
            // 
            this.ignoreUnknownCACheckBox.AutoSize = true;
            this.ignoreUnknownCACheckBox.Location = new System.Drawing.Point(172, 298);
            this.ignoreUnknownCACheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ignoreUnknownCACheckBox.Name = "ignoreUnknownCACheckBox";
            this.ignoreUnknownCACheckBox.Size = new System.Drawing.Size(120, 17);
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
            // generalSettingsGroupBox
            // 
            this.generalSettingsGroupBox.Controls.Add(this.SnykRegionsLink);
            this.generalSettingsGroupBox.Controls.Add(this.endpointDescriptionText);
            this.generalSettingsGroupBox.Controls.Add(this.authMethodDescription);
            this.generalSettingsGroupBox.Controls.Add(this.authType);
            this.generalSettingsGroupBox.Controls.Add(this.label2);
            this.generalSettingsGroupBox.Controls.Add(this.tokenLabel);
            this.generalSettingsGroupBox.Controls.Add(this.tokenTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.authenticateButton);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointLabel);
            this.generalSettingsGroupBox.Controls.Add(this.customEndpointTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.organizationLabel);
            this.generalSettingsGroupBox.Controls.Add(this.organizationTextBox);
            this.generalSettingsGroupBox.Controls.Add(this.ignoreUnknownCACheckBox);
            this.generalSettingsGroupBox.Location = new System.Drawing.Point(10, 10);
            this.generalSettingsGroupBox.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.generalSettingsGroupBox.Name = "generalSettingsGroupBox";
            this.generalSettingsGroupBox.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.generalSettingsGroupBox.Size = new System.Drawing.Size(747, 427);
            this.generalSettingsGroupBox.TabIndex = 17;
            this.generalSettingsGroupBox.TabStop = false;
            this.generalSettingsGroupBox.Text = "General Settings";
            // 
            // SnykRegionsLink
            // 
            this.SnykRegionsLink.AutoSize = true;
            this.SnykRegionsLink.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SnykRegionsLink.Location = new System.Drawing.Point(175, 256);
            this.SnykRegionsLink.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SnykRegionsLink.Name = "SnykRegionsLink";
            this.SnykRegionsLink.Size = new System.Drawing.Size(114, 13);
            this.SnykRegionsLink.TabIndex = 21;
            this.SnykRegionsLink.TabStop = true;
            this.SnykRegionsLink.Text = "Available Snyk regions";
            this.SnykRegionsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SnykRegionsLink_LinkClicked);
            // 
            // endpointDescriptionText
            // 
            this.endpointDescriptionText.BackColor = System.Drawing.SystemColors.Control;
            this.endpointDescriptionText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.endpointDescriptionText.Location = new System.Drawing.Point(169, 186);
            this.endpointDescriptionText.Margin = new System.Windows.Forms.Padding(4);
            this.endpointDescriptionText.Name = "endpointDescriptionText";
            this.endpointDescriptionText.ReadOnly = true;
            this.endpointDescriptionText.Size = new System.Drawing.Size(571, 68);
            this.endpointDescriptionText.TabIndex = 20;
            this.endpointDescriptionText.Text = resources.GetString("endpointDescriptionText.Text");
            // 
            // authMethodDescription
            // 
            this.authMethodDescription.BackColor = System.Drawing.SystemColors.Control;
            this.authMethodDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.authMethodDescription.Location = new System.Drawing.Point(169, 64);
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
            this.authType.Location = new System.Drawing.Point(169, 33);
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
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = " Authentication Method:";
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
            // customCliPathFileDialog
            // 
            this.customCliPathFileDialog.SupportMultiDottedExtensions = true;
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.generalSettingsGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(789, 449);
            this.mainPanel.TabIndex = 20;
            // 
            // SnykGeneralSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "SnykGeneralSettingsUserControl";
            this.Size = new System.Drawing.Size(789, 449);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.generalSettingsGroupBox.ResumeLayout(false);
            this.generalSettingsGroupBox.PerformLayout();
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
        private System.Windows.Forms.GroupBox generalSettingsGroupBox;
        private System.Windows.Forms.ToolTip ossInfoToolTip;
        private System.Windows.Forms.ToolTip snykCodeSecurityInfoToolTip;
        private OpenFileDialog customCliPathFileDialog;
        private Label label2;
        private ComboBox authType;
        private RichTextBox authMethodDescription;
        private Panel mainPanel;
        private RichTextBox endpointDescriptionText;
        private LinkLabel SnykRegionsLink;
    }
}
