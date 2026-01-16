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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SnykExperimentalUserControl));
            this.experimentalGroupBox = new System.Windows.Forms.GroupBox();
            this.btnOpenSettingsV2 = new System.Windows.Forms.Button();
            this.ignoreGroupbox = new System.Windows.Forms.GroupBox();
            this.filterNoteLabel = new System.Windows.Forms.Label();
            this.cbIgnoredIssues = new System.Windows.Forms.CheckBox();
            this.cbOpenIssues = new System.Windows.Forms.CheckBox();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.openIssuesToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ignoredIssuesToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.experimentalGroupBox.SuspendLayout();
            this.ignoreGroupbox.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // experimentalGroupBox
            //
            this.experimentalGroupBox.Controls.Add(this.btnOpenSettingsV2);
            this.experimentalGroupBox.Controls.Add(this.ignoreGroupbox);
            this.experimentalGroupBox.Location = new System.Drawing.Point(2, 11);
            this.experimentalGroupBox.Margin = new System.Windows.Forms.Padding(2);
            this.experimentalGroupBox.Name = "experimentalGroupBox";
            this.experimentalGroupBox.Padding = new System.Windows.Forms.Padding(8);
            this.experimentalGroupBox.Size = new System.Drawing.Size(590, 225);
            this.experimentalGroupBox.TabIndex = 21;
            this.experimentalGroupBox.TabStop = false;
            this.experimentalGroupBox.Text = "Experimental";
            //
            // btnOpenSettingsV2
            //
            this.btnOpenSettingsV2.Location = new System.Drawing.Point(10, 180);
            this.btnOpenSettingsV2.Margin = new System.Windows.Forms.Padding(2);
            this.btnOpenSettingsV2.Name = "btnOpenSettingsV2";
            this.btnOpenSettingsV2.Size = new System.Drawing.Size(150, 25);
            this.btnOpenSettingsV2.TabIndex = 25;
            this.btnOpenSettingsV2.Text = "Open settings v2 page";
            this.btnOpenSettingsV2.UseVisualStyleBackColor = true;
            this.btnOpenSettingsV2.Click += new System.EventHandler(this.btnOpenSettingsV2_Click);
            // 
            // ignoreGroupbox
            // 
            this.ignoreGroupbox.Controls.Add(this.filterNoteLabel);
            this.ignoreGroupbox.Controls.Add(this.cbIgnoredIssues);
            this.ignoreGroupbox.Controls.Add(this.cbOpenIssues);
            this.ignoreGroupbox.Location = new System.Drawing.Point(10, 23);
            this.ignoreGroupbox.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.ignoreGroupbox.Name = "ignoreGroupbox";
            this.ignoreGroupbox.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.ignoreGroupbox.Size = new System.Drawing.Size(562, 150);
            this.ignoreGroupbox.TabIndex = 24;
            this.ignoreGroupbox.TabStop = false;
            this.ignoreGroupbox.Text = "Show the following issues";
            // 
            // filterNoteLabel
            // 
            this.filterNoteLabel.AutoSize = true;
            this.filterNoteLabel.Location = new System.Drawing.Point(14, 79);
            this.filterNoteLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.filterNoteLabel.Name = "filterNoteLabel";
            this.filterNoteLabel.Size = new System.Drawing.Size(461, 52);
            this.filterNoteLabel.TabIndex = 26;
            this.filterNoteLabel.Text = resources.GetString("filterNoteLabel.Text");
            // 
            // cbIgnoredIssues
            // 
            this.cbIgnoredIssues.AutoSize = true;
            this.cbIgnoredIssues.Checked = true;
            this.cbIgnoredIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbIgnoredIssues.Location = new System.Drawing.Point(16, 54);
            this.cbIgnoredIssues.Margin = new System.Windows.Forms.Padding(2);
            this.cbIgnoredIssues.Name = "cbIgnoredIssues";
            this.cbIgnoredIssues.Size = new System.Drawing.Size(125, 17);
            this.cbIgnoredIssues.TabIndex = 25;
            this.cbIgnoredIssues.Text = "Show Ignored Issues";
            this.ignoredIssuesToolTip.SetToolTip(this.cbIgnoredIssues, resources.GetString("cbIgnoredIssues.ToolTip"));
            this.cbIgnoredIssues.UseVisualStyleBackColor = true;
            this.cbIgnoredIssues.CheckedChanged += new System.EventHandler(this.cbIgnoredIssues_CheckedChanged);
            // 
            // cbOpenIssues
            // 
            this.cbOpenIssues.AutoSize = true;
            this.cbOpenIssues.Checked = true;
            this.cbOpenIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOpenIssues.Location = new System.Drawing.Point(16, 29);
            this.cbOpenIssues.Margin = new System.Windows.Forms.Padding(2);
            this.cbOpenIssues.Name = "cbOpenIssues";
            this.cbOpenIssues.Size = new System.Drawing.Size(115, 17);
            this.cbOpenIssues.TabIndex = 24;
            this.cbOpenIssues.Text = "Show Open Issues";
            this.openIssuesToolTip.SetToolTip(this.cbOpenIssues, resources.GetString("cbOpenIssues.ToolTip"));
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
            this.mainPanel.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(624, 428);
            this.mainPanel.TabIndex = 2;
            // 
            // SnykExperimentalUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "SnykExperimentalUserControl";
            this.Size = new System.Drawing.Size(624, 428);
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
        private System.Windows.Forms.ToolTip openIssuesToolTip;
        private System.Windows.Forms.ToolTip ignoredIssuesToolTip;
        private System.Windows.Forms.Button btnOpenSettingsV2;
    }
}
