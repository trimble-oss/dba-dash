namespace DBADashGUI.DBFiles
{
    partial class DBSpaceHistoryView
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
            this.dbSpaceHistory1 = new DBADashGUI.DBFiles.DBSpaceHistory();
            this.SuspendLayout();
            // 
            // dbSpaceHistory1
            // 
            this.dbSpaceHistory1.DatabaseID = 0;
            this.dbSpaceHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbSpaceHistory1.Location = new System.Drawing.Point(0, 0);
            this.dbSpaceHistory1.Name = "dbSpaceHistory1";
            this.dbSpaceHistory1.Size = new System.Drawing.Size(800, 450);
            this.dbSpaceHistory1.SmoothLines = true;
            this.dbSpaceHistory1.TabIndex = 0;
            // 
            // DBSpaceHistoryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dbSpaceHistory1);
            this.Name = "DBSpaceHistoryView";
            this.Text = "DB Space";
            this.Load += new System.EventHandler(this.DBSpaceHistoryView_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private DBSpaceHistory dbSpaceHistory1;
    }
}