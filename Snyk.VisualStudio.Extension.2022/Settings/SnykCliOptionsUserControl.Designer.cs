namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykCliOptionsUserControl
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
            this.mainPanel = new System.Windows.Forms.Panel();
            this.ExecutablesGroupBox = new System.Windows.Forms.GroupBox();
            this.ReleaseChannelLink = new System.Windows.Forms.LinkLabel();
            this.releaseChannel = new System.Windows.Forms.ComboBox();
            this.cliReleaseChannelLabel = new System.Windows.Forms.Label();
            this.cliBaseDownloadUrl = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.cliDownloadUrlTextBox = new System.Windows.Forms.TextBox();
            this.CliPathLabel = new System.Windows.Forms.Label();
            this.resetCliPathToDefaultButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.CliPathBrowseButton = new System.Windows.Forms.Button();
            this.manageBinariesAutomaticallyCheckbox = new System.Windows.Forms.CheckBox();
            this.CliPathTextBox = new System.Windows.Forms.TextBox();
            this.customCliPathFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.mainPanel.SuspendLayout();
            this.ExecutablesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.ExecutablesGroupBox);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(803, 339);
            this.mainPanel.TabIndex = 0;
            // 
            // ExecutablesGroupBox
            // 
            this.ExecutablesGroupBox.Controls.Add(this.ReleaseChannelLink);
            this.ExecutablesGroupBox.Controls.Add(this.releaseChannel);
            this.ExecutablesGroupBox.Controls.Add(this.cliReleaseChannelLabel);
            this.ExecutablesGroupBox.Controls.Add(this.cliBaseDownloadUrl);
            this.ExecutablesGroupBox.Controls.Add(this.richTextBox1);
            this.ExecutablesGroupBox.Controls.Add(this.cliDownloadUrlTextBox);
            this.ExecutablesGroupBox.Controls.Add(this.CliPathLabel);
            this.ExecutablesGroupBox.Controls.Add(this.resetCliPathToDefaultButton);
            this.ExecutablesGroupBox.Controls.Add(this.label1);
            this.ExecutablesGroupBox.Controls.Add(this.CliPathBrowseButton);
            this.ExecutablesGroupBox.Controls.Add(this.manageBinariesAutomaticallyCheckbox);
            this.ExecutablesGroupBox.Controls.Add(this.CliPathTextBox);
            this.ExecutablesGroupBox.Location = new System.Drawing.Point(11, 10);
            this.ExecutablesGroupBox.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.ExecutablesGroupBox.Name = "ExecutablesGroupBox";
            this.ExecutablesGroupBox.Padding = new System.Windows.Forms.Padding(4);
            this.ExecutablesGroupBox.Size = new System.Drawing.Size(729, 261);
            this.ExecutablesGroupBox.TabIndex = 20;
            this.ExecutablesGroupBox.TabStop = false;
            this.ExecutablesGroupBox.Text = "CLI Settings";
            // 
            // ReleaseChannelLink
            // 
            this.ReleaseChannelLink.AutoSize = true;
            this.ReleaseChannelLink.Location = new System.Drawing.Point(8, 241);
            this.ReleaseChannelLink.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ReleaseChannelLink.Name = "ReleaseChannelLink";
            this.ReleaseChannelLink.Size = new System.Drawing.Size(219, 16);
            this.ReleaseChannelLink.TabIndex = 20;
            this.ReleaseChannelLink.TabStop = true;
            this.ReleaseChannelLink.Text = "Find out about our release channels";
            this.ReleaseChannelLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ReleaseChannelLink_LinkClicked);
            // 
            // releaseChannel
            // 
            this.releaseChannel.FormattingEnabled = true;
            this.releaseChannel.Location = new System.Drawing.Point(239, 208);
            this.releaseChannel.Margin = new System.Windows.Forms.Padding(4);
            this.releaseChannel.Name = "releaseChannel";
            this.releaseChannel.Size = new System.Drawing.Size(160, 24);
            this.releaseChannel.TabIndex = 23;
            this.releaseChannel.SelectionChangeCommitted += new System.EventHandler(this.releaseChannel_SelectionChangeCommitted);
            // 
            // cliReleaseChannelLabel
            // 
            this.cliReleaseChannelLabel.AutoSize = true;
            this.cliReleaseChannelLabel.Location = new System.Drawing.Point(5, 212);
            this.cliReleaseChannelLabel.Name = "cliReleaseChannelLabel";
            this.cliReleaseChannelLabel.Size = new System.Drawing.Size(128, 16);
            this.cliReleaseChannelLabel.TabIndex = 22;
            this.cliReleaseChannelLabel.Text = "CLI release channel:";
            // 
            // cliBaseDownloadUrl
            // 
            this.cliBaseDownloadUrl.AutoSize = true;
            this.cliBaseDownloadUrl.Location = new System.Drawing.Point(5, 27);
            this.cliBaseDownloadUrl.Name = "cliBaseDownloadUrl";
            this.cliBaseDownloadUrl.Size = new System.Drawing.Size(194, 16);
            this.cliBaseDownloadUrl.TabIndex = 20;
            this.cliBaseDownloadUrl.Text = "Base URL to download the CLI: ";
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(9, 155);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(4);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(684, 39);
            this.richTextBox1.TabIndex = 18;
            this.richTextBox1.Text = "Snyk will download, install and update the dependencies for you. If this option i" +
    "s disabled, make sure valid paths to the dependencies are provided.";
            // 
            // cliDownloadUrlTextBox
            // 
            this.cliDownloadUrlTextBox.Location = new System.Drawing.Point(241, 23);
            this.cliDownloadUrlTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.cliDownloadUrlTextBox.Name = "cliDownloadUrlTextBox";
            this.cliDownloadUrlTextBox.Size = new System.Drawing.Size(399, 22);
            this.cliDownloadUrlTextBox.TabIndex = 21;
            // 
            // CliPathLabel
            // 
            this.CliPathLabel.AutoSize = true;
            this.CliPathLabel.Location = new System.Drawing.Point(5, 71);
            this.CliPathLabel.Name = "CliPathLabel";
            this.CliPathLabel.Size = new System.Drawing.Size(92, 16);
            this.CliPathLabel.TabIndex = 14;
            this.CliPathLabel.Text = "Snyk CLI Path:";
            // 
            // resetCliPathToDefaultButton
            // 
            this.resetCliPathToDefaultButton.Location = new System.Drawing.Point(347, 65);
            this.resetCliPathToDefaultButton.Margin = new System.Windows.Forms.Padding(4);
            this.resetCliPathToDefaultButton.Name = "resetCliPathToDefaultButton";
            this.resetCliPathToDefaultButton.Size = new System.Drawing.Size(129, 28);
            this.resetCliPathToDefaultButton.TabIndex = 17;
            this.resetCliPathToDefaultButton.Text = "Reset to default";
            this.resetCliPathToDefaultButton.UseVisualStyleBackColor = true;
            this.resetCliPathToDefaultButton.Click += new System.EventHandler(this.ClearCliCustomPathButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 132);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(320, 16);
            this.label1.TabIndex = 12;
            this.label1.Text = "Update and install Snyk dependencies automatically";
            // 
            // CliPathBrowseButton
            // 
            this.CliPathBrowseButton.Location = new System.Drawing.Point(239, 65);
            this.CliPathBrowseButton.Margin = new System.Windows.Forms.Padding(4);
            this.CliPathBrowseButton.Name = "CliPathBrowseButton";
            this.CliPathBrowseButton.Size = new System.Drawing.Size(100, 28);
            this.CliPathBrowseButton.TabIndex = 16;
            this.CliPathBrowseButton.Text = "Browse";
            this.CliPathBrowseButton.UseVisualStyleBackColor = true;
            this.CliPathBrowseButton.Click += new System.EventHandler(this.CliPathBrowseButton_Click);
            // 
            // manageBinariesAutomaticallyCheckbox
            // 
            this.manageBinariesAutomaticallyCheckbox.AutoSize = true;
            this.manageBinariesAutomaticallyCheckbox.Location = new System.Drawing.Point(16, 132);
            this.manageBinariesAutomaticallyCheckbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.manageBinariesAutomaticallyCheckbox.Name = "manageBinariesAutomaticallyCheckbox";
            this.manageBinariesAutomaticallyCheckbox.Size = new System.Drawing.Size(18, 17);
            this.manageBinariesAutomaticallyCheckbox.TabIndex = 13;
            this.manageBinariesAutomaticallyCheckbox.UseVisualStyleBackColor = true;
            this.manageBinariesAutomaticallyCheckbox.CheckedChanged += new System.EventHandler(this.manageBinariesAutomaticallyCheckbox_CheckedChanged);
            // 
            // CliPathTextBox
            // 
            this.CliPathTextBox.Location = new System.Drawing.Point(241, 96);
            this.CliPathTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CliPathTextBox.Name = "CliPathTextBox";
            this.CliPathTextBox.ReadOnly = true;
            this.CliPathTextBox.Size = new System.Drawing.Size(399, 22);
            this.CliPathTextBox.TabIndex = 15;
            // 
            // customCliPathFileDialog
            // 
            this.customCliPathFileDialog.SupportMultiDottedExtensions = true;
            // 
            // SnykCliOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Name = "SnykCliOptionsUserControl";
            this.Size = new System.Drawing.Size(803, 339);
            this.mainPanel.ResumeLayout(false);
            this.ExecutablesGroupBox.ResumeLayout(false);
            this.ExecutablesGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.GroupBox ExecutablesGroupBox;
        private System.Windows.Forms.LinkLabel ReleaseChannelLink;
        private System.Windows.Forms.ComboBox releaseChannel;
        private System.Windows.Forms.Label cliReleaseChannelLabel;
        private System.Windows.Forms.Label cliBaseDownloadUrl;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox cliDownloadUrlTextBox;
        private System.Windows.Forms.Label CliPathLabel;
        private System.Windows.Forms.Button resetCliPathToDefaultButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button CliPathBrowseButton;
        private System.Windows.Forms.CheckBox manageBinariesAutomaticallyCheckbox;
        private System.Windows.Forms.TextBox CliPathTextBox;
        private System.Windows.Forms.OpenFileDialog customCliPathFileDialog;
    }
}
