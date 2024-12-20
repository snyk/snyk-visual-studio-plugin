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
            this.mainPanel = new System.Windows.Forms.Panel();
            this.ignoreGroupbox = new System.Windows.Forms.GroupBox();
            this.cbIgnoredIssues = new System.Windows.Forms.CheckBox();
            this.cbOpenIssues = new System.Windows.Forms.CheckBox();
            this.experimentalGroupBox.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.ignoreGroupbox.SuspendLayout();
            this.SuspendLayout();
            // 
            // experimentalGroupBox
            // 
            this.experimentalGroupBox.Controls.Add(this.ignoreGroupbox);
            this.experimentalGroupBox.Location = new System.Drawing.Point(3, 14);
            this.experimentalGroupBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.experimentalGroupBox.Name = "experimentalGroupBox";
            this.experimentalGroupBox.Padding = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.experimentalGroupBox.Size = new System.Drawing.Size(747, 180);
            this.experimentalGroupBox.TabIndex = 21;
            this.experimentalGroupBox.TabStop = false;
            this.experimentalGroupBox.Text = "Experimental";
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.experimentalGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(832, 527);
            this.mainPanel.TabIndex = 2;
            // 
            // ignoreGroupbox
            // 
            this.ignoreGroupbox.Controls.Add(this.cbIgnoredIssues);
            this.ignoreGroupbox.Controls.Add(this.cbOpenIssues);
            this.ignoreGroupbox.Location = new System.Drawing.Point(14, 28);
            this.ignoreGroupbox.Name = "ignoreGroupbox";
            this.ignoreGroupbox.Size = new System.Drawing.Size(240, 80);
            this.ignoreGroupbox.TabIndex = 24;
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
            // SnykExperimentalUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Name = "SnykExperimentalUserControl";
            this.Size = new System.Drawing.Size(832, 527);
            this.experimentalGroupBox.ResumeLayout(false);
            this.mainPanel.ResumeLayout(false);
            this.ignoreGroupbox.ResumeLayout(false);
            this.ignoreGroupbox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox experimentalGroupBox;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.GroupBox ignoreGroupbox;
        private System.Windows.Forms.CheckBox cbIgnoredIssues;
        private System.Windows.Forms.CheckBox cbOpenIssues;
    }
}
