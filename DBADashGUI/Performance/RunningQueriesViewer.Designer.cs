namespace DBADashGUI.Performance
{
    partial class RunningQueriesViewer
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
            this.runningQueries1 = new DBADashGUI.Performance.RunningQueries();
            this.SuspendLayout();
            // 
            // runningQueries1
            // 
            this.runningQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.runningQueries1.Location = new System.Drawing.Point(0, 0);
            this.runningQueries1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.runningQueries1.Name = "runningQueries1";
            this.runningQueries1.Size = new System.Drawing.Size(1160, 723);
            this.runningQueries1.TabIndex = 0;
            // 
            // RunningQueriesViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1160, 723);
            this.Controls.Add(this.runningQueries1);
            this.Name = "RunningQueriesViewer";
            this.Text = "Running Queries Viewer";
            this.Load += new System.EventHandler(this.RunningQueriesViewer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private RunningQueries runningQueries1;
    }
}