namespace DBADashGUI.Performance
{
    partial class IOSummaryForm
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
            this.ioSummary1 = new DBADashGUI.Performance.IOSummary();
            this.SuspendLayout();
            // 
            // ioSummary1
            // 
            this.ioSummary1.DatabaseID = null;
            this.ioSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioSummary1.FromDate = new System.DateTime(((long)(0)));
            this.ioSummary1.GroupBy = DBADashGUI.Performance.IOSummary.IOSummaryGroupByOptions.Database;
            this.ioSummary1.InstanceID = 0;
            this.ioSummary1.Location = new System.Drawing.Point(0, 0);
            this.ioSummary1.Name = "ioSummary1";
            this.ioSummary1.Size = new System.Drawing.Size(1314, 711);
            this.ioSummary1.TabIndex = 0;
            this.ioSummary1.ToDate = new System.DateTime(((long)(0)));
            // 
            // IOSummaryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1314, 711);
            this.Controls.Add(this.ioSummary1);
            this.Name = "IOSummaryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "IO Performance Summary";
            this.Load += new System.EventHandler(this.IOSummaryForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private IOSummary ioSummary1;
    }
}