namespace DBADashGUI.Performance
{
    partial class QueryStoreForcedPlans
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
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsTriggerCollection = new System.Windows.Forms.ToolStripButton();
            dgv = new System.Windows.Forms.DataGridView();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvLog = new System.Windows.Forms.DataGridView();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            tsExportHistoryExcel = new System.Windows.Forms.ToolStripButton();
            tsTop = new System.Windows.Forms.ToolStripDropDownButton();
            tsTop10 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop100 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop1000 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop5000 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            tsTopCustom = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLog).BeginInit();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopy, tsExcel, tsTriggerCollection });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1256, 27);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsTriggerCollection
            // 
            tsTriggerCollection.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTriggerCollection.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTriggerCollection.Name = "tsTriggerCollection";
            tsTriggerCollection.Size = new System.Drawing.Size(84, 24);
            tsTriggerCollection.Text = "Execute";
            tsTriggerCollection.Click += TsTriggerCollection_Click;
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1256, 301);
            dgv.TabIndex = 1;
            dgv.CellContentClick += CellContentClick;
            dgv.CellFormatting += CellFormatting;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 635);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1256, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgv);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvLog);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(1256, 608);
            splitContainer1.SplitterDistance = 301;
            splitContainer1.TabIndex = 3;
            // 
            // dgvLog
            // 
            dgvLog.AllowUserToAddRows = false;
            dgvLog.AllowUserToDeleteRows = false;
            dgvLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLog.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvLog.Location = new System.Drawing.Point(0, 27);
            dgvLog.Name = "dgvLog";
            dgvLog.ReadOnly = true;
            dgvLog.RowHeadersVisible = false;
            dgvLog.RowHeadersWidth = 51;
            dgvLog.Size = new System.Drawing.Size(1256, 276);
            dgvLog.TabIndex = 0;
            dgvLog.CellContentClick += CellContentClick;
            dgvLog.CellFormatting += CellFormatting;
            dgvLog.RowsAdded += DgvLog_RowsAdded;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopyHistory, tsExportHistoryExcel, tsTop, toolStripLabel1 });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(1256, 27);
            toolStrip2.TabIndex = 2;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsCopyHistory
            // 
            tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyHistory.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // tsExportHistoryExcel
            // 
            tsExportHistoryExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExportHistoryExcel.Image = Properties.Resources.excel16x16;
            tsExportHistoryExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExportHistoryExcel.Name = "tsExportHistoryExcel";
            tsExportHistoryExcel.Size = new System.Drawing.Size(29, 24);
            tsExportHistoryExcel.Text = "Excel";
            tsExportHistoryExcel.Click += TsExportHistoryExcel_Click;
            // 
            // tsTop
            // 
            tsTop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsTop.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsTop10, tsTop100, tsTop1000, tsTop5000, toolStripMenuItem1, tsTopCustom });
            tsTop.Image = Properties.Resources.StatusAnnotations_Warning_16xLG_color;
            tsTop.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTop.Name = "tsTop";
            tsTop.Size = new System.Drawing.Size(76, 24);
            tsTop.Tag = "100";
            tsTop.Text = "Top 100";
            // 
            // tsTop10
            // 
            tsTop10.Name = "tsTop10";
            tsTop10.Size = new System.Drawing.Size(142, 26);
            tsTop10.Tag = "10";
            tsTop10.Text = "10";
            tsTop10.Click += Top_Select;
            // 
            // tsTop100
            // 
            tsTop100.Checked = true;
            tsTop100.CheckState = System.Windows.Forms.CheckState.Checked;
            tsTop100.Name = "tsTop100";
            tsTop100.Size = new System.Drawing.Size(142, 26);
            tsTop100.Tag = "100";
            tsTop100.Text = "100";
            tsTop100.Click += Top_Select;
            // 
            // tsTop1000
            // 
            tsTop1000.Name = "tsTop1000";
            tsTop1000.Size = new System.Drawing.Size(142, 26);
            tsTop1000.Tag = "1000";
            tsTop1000.Text = "1000";
            tsTop1000.Click += Top_Select;
            // 
            // tsTop5000
            // 
            tsTop5000.Name = "tsTop5000";
            tsTop5000.Size = new System.Drawing.Size(142, 26);
            tsTop5000.Tag = "5000";
            tsTop5000.Text = "5000";
            tsTop5000.Click += Top_Select;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(139, 6);
            // 
            // tsTopCustom
            // 
            tsTopCustom.Name = "tsTopCustom";
            tsTopCustom.Size = new System.Drawing.Size(142, 26);
            tsTopCustom.Text = "Custom";
            tsTopCustom.Click += Top_Select;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(328, 24);
            toolStripLabel1.Text = "History of DBA Dash plan forcing operations";
            toolStripLabel1.ToolTipText = "Note: History is limited to plan forcing performed using DBA Dash";
            // 
            // QueryStoreForcedPlans
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Name = "QueryStoreForcedPlans";
            Size = new System.Drawing.Size(1256, 657);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvLog).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsTriggerCollection;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvLog;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripButton tsExportHistoryExcel;
        private System.Windows.Forms.ToolStripDropDownButton tsTop;
        private System.Windows.Forms.ToolStripMenuItem tsTop100;
        private System.Windows.Forms.ToolStripMenuItem tsTop1000;
        private System.Windows.Forms.ToolStripMenuItem tsTop5000;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem tsTopCustom;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripMenuItem tsTop10;
    }
}
