﻿namespace DBADashGUI.CustomReports
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
            tsScriptResults = new System.Windows.Forms.ToolStripDropDownButton();
            scriptDataTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            scriptGridsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            tsNewWindow = new System.Windows.Forms.ToolStripButton();
            tsReset = new System.Windows.Forms.ToolStripButton();
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
            toolStrip1.SuspendLayout();
            pnlParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsExecute, tsRefresh, tsCancel, tsCopy, tsExcel, tsCols, tsConfigure, cboResults, lblSelectResults, tsParams, tsScriptResults, tsClearFilter, tsTrigger, tsReset, tsNewWindow });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip1.Size = new System.Drawing.Size(1242, 31);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsExecute
            // 
            tsExecute.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsExecute.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExecute.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsExecute.Name = "tsExecute";
            tsExecute.Size = new System.Drawing.Size(84, 24);
            tsExecute.Text = "Execute";
            tsExecute.Visible = false;
            tsExecute.Click += TsRefresh_Click;
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 24);
            tsRefresh.Text = "Refresh Data";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCancel
            // 
            tsCancel.Enabled = false;
            tsCancel.Image = Properties.Resources.Close_red_16x;
            tsCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCancel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCancel.Name = "tsCancel";
            tsCancel.Size = new System.Drawing.Size(77, 24);
            tsCancel.Text = "Cancel";
            tsCancel.Click += TsCancel_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += Copy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export to Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += Columns_Click;
            // 
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { setTitleToolStripMenuItem, setDescriptionToolStripMenuItem, renameResultSetToolStripMenuItem, associateCollectionToolStripMenuItem, saveLayoutToolStripMenuItem, resetLayoutToolStripMenuItem, editPickersToolStripMenuItem, scriptReportToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
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
            renameResultSetToolStripMenuItem.Click += RenameResultSet_Click;
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
            cboResults.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            cboResults.Name = "cboResults";
            cboResults.Size = new System.Drawing.Size(150, 31);
            cboResults.Visible = false;
            cboResults.SelectedIndexChanged += CboResults_SelectedIndexChanged;
            // 
            // lblSelectResults
            // 
            lblSelectResults.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblSelectResults.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            lblSelectResults.Name = "lblSelectResults";
            lblSelectResults.Size = new System.Drawing.Size(102, 24);
            lblSelectResults.Text = "Select Results:";
            lblSelectResults.Visible = false;
            // 
            // tsParams
            // 
            tsParams.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsParams.Image = Properties.Resources.ReportParameter_16x;
            tsParams.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsParams.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsParams.Name = "tsParams";
            tsParams.Size = new System.Drawing.Size(34, 24);
            tsParams.Text = "Parameters";
            // 
            // tsScriptResults
            // 
            tsScriptResults.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsScriptResults.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { scriptDataTablesToolStripMenuItem, scriptGridsToolStripMenuItem });
            tsScriptResults.Image = Properties.Resources.TableScript_16x;
            tsScriptResults.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsScriptResults.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsScriptResults.Name = "tsScriptResults";
            tsScriptResults.Size = new System.Drawing.Size(34, 24);
            tsScriptResults.Text = "Script Results";
            // 
            // scriptDataTablesToolStripMenuItem
            // 
            scriptDataTablesToolStripMenuItem.Image = Properties.Resources.SQLScript_16x;
            scriptDataTablesToolStripMenuItem.Name = "scriptDataTablesToolStripMenuItem";
            scriptDataTablesToolStripMenuItem.Size = new System.Drawing.Size(211, 26);
            scriptDataTablesToolStripMenuItem.Text = "Script Data Tables";
            // 
            // scriptGridsToolStripMenuItem
            // 
            scriptGridsToolStripMenuItem.Image = Properties.Resources.TableScript_16x;
            scriptGridsToolStripMenuItem.Name = "scriptGridsToolStripMenuItem";
            scriptGridsToolStripMenuItem.Size = new System.Drawing.Size(211, 26);
            scriptGridsToolStripMenuItem.Text = "Script Grids";
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            tsClearFilter.Click += TsClearFilter_Click;
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(151, 24);
            tsTrigger.Text = "Trigger Collection";
            tsTrigger.Visible = false;
            tsTrigger.Click += TsTrigger_Click;
            // 
            // tsNewWindow
            // 
            tsNewWindow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsNewWindow.Image = Properties.Resources.NewWindow_16x;
            tsNewWindow.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsNewWindow.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsNewWindow.Name = "tsNewWindow";
            tsNewWindow.Size = new System.Drawing.Size(29, 24);
            tsNewWindow.Text = "Open in new window";
            tsNewWindow.Click += TsNewWindow_Click;
            // 
            // tsReset
            // 
            tsReset.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsReset.Image = Properties.Resources.RestoreDefaultView_16x;
            tsReset.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsReset.Name = "tsReset";
            tsReset.Size = new System.Drawing.Size(29, 28);
            tsReset.Text = "Restore Default View";
            tsReset.ToolTipText = "Restore default view";
            tsReset.Click += Reset_Click;
            // 
            // lnkParams
            // 
            lnkParams.Dock = System.Windows.Forms.DockStyle.Fill;
            lnkParams.Font = new System.Drawing.Font("Segoe UI", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lnkParams.Location = new System.Drawing.Point(0, 0);
            lnkParams.Name = "lnkParams";
            lnkParams.Size = new System.Drawing.Size(824, 251);
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
            pnlParams.Size = new System.Drawing.Size(824, 555);
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
            splitContainer2.Size = new System.Drawing.Size(824, 555);
            splitContainer2.SplitterDistance = 251;
            splitContainer2.TabIndex = 4;
            // 
            // lblParamsRequired
            // 
            lblParamsRequired.Dock = System.Windows.Forms.DockStyle.Fill;
            lblParamsRequired.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblParamsRequired.Location = new System.Drawing.Point(0, 0);
            lblParamsRequired.Name = "lblParamsRequired";
            lblParamsRequired.Size = new System.Drawing.Size(824, 300);
            lblParamsRequired.TabIndex = 3;
            lblParamsRequired.Text = "Parameters are required to run the report";
            lblParamsRequired.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 31);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.AutoScroll = true;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pnlParams);
            splitContainer1.Size = new System.Drawing.Size(1242, 555);
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
            AutoScroll = true;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            DoubleBuffered = true;
            Name = "CustomReportView";
            Size = new System.Drawing.Size(1242, 612);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            pnlParams.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
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
        private System.Windows.Forms.ToolStripButton tsNewWindow;
        private System.Windows.Forms.ToolStripDropDownButton tsScriptResults;
        private System.Windows.Forms.ToolStripMenuItem scriptDataTablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scriptGridsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsReset;
    }
}
