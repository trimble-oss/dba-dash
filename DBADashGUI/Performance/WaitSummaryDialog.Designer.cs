namespace DBADashGUI.Performance
{
    partial class WaitSummaryDialog
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
            this.waitsSummary1 = new DBADashGUI.Performance.WaitsSummary();
            this.SuspendLayout();
            // 
            // waitsSummary1
            // 
            this.waitsSummary1.DateGrouping = 1;
            this.waitsSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitsSummary1.Location = new System.Drawing.Point(0, 0);
            this.waitsSummary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.waitsSummary1.Name = "waitsSummary1";
            this.waitsSummary1.Size = new System.Drawing.Size(1596, 616);
            this.waitsSummary1.TabIndex = 0;
            // 
            // WaitSummaryDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1596, 616);
            this.Controls.Add(this.waitsSummary1);
            this.Name = "WaitSummaryDialog";
            this.Text = "Wait Summary";
            this.Load += new System.EventHandler(this.WaitSummaryDialog_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private WaitsSummary waitsSummary1;
    }
}