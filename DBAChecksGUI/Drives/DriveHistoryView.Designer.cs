namespace DBAChecksGUI.Drives
{
    partial class DriveHistoryView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DriveHistoryView));
            this.driveHistory1 = new DBAChecksGUI.Drives.DriveHistory();
            this.SuspendLayout();
            // 
            // driveHistory1
            // 
            this.driveHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.driveHistory1.DriveID = 0;
            this.driveHistory1.Location = new System.Drawing.Point(0, 0);
            this.driveHistory1.Name = "driveHistory1";
            this.driveHistory1.Size = new System.Drawing.Size(800, 450);
            this.driveHistory1.SmoothLines = true;
            this.driveHistory1.TabIndex = 0;
            // 
            // DriveHistoryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.driveHistory1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DriveHistoryView";
            this.Text = "Drive History";
            this.Load += new System.EventHandler(this.DriveHistoryView_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private DriveHistory driveHistory1;
    }
}