namespace DBADashGUI
{
    partial class Refresh
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
            this.lblRefresh = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lblRefresh
            // 
            this.lblRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRefresh.Location = new System.Drawing.Point(0, 0);
            this.lblRefresh.Name = "lblRefresh";
            this.lblRefresh.Size = new System.Drawing.Size(150, 150);
            this.lblRefresh.TabIndex = 0;
            this.lblRefresh.Text = "Refresh in progress";
            this.lblRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Refresh
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblRefresh);
            this.Name = "Refresh";
            this.VisibleChanged += new System.EventHandler(this.Refresh_VisibilityChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblRefresh;
        private System.Windows.Forms.Timer timer1;
    }
}
