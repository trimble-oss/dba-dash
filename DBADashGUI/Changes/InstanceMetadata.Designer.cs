namespace DBADashGUI.Changes
{
    partial class InstanceMetadata
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
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            themedTabControl1 = new DBADashGUI.Theme.ThemedTabControl();
            tabMeta = new System.Windows.Forms.TabPage();
            jsonTreeView = new System.Windows.Forms.TreeView();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            contextCopy = new System.Windows.Forms.ToolStripMenuItem();
            tabJson = new System.Windows.Forms.TabPage();
            tabDiff = new System.Windows.Forms.TabPage();
            diffControl1 = new DiffControl();
            customReportView1 = new DBADashGUI.CustomReports.CustomReportView();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            themedTabControl1.SuspendLayout();
            tabMeta.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            tabDiff.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(themedTabControl1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(customReportView1);
            splitContainer1.Size = new System.Drawing.Size(1350, 886);
            splitContainer1.SplitterDistance = 450;
            splitContainer1.TabIndex = 0;
            // 
            // themedTabControl1
            // 
            themedTabControl1.Controls.Add(tabMeta);
            themedTabControl1.Controls.Add(tabJson);
            themedTabControl1.Controls.Add(tabDiff);
            themedTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            themedTabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            themedTabControl1.Location = new System.Drawing.Point(0, 0);
            themedTabControl1.Name = "themedTabControl1";
            themedTabControl1.Padding = new System.Drawing.Point(20, 8);
            themedTabControl1.SelectedIndex = 0;
            themedTabControl1.Size = new System.Drawing.Size(1350, 450);
            themedTabControl1.TabIndex = 0;
            // 
            // tabMeta
            // 
            tabMeta.Controls.Add(jsonTreeView);
            tabMeta.Location = new System.Drawing.Point(4, 39);
            tabMeta.Name = "tabMeta";
            tabMeta.Padding = new System.Windows.Forms.Padding(3);
            tabMeta.Size = new System.Drawing.Size(1342, 407);
            tabMeta.TabIndex = 2;
            tabMeta.Text = "Metadata";
            tabMeta.UseVisualStyleBackColor = true;
            // 
            // jsonTreeView
            // 
            jsonTreeView.ContextMenuStrip = contextMenuStrip1;
            jsonTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            jsonTreeView.Location = new System.Drawing.Point(3, 3);
            jsonTreeView.Name = "jsonTreeView";
            jsonTreeView.Size = new System.Drawing.Size(1336, 401);
            jsonTreeView.TabIndex = 0;
            jsonTreeView.NodeMouseClick += NodeMouseClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { contextCopy });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(148, 30);
            // 
            // contextCopy
            // 
            contextCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            contextCopy.Name = "contextCopy";
            contextCopy.Size = new System.Drawing.Size(147, 26);
            contextCopy.Text = "Copy Text";
            // 
            // tabJson
            // 
            tabJson.Location = new System.Drawing.Point(4, 39);
            tabJson.Name = "tabJson";
            tabJson.Padding = new System.Windows.Forms.Padding(3);
            tabJson.Size = new System.Drawing.Size(1342, 407);
            tabJson.TabIndex = 0;
            tabJson.Text = "Json";
            tabJson.UseVisualStyleBackColor = true;
            // 
            // tabDiff
            // 
            tabDiff.Controls.Add(diffControl1);
            tabDiff.Location = new System.Drawing.Point(4, 39);
            tabDiff.Name = "tabDiff";
            tabDiff.Padding = new System.Windows.Forms.Padding(3);
            tabDiff.Size = new System.Drawing.Size(1342, 407);
            tabDiff.TabIndex = 1;
            tabDiff.Text = "Diff";
            tabDiff.UseVisualStyleBackColor = true;
            // 
            // diffControl1
            // 
            diffControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            diffControl1.Location = new System.Drawing.Point(3, 3);
            diffControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            diffControl1.Mode = DiffControl.ViewMode.Diff;
            diffControl1.Name = "diffControl1";
            diffControl1.NewText = null;
            diffControl1.OldText = null;
            diffControl1.Size = new System.Drawing.Size(1336, 401);
            diffControl1.TabIndex = 0;
            // 
            // customReportView1
            // 
            customReportView1.AutoScroll = true;
            customReportView1.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            customReportView1.Dock = System.Windows.Forms.DockStyle.Fill;
            customReportView1.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            customReportView1.Location = new System.Drawing.Point(0, 0);
            customReportView1.Name = "customReportView1";
            customReportView1.Report = null;
            customReportView1.Size = new System.Drawing.Size(1350, 432);
            customReportView1.TabIndex = 0;
            // 
            // InstanceMetadata
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Name = "InstanceMetadata";
            Size = new System.Drawing.Size(1350, 886);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            themedTabControl1.ResumeLayout(false);
            tabMeta.ResumeLayout(false);
            contextMenuStrip1.ResumeLayout(false);
            tabDiff.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private CustomReports.CustomReportView customReportView1;
        private Theme.ThemedTabControl themedTabControl1;
        private System.Windows.Forms.TabPage tabJson;
        private System.Windows.Forms.TabPage tabDiff;
        private DiffControl diffControl1;
        private System.Windows.Forms.TabPage tabMeta;
        private System.Windows.Forms.TreeView jsonTreeView;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem contextCopy;
    }
}
