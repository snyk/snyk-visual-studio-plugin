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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnykScanOptionsUserControl));
            this.codeSecurityToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.codeSecurityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.productSelectionGroupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbDelta = new System.Windows.Forms.ComboBox();
            this.snykIacInfoLabel = new System.Windows.Forms.Label();
            this.iacEnabledCheckbox = new System.Windows.Forms.CheckBox();
            this.snykCodeSecurityInfoLabel = new System.Windows.Forms.Label();
            this.ossInfoLabel = new System.Windows.Forms.Label();
            this.ossEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.mainPanel.SuspendLayout();
            this.productSelectionGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // codeSecurityEnabledCheckBox
            // 
            this.codeSecurityEnabledCheckBox.AutoSize = true;
            this.codeSecurityEnabledCheckBox.Checked = true;
            this.codeSecurityEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.codeSecurityEnabledCheckBox.Location = new System.Drawing.Point(28, 173);
            this.codeSecurityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.codeSecurityEnabledCheckBox.Name = "codeSecurityEnabledCheckBox";
            this.codeSecurityEnabledCheckBox.Size = new System.Drawing.Size(860, 66);
            this.codeSecurityEnabledCheckBox.TabIndex = 12;
            this.codeSecurityEnabledCheckBox.Text = "Snyk Code Security. \r\nNote: Snyk Code scans must be enabled for the organization " +
    "to run.";
            this.codeSecurityToolTip.SetToolTip(this.codeSecurityEnabledCheckBox, "Snyk Code scans must be enabled for the organization to run.");
            this.codeSecurityEnabledCheckBox.UseVisualStyleBackColor = true;
            this.codeSecurityEnabledCheckBox.CheckedChanged += new System.EventHandler(this.CodeSecurityEnabledCheckBox_CheckedChanged);
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.productSelectionGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1606, 657);
            this.mainPanel.TabIndex = 1;
            // 
            // productSelectionGroupBox
            // 
            this.productSelectionGroupBox.Controls.Add(this.label4);
            this.productSelectionGroupBox.Controls.Add(this.label3);
            this.productSelectionGroupBox.Controls.Add(this.cbDelta);
            this.productSelectionGroupBox.Controls.Add(this.snykIacInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.iacEnabledCheckbox);
            this.productSelectionGroupBox.Controls.Add(this.snykCodeSecurityInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.ossInfoLabel);
            this.productSelectionGroupBox.Controls.Add(this.ossEnabledCheckBox);
            this.productSelectionGroupBox.Controls.Add(this.codeSecurityEnabledCheckBox);
            this.productSelectionGroupBox.Location = new System.Drawing.Point(34, 25);
            this.productSelectionGroupBox.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.productSelectionGroupBox.Name = "productSelectionGroupBox";
            this.productSelectionGroupBox.Padding = new System.Windows.Forms.Padding(22, 19, 22, 19);
            this.productSelectionGroupBox.Size = new System.Drawing.Size(1494, 431);
            this.productSelectionGroupBox.TabIndex = 20;
            this.productSelectionGroupBox.TabStop = false;
            this.productSelectionGroupBox.Text = "Issue view options";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 288);
            this.label4.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(377, 31);
            this.label4.TabIndex = 26;
            this.label4.Text = "All Issues Vs Net New Issues:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 336);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(712, 31);
            this.label3.TabIndex = 24;
            this.label3.Text = "Specifies whether to see only net new issues or all issues.";
            // 
            // cbDelta
            // 
            this.cbDelta.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDelta.FormattingEnabled = true;
            this.cbDelta.Location = new System.Drawing.Point(458, 280);
            this.cbDelta.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cbDelta.Name = "cbDelta";
            this.cbDelta.Size = new System.Drawing.Size(316, 39);
            this.cbDelta.TabIndex = 25;
            this.cbDelta.SelectionChangeCommitted += new System.EventHandler(this.cbDelta_SelectionChangeCommitted);
            // 
            // snykIacInfoLabel
            // 
            this.snykIacInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykIacInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykIacInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykIacInfoLabel.Image")));
            this.snykIacInfoLabel.Location = new System.Drawing.Point(422, 124);
            this.snykIacInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.snykIacInfoLabel.MaximumSize = new System.Drawing.Size(42, 39);
            this.snykIacInfoLabel.MinimumSize = new System.Drawing.Size(42, 39);
            this.snykIacInfoLabel.Name = "snykIacInfoLabel";
            this.snykIacInfoLabel.Size = new System.Drawing.Size(42, 39);
            this.snykIacInfoLabel.TabIndex = 22;
            this.snykIacInfoLabel.Text = "   ";
            // 
            // iacEnabledCheckbox
            // 
            this.iacEnabledCheckbox.AutoSize = true;
            this.iacEnabledCheckbox.Checked = true;
            this.iacEnabledCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.iacEnabledCheckbox.Location = new System.Drawing.Point(32, 128);
            this.iacEnabledCheckbox.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.iacEnabledCheckbox.Name = "iacEnabledCheckbox";
            this.iacEnabledCheckbox.Size = new System.Drawing.Size(384, 35);
            this.iacEnabledCheckbox.TabIndex = 21;
            this.iacEnabledCheckbox.Text = "Snyk Infrastructure as Code";
            this.iacEnabledCheckbox.UseVisualStyleBackColor = true;
            this.iacEnabledCheckbox.CheckedChanged += new System.EventHandler(this.iacEnabledCheckbox_CheckedChanged);
            // 
            // snykCodeSecurityInfoLabel
            // 
            this.snykCodeSecurityInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.snykCodeSecurityInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.snykCodeSecurityInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("snykCodeSecurityInfoLabel.Image")));
            this.snykCodeSecurityInfoLabel.Location = new System.Drawing.Point(894, 191);
            this.snykCodeSecurityInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.snykCodeSecurityInfoLabel.Name = "snykCodeSecurityInfoLabel";
            this.snykCodeSecurityInfoLabel.Size = new System.Drawing.Size(54, 48);
            this.snykCodeSecurityInfoLabel.TabIndex = 20;
            this.snykCodeSecurityInfoLabel.Text = "    ";
            // 
            // ossInfoLabel
            // 
            this.ossInfoLabel.BackColor = System.Drawing.Color.Transparent;
            this.ossInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ossInfoLabel.Image = ((System.Drawing.Image)(resources.GetObject("ossInfoLabel.Image")));
            this.ossInfoLabel.Location = new System.Drawing.Point(311, 68);
            this.ossInfoLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ossInfoLabel.MaximumSize = new System.Drawing.Size(42, 39);
            this.ossInfoLabel.MinimumSize = new System.Drawing.Size(42, 39);
            this.ossInfoLabel.Name = "ossInfoLabel";
            this.ossInfoLabel.Size = new System.Drawing.Size(42, 39);
            this.ossInfoLabel.TabIndex = 20;
            this.ossInfoLabel.Text = "   ";
            // 
            // ossEnabledCheckBox
            // 
            this.ossEnabledCheckBox.AutoSize = true;
            this.ossEnabledCheckBox.Checked = true;
            this.ossEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ossEnabledCheckBox.Location = new System.Drawing.Point(32, 72);
            this.ossEnabledCheckBox.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.ossEnabledCheckBox.Name = "ossEnabledCheckBox";
            this.ossEnabledCheckBox.Size = new System.Drawing.Size(273, 35);
            this.ossEnabledCheckBox.TabIndex = 11;
            this.ossEnabledCheckBox.Text = "Snyk Open Source";
            this.ossEnabledCheckBox.UseVisualStyleBackColor = true;
            this.ossEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OssEnabledCheckBox_CheckedChanged);
            // 
            // SnykScanOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "SnykScanOptionsUserControl";
            this.Size = new System.Drawing.Size(1606, 657);
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
        private System.Windows.Forms.Label snykCodeSecurityInfoLabel;
        private System.Windows.Forms.Label ossInfoLabel;
        private System.Windows.Forms.CheckBox ossEnabledCheckBox;
        private System.Windows.Forms.CheckBox codeSecurityEnabledCheckBox;
        private System.Windows.Forms.ToolTip codeSecurityToolTip;
    }
}
