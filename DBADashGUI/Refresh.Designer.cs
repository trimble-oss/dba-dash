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
            components = new System.ComponentModel.Container();
            lblRefresh = new System.Windows.Forms.Label();
            timer1 = new System.Windows.Forms.Timer(components);
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            SuspendLayout();
            // 
            // lblRefresh
            // 
            lblRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            lblRefresh.Location = new System.Drawing.Point(0, 0);
            lblRefresh.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblRefresh.Name = "lblRefresh";
            lblRefresh.Size = new System.Drawing.Size(188, 188);
            lblRefresh.TabIndex = 0;
            lblRefresh.Text = "Refresh in progress";
            lblRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            toolTip1.SetToolTip(lblRefresh, "Double click to copy text");
            // 
            // timer1
            // 
            timer1.Interval = 500;
            timer1.Tick += Timer1_Tick;
            // 
            // Refresh
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lblRefresh);
            Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "Refresh";
            Size = new System.Drawing.Size(188, 188);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblRefresh;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
