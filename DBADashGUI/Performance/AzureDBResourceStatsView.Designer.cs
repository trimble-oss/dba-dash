namespace DBADashGUI.Performance
{
    partial class AzureDBResourceStatsView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureDBResourceStatsView));
            this.azureDBResourceStats1 = new DBADashGUI.Performance.AzureDBResourceStats();
            this.SuspendLayout();
            // 
            // azureDBResourceStats1
            // 
            this.azureDBResourceStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureDBResourceStats1.Location = new System.Drawing.Point(0, 0);
            this.azureDBResourceStats1.Name = "azureDBResourceStats1";
            this.azureDBResourceStats1.Size = new System.Drawing.Size(800, 450);
            this.azureDBResourceStats1.TabIndex = 0;
            // 
            // AzureDBResourceStatsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.azureDBResourceStats1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AzureDBResourceStatsView";
            this.Text = "Azure DB Resource Stats";
            this.Load += new System.EventHandler(this.AzureDBResourceStatsView_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private AzureDBResourceStats azureDBResourceStats1;
    }
}