namespace Arbitrader
{
    partial class LogForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox_Logs = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBox_Logs
            // 
            this.richTextBox_Logs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox_Logs.Location = new System.Drawing.Point(12, 12);
            this.richTextBox_Logs.Name = "richTextBox_Logs";
            this.richTextBox_Logs.ReadOnly = true;
            this.richTextBox_Logs.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBox_Logs.Size = new System.Drawing.Size(776, 426);
            this.richTextBox_Logs.TabIndex = 7;
            this.richTextBox_Logs.Text = "";
            // 
            // LogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.richTextBox_Logs);
            this.Name = "LogForm";
            this.Text = "Logs";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LogForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox_Logs;
    }
}