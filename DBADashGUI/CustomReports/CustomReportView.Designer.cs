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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomReportView));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsExecute = new System.Windows.Forms.ToolStripButton();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCancel = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsParams = new System.Windows.Forms.ToolStripDropDownButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            addChartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addSystemChartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            blockingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            iOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            objectExecutionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            performanceCounterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            resourcePoolAnalysisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            waitsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            workloadGroupAnalysisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            associateCollectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            chartLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            chartLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deleteAllChartsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editPickersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            renameResultSetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            resetLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveSystemChartStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            scriptReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setDescriptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setTitleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cboResults = new System.Windows.Forms.ToolStripComboBox();
            lblSelectResults = new System.Windows.Forms.ToolStripLabel();
            tsScriptResults = new System.Windows.Forms.ToolStripDropDownButton();
            scriptDataTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            scriptGridsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            tsReset = new System.Windows.Forms.ToolStripButton();
            tsNewWindow = new System.Windows.Forms.ToolStripButton();
            splitToggle1 = new System.Windows.Forms.ToolStripSeparator();
            tsToggleCharts = new System.Windows.Forms.ToolStripButton();
            tsToggleGrids = new System.Windows.Forms.ToolStripButton();
            splitToggle2 = new System.Windows.Forms.ToolStripSeparator();
            lnkParams = new System.Windows.Forms.LinkLabel();
            pnlParams = new System.Windows.Forms.Panel();
            splitParams = new System.Windows.Forms.SplitContainer();
            lblParamsRequired = new System.Windows.Forms.Label();
            splitResultsAndParams = new System.Windows.Forms.SplitContainer();
            splitTablesCharts = new System.Windows.Forms.SplitContainer();
            chartLayout = new System.Windows.Forms.TableLayoutPanel();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblTimer = new System.Windows.Forms.ToolStripStatusLabel();
            tsSep = new System.Windows.Forms.ToolStripStatusLabel();
            lblURL = new System.Windows.Forms.ToolStripStatusLabel();
            lblDescription = new System.Windows.Forms.ToolStripStatusLabel();
            timer1 = new System.Windows.Forms.Timer(components);
            toolStrip1.SuspendLayout();
            pnlParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitParams).BeginInit();
            splitParams.Panel1.SuspendLayout();
            splitParams.Panel2.SuspendLayout();
            splitParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitResultsAndParams).BeginInit();
            splitResultsAndParams.Panel1.SuspendLayout();
            splitResultsAndParams.Panel2.SuspendLayout();
            splitResultsAndParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitTablesCharts).BeginInit();
            splitTablesCharts.Panel1.SuspendLayout();
            splitTablesCharts.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsExecute, tsRefresh, tsCancel, tsCopy, tsParams, tsExcel, tsCols, tsConfigure, cboResults, lblSelectResults, tsScriptResults, tsClearFilter, tsTrigger, tsReset, tsNewWindow, splitToggle1, tsToggleCharts, tsToggleGrids, splitToggle2 });
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
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { addChartToolStripMenuItem, addSystemChartToolStripMenuItem, associateCollectionToolStripMenuItem, chartLayoutToolStripMenuItem, chartLocationToolStripMenuItem, deleteAllChartsToolStripMenuItem, editPickersToolStripMenuItem, renameResultSetToolStripMenuItem, resetLayoutToolStripMenuItem, saveLayoutToolStripMenuItem, saveSystemChartStateToolStripMenuItem, scriptReportToolStripMenuItem, setDescriptionToolStripMenuItem, setTitleToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            // 
            // addChartToolStripMenuItem
            // 
            addChartToolStripMenuItem.Image = Properties.Resources.StackedAreaChart;
            addChartToolStripMenuItem.Name = "addChartToolStripMenuItem";
            addChartToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            addChartToolStripMenuItem.Text = "Add Chart";
            addChartToolStripMenuItem.Click += AddChartToolStripMenuItem_Click;
            // 
            // addSystemChartToolStripMenuItem
            // 
            addSystemChartToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { blockingToolStripMenuItem, cPUToolStripMenuItem, iOToolStripMenuItem, objectExecutionToolStripMenuItem, performanceCounterToolStripMenuItem, resourcePoolAnalysisToolStripMenuItem, waitsToolStripMenuItem, workloadGroupAnalysisToolStripMenuItem });
            addSystemChartToolStripMenuItem.Image = Properties.Resources.StackedAreaChart;
            addSystemChartToolStripMenuItem.Name = "addSystemChartToolStripMenuItem";
            addSystemChartToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            addSystemChartToolStripMenuItem.Text = "Add System Chart";
            // 
            // blockingToolStripMenuItem
            // 
            blockingToolStripMenuItem.Name = "blockingToolStripMenuItem";
            blockingToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            blockingToolStripMenuItem.Tag = "Blocking";
            blockingToolStripMenuItem.Text = "Blocking";
            blockingToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // cPUToolStripMenuItem
            // 
            cPUToolStripMenuItem.Name = "cPUToolStripMenuItem";
            cPUToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            cPUToolStripMenuItem.Tag = "CPU";
            cPUToolStripMenuItem.Text = "CPU";
            cPUToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // iOToolStripMenuItem
            // 
            iOToolStripMenuItem.Name = "iOToolStripMenuItem";
            iOToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            iOToolStripMenuItem.Tag = "IO";
            iOToolStripMenuItem.Text = "IO";
            iOToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // objectExecutionToolStripMenuItem
            // 
            objectExecutionToolStripMenuItem.Name = "objectExecutionToolStripMenuItem";
            objectExecutionToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            objectExecutionToolStripMenuItem.Tag = "ObjectExecution";
            objectExecutionToolStripMenuItem.Text = "Object Execution";
            objectExecutionToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // performanceCounterToolStripMenuItem
            // 
            performanceCounterToolStripMenuItem.Name = "performanceCounterToolStripMenuItem";
            performanceCounterToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            performanceCounterToolStripMenuItem.Text = "Performance Counter";
            performanceCounterToolStripMenuItem.Click += AddPerformanceCounter;
            // 
            // resourcePoolAnalysisToolStripMenuItem
            // 
            resourcePoolAnalysisToolStripMenuItem.Name = "resourcePoolAnalysisToolStripMenuItem";
            resourcePoolAnalysisToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            resourcePoolAnalysisToolStripMenuItem.Tag = "ResourceGovernorResourcePools";
            resourcePoolAnalysisToolStripMenuItem.Text = "Resource Pool Analysis";
            resourcePoolAnalysisToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // waitsToolStripMenuItem
            // 
            waitsToolStripMenuItem.Name = "waitsToolStripMenuItem";
            waitsToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            waitsToolStripMenuItem.Tag = "Waits";
            waitsToolStripMenuItem.Text = "Waits";
            waitsToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // workloadGroupAnalysisToolStripMenuItem
            // 
            workloadGroupAnalysisToolStripMenuItem.Name = "workloadGroupAnalysisToolStripMenuItem";
            workloadGroupAnalysisToolStripMenuItem.Size = new System.Drawing.Size(258, 26);
            workloadGroupAnalysisToolStripMenuItem.Tag = "ResourceGovernorWorkloadGroups";
            workloadGroupAnalysisToolStripMenuItem.Text = "Workload Group Analysis";
            workloadGroupAnalysisToolStripMenuItem.Click += AddSystemChart_Click;
            // 
            // associateCollectionToolStripMenuItem
            // 
            associateCollectionToolStripMenuItem.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            associateCollectionToolStripMenuItem.Name = "associateCollectionToolStripMenuItem";
            associateCollectionToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            associateCollectionToolStripMenuItem.Text = "Associate Collection";
            associateCollectionToolStripMenuItem.ToolTipText = "Associate Collection - Allow collection to be triggered from report";
            associateCollectionToolStripMenuItem.Click += AssociateCollectionToolStripMenuItem_Click;
            // 
            // chartLayoutToolStripMenuItem
            // 
            chartLayoutToolStripMenuItem.Name = "chartLayoutToolStripMenuItem";
            chartLayoutToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            chartLayoutToolStripMenuItem.Text = "Chart Layout";
            chartLayoutToolStripMenuItem.Click += ChartLayout_Click;
            // 
            // chartLocationToolStripMenuItem
            // 
            chartLocationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { topToolStripMenuItem, bottomToolStripMenuItem, leftToolStripMenuItem, rightToolStripMenuItem });
            chartLocationToolStripMenuItem.Name = "chartLocationToolStripMenuItem";
            chartLocationToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            chartLocationToolStripMenuItem.Text = "Chart Location";
            // 
            // topToolStripMenuItem
            // 
            topToolStripMenuItem.Name = "topToolStripMenuItem";
            topToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            topToolStripMenuItem.Tag = "Top";
            topToolStripMenuItem.Text = "Top";
            topToolStripMenuItem.Click += SetChartLocation;
            // 
            // bottomToolStripMenuItem
            // 
            bottomToolStripMenuItem.Name = "bottomToolStripMenuItem";
            bottomToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            bottomToolStripMenuItem.Tag = "Bottom";
            bottomToolStripMenuItem.Text = "Bottom";
            bottomToolStripMenuItem.Click += SetChartLocation;
            // 
            // leftToolStripMenuItem
            // 
            leftToolStripMenuItem.Name = "leftToolStripMenuItem";
            leftToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            leftToolStripMenuItem.Tag = "Left";
            leftToolStripMenuItem.Text = "Left";
            leftToolStripMenuItem.Click += SetChartLocation;
            // 
            // rightToolStripMenuItem
            // 
            rightToolStripMenuItem.Name = "rightToolStripMenuItem";
            rightToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            rightToolStripMenuItem.Tag = "Right";
            rightToolStripMenuItem.Text = "Right";
            rightToolStripMenuItem.Click += SetChartLocation;
            // 
            // deleteAllChartsToolStripMenuItem
            // 
            deleteAllChartsToolStripMenuItem.Image = Properties.Resources.Close_red_16x;
            deleteAllChartsToolStripMenuItem.Name = "deleteAllChartsToolStripMenuItem";
            deleteAllChartsToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            deleteAllChartsToolStripMenuItem.Text = "Delete All Charts";
            deleteAllChartsToolStripMenuItem.Click += DeleteAllCharts;
            // 
            // editPickersToolStripMenuItem
            // 
            editPickersToolStripMenuItem.Image = Properties.Resources.ReportParameter_16x;
            editPickersToolStripMenuItem.Name = "editPickersToolStripMenuItem";
            editPickersToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            editPickersToolStripMenuItem.Text = "Edit Pickers";
            editPickersToolStripMenuItem.Click += EditPickersToolStripMenuItem_Click;
            // 
            // renameResultSetToolStripMenuItem
            // 
            renameResultSetToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            renameResultSetToolStripMenuItem.Name = "renameResultSetToolStripMenuItem";
            renameResultSetToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            renameResultSetToolStripMenuItem.Text = "Rename Result Set";
            renameResultSetToolStripMenuItem.Click += RenameResultSet_Click;
            // 
            // resetLayoutToolStripMenuItem
            // 
            resetLayoutToolStripMenuItem.Image = Properties.Resources.Undo_grey_16x;
            resetLayoutToolStripMenuItem.Name = "resetLayoutToolStripMenuItem";
            resetLayoutToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            resetLayoutToolStripMenuItem.Text = "Reset Layout";
            resetLayoutToolStripMenuItem.ToolTipText = "Resets column visibility, order and size";
            resetLayoutToolStripMenuItem.Click += ResetLayoutToolStripMenuItem_Click;
            // 
            // saveLayoutToolStripMenuItem
            // 
            saveLayoutToolStripMenuItem.Image = Properties.Resources.Save_16x;
            saveLayoutToolStripMenuItem.Name = "saveLayoutToolStripMenuItem";
            saveLayoutToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            saveLayoutToolStripMenuItem.Text = "Save Layout";
            saveLayoutToolStripMenuItem.ToolTipText = "Saves column visibility, order and size";
            saveLayoutToolStripMenuItem.Click += SaveLayoutToolStripMenuItem_Click;
            // 
            // saveSystemChartStateToolStripMenuItem
            // 
            saveSystemChartStateToolStripMenuItem.Image = Properties.Resources.Save_16x;
            saveSystemChartStateToolStripMenuItem.Name = "saveSystemChartStateToolStripMenuItem";
            saveSystemChartStateToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            saveSystemChartStateToolStripMenuItem.Text = "Save System Chart State";
            saveSystemChartStateToolStripMenuItem.Visible = false;
            saveSystemChartStateToolStripMenuItem.Click += SaveSystemChartState;
            // 
            // scriptReportToolStripMenuItem
            // 
            scriptReportToolStripMenuItem.Image = Properties.Resources.SQLScript_16x;
            scriptReportToolStripMenuItem.Name = "scriptReportToolStripMenuItem";
            scriptReportToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            scriptReportToolStripMenuItem.Text = "Script Report";
            scriptReportToolStripMenuItem.ToolTipText = "Generate a script for this custom report to share with other users of DBA Dash";
            scriptReportToolStripMenuItem.Click += ScriptReportToolStripMenuItem_Click;
            // 
            // setDescriptionToolStripMenuItem
            // 
            setDescriptionToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            setDescriptionToolStripMenuItem.Name = "setDescriptionToolStripMenuItem";
            setDescriptionToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            setDescriptionToolStripMenuItem.Text = "Set Description";
            setDescriptionToolStripMenuItem.ToolTipText = "Add a description for the report";
            setDescriptionToolStripMenuItem.Click += SetDescriptionToolStripMenuItem_Click;
            // 
            // setTitleToolStripMenuItem
            // 
            setTitleToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            setTitleToolStripMenuItem.Name = "setTitleToolStripMenuItem";
            setTitleToolStripMenuItem.Size = new System.Drawing.Size(251, 26);
            setTitleToolStripMenuItem.Text = "Set Title";
            setTitleToolStripMenuItem.ToolTipText = "Change the name of the report";
            setTitleToolStripMenuItem.Click += SetTitleToolStripMenuItem_Click;
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
            // splitToggle1
            // 
            splitToggle1.Name = "splitToggle1";
            splitToggle1.Size = new System.Drawing.Size(6, 31);
            // 
            // tsToggleCharts
            // 
            tsToggleCharts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsToggleCharts.Image = (System.Drawing.Image)resources.GetObject("tsToggleCharts.Image");
            tsToggleCharts.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsToggleCharts.Name = "tsToggleCharts";
            tsToggleCharts.Size = new System.Drawing.Size(29, 28);
            tsToggleCharts.Text = "Toggle Charts";
            tsToggleCharts.Click += ToggleCharts_Click;
            // 
            // tsToggleGrids
            // 
            tsToggleGrids.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsToggleGrids.Image = Properties.Resources.Table_16x;
            tsToggleGrids.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsToggleGrids.Name = "tsToggleGrids";
            tsToggleGrids.Size = new System.Drawing.Size(29, 28);
            tsToggleGrids.Text = "Toggle Grid";
            tsToggleGrids.Click += ToggleGrids;
            // 
            // splitToggle2
            // 
            splitToggle2.Name = "splitToggle2";
            splitToggle2.Size = new System.Drawing.Size(6, 31);
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
            pnlParams.Controls.Add(splitParams);
            pnlParams.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlParams.Location = new System.Drawing.Point(0, 0);
            pnlParams.Name = "pnlParams";
            pnlParams.Size = new System.Drawing.Size(824, 555);
            pnlParams.TabIndex = 3;
            // 
            // splitParams
            // 
            splitParams.Dock = System.Windows.Forms.DockStyle.Fill;
            splitParams.IsSplitterFixed = true;
            splitParams.Location = new System.Drawing.Point(0, 0);
            splitParams.Name = "splitParams";
            splitParams.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitParams.Panel1
            // 
            splitParams.Panel1.Controls.Add(lnkParams);
            splitParams.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // splitParams.Panel2
            // 
            splitParams.Panel2.Controls.Add(lblParamsRequired);
            splitParams.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            splitParams.Size = new System.Drawing.Size(824, 555);
            splitParams.SplitterDistance = 251;
            splitParams.TabIndex = 4;
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
            // splitResultsAndParams
            // 
            splitResultsAndParams.Dock = System.Windows.Forms.DockStyle.Fill;
            splitResultsAndParams.Location = new System.Drawing.Point(0, 31);
            splitResultsAndParams.Name = "splitResultsAndParams";
            // 
            // splitResultsAndParams.Panel1
            // 
            splitResultsAndParams.Panel1.Controls.Add(splitTablesCharts);
            // 
            // splitResultsAndParams.Panel2
            // 
            splitResultsAndParams.Panel2.Controls.Add(pnlParams);
            splitResultsAndParams.Size = new System.Drawing.Size(1242, 555);
            splitResultsAndParams.SplitterDistance = 414;
            splitResultsAndParams.TabIndex = 4;
            // 
            // splitTablesCharts
            // 
            splitTablesCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            splitTablesCharts.Location = new System.Drawing.Point(0, 0);
            splitTablesCharts.Name = "splitTablesCharts";
            splitTablesCharts.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitTablesCharts.Panel1
            // 
            splitTablesCharts.Panel1.Controls.Add(chartLayout);
            splitTablesCharts.Panel1Collapsed = true;
            // 
            // splitTablesCharts.Panel2
            // 
            splitTablesCharts.Panel2.AutoScroll = true;
            splitTablesCharts.Size = new System.Drawing.Size(414, 555);
            splitTablesCharts.SplitterDistance = 305;
            splitTablesCharts.TabIndex = 0;
            // 
            // chartLayout
            // 
            chartLayout.ColumnCount = 1;
            chartLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            chartLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            chartLayout.Location = new System.Drawing.Point(0, 0);
            chartLayout.Name = "chartLayout";
            chartLayout.RowCount = 1;
            chartLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            chartLayout.Size = new System.Drawing.Size(414, 305);
            chartLayout.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblTimer, tsSep, lblURL, lblDescription });
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
            // lblURL
            // 
            lblURL.IsLink = true;
            lblURL.LinkColor = System.Drawing.Color.FromArgb(14, 65, 108);
            lblURL.Name = "lblURL";
            lblURL.Size = new System.Drawing.Size(0, 20);
            lblURL.Visible = false;
            lblURL.Click += URL_Click;
            // 
            // lblDescription
            // 
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new System.Drawing.Size(94, 20);
            lblDescription.Text = "Description...";
            lblDescription.Visible = false;
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
            Controls.Add(splitResultsAndParams);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            DoubleBuffered = true;
            Name = "CustomReportView";
            Size = new System.Drawing.Size(1242, 612);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            pnlParams.ResumeLayout(false);
            splitParams.Panel1.ResumeLayout(false);
            splitParams.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitParams).EndInit();
            splitParams.ResumeLayout(false);
            splitResultsAndParams.Panel1.ResumeLayout(false);
            splitResultsAndParams.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitResultsAndParams).EndInit();
            splitResultsAndParams.ResumeLayout(false);
            splitTablesCharts.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitTablesCharts).EndInit();
            splitTablesCharts.ResumeLayout(false);
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
        private System.Windows.Forms.SplitContainer splitResultsAndParams;
        private System.Windows.Forms.SplitContainer splitParams;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripMenuItem saveLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox cboResults;
        private System.Windows.Forms.ToolStripLabel lblSelectResults;
        private System.Windows.Forms.ToolStripMenuItem renameResultSetToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
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
        private System.Windows.Forms.ToolStripStatusLabel lblDescription;
        private System.Windows.Forms.SplitContainer splitTablesCharts;
        private System.Windows.Forms.TableLayoutPanel chartLayout;
        private System.Windows.Forms.ToolStripButton tsToggleCharts;
        private System.Windows.Forms.ToolStripButton tsToggleGrids;
        private System.Windows.Forms.ToolStripSeparator splitToggle1;
        private System.Windows.Forms.ToolStripSeparator splitToggle2;
        private System.Windows.Forms.ToolStripMenuItem addChartToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem chartLocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addSystemChartToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blockingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem iOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem objectExecutionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem workloadGroupAnalysisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resourcePoolAnalysisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem waitsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSystemChartStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem performanceCounterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteAllChartsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem chartLayoutToolStripMenuItem;
    }
}
