
namespace DBADashGUI.AgentJobs
{
    partial class JobStep
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.lblJobStep = new System.Windows.Forms.ToolStripLabel();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.txtJobStep = new DBADashGUI.SchemaCompare.CodeEditor();
            this.tsLineNumbers = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCopy,
            this.lblJobStep,
            this.tsLineNumbers});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(968, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 28);
            // 
            // lblJobStep
            // 
            this.lblJobStep.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblJobStep.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblJobStep.Name = "lblJobStep";
            this.lblJobStep.Size = new System.Drawing.Size(78, 28);
            this.lblJobStep.Text = "Job | Step";
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 27);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(968, 522);
            this.elementHost1.TabIndex = 2;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.txtJobStep;
            // 
            // tsLineNumbers
            // 
            this.tsLineNumbers.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsLineNumbers.Image = global::DBADashGUI.Properties.Resources.List_NumberedHS;
            this.tsLineNumbers.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLineNumbers.Name = "tsLineNumbers";
            this.tsLineNumbers.Size = new System.Drawing.Size(29, 28);
            this.tsLineNumbers.Text = "Toggle Line Numbers";
            this.tsLineNumbers.Click += new System.EventHandler(this.TsLineNumbers_Click);
            // 
            // JobStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.elementHost1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "JobStep";
            this.Size = new System.Drawing.Size(968, 549);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripLabel lblJobStep;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private SchemaCompare.CodeEditor txtJobStep;
        private System.Windows.Forms.ToolStripButton tsLineNumbers;
    }
}
