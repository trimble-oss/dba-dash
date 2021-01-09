namespace DBADashGUI
{
    partial class DiffControl
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
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.diffViewer1 = new DiffPlex.Wpf.Controls.DiffViewer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCode = new System.Windows.Forms.ToolStripButton();
            this.tsDiff = new System.Windows.Forms.ToolStripButton();
            this.tsInline = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.copyLeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyRightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtCode = new ICSharpCode.TextEditor.TextEditorControl();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 31);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(242, 153);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.diffViewer1;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCode,
            this.tsDiff,
            this.tsInline,
            this.toolStripDropDownButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(242, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsCode
            // 
            this.tsCode.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCode.Image = global::DBADashGUI.Properties.Resources.Code_16x;
            this.tsCode.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCode.Name = "tsCode";
            this.tsCode.Size = new System.Drawing.Size(29, 28);
            this.tsCode.Text = "Code View";
            this.tsCode.Click += new System.EventHandler(this.tsCode_Click);
            // 
            // tsDiff
            // 
            this.tsDiff.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDiff.Image = global::DBADashGUI.Properties.Resources.Diff_16x;
            this.tsDiff.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDiff.Name = "tsDiff";
            this.tsDiff.Size = new System.Drawing.Size(29, 28);
            this.tsDiff.Text = "Side by Side Diff";
            this.tsDiff.Click += new System.EventHandler(this.tsDiff_Click);
            // 
            // tsInline
            // 
            this.tsInline.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsInline.Image = global::DBADashGUI.Properties.Resources.Inline_16x;
            this.tsInline.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsInline.Name = "tsInline";
            this.tsInline.Size = new System.Drawing.Size(29, 28);
            this.tsInline.Text = "Inline Diff";
            this.tsInline.Click += new System.EventHandler(this.tsInline_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyLeftToolStripMenuItem,
            this.copyRightToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_grey_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 28);
            this.toolStripDropDownButton1.Text = "Copy";
            // 
            // copyLeftToolStripMenuItem
            // 
            this.copyLeftToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.copyLeftToolStripMenuItem.Name = "copyLeftToolStripMenuItem";
            this.copyLeftToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            this.copyLeftToolStripMenuItem.Text = "Copy Left";
            this.copyLeftToolStripMenuItem.Click += new System.EventHandler(this.copyLeftToolStripMenuItem_Click);
            // 
            // copyRightToolStripMenuItem
            // 
            this.copyRightToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_grey_16x;
            this.copyRightToolStripMenuItem.Name = "copyRightToolStripMenuItem";
            this.copyRightToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            this.copyRightToolStripMenuItem.Text = "Copy Right";
            this.copyRightToolStripMenuItem.Click += new System.EventHandler(this.copyRightToolStripMenuItem_Click);
            // 
            // txtCode
            // 
            this.txtCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCode.IsReadOnly = false;
            this.txtCode.Location = new System.Drawing.Point(0, 31);
            this.txtCode.Name = "txtCode";
            this.txtCode.ShowVRuler = false;
            this.txtCode.Size = new System.Drawing.Size(242, 153);
            this.txtCode.TabIndex = 2;
            // 
            // DiffControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.elementHost1);
            this.Controls.Add(this.txtCode);
            this.Controls.Add(this.toolStrip1);
            this.Name = "DiffControl";
            this.Size = new System.Drawing.Size(242, 184);
            this.Load += new System.EventHandler(this.DiffControl_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private DiffPlex.Wpf.Controls.DiffViewer diffViewer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCode;
        private System.Windows.Forms.ToolStripButton tsDiff;
        private System.Windows.Forms.ToolStripButton tsInline;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem copyLeftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyRightToolStripMenuItem;
        private ICSharpCode.TextEditor.TextEditorControl txtCode;
    }
}
