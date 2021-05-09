
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
            this.txtJobStep = new ICSharpCode.TextEditor.TextEditorControl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.lblJobStep = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtJobStep
            // 
            this.txtJobStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtJobStep.IsReadOnly = false;
            this.txtJobStep.Location = new System.Drawing.Point(0, 27);
            this.txtJobStep.Name = "txtJobStep";
            this.txtJobStep.ShowVRuler = false;
            this.txtJobStep.Size = new System.Drawing.Size(968, 522);
            this.txtJobStep.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCopy,
            this.lblJobStep});
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
            // JobStep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtJobStep);
            this.Controls.Add(this.toolStrip1);
            this.Name = "JobStep";
            this.Size = new System.Drawing.Size(968, 549);
            this.Load += new System.EventHandler(this.JobStep_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ICSharpCode.TextEditor.TextEditorControl txtJobStep;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripLabel lblJobStep;
    }
}
