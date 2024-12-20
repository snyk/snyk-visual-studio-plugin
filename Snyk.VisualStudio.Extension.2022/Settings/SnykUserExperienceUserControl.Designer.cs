namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykUserExperienceUserControl
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
            this.autoScanCheckBox = new System.Windows.Forms.CheckBox();
            this.userExperienceGroupBox = new System.Windows.Forms.GroupBox();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.userExperienceGroupBox.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.SuspendLayout();
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
            // userExperienceGroupBox
            // 
            this.userExperienceGroupBox.Controls.Add(this.autoScanCheckBox);
            this.userExperienceGroupBox.Location = new System.Drawing.Point(3, 14);
            this.userExperienceGroupBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.userExperienceGroupBox.Name = "userExperienceGroupBox";
            this.userExperienceGroupBox.Padding = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.userExperienceGroupBox.Size = new System.Drawing.Size(747, 64);
            this.userExperienceGroupBox.TabIndex = 21;
            this.userExperienceGroupBox.TabStop = false;
            this.userExperienceGroupBox.Text = "User experience";
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.userExperienceGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(862, 347);
            this.mainPanel.TabIndex = 3;
            // 
            // SnykUserExperienceUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Name = "SnykUserExperienceUserControl";
            this.Size = new System.Drawing.Size(862, 347);
            this.userExperienceGroupBox.ResumeLayout(false);
            this.userExperienceGroupBox.PerformLayout();
            this.mainPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox autoScanCheckBox;
        private System.Windows.Forms.GroupBox userExperienceGroupBox;
        private System.Windows.Forms.Panel mainPanel;
    }
}
