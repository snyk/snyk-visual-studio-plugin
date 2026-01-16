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
            this.codeSecurityEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.productSelectionGroupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbDelta = new System.Windows.Forms.ComboBox();
            this.iacEnabledCheckbox = new System.Windows.Forms.CheckBox();
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
            this.codeSecurityEnabledCheckBox.Location = new System.Drawing.Point(28, 162);
            this.codeSecurityEnabledCheckBox.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.codeSecurityEnabledCheckBox.Name = "codeSecurityEnabledCheckBox";
            this.codeSecurityEnabledCheckBox.Size = new System.Drawing.Size(996, 62);
            this.codeSecurityEnabledCheckBox.TabIndex = 12;
            this.codeSecurityEnabledCheckBox.Text = "Snyk Code Security. \r\nFor these scans to run, Snyk Code must be enabled for your " +
    "organization in Snyk settings.";
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
            this.mainPanel.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1405, 615);
            this.mainPanel.TabIndex = 1;
            // 
            // productSelectionGroupBox
            // 
            this.productSelectionGroupBox.Controls.Add(this.label4);
            this.productSelectionGroupBox.Controls.Add(this.label3);
            this.productSelectionGroupBox.Controls.Add(this.cbDelta);
            this.productSelectionGroupBox.Controls.Add(this.iacEnabledCheckbox);
            this.productSelectionGroupBox.Controls.Add(this.ossEnabledCheckBox);
            this.productSelectionGroupBox.Controls.Add(this.codeSecurityEnabledCheckBox);
            this.productSelectionGroupBox.Location = new System.Drawing.Point(30, 23);
            this.productSelectionGroupBox.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.productSelectionGroupBox.Name = "productSelectionGroupBox";
            this.productSelectionGroupBox.Padding = new System.Windows.Forms.Padding(19, 18, 19, 18);
            this.productSelectionGroupBox.Size = new System.Drawing.Size(1307, 403);
            this.productSelectionGroupBox.TabIndex = 20;
            this.productSelectionGroupBox.TabStop = false;
            this.productSelectionGroupBox.Text = "Issue view options";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 269);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(329, 29);
            this.label4.TabIndex = 26;
            this.label4.Text = "All Issues Vs Net New Issues:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 314);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(632, 29);
            this.label3.TabIndex = 24;
            this.label3.Text = "Specifies whether to see only net new issues or all issues.";
            // 
            // cbDelta
            // 
            this.cbDelta.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDelta.FormattingEnabled = true;
            this.cbDelta.Location = new System.Drawing.Point(401, 262);
            this.cbDelta.Margin = new System.Windows.Forms.Padding(7);
            this.cbDelta.Name = "cbDelta";
            this.cbDelta.Size = new System.Drawing.Size(277, 37);
            this.cbDelta.TabIndex = 25;
            this.cbDelta.SelectionChangeCommitted += new System.EventHandler(this.cbDelta_SelectionChangeCommitted);
            // 
            // iacEnabledCheckbox
            // 
            this.iacEnabledCheckbox.AutoSize = true;
            this.iacEnabledCheckbox.Checked = true;
            this.iacEnabledCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.iacEnabledCheckbox.Location = new System.Drawing.Point(28, 120);
            this.iacEnabledCheckbox.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.iacEnabledCheckbox.Name = "iacEnabledCheckbox";
            this.iacEnabledCheckbox.Size = new System.Drawing.Size(337, 33);
            this.iacEnabledCheckbox.TabIndex = 21;
            this.iacEnabledCheckbox.Text = "Snyk Infrastructure as Code";
            this.iacEnabledCheckbox.UseVisualStyleBackColor = true;
            this.iacEnabledCheckbox.CheckedChanged += new System.EventHandler(this.iacEnabledCheckbox_CheckedChanged);
            // 
            // ossEnabledCheckBox
            // 
            this.ossEnabledCheckBox.AutoSize = true;
            this.ossEnabledCheckBox.Checked = true;
            this.ossEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ossEnabledCheckBox.Location = new System.Drawing.Point(28, 67);
            this.ossEnabledCheckBox.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.ossEnabledCheckBox.Name = "ossEnabledCheckBox";
            this.ossEnabledCheckBox.Size = new System.Drawing.Size(246, 33);
            this.ossEnabledCheckBox.TabIndex = 11;
            this.ossEnabledCheckBox.Text = "Snyk Open Source";
            this.ossEnabledCheckBox.UseVisualStyleBackColor = true;
            this.ossEnabledCheckBox.CheckedChanged += new System.EventHandler(this.OssEnabledCheckBox_CheckedChanged);
            // 
            // SnykScanOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Name = "SnykScanOptionsUserControl";
            this.Size = new System.Drawing.Size(1405, 615);
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
        private System.Windows.Forms.CheckBox iacEnabledCheckbox;
        private System.Windows.Forms.CheckBox ossEnabledCheckBox;
        private System.Windows.Forms.CheckBox codeSecurityEnabledCheckBox;
    }
}
