namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykScanOptionsUserControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnykScanOptionsUserControl));
            this.mainPanel = new System.Windows.Forms.Panel();
            this.productSelectionGroupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbDelta = new System.Windows.Forms.ComboBox();
            this.snykIacInfoLabel = new System.Windows.Forms.Label();
            this.iacEnabledCheckbox = new System.Windows.Forms.CheckBox();
            this.snykCodeQualityInfoLabel = new System.Windows.Forms.Label();
            this.snykCodeSecurityInfoLabel = new System.Windows.Forms.Label();
            this.ossInfoLabel = new System.Windows.Forms.Label();
            this.checkAgainLinkLabel = new System.Windows.Forms.LinkLabel();
            this.snykCodeSettingsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.snykCodeDisabledInfoLabel = new System.Windows.Forms.Label();
            this.codeQualityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.ossEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.codeSecurityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.mainPanel.SuspendLayout();
            this.productSelectionGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.productSelectionGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(803, 339);
            this.mainPanel.TabIndex = 1;
            // 
            // productSelectionGroupBox
            // 
            this.productSelectionGroupBox.Controls.Add(this.label4);
            this.productSelectionGroupBox.Controls.Add(this.label3);
            this.productSelectionGroupBox.Controls.Add(this.cbDelta);
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
            this.productSelectionGroupBox.Location = new System.Drawing.Point(17, 13);
            this.productSelectionGroupBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.productSelectionGroupBox.Name = "productSelectionGroupBox";
            this.productSelectionGroupBox.Padding = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.productSelectionGroupBox.Size = new System.Drawing.Size(747, 242);
            this.productSelectionGroupBox.TabIndex = 20;
            this.productSelectionGroupBox.TabStop = false;
            this.productSelectionGroupBox.Text = "Issue view options";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 128);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(182, 16);
            this.label4.TabIndex = 26;
            this.label4.Text = "All Issues Vs Net New Issues:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 153);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(347, 16);
            this.label3.TabIndex = 24;
            this.label3.Text = "Specifies whether to see only net new issues or all issues.";
            // 
            // cbDelta
            // 
            this.cbDelta.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDelta.FormattingEnabled = true;
            this.cbDelta.Location = new System.Drawing.Point(229, 120);
            this.cbDelta.Margin = new System.Windows.Forms.Padding(4);
            this.cbDelta.Name = "cbDelta";
            this.cbDelta.Size = new System.Drawing.Size(160, 24);
            this.cbDelta.TabIndex = 25;
            this.cbDelta.SelectionChangeCommitted += new System.EventHandler(this.cbDelta_SelectionChangeCommitted);
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
            // 
            // checkAgainLinkLabel
            // 
            this.checkAgainLinkLabel.AutoSize = true;
            this.checkAgainLinkLabel.Location = new System.Drawing.Point(205, 217);
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
            this.snykCodeSettingsLinkLabel.Location = new System.Drawing.Point(6, 217);
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
            this.snykCodeDisabledInfoLabel.Location = new System.Drawing.Point(6, 195);
            this.snykCodeDisabledInfoLabel.Name = "snykCodeDisabledInfoLabel";
            this.snykCodeDisabledInfoLabel.Size = new System.Drawing.Size(358, 16);
            this.snykCodeDisabledInfoLabel.TabIndex = 14;
            this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled by your organisation\'s configuration:";
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
            // SnykScanOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Name = "SnykScanOptionsUserControl";
            this.Size = new System.Drawing.Size(803, 339);
            this.mainPanel.ResumeLayout(false);
            this.productSelectionGroupBox.ResumeLayout(false);
            this.productSelectionGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.GroupBox productSelectionGroupBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbDelta;
        private System.Windows.Forms.Label snykIacInfoLabel;
        private System.Windows.Forms.CheckBox iacEnabledCheckbox;
        private System.Windows.Forms.Label snykCodeQualityInfoLabel;
        private System.Windows.Forms.Label snykCodeSecurityInfoLabel;
        private System.Windows.Forms.Label ossInfoLabel;
        private System.Windows.Forms.LinkLabel checkAgainLinkLabel;
        private System.Windows.Forms.LinkLabel snykCodeSettingsLinkLabel;
        private System.Windows.Forms.Label snykCodeDisabledInfoLabel;
        private System.Windows.Forms.CheckBox codeQualityEnabledCheckBox;
        private System.Windows.Forms.CheckBox ossEnabledCheckBox;
        private System.Windows.Forms.CheckBox codeSecurityEnabledCheckBox;
    }
}
