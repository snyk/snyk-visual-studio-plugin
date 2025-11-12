namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykExperimentalUserControl
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
            this.experimentalGroupBox = new System.Windows.Forms.GroupBox();
            this.ignoreGroupbox = new System.Windows.Forms.GroupBox();
            this.filterNoteLabel = new System.Windows.Forms.Label();
            this.cbIgnoredIssues = new System.Windows.Forms.CheckBox();
            this.cbOpenIssues = new System.Windows.Forms.CheckBox();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.experimentalGroupBox.SuspendLayout();
            this.ignoreGroupbox.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // experimentalGroupBox
            // 
            this.experimentalGroupBox.Controls.Add(this.ignoreGroupbox);
            this.experimentalGroupBox.Location = new System.Drawing.Point(6, 27);
            this.experimentalGroupBox.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.experimentalGroupBox.Name = "experimentalGroupBox";
            this.experimentalGroupBox.Padding = new System.Windows.Forms.Padding(22, 19, 22, 19);
            this.experimentalGroupBox.Size = new System.Drawing.Size(1614, 349);
            this.experimentalGroupBox.TabIndex = 21;
            this.experimentalGroupBox.TabStop = false;
            this.experimentalGroupBox.Text = "Experimental";
            // 
            // ignoreGroupbox
            // 
            this.ignoreGroupbox.Controls.Add(this.filterNoteLabel);
            this.ignoreGroupbox.Controls.Add(this.cbIgnoredIssues);
            this.ignoreGroupbox.Controls.Add(this.cbOpenIssues);
            this.ignoreGroupbox.Location = new System.Drawing.Point(28, 54);
            this.ignoreGroupbox.Margin = new System.Windows.Forms.Padding(6);
            this.ignoreGroupbox.Name = "ignoreGroupbox";
            this.ignoreGroupbox.Padding = new System.Windows.Forms.Padding(6);
            this.ignoreGroupbox.Size = new System.Drawing.Size(1438, 259);
            this.ignoreGroupbox.TabIndex = 24;
            this.ignoreGroupbox.TabStop = false;
            this.ignoreGroupbox.Text = "Show the following issues";
            // 
            // filterNoteLabel
            // 
            this.filterNoteLabel.AutoSize = true;
            this.filterNoteLabel.Location = new System.Drawing.Point(36, 188);
            this.filterNoteLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.filterNoteLabel.Name = "filterNoteLabel";
            this.filterNoteLabel.Size = new System.Drawing.Size(1177, 31);
            this.filterNoteLabel.TabIndex = 26;
            this.filterNoteLabel.Text = "Note: These filters will only take effect if Code Consistent Ignores is enabled f" +
    "or the organization.";
            // 
            // cbIgnoredIssues
            // 
            this.cbIgnoredIssues.AutoSize = true;
            this.cbIgnoredIssues.Checked = true;
            this.cbIgnoredIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbIgnoredIssues.Location = new System.Drawing.Point(42, 128);
            this.cbIgnoredIssues.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.cbIgnoredIssues.Name = "cbIgnoredIssues";
            this.cbIgnoredIssues.Size = new System.Drawing.Size(221, 35);
            this.cbIgnoredIssues.TabIndex = 25;
            this.cbIgnoredIssues.Text = "Show Ignored.";
            this.cbIgnoredIssues.UseVisualStyleBackColor = true;
            this.cbIgnoredIssues.CheckedChanged += new System.EventHandler(this.cbIgnoredIssues_CheckedChanged);
            // 
            // cbOpenIssues
            // 
            this.cbOpenIssues.AutoSize = true;
            this.cbOpenIssues.Checked = true;
            this.cbOpenIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOpenIssues.Location = new System.Drawing.Point(42, 70);
            this.cbOpenIssues.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.cbOpenIssues.Name = "cbOpenIssues";
            this.cbOpenIssues.Size = new System.Drawing.Size(274, 35);
            this.cbOpenIssues.TabIndex = 24;
            this.cbOpenIssues.Text = "Show Open Issues";
            this.cbOpenIssues.UseVisualStyleBackColor = true;
            this.cbOpenIssues.CheckedChanged += new System.EventHandler(this.cbOpenIssues_CheckedChanged);
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.experimentalGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Margin = new System.Windows.Forms.Padding(6);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1664, 1021);
            this.mainPanel.TabIndex = 2;
            // 
            // SnykExperimentalUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "SnykExperimentalUserControl";
            this.Size = new System.Drawing.Size(1664, 1021);
            this.experimentalGroupBox.ResumeLayout(false);
            this.ignoreGroupbox.ResumeLayout(false);
            this.ignoreGroupbox.PerformLayout();
            this.mainPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox experimentalGroupBox;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.GroupBox ignoreGroupbox;
        private System.Windows.Forms.Label filterNoteLabel;
        private System.Windows.Forms.CheckBox cbIgnoredIssues;
        private System.Windows.Forms.CheckBox cbOpenIssues;
    }
}
