namespace DBADashGUI
{
    partial class CodeViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CodeViewer));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.bttnCopy = new System.Windows.Forms.ToolStripButton();
            this.tsLineNumbers = new System.Windows.Forms.ToolStripButton();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.codeEditor1 = new DBADashGUI.SchemaCompare.CodeEditor();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bttnCopy,
            this.tsLineNumbers});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // bttnCopy
            // 
            this.bttnCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bttnCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.bttnCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bttnCopy.Name = "bttnCopy";
            this.bttnCopy.Size = new System.Drawing.Size(29, 28);
            this.bttnCopy.Text = "Copy";
            this.bttnCopy.Click += new System.EventHandler(this.bttnCopy_Click);
            // 
            // tsLineNumbers
            // 
            this.tsLineNumbers.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsLineNumbers.Image = global::DBADashGUI.Properties.Resources.List_NumberedHS;
            this.tsLineNumbers.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLineNumbers.Name = "tsLineNumbers";
            this.tsLineNumbers.Size = new System.Drawing.Size(29, 28);
            this.tsLineNumbers.Text = "Toggle Line Numbers";
            this.tsLineNumbers.Click += new System.EventHandler(this.tsLineNumbers_Click);
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 31);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(800, 419);
            this.elementHost1.TabIndex = 2;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.codeEditor1;
            // 
            // CodeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.elementHost1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CodeViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Code Viewer";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton bttnCopy;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private SchemaCompare.CodeEditor codeEditor1;
        private System.Windows.Forms.ToolStripButton tsLineNumbers;
    }
}