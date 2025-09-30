namespace Snyk.VisualStudio.Extension.Settings
{
    partial class SnykSolutionOptionsUserControl
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
            this.additionalOptionsTextBox = new System.Windows.Forms.TextBox();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.organizationLabel = new System.Windows.Forms.Label();
            this.organizationTextBox = new System.Windows.Forms.TextBox();
            this.OrganizationInfoLink = new System.Windows.Forms.LinkLabel();
            this.OrgDescriptionText = new System.Windows.Forms.Label();
            this.additionalParamsInfoLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            //
            // additionalOptionsTextBox
            //
            this.additionalOptionsTextBox.Location = new System.Drawing.Point(6, 33);
            this.additionalOptionsTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.additionalOptionsTextBox.Multiline = true;
            this.additionalOptionsTextBox.Name = "additionalOptionsTextBox";
            this.additionalOptionsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.additionalOptionsTextBox.Size = new System.Drawing.Size(441, 118);
            this.additionalOptionsTextBox.TabIndex = 0;
            this.additionalOptionsTextBox.TextChanged += new System.EventHandler(this.AdditionalOptionsTextBox_TextChanged);
            //
            // errorProvider
            //
            this.errorProvider.ContainerControl = this;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 18);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Additional Parameters:";
            //
            // organizationLabel
            //
            this.organizationLabel.AutoSize = true;
            this.organizationLabel.Location = new System.Drawing.Point(6, 180);
            this.organizationLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.organizationLabel.Name = "organizationLabel";
            this.organizationLabel.Size = new System.Drawing.Size(69, 13);
            this.organizationLabel.TabIndex = 3;
            this.organizationLabel.Text = "Organization:";
            //
            // organizationTextBox
            //
            this.organizationTextBox.Location = new System.Drawing.Point(6, 199);
            this.organizationTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(441, 20);
            this.organizationTextBox.TabIndex = 4;
            this.organizationTextBox.TextChanged += new System.EventHandler(this.OrganizationTextBox_TextChanged);
            //
            // OrganizationInfoLink
            //
            this.OrganizationInfoLink.AutoSize = true;
            this.OrganizationInfoLink.Location = new System.Drawing.Point(6, 268);
            this.OrganizationInfoLink.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.OrganizationInfoLink.Name = "OrganizationInfoLink";
            this.OrganizationInfoLink.Size = new System.Drawing.Size(150, 13);
            this.OrganizationInfoLink.TabIndex = 5;
            this.OrganizationInfoLink.TabStop = true;
            this.OrganizationInfoLink.Text = "Learn more about organization";
            this.OrganizationInfoLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OrganizationInfoLink_LinkClicked);
            //
            // OrgDescriptionText
            //
            this.OrgDescriptionText.AutoSize = true;
            this.OrgDescriptionText.Location = new System.Drawing.Point(6, 226);
            this.OrgDescriptionText.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.OrgDescriptionText.Name = "OrgDescriptionText";
            this.OrgDescriptionText.Size = new System.Drawing.Size(376, 39);
            this.OrgDescriptionText.TabIndex = 6;
            this.OrgDescriptionText.Text = "Specify an organization slug name to run tests for that organization.\r\nIt must ma" +
    "tch the URL slug as displayed in the URL of your org in the Snyk UI:\r\nhttps://ap" +
    "p.snyk.io/org/[OrgSlugName]";
            //
            // additionalParamsInfoLabel
            //
            this.additionalParamsInfoLabel.AutoSize = true;
            this.additionalParamsInfoLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.additionalParamsInfoLabel.Location = new System.Drawing.Point(6, 153);
            this.additionalParamsInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.additionalParamsInfoLabel.Name = "additionalParamsInfoLabel";
            this.additionalParamsInfoLabel.Size = new System.Drawing.Size(359, 13);
            this.additionalParamsInfoLabel.TabIndex = 7;
            this.additionalParamsInfoLabel.Text = "Additional parameters used when calling the CLI, e.g. `-d` or `--exclude=bin`";
            //
            // SnykSolutionOptionsUserControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.additionalParamsInfoLabel);
            this.Controls.Add(this.OrgDescriptionText);
            this.Controls.Add(this.OrganizationInfoLink);
            this.Controls.Add(this.organizationTextBox);
            this.Controls.Add(this.organizationLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.additionalOptionsTextBox);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "SnykSolutionOptionsUserControl";
            this.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Size = new System.Drawing.Size(1065, 343);
            this.Load += new System.EventHandler(this.SnykProjectOptionsUserControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox additionalOptionsTextBox;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label additionalParamsInfoLabel;
        private System.Windows.Forms.Label organizationLabel;
        private System.Windows.Forms.TextBox organizationTextBox;
        private System.Windows.Forms.LinkLabel OrganizationInfoLink;
        private System.Windows.Forms.Label OrgDescriptionText;
    }
}
