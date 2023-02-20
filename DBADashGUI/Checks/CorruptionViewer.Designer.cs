using DBADashGUI.Checks;

namespace DBADashGUI
{
    partial class CorruptionViewer
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
            this.corruption1 = new DBADashGUI.Corruption();
            this.SuspendLayout();
            // 
            // corruption1
            // 
            this.corruption1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.corruption1.Location = new System.Drawing.Point(0, 0);
            this.corruption1.Name = "corruption1";
            this.corruption1.Size = new System.Drawing.Size(800, 450);
            this.corruption1.TabIndex = 0;
            // 
            // CorruptionViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.corruption1);
            this.Name = "CorruptionViewer";
            this.Text = "Corruption Info";
            this.Load += new System.EventHandler(this.CorruptionViewer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Corruption corruption1;
    }
}