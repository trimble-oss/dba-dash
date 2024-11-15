namespace DBADashGUI.SchemaCompare
{
    partial class CodeEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CodeEditorForm));
            panel1 = new System.Windows.Forms.Panel();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsSave = new System.Windows.Forms.ToolStripButton();
            bttnCopy = new System.Windows.Forms.ToolStripButton();
            tsLineNumbers = new System.Windows.Forms.ToolStripButton();
            tsWrapText = new System.Windows.Forms.ToolStripButton();
            tsMarkdownView = new System.Windows.Forms.ToolStripDropDownButton();
            markdownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            markdownPreviewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            previewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pnlDialog = new System.Windows.Forms.Panel();
            bttnCancel = new System.Windows.Forms.Button();
            bttnOK = new System.Windows.Forms.Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            toolStrip1.SuspendLayout();
            pnlDialog.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(splitContainer1);
            panel1.Controls.Add(toolStrip1);
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(800, 397);
            panel1.TabIndex = 0;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(webView);
            splitContainer1.Panel2Collapsed = true;
            splitContainer1.Size = new System.Drawing.Size(800, 370);
            splitContainer1.SplitterDistance = 366;
            splitContainer1.TabIndex = 0;
            // 
            // webView
            // 
            webView.AllowExternalDrop = true;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = System.Drawing.Color.White;
            webView.Dock = System.Windows.Forms.DockStyle.Fill;
            webView.Location = new System.Drawing.Point(0, 0);
            webView.Name = "webView";
            webView.Size = new System.Drawing.Size(96, 100);
            webView.TabIndex = 0;
            webView.ZoomFactor = 1D;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsSave, bttnCopy, tsLineNumbers, tsWrapText, tsMarkdownView });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(800, 27);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsSave
            // 
            tsSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSave.Image = Properties.Resources.Save_16x;
            tsSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSave.Name = "tsSave";
            tsSave.Size = new System.Drawing.Size(29, 24);
            tsSave.Text = "Save As";
            tsSave.Click += TsSave_Click;
            // 
            // bttnCopy
            // 
            bttnCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            bttnCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            bttnCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            bttnCopy.Name = "bttnCopy";
            bttnCopy.Size = new System.Drawing.Size(29, 24);
            bttnCopy.Text = "Copy";
            bttnCopy.Click += BttnCopy_Click;
            // 
            // tsLineNumbers
            // 
            tsLineNumbers.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsLineNumbers.Image = Properties.Resources.List_NumberedHS;
            tsLineNumbers.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsLineNumbers.Name = "tsLineNumbers";
            tsLineNumbers.Size = new System.Drawing.Size(29, 24);
            tsLineNumbers.Text = "Toggle Line Numbers";
            tsLineNumbers.Click += TsLineNumbers_Click;
            // 
            // tsWrapText
            // 
            tsWrapText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsWrapText.Image = Properties.Resources.WordWrap_16x;
            tsWrapText.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsWrapText.Name = "tsWrapText";
            tsWrapText.Size = new System.Drawing.Size(29, 24);
            tsWrapText.Text = "Wrap Text";
            tsWrapText.Click += TsWrapText_Click;
            // 
            // tsMarkdownView
            // 
            tsMarkdownView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsMarkdownView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { markdownToolStripMenuItem, markdownPreviewToolStripMenuItem, previewToolStripMenuItem });
            tsMarkdownView.Image = (System.Drawing.Image)resources.GetObject("tsMarkdownView.Image");
            tsMarkdownView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMarkdownView.Name = "tsMarkdownView";
            tsMarkdownView.Size = new System.Drawing.Size(55, 24);
            tsMarkdownView.Text = "View";
            tsMarkdownView.Visible = false;
            // 
            // markdownToolStripMenuItem
            // 
            markdownToolStripMenuItem.Name = "markdownToolStripMenuItem";
            markdownToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            markdownToolStripMenuItem.Text = "Markdown";
            markdownToolStripMenuItem.Click += MarkdownToolStripMenuItem_Click;
            // 
            // markdownPreviewToolStripMenuItem
            // 
            markdownPreviewToolStripMenuItem.Name = "markdownPreviewToolStripMenuItem";
            markdownPreviewToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            markdownPreviewToolStripMenuItem.Text = "Markdown + Preview";
            markdownPreviewToolStripMenuItem.Click += MarkdownPreviewToolStripMenuItem_Click;
            // 
            // previewToolStripMenuItem
            // 
            previewToolStripMenuItem.Name = "previewToolStripMenuItem";
            previewToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            previewToolStripMenuItem.Text = "Preview";
            previewToolStripMenuItem.Click += PreviewToolStripMenuItem_Click;
            // 
            // pnlDialog
            // 
            pnlDialog.Controls.Add(bttnCancel);
            pnlDialog.Controls.Add(bttnOK);
            pnlDialog.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlDialog.Location = new System.Drawing.Point(0, 397);
            pnlDialog.Name = "pnlDialog";
            pnlDialog.Size = new System.Drawing.Size(800, 53);
            pnlDialog.TabIndex = 1;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bttnCancel.Location = new System.Drawing.Point(594, 12);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 1;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            bttnOK.Location = new System.Drawing.Point(694, 12);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 0;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            // 
            // CodeEditorForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(panel1);
            Controls.Add(pnlDialog);
            Name = "CodeEditorForm";
            Text = "Code Editor";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            pnlDialog.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel pnlDialog;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsSave;
        private System.Windows.Forms.ToolStripButton bttnCopy;
        private System.Windows.Forms.ToolStripButton tsLineNumbers;
        private System.Windows.Forms.ToolStripButton tsWrapText;
        private System.Windows.Forms.ToolStripDropDownButton tsMarkdownView;
        private System.Windows.Forms.ToolStripMenuItem markdownToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem markdownPreviewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem previewToolStripMenuItem;
    }
}