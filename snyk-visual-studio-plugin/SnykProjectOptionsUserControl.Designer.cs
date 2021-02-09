namespace Snyk.VisualStudio.Extension.UI
{
    partial class SnykProjectOptionsUserControl
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
            this.additionalOptionsTextBox = new System.Windows.Forms.TextBox();
            this.allProjectsCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // additionalOptionsTextBox
            // 
            this.additionalOptionsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.additionalOptionsTextBox.Location = new System.Drawing.Point(13, 22);
            this.additionalOptionsTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.additionalOptionsTextBox.Multiline = true;
            this.additionalOptionsTextBox.Name = "additionalOptionsTextBox";
            this.additionalOptionsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.additionalOptionsTextBox.Size = new System.Drawing.Size(1260, 223);
            this.additionalOptionsTextBox.TabIndex = 0;
            this.additionalOptionsTextBox.TextChanged += new System.EventHandler(this.additionalOptionsTextBox_TextChanged);
            // 
            // allProjectsCheckBox
            // 
            this.allProjectsCheckBox.AutoSize = true;
            this.allProjectsCheckBox.Location = new System.Drawing.Point(13, 253);
            this.allProjectsCheckBox.Name = "allProjectsCheckBox";
            this.allProjectsCheckBox.Size = new System.Drawing.Size(407, 29);
            this.allProjectsCheckBox.TabIndex = 1;
            this.allProjectsCheckBox.Text = "Scan all projects (--all-projects option)";
            this.allProjectsCheckBox.UseVisualStyleBackColor = true;
            this.allProjectsCheckBox.CheckedChanged += new System.EventHandler(this.allProjectsCheckBox_CheckedChanged);
            // 
            // SnykProjectOptionsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.allProjectsCheckBox);
            this.Controls.Add(this.additionalOptionsTextBox);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "SnykProjectOptionsUserControl";
            this.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.Size = new System.Drawing.Size(1274, 509);
            this.Load += new System.EventHandler(this.SnykProjectOptionsUserControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox additionalOptionsTextBox;
        private System.Windows.Forms.CheckBox allProjectsCheckBox;
    }
}
