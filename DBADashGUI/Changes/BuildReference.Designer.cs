namespace DBADashGUI.Changes
{
    partial class BuildReference
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
            dgv = new System.Windows.Forms.DataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            versionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            latestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsReset = new System.Windows.Forms.ToolStripButton();
            tsLinks = new System.Windows.Forms.ToolStripDropDownButton();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.RowTemplate.Height = 29;
            dgv.Size = new System.Drawing.Size(635, 421);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.ColumnHeaderMouseClick += Dgv_ColumnHeaderMouseClick;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsFilter, tsCopy, tsExcel, tsReset, tsLinks });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(635, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsFilter
            // 
            tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { versionToolStripMenuItem, latestToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            tsFilter.Text = "Filter";
            // 
            // versionToolStripMenuItem
            // 
            versionToolStripMenuItem.Name = "versionToolStripMenuItem";
            versionToolStripMenuItem.Size = new System.Drawing.Size(140, 26);
            versionToolStripMenuItem.Text = "Version";
            // 
            // latestToolStripMenuItem
            // 
            latestToolStripMenuItem.Checked = true;
            latestToolStripMenuItem.CheckOnClick = true;
            latestToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            latestToolStripMenuItem.Name = "latestToolStripMenuItem";
            latestToolStripMenuItem.Size = new System.Drawing.Size(140, 26);
            latestToolStripMenuItem.Text = "Latest";
            latestToolStripMenuItem.Click += latestToolStripMenuItem_Click;
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
            // tsReset
            // 
            tsReset.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsReset.Image = Properties.Resources.Undo_grey_16x;
            tsReset.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsReset.Name = "tsReset";
            tsReset.Size = new System.Drawing.Size(29, 24);
            tsReset.Text = "Reset";
            tsReset.Click += TsReset_Click;
            // 
            // tsLinks
            // 
            tsLinks.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsLinks.Image = Properties.Resources.WebURL_16x;
            tsLinks.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsLinks.Name = "tsLinks";
            tsLinks.Size = new System.Drawing.Size(34, 24);
            tsLinks.Text = "Links";
            // 
            // BuildReference
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgv);
            Controls.Add(toolStrip1);
            Name = "BuildReference";
            Size = new System.Drawing.Size(635, 448);
            Load += BuildReference_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem versionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem latestToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsReset;
        private System.Windows.Forms.ToolStripDropDownButton tsLinks;
    }
}
