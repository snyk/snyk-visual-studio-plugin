namespace Snyk.VisualStudio.Extension.UI
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
            this.customEndpointTextBox = new System.Windows.Forms.TextBox();
            this.customEndpointLabel = new System.Windows.Forms.Label();
            this.organizationLabel = new System.Windows.Forms.Label();
            this.organizationTextBox = new System.Windows.Forms.TextBox();
            this.tokenLabel = new System.Windows.Forms.Label();
            this.tokenTextBox = new System.Windows.Forms.TextBox();
            this.ignoreUnknownCACheckBox = new System.Windows.Forms.CheckBox();
            this.authenticateButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // customEndpointTextBox
            // 
            this.customEndpointTextBox.Location = new System.Drawing.Point(179, 50);
            this.customEndpointTextBox.Name = "customEndpointTextBox";
            this.customEndpointTextBox.Size = new System.Drawing.Size(699, 26);
            this.customEndpointTextBox.TabIndex = 0;
            this.customEndpointTextBox.TextChanged += new System.EventHandler(this.customEndpointTextBox_TextChanged);
            // 
            // customEndpointLabel
            // 
            this.customEndpointLabel.AutoSize = true;
            this.customEndpointLabel.Location = new System.Drawing.Point(3, 56);
            this.customEndpointLabel.Name = "customEndpointLabel";
            this.customEndpointLabel.Size = new System.Drawing.Size(134, 20);
            this.customEndpointLabel.TabIndex = 1;
            this.customEndpointLabel.Text = "Custom endpoint:";
            // 
            // organizationLabel
            // 
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(3, 113);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(103, 20);
            this.organizationLabel.TabIndex = 2;
            this.organizationLabel.Text = "Organization:";
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.Location = new System.Drawing.Point(179, 113);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(699, 26);
            this.organizationTextBox.TabIndex = 3;
            this.organizationTextBox.TextChanged += new System.EventHandler(this.organizationTextBox_TextChanged);
            // 
            // tokenLabel
            // 
            this.tokenLabel.AutoSize = true;
            this.tokenLabel.Location = new System.Drawing.Point(3, 24);
            this.tokenLabel.Name = "tokenLabel";
            this.tokenLabel.Size = new System.Drawing.Size(57, 20);
            this.tokenLabel.TabIndex = 4;
            this.tokenLabel.Text = "Token:";
            // 
            // tokenTextBox
            // 
            this.tokenTextBox.Location = new System.Drawing.Point(179, 18);
            this.tokenTextBox.Margin = new System.Windows.Forms.Padding(5);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.Size = new System.Drawing.Size(699, 26);
            this.tokenTextBox.TabIndex = 5;
            this.tokenTextBox.TextChanged += new System.EventHandler(this.tokenTextBox_TextChanged);
            // 
            // ignoreUnknownCACheckBox
            // 
            this.ignoreUnknownCACheckBox.AutoSize = true;
            this.ignoreUnknownCACheckBox.Location = new System.Drawing.Point(179, 83);
            this.ignoreUnknownCACheckBox.Name = "ignoreUnknownCACheckBox";
            this.ignoreUnknownCACheckBox.Size = new System.Drawing.Size(175, 24);
            this.ignoreUnknownCACheckBox.TabIndex = 6;
            this.ignoreUnknownCACheckBox.Text = "Ignore unknown CA";
            this.ignoreUnknownCACheckBox.UseVisualStyleBackColor = true;
            this.ignoreUnknownCACheckBox.CheckedChanged += new System.EventHandler(this.ignoreUnknownCACheckBox_CheckedChanged);
            // 
            // authenticateButton
            // 
            this.authenticateButton.Location = new System.Drawing.Point(884, 16);
            this.authenticateButton.Margin = new System.Windows.Forms.Padding(5);
            this.authenticateButton.Name = "authenticateButton";
            this.authenticateButton.Size = new System.Drawing.Size(124, 31);
            this.authenticateButton.TabIndex = 7;
            this.authenticateButton.Text = "Authenticate";
            this.authenticateButton.UseVisualStyleBackColor = true;
            this.authenticateButton.Click += new System.EventHandler(this.authenticateButton_Click);
            // 
            // SnykGeneralSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.authenticateButton);
            this.Controls.Add(this.ignoreUnknownCACheckBox);
            this.Controls.Add(this.tokenTextBox);
            this.Controls.Add(this.tokenLabel);
            this.Controls.Add(this.organizationTextBox);
            this.Controls.Add(this.organizationLabel);
            this.Controls.Add(this.customEndpointLabel);
            this.Controls.Add(this.customEndpointTextBox);
            this.Name = "SnykGeneralSettingsUserControl";
            this.Size = new System.Drawing.Size(1026, 469);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}
