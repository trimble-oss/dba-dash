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
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            bttnCopy = new System.Windows.Forms.ToolStripButton();
            tsLineNumbers = new System.Windows.Forms.ToolStripButton();
            tsWrapText = new System.Windows.Forms.ToolStripButton();
            elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            codeEditor1 = new DBADashGUI.SchemaCompare.CodeEditor();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { bttnCopy, tsLineNumbers, tsWrapText });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(800, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
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
            tsWrapText.Click += tsWrapText_Click;
            // 
            // elementHost1
            // 
            elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            elementHost1.Location = new System.Drawing.Point(0, 27);
            elementHost1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            elementHost1.Name = "elementHost1";
            elementHost1.Size = new System.Drawing.Size(800, 535);
            elementHost1.TabIndex = 2;
            elementHost1.Text = "elementHost1";
            elementHost1.Child = codeEditor1;
            // 
            // CodeViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 562);
            Controls.Add(elementHost1);
            Controls.Add(toolStrip1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CodeViewer";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Code Viewer";
            FormClosing += CodeViewer_FormClosing;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton bttnCopy;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private SchemaCompare.CodeEditor codeEditor1;
        private System.Windows.Forms.ToolStripButton tsLineNumbers;
        private System.Windows.Forms.ToolStripButton tsWrapText;
    }
}