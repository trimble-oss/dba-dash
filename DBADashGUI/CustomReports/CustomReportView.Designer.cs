namespace DBADashGUI.CustomReports
{
    partial class CustomReportView
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
            dgv = new System.Windows.Forms.DataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsExecute = new System.Windows.Forms.ToolStripButton();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCancel = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            setTitleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setDescriptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            renameResultSetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            associateCollectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            resetLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editPickersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            scriptReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cboResults = new System.Windows.Forms.ToolStripComboBox();
            lblSelectResults = new System.Windows.Forms.ToolStripLabel();
            tsParams = new System.Windows.Forms.ToolStripDropDownButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            lnkParams = new System.Windows.Forms.LinkLabel();
            pnlParams = new System.Windows.Forms.Panel();
            splitContainer2 = new System.Windows.Forms.SplitContainer();
            lblParamsRequired = new System.Windows.Forms.Label();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblTimer = new System.Windows.Forms.ToolStripStatusLabel();
            tsSep = new System.Windows.Forms.ToolStripStatusLabel();
            lblDescription = new System.Windows.Forms.ToolStripStatusLabel();
            lblURL = new System.Windows.Forms.ToolStripStatusLabel();
            timer1 = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            pnlParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(414, 558);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsExecute, tsRefresh, tsCancel, tsCopy, tsExcel, tsCols, tsConfigure, cboResults, lblSelectResults, tsParams, tsClearFilter, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1242, 28);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsExecute
            // 
            tsExecute.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsExecute.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExecute.Name = "tsExecute";
            tsExecute.Size = new System.Drawing.Size(84, 25);
            tsExecute.Text = "Execute";
            tsExecute.Visible = false;
            tsExecute.Click += TsRefresh_Click;
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 25);
            tsRefresh.Text = "Refresh Data";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCancel
            // 
            tsCancel.Enabled = false;
            tsCancel.Image = Properties.Resources.Close_red_16x;
            tsCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCancel.Name = "tsCancel";
            tsCancel.Size = new System.Drawing.Size(77, 25);
            tsCancel.Text = "Cancel";
            tsCancel.Click += TsCancel_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 25);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 25);
            tsExcel.Text = "Export to Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 25);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { setTitleToolStripMenuItem, setDescriptionToolStripMenuItem, renameResultSetToolStripMenuItem, associateCollectionToolStripMenuItem, saveLayoutToolStripMenuItem, resetLayoutToolStripMenuItem, editPickersToolStripMenuItem, scriptReportToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 25);
            tsConfigure.Text = "Configure";
            // 
            // setTitleToolStripMenuItem
            // 
            setTitleToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            setTitleToolStripMenuItem.Name = "setTitleToolStripMenuItem";
            setTitleToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            setTitleToolStripMenuItem.Text = "Set Title";
            setTitleToolStripMenuItem.ToolTipText = "Change the name of the report";
            setTitleToolStripMenuItem.Click += SetTitleToolStripMenuItem_Click;
            // 
            // setDescriptionToolStripMenuItem
            // 
            setDescriptionToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            setDescriptionToolStripMenuItem.Name = "setDescriptionToolStripMenuItem";
            setDescriptionToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            setDescriptionToolStripMenuItem.Text = "Set Description";
            setDescriptionToolStripMenuItem.ToolTipText = "Add a description for the report";
            setDescriptionToolStripMenuItem.Click += SetDescriptionToolStripMenuItem_Click;
            // 
            // renameResultSetToolStripMenuItem
            // 
            renameResultSetToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            renameResultSetToolStripMenuItem.Name = "renameResultSetToolStripMenuItem";
            renameResultSetToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            renameResultSetToolStripMenuItem.Text = "Rename Result Set";
            renameResultSetToolStripMenuItem.Click += RenameResultSetToolStripMenuItem_Click;
            // 
            // associateCollectionToolStripMenuItem
            // 
            associateCollectionToolStripMenuItem.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            associateCollectionToolStripMenuItem.Name = "associateCollectionToolStripMenuItem";
            associateCollectionToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            associateCollectionToolStripMenuItem.Text = "Associate Collection";
            associateCollectionToolStripMenuItem.ToolTipText = "Associate Collection - Allow collection to be triggered from report";
            associateCollectionToolStripMenuItem.Click += AssociateCollectionToolStripMenuItem_Click;
            // 
            // saveLayoutToolStripMenuItem
            // 
            saveLayoutToolStripMenuItem.Image = Properties.Resources.Save_16x;
            saveLayoutToolStripMenuItem.Name = "saveLayoutToolStripMenuItem";
            saveLayoutToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            saveLayoutToolStripMenuItem.Text = "Save Layout";
            saveLayoutToolStripMenuItem.ToolTipText = "Saves column visibility, order and size";
            saveLayoutToolStripMenuItem.Click += SaveLayoutToolStripMenuItem_Click;
            // 
            // resetLayoutToolStripMenuItem
            // 
            resetLayoutToolStripMenuItem.Image = Properties.Resources.Undo_grey_16x;
            resetLayoutToolStripMenuItem.Name = "resetLayoutToolStripMenuItem";
            resetLayoutToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            resetLayoutToolStripMenuItem.Text = "Reset Layout";
            resetLayoutToolStripMenuItem.ToolTipText = "Resets column visibility, order and size";
            resetLayoutToolStripMenuItem.Click += ResetLayoutToolStripMenuItem_Click;
            // 
            // editPickersToolStripMenuItem
            // 
            editPickersToolStripMenuItem.Image = Properties.Resources.ReportParameter_16x;
            editPickersToolStripMenuItem.Name = "editPickersToolStripMenuItem";
            editPickersToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            editPickersToolStripMenuItem.Text = "Edit Pickers";
            editPickersToolStripMenuItem.Click += EditPickersToolStripMenuItem_Click;
            // 
            // scriptReportToolStripMenuItem
            // 
            scriptReportToolStripMenuItem.Image = Properties.Resources.SQLScript_16x;
            scriptReportToolStripMenuItem.Name = "scriptReportToolStripMenuItem";
            scriptReportToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            scriptReportToolStripMenuItem.Text = "Script Report";
            scriptReportToolStripMenuItem.ToolTipText = "Generate a script for this custom report to share with other users of DBA Dash";
            scriptReportToolStripMenuItem.Click += ScriptReportToolStripMenuItem_Click;
            // 
            // cboResults
            // 
            cboResults.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            cboResults.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboResults.Name = "cboResults";
            cboResults.Size = new System.Drawing.Size(150, 28);
            cboResults.Visible = false;
            cboResults.SelectedIndexChanged += CboResults_SelectedIndexChanged;
            // 
            // lblSelectResults
            // 
            lblSelectResults.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblSelectResults.Name = "lblSelectResults";
            lblSelectResults.Size = new System.Drawing.Size(102, 25);
            lblSelectResults.Text = "Select Results:";
            lblSelectResults.Visible = false;
            // 
            // tsParams
            // 
            tsParams.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsParams.Image = Properties.Resources.ReportParameter_16x;
            tsParams.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsParams.Name = "tsParams";
            tsParams.Size = new System.Drawing.Size(34, 25);
            tsParams.Text = "Parameters";
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 25);
            tsClearFilter.Text = "Clear Filter";
            tsClearFilter.Click += TsClearFilter_Click;
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(151, 25);
            tsTrigger.Text = "Trigger Collection";
            tsTrigger.Visible = false;
            tsTrigger.Click += TsTrigger_Click;
            // 
            // lnkParams
            // 
            lnkParams.Dock = System.Windows.Forms.DockStyle.Fill;
            lnkParams.Font = new System.Drawing.Font("Segoe UI", 16F);
            lnkParams.Location = new System.Drawing.Point(0, 0);
            lnkParams.Name = "lnkParams";
            lnkParams.Size = new System.Drawing.Size(824, 259);
            lnkParams.TabIndex = 2;
            lnkParams.TabStop = true;
            lnkParams.Text = "Set Parameters";
            lnkParams.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            lnkParams.LinkClicked += LnkParams_LinkClicked;
            // 
            // pnlParams
            // 
            pnlParams.Controls.Add(splitContainer2);
            pnlParams.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlParams.Location = new System.Drawing.Point(0, 0);
            pnlParams.Name = "pnlParams";
            pnlParams.Size = new System.Drawing.Size(824, 558);
            pnlParams.TabIndex = 3;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer2.IsSplitterFixed = true;
            splitContainer2.Location = new System.Drawing.Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(lnkParams);
            splitContainer2.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(lblParamsRequired);
            splitContainer2.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            splitContainer2.Size = new System.Drawing.Size(824, 558);
            splitContainer2.SplitterDistance = 259;
            splitContainer2.TabIndex = 4;
            // 
            // lblParamsRequired
            // 
            lblParamsRequired.Dock = System.Windows.Forms.DockStyle.Fill;
            lblParamsRequired.Font = new System.Drawing.Font("Segoe UI", 10F);
            lblParamsRequired.Location = new System.Drawing.Point(0, 0);
            lblParamsRequired.Name = "lblParamsRequired";
            lblParamsRequired.Size = new System.Drawing.Size(824, 295);
            lblParamsRequired.TabIndex = 3;
            lblParamsRequired.Text = "Parameters are required to run the report";
            lblParamsRequired.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 28);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgv);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pnlParams);
            splitContainer1.Size = new System.Drawing.Size(1242, 558);
            splitContainer1.SplitterDistance = 414;
            splitContainer1.TabIndex = 4;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblTimer, tsSep, lblDescription, lblURL });
            statusStrip1.Location = new System.Drawing.Point(0, 586);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.ShowItemToolTips = true;
            statusStrip1.Size = new System.Drawing.Size(1242, 26);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblTimer
            // 
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new System.Drawing.Size(63, 20);
            lblTimer.Text = "00:00:00";
            // 
            // tsSep
            // 
            tsSep.Name = "tsSep";
            tsSep.Size = new System.Drawing.Size(13, 20);
            tsSep.Text = "|";
            // 
            // lblDescription
            // 
            lblDescription.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new System.Drawing.Size(1151, 20);
            lblDescription.Spring = true;
            lblDescription.Text = "Description...";
            lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblDescription.Visible = false;
            // 
            // lblURL
            // 
            lblURL.IsLink = true;
            lblURL.LinkColor = System.Drawing.Color.FromArgb(14, 65, 108);
            lblURL.Name = "lblURL";
            lblURL.Size = new System.Drawing.Size(0, 20);
            lblURL.Visible = false;
            lblURL.Click += URL_Click;
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += Timer1_Tick;
            // 
            // CustomReportView
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "CustomReportView";
            Size = new System.Drawing.Size(1242, 612);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            pnlParams.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem setTitleToolStripMenuItem;
        private System.Windows.Forms.LinkLabel lnkParams;
        private System.Windows.Forms.Panel pnlParams;
        private System.Windows.Forms.Label lblParamsRequired;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripMenuItem saveLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox cboResults;
        private System.Windows.Forms.ToolStripLabel lblSelectResults;
        private System.Windows.Forms.ToolStripMenuItem renameResultSetToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblDescription;
        private System.Windows.Forms.ToolStripMenuItem setDescriptionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scriptReportToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripDropDownButton tsParams;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.ToolStripMenuItem associateCollectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editPickersToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsExecute;
        private System.Windows.Forms.ToolStripStatusLabel lblURL;
        private System.Windows.Forms.ToolStripStatusLabel lblTimer;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripStatusLabel tsSep;
        private System.Windows.Forms.ToolStripButton tsCancel;
    }
}
