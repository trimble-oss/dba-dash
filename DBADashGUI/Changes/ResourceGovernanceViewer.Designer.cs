
namespace DBADashGUI.Changes
{
    partial class ResourceGovernanceViewer
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
            this.azureDBResourceGovernance1 = new DBADashGUI.Changes.AzureDBResourceGovernance();
            this.SuspendLayout();
            // 
            // azureDBResourceGovernance1
            // 
            this.azureDBResourceGovernance1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureDBResourceGovernance1.Location = new System.Drawing.Point(0, 0);
            this.azureDBResourceGovernance1.Name = "azureDBResourceGovernance1";
            this.azureDBResourceGovernance1.Size = new System.Drawing.Size(800, 450);
            this.azureDBResourceGovernance1.TabIndex = 0;
            // 
            // ResourceGovernanceViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.azureDBResourceGovernance1);
            this.Name = "ResourceGovernanceViewer";
            this.Text = "Resource Governance";
            this.Load += new System.EventHandler(this.ResourceGovernanceViewer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private AzureDBResourceGovernance azureDBResourceGovernance1;
    }
}