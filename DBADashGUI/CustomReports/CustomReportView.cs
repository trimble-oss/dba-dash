using DBADash;
using DBADash.Messaging;
using DBADashGUI.Charts;
using DBADashGUI.CommunityTools;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Performance;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using LiveChartsCore.SkiaSharpView.WinForms;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataTable = System.Data.DataTable;

namespace DBADashGUI.CustomReports
{
    public partial class CustomReportView : UserControl, ISetContext, IRefreshData, ISetStatus, INavigation
    {
        public event EventHandler ReportNameChanged;

        protected DataSet reportDS;
        private bool suppressCboResultsIndexChanged;
        public ToolStripStatusLabel StatusLabel => lblDescription;
        protected DBADashContext context;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CustomReport Report { get; set; }

        protected List<CustomSqlParameter> customParams = new();
        /// <summary>
        /// When true, the Report property will not be overwritten by SetContext when a
        /// context contains a Report. This allows hosts to lock the initially-assigned
        /// Report (for example, the Performance report) so subsequent SetContext calls
        /// don't replace it.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool PreventReportOverwrite { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        private Guid CurrentMessageGroup;
        public EventHandler PostGridRefresh;
        public ToolStrip ToolStrip => toolStrip1;
        public DateTime RefreshDate { private set; get; }

        public TimeSpan TimeSinceRefresh => DateTime.Now.Subtract(RefreshDate);

        private bool AutoLoad => Report is not DirectExecutionReport;

        private const string ResultSetLabelControlName = "ResultLabel";

        private ChartLayoutHelper chartLayoutHelper = new();

        // Keep track of GridFilterChanged handlers so we can unsubscribe them when disposing/clearing
        private readonly Dictionary<DBADashDataGridView, EventHandler> gridFilterHandlers = new();

        // Caches each grid's summed row height (the only O(rowCount) part of NaturalPanelHeight) so resize events,
        // which fire frequently during a window/splitter drag, don't re-walk every row. Keyed by grid and validated
        // against the row count; cleared with the grids in CleanupGrids.
        private readonly Dictionary<DBADashDataGridView, (int RowCount, int RowsHeight)> rowsHeightCache = new();

        private bool wasTableCollapsedBeforeMaximize = false;

        #region Navigation stack for in-place drill-down

        /// <summary>
        /// Represents a saved navigation state for back-navigation.  <see cref="StatusFilter"/> captures the
        /// status filter selection (null when the report has no status filter) so back-navigation restores it -
        /// the checkbox state is a separate source of truth from <see cref="Params"/> and would otherwise be
        /// re-applied stale over the restored parameters by <see cref="ApplyStatusFilterParams"/>.
        /// </summary>
        private sealed record NavigationState(CustomReport Report, DBADashContext Context, List<CustomSqlParameter> Params, Dictionary<int, string> GridFilters, StatusFilterSelection StatusFilter);

        /// <summary>
        /// Snapshot of the status filter selection for back-navigation.
        /// </summary>
        private sealed record StatusFilterSelection(bool Critical, bool Warning, bool NA, bool OK, bool Acknowledged);

        private readonly Stack<NavigationState> navigationStack = new();
        private ToolStripButton tsBack;

        /// <summary>
        /// Programmatic grid filters applied by drill-down navigation.
        /// These are re-applied on every grid refresh so they persist across tab changes.
        /// Key = result set index, Value = DataView RowFilter expression.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<int, string> DrillDownGridFilters { get; set; }

        public bool CanNavigateBack => navigationStack.Count > 0;

        public bool NavigateBack()
        {
            if (!CanNavigateBack) return false;
            var state = navigationStack.Pop();
            Report = state.Report;
            DrillDownGridFilters = state.GridFilters;
            // Restore the status filter selection before refreshing: the refresh re-applies the checkbox state into
            // the @Include* parameters, so it must reflect the saved state rather than whatever the drill-down left.
            RestoreStatusFilter(state.StatusFilter);
            if (state.Context == context)
            {
                // Context reference unchanged (e.g., switching between summary/detail on same tree node).
                // Restore params and refresh directly since SetContext would short-circuit.
                customParams = state.Params;
                RefreshData();
            }
            else
            {
                _ = SetContext(state.Context, state.Params);
            }
            UpdateBackButtonVisibility();
            return true;
        }

        /// <summary>
        /// Saves the current report, context, and parameters onto the navigation stack.
        /// Call before modifying the view in-place (e.g., drill-down or report switch).
        /// </summary>
        public void PushNavigationState()
        {
            navigationStack.Push(new NavigationState(Report, context, new List<CustomSqlParameter>(customParams), DrillDownGridFilters, CaptureStatusFilter()));
            UpdateBackButtonVisibility();
        }

        /// <summary>
        /// Clears the entire navigation stack (e.g., when returning to a known home state).
        /// </summary>
        public void ClearNavigationStack()
        {
            navigationStack.Clear();
            UpdateBackButtonVisibility();
        }

        private void UpdateBackButtonVisibility()
        {
            if (tsBack != null)
            {
                tsBack.Visible = navigationStack.Count > 0;
            }
        }

        private void AddBackButton()
        {
            tsBack = new ToolStripButton() { ToolTipText = "Navigate back to previous view", Visible = false, Image = Properties.Resources.arrow_back_16xLG, DisplayStyle = ToolStripItemDisplayStyle.Image };
            tsBack.Click += (_, _) => NavigateBack();
            toolStrip1.Items.Insert(0, tsBack);
        }


        private void TsSinglePage_CheckedChanged(object sender, EventArgs e)
        {
            if (Report == null || Report.SinglePageLayout == tsToggleSinglePage.Checked) return;
            Report.SinglePageLayout = tsToggleSinglePage.Checked;
            if (Report.CanEditReport)
            {
                try { Report.Update(); } catch (Exception ex) { Debug.WriteLine($"Error saving SinglePageLayout: {ex}"); }
            }
            ApplyLayoutMode();
        }

        /// <summary>
        /// Applies the current layout mode to the table container: a scrollable report lets the stacked result
        /// sets expand and the page scroll, a single-page report fits them into the available height.
        /// </summary>
        private void ApplyLayoutMode()
        {
            var multi = TablePanel.Controls.OfType<Panel>().Count(p => p.Tag is int) > 1;
            TablePanel.AutoScroll = multi && !(Report?.SinglePageLayout ?? false);
            ResizeResultPanels();
        }

        #endregion

        // Map chart location to the appropriate split panel.
        // For Top or Left charts we host them in Panel1; for Bottom or Right use Panel2.
        private Panel ChartPanel => Report != null && Report.ChartLocation is CustomReport.ChartLocations.Top or CustomReport.ChartLocations.Left ? splitTablesCharts.Panel1 : splitTablesCharts.Panel2;

        private Panel TablePanel => Report != null && ChartPanel == splitTablesCharts.Panel1 ? splitTablesCharts.Panel2 : splitTablesCharts.Panel1;

        // Helpers to set collapsed state on the underlying split panels depending on which side
        private void SetChartPanelCollapsed(bool collapsed)
        {
            if (splitTablesCharts.Panel1 == ChartPanel) splitTablesCharts.Panel1Collapsed = collapsed;
            else splitTablesCharts.Panel2Collapsed = collapsed;
        }

        private void SetTablePanelCollapsed(bool collapsed)
        {
            if (splitTablesCharts.Panel1 == TablePanel) splitTablesCharts.Panel1Collapsed = collapsed;
            else splitTablesCharts.Panel2Collapsed = collapsed;
        }

        private bool IsChartPanelCollapsed()
        {
            return splitTablesCharts.Panel1 == ChartPanel ? splitTablesCharts.Panel1Collapsed : splitTablesCharts.Panel2Collapsed;
        }

        private bool IsTablePanelCollapsed()
        {
            return splitTablesCharts.Panel1 == TablePanel ? splitTablesCharts.Panel1Collapsed : splitTablesCharts.Panel2Collapsed;
        }

        public CustomReportView()
        {
            Grids = new();
            InitializeComponent();
            AddBackButton();
            // Re-flow stacked result sets when the available area changes (window resize / splitter move).
            splitTablesCharts.Panel1.SizeChanged += (_, _) => { if (Grids.Count > 0) ResizeResultPanels(); };
            splitTablesCharts.Panel2.SizeChanged += (_, _) => { if (Grids.Count > 0) ResizeResultPanels(); };
            ShowParamPrompt(false);
            this.ApplyTheme();
            scriptDataTablesToolStripMenuItem.Click += (_, _) => ScriptDataTables(false);
            scriptGridsToolStripMenuItem.Click += (_, _) => ScriptDataTables(true);
            chartLayoutHelper.PanelMaximizeChanged += ChartLayoutHelper_PanelMaximizeChanged;
            // Ensure managed resources are cleaned up when the control is disposed
            this.Disposed += (s, e) => DisposeManagedResources();
            chartLayout.ColumnStyles.Clear();
            chartLayout.ColumnCount = 1;
            chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        }

        private void DisposeManagedResources()
        {
            try
            {
                // Unsubscribe events from chartLayoutHelper
                if (chartLayoutHelper != null)
                {
                    chartLayoutHelper.PanelMaximizeChanged -= ChartLayoutHelper_PanelMaximizeChanged;
                }
                // Perform UI cleanup (dispose controls, unhook handlers)
                CleanupUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisposeManagedResources error: {ex}");
            }
        }

        // Centralized cleanup routine used by ClearResults and DisposeManagedResources to avoid duplication
        private void CleanupUI()
        {
            try
            {
                // If the designer-owned TableLayoutPanel (`chartLayout`) exists, disable resizing and
                // unhook events for its child panels without disposing the TableLayoutPanel itself.
                // If `chartLayout` is not available, fall back to disposing any table layouts found in the split panels.
                try
                {
                    Action<Control> unhook = control =>
                    {
                        if (control is Panel pnl)
                        {
                            foreach (var ts in pnl.Controls.OfType<ToolStrip>())
                            {
                                foreach (ToolStripItem item in ts.Items)
                                {
                                    try { if (item is ToolStripButton btn) btn.Click -= Maximize_Click; } catch { }
                                }
                            }

                            // Ensure layout is visible and charts are invalidated so they render
                            try
                            {
                                chartLayout.Visible = true;
                                chartLayout.ResumeLayout(true);
                                chartLayout.Invalidate(true);
                                foreach (var panelChild in chartLayout.Controls.OfType<Panel>())
                                {
                                    foreach (Control child in panelChild.Controls)
                                    {
                                        try
                                        {
                                            child.Visible = true;
                                            child.Invalidate();
                                            child.Refresh();
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"CleanupUI error: {ex}");
                            }
                        }
                    };

                    if (chartLayout != null)
                    {
                        try { chartLayoutHelper.DisableResizingAndUnhookTableLayout(chartLayout, unhook); } catch { }
                    }
                    else
                    {
                        try { chartLayoutHelper.DisposeTableLayoutWithResizablePanels(splitTablesCharts.Panel1, unhook); } catch { }
                        try { chartLayoutHelper.DisposeTableLayoutWithResizablePanels(splitTablesCharts.Panel2, unhook); } catch { }
                    }
                }
                catch { }

                // Unsubscribe and dispose grids
                try { CleanupGrids(); } catch (Exception ex) { Debug.WriteLine($"CleanupUI: CleanupGrids error: {ex}"); }

                // Dispose and clear chart panels (separate helper so it can be reused)
                try { CleanupCharts(); } catch (Exception ex) { Debug.WriteLine($"CleanupUI: CleanupCharts error: {ex}"); }

                previousSchema = string.Empty;
                try { UpdateClearFilter(); } catch { }
            }
            catch { }
        }

        private void ChartLayoutHelper_PanelMaximizeChanged(object sender, ChartLayoutHelper.PanelMaximizeChangedEventArgs e)
        {
            if (e.IsMaximized)
            {
                wasTableCollapsedBeforeMaximize = IsTablePanelCollapsed();
            }

            SetTablePanelCollapsed(e.IsMaximized || wasTableCollapsedBeforeMaximize);
        }

        // Cleanup helper for grids
        private void CleanupGrids()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((Action)CleanupGrids);
                    return;
                }

                foreach (var grid in Grids.ToArray())
                {
                    if (grid == null) continue;
                    try
                    {
                        grid.DataSource = null;
                        grid.RowsAdded -= Dgv_RowsAdded;
                        grid.CellContentClick -= Dgv_CellContentClick;
                        grid.DataBindingComplete -= Dgv_DataBindingComplete;
                        if (gridFilterHandlers.TryGetValue(grid, out var handler))
                        {
                            try { grid.GridFilterChanged -= handler; } catch (Exception ex) { Debug.WriteLine($"CleanupGrids: error removing handler: {ex}"); }
                            gridFilterHandlers.Remove(grid);
                        }
                        try { grid.Dispose(); } catch (Exception ex) { Debug.WriteLine($"CleanupGrids: error disposing grid: {ex}"); }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CleanupGrids: unexpected error: {ex}");
                    }
                }

                // Remove and dispose any Panel containers that were created for grids in ShowTable
                // Panels created there use the resultset index as the Tag (an int). Use that to identify and remove them.
                try
                {
                    var panels = new List<Panel>();
                    panels.AddRange(splitTablesCharts.Panel1.Controls.OfType<Panel>());
                    panels.AddRange(splitTablesCharts.Panel2.Controls.OfType<Panel>());
                    foreach (var pnl in panels.ToArray())
                    {
                        try
                        {
                            if (pnl.Tag is int)
                            {
                                // Remove from parent then dispose
                                pnl.Parent?.Controls.Remove(pnl);
                                pnl.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"CleanupGrids: error disposing grid panel: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CleanupGrids: error removing grid panels: {ex}");
                }

                Grids.Clear();
                gridFilterHandlers.Clear();
                rowsHeightCache.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CleanupGrids: error cleaning up grids: {ex}");
            }
        }

        // Cleanup helper for charts and layout
        private void CleanupCharts()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((Action)CleanupCharts);
                    return;
                }

                // Dispose chart controls tracked in Charts and remove them from their parents
                try
                {
                    foreach (var chart in Charts.ToArray())
                    {
                        if (chart == null) continue;
                        try
                        {
                            var parent = chart.Parent;
                            if (parent != null && parent.Controls.Contains(chart))
                            {
                                parent.Controls.Remove(chart);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"CleanupCharts: failed removing chart from parent: {ex}");
                        }

                        try { chart.Dispose(); }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"CleanupCharts: failed disposing chart: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CleanupCharts: error disposing charts collection: {ex}");
                }

                Charts.Clear();

                // Clear chartLayout children and styles
                try
                {
                    foreach (var ctrl in chartLayout.Controls.OfType<Control>().ToArray())
                    {
                        try { chartLayout.Controls.Remove(ctrl); ctrl.Dispose(); }
                        catch (Exception ex) { Debug.WriteLine($"CleanupCharts: error disposing chartLayout child: {ex}"); }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CleanupCharts: error clearing chartLayout controls: {ex}");
                }

                try { chartLayout.RowStyles.Clear(); } catch (Exception ex) { Debug.WriteLine($"CleanupCharts: error clearing row styles: {ex}"); }
                try { chartLayout.ColumnStyles.Clear(); } catch (Exception ex) { Debug.WriteLine($"CleanupCharts: error clearing column styles: {ex}"); }

                // Do not remove controls from the split panels here — grid panels are hosted in the same split
                // containers and should be managed by CleanupGrids / ShowTable. Only clear `chartLayout` children
                // and chart tracking to avoid disposing non-chart UI when toggling visibility.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CleanupCharts: unexpected error: {ex}");
            }
        }

        private void ScriptDataTables(bool fromGrid)
        {
            try
            {
                var i = 0;
                StringBuilder sb = new();
                foreach (var grid in Grids)
                {
                    if (i > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("/*********************************************************");
                        sb.AppendLine($" Result Set {i + 1}");
                        sb.AppendLine("*********************************************************/");
                        sb.AppendLine();
                    }
                    var tableName = $"#DBADashGrid{i + 1}";
                    sb.Append(grid.ScriptTable(fromGrid, i == 0, tableName));
                    sb.AppendLine();
                    i += 1;
                }

                var frm = new CodeViewer() { Code = sb.ToString(), Language = CodeEditor.CodeEditorModes.SQL };
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error scripting tables");
            }
        }

        private void SetCellHighlightingRules(DBADashDataGridView dgv)
        {
            if (dgv == null) return;
            try
            {
                var customReportResult = Report.CustomReportResults[dgv.ResultSetID];
                var columnName = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
                var colInfo = GetColumnMetadata(customReportResult, columnName);

                var frm = new CellHighlightingRulesConfig()
                {
                    ColumnList = dgv.Columns,
                    CellHighlightingRules = new KeyValuePair<string, CellHighlightingRuleSet>(columnName,
                          colInfo.Highlighting.DeepCopy() ?? new CellHighlightingRuleSet() { TargetColumn = columnName }),
                    CellValue = dgv.ClickedRowIndex >= 0
                        ? dgv.Rows[dgv.ClickedRowIndex].Cells[dgv.ClickedColumnIndex].Value
                        : null,
                    CellValueIsNull = dgv.ClickedRowIndex >= 0 &&
                                      dgv.Rows[dgv.ClickedRowIndex].Cells[dgv.ClickedColumnIndex].Value
                                          .DBNullToNull() == null
                };

                frm.ShowDialog();
                if (frm.DialogResult != DialogResult.OK) return;

                colInfo.Highlighting = frm.CellHighlightingRules.Value;

                Report.Update();
                ShowTable();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error setting highlighting rules");
            }
        }

        private void AddLink_Click(DBADashDataGridView dgv)
        {
            var tableIndex = dgv.ResultSetID;
            try
            {
                var customReportResult = Report.CustomReportResults[tableIndex];
                var col = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
                var colInfo = GetColumnMetadata(customReportResult, col);
                var colList = dgv.Columns.Cast<DataGridViewColumn>().Select(c => c.DataPropertyName).ToList();
                var frm = new LinkColumnTypeSelector()
                { LinkColumnInfo = colInfo.Link, ColumnList = colList, Context = context, LinkColumn = col };
                frm.ShowDialog();
                if (frm.DialogResult != DialogResult.OK) return;

                colInfo.Link = frm.LinkColumnInfo;
                previousSchema = null; // Force grids to be re-generated
                dgv.Columns.Clear();
                Report.Update();
                ShowTable();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error adding link");
            }
        }

        private void SetFormatStringMenuItem_Click(DBADashDataGridView dgv)
        {
            var formatString = dgv.Columns[dgv.ClickedColumnIndex].DefaultCellStyle.Format;
            var colName = dgv.Columns[dgv.ClickedColumnIndex].Name;
            var result = Report.CustomReportResults[dgv.ResultSetID];
            if (CommonShared.ShowInputDialog(ref formatString, "Enter format string (e.g. N1, P1, yyyy-MM-dd)") !=
                DialogResult.OK) return;
            dgv.Columns[dgv.ClickedColumnIndex].DefaultCellStyle.Format = formatString;

            var colInfo = GetColumnMetadata(result, colName);
            colInfo.FormatString = formatString;

            Report.Update();
        }

        private ColumnMetadata GetColumnMetadata(CustomReportResult result, string colName)
        {
            var colInfo = result.Columns?.TryGetValue(colName, out var column) is true
                ? column
                : new ColumnMetadata();
            result.Columns.TryAdd(colName, colInfo);
            return colInfo;
        }

        private void ConvertLocalMenuItem_Click(object sender, DBADashDataGridView dgv)
        {
            var convertLocalMenuItem = (ToolStripMenuItem)sender;
            if (dgv.ClickedColumnIndex < 0) return;
            var name = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
            var colInfo = GetColumnMetadata(Report.CustomReportResults[dgv.ResultSetID], name);
            switch (convertLocalMenuItem.Checked)
            {
                case true when Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Contains(name):
                    if (MessageBox.Show("Convert column from UTC to local time?", "Convert?", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.No) return;
                    colInfo.ConvertToLocalTimeZone = true;
                    break;

                case false when !Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Contains(name):
                    if (MessageBox.Show("Remove UTC to local time zone conversion for this column?", "Convert?",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
                    colInfo.ConvertToLocalTimeZone = false;
                    break;
            }

            try
            {
                Report.Update();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving time zone preference");
            }

            RefreshData();
        }

        private void RenameColumnMenuItem_Click(DBADashDataGridView dgv)
        {
            if (dgv.ClickedColumnIndex < 0) return;
            // Show a dialog for renaming
            var newName = dgv.Columns[dgv.ClickedColumnIndex].HeaderText;
            if (CommonShared.ShowInputDialog(ref newName, "Enter new column name:") != DialogResult.OK) return;
            if (string.IsNullOrEmpty(newName))
            {
                newName = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
            }

            dgv.Columns[dgv.ClickedColumnIndex].HeaderText = newName;
            try
            {
                var alias = dgv.Columns.Cast<DataGridViewColumn>()
                    .Where(column => column.DataPropertyName != column.HeaderText)
                    .ToDictionary(column => column.DataPropertyName, column => column.HeaderText);
                Report.CustomReportResults[dgv.ResultSetID].ColumnAlias = alias;
                Report.Update();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving alias");
            }
        }

        private void SetColumnTooltip_Click(DBADashDataGridView dgv)
        {
            if (dgv.ClickedColumnIndex < 0) return;
            // Show a dialog for renaming
            var columnName = dgv.Columns[dgv.ClickedColumnIndex].DataPropertyName;
            var description = dgv.Columns[dgv.ClickedColumnIndex].ToolTipText;
            if (CommonShared.ShowInputDialog(ref description, "Enter Tooltip:") != DialogResult.OK) return;

            dgv.Columns[dgv.ClickedColumnIndex].ToolTipText = description;
            try
            {
                var colInfo = GetColumnMetadata(Report.CustomReportResults[dgv.ResultSetID], columnName);
                colInfo.Description = description;
                Report.Update();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving tooltip");
            }
        }

        /// <summary>
        /// Param prompt is shown instead of grid if we have a 201 error indicating that parameter values are required.
        /// </summary>
        /// <param name="show">True = parameter prompt is visible.  False = Grid is visible</param>
        private void ShowParamPrompt(bool show)
        {
            splitResultsAndParams.Panel2Collapsed = !show;
            splitResultsAndParams.Panel1Collapsed = show;
        }

        /// <summary>
        /// Convert DateTime columns to local timezone unless user has specified that conversion should be excluded for the column
        /// </summary>
        /// <param name="ds"></param>
        private void ConvertDateTimeColsToLocalTimeZone(DataSet ds)
        {
            for (var i = 0; i < ds.Tables.Count; i++)
            {
                if (!Report.CustomReportResults.TryGetValue(i, out var result)) continue;
                var dt = ds.Tables[i];
                var convertCols = dt.Columns.Cast<DataColumn>()
                    .Where(column =>
                        (column.DataType == typeof(DateTime) || column.DataType == typeof(DateTimeOffset)) &&
                        !result.DoNotConvertToLocalTimeZone.Contains(column.ColumnName))
                    .Select(column => column.ColumnName).ToList();
                if (convertCols.Count > 0)
                {
                    DateHelper.ConvertUTCToAppTimeZone(ref dt, convertCols);
                }
            }
        }

        /// <summary>
        /// Results combobox allows user to select which result set to display if the report returns multiple result sets
        /// </summary>
        protected void LoadResultsCombo()
        {
            suppressCboResultsIndexChanged = true;
            for (var i = 0; i < reportDS.Tables.Count; i++)
            {
                if (Report.CustomReportResults.TryGetValue(i, out var result))
                {
                    result.ResultName ??= "Result" + (i + 1); // ensure we have a name
                }
                else
                {
                    Report.CustomReportResults.Add(i, new CustomReportResult() { ResultName = "Result" + (i + 1) });
                }
            }

            cboResults.Items.Clear();
            for (var i = 0; i < reportDS.Tables.Count; i++)
            {
                cboResults.Items.Add(Report.CustomReportResults[i].ResultName);
            }

            cboResults.Items.Add("ALL");

            cboResults.Visible = cboResults.Items.Count > 2;
            lblSelectResults.Visible = cboResults.Items.Count > 2;
            cboResults.SelectedIndex = cboResults.Items.Count - 1;
            suppressCboResultsIndexChanged = false;
        }

        private bool ShowAllResults => cboResults.SelectedIndex == cboResults.Items.Count - 1;

        public void RefreshData()
        {
            if (!Report.HasAccess())
            {
                MessageBox.Show("You do not have access to this report", "Access Denied", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Push the shared status filter selection (when enabled) into the @Include* parameters, then allow
            // derived views to apply any further view-specific parameters into customParams before the query runs.
            // Called for every refresh path, including the initial auto-load from SetContext.
            ApplyStatusFilterParams();
            OnBeforeRefresh();

            if (Report is DirectExecutionReport)
            {
                RefreshDataMessage();
            }
            else
            {
                StartTimer();
                // Replace any existing CTS and request cancellation of the old one.
                // Do NOT dispose the old CTS here because the running task may still be
                // using its Token/registrations; disposal is performed in the completion
                // path (RefreshDataRepository) to avoid races.
                var newCts = new CancellationTokenSource();
                var old = Interlocked.Exchange(ref cancellationTokenSource, newCts);
                try { old?.Cancel(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RefreshData: error cancelling previous CTS: {ex}");
                }
                IsMessageInProgress = true;
                // Pass the CTS instance so the repository runner can dispose the exact CTS it used when it completes
                Task.Run(() => { _ = RefreshDataRepository(newCts); });
            }
            RefreshDate = DateTime.Now;
        }

        private void StartTimer()
        {
            ResetTimer();
            timer1.Start();
        }

        private void StopTimer()
        {
            timer1.Stop();
            UpdateTimer();
        }

        private void ResetTimer()
        {
            timer1.Stop();
            TimerStart = DateTime.Now;
            lblTimer.Text = "00:00:00";
        }

        public async Task RefreshDataRepository(CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                while (!IsHandleCreated)
                {
                    await Task.Delay(100, token);
                }
                SetStatus("Running report", string.Empty, DashColors.Information);
                reportDS = await GetReportDataAsync(token);
                this.Invoke(() =>
                {
                    // Guard: if this CTS has been superseded by a newer RefreshData call,
                    // discard results to avoid rendering stale data with the wrong Report.
                    if (!ReferenceEquals(cts, cancellationTokenSource)) return;
                    ShowParamPrompt(false); // Show grid
                    LoadResultsCombo();
                    if (reportDS.Tables.Count > 0)
                    {
                        ShowTable();
                    }
                    else if (Report?.Charts != null && Report.Charts.Any(c => c.Metric != null))
                    {
                        // Report returned no result sets but includes metric/system charts - render charts
                        ShowTable();
                    }
                    else
                    {
                        MessageBox.Show("Report didn't return a result set", "Warning", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                });
                if (ReferenceEquals(cts, cancellationTokenSource))
                    SetStatus("Completed", "Success", DashColors.Success);
            }
            catch (SqlException ex) when (ex.Number == 201) // Parameter required
            {
                ShowParamPrompt(
                    true); // Show parameter prompt instead of grid as we have required parameters that were not supplied
            }
            catch (Exception ex)
            {
                SetStatus("Error running report:" + ex.Message, ex.ToString(), DashColors.Fail);
            }
            finally
            {
                if (ReferenceEquals(cts, cancellationTokenSource))
                {
                    IsMessageInProgress = false;
                    StopTimer();
                }
                try
                {
                    // Clear the static field if it still references this run's CTS
                    System.Threading.Interlocked.CompareExchange(ref cancellationTokenSource, null, cts);
                    // Dispose the CTS that was used for this run. It's safe to dispose here even if CancelProcessing
                    // attempted to cancel/dispose earlier; log any exceptions encountered during dispose.
                    try { cts.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"RefreshDataRepository: error disposing CTS: {ex}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RefreshDataRepository: unexpected error in finally block: {ex}");
                }
            }
        }

        public void ShowData(DataSet ds)
        {
            reportDS = ds;
            ShowTable();
            LoadResultsCombo();
        }

        private bool IsMessageInProgress
        {
            get => tsCancel.Enabled;
            set
            {
                tsCancel.Enabled = value;
                tsRefresh.Enabled = !tsCancel.Enabled;
                tsExecute.Enabled = !tsCancel.Enabled;
            }
        }

        public void RefreshDataMessage()
        {
            if (IsMessageInProgress)
            {
                SetStatus("A message is already in progress.  Please wait for the current message to complete before running another.", "Warning", DashColors.Warning);
                return;
            }
            var rpt = Report as DirectExecutionReport;
            if (rpt == null)
            {
                SetStatus("Invalid report type.  Expected a DirectExecutionReport", tooltip: null, DashColors.Fail);
                return;
            }
            var msg = new ProcedureExecutionMessage
            {
                EmbeddedScript = rpt is SystemDirectExecutionReport srpt ? srpt.EmbeddedScript : null,
                ProcedureName = Report.ProcedureName,
                SchemaName = Report.SchemaName,
                Parameters = customParams,
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = Config.DefaultCommandTimeout
            };

            SetStatus("Message sent", "Running", DashColors.Information);
            IsMessageInProgress = true;
            CurrentMessageGroup = Guid.NewGuid();
            StartTimer();
            Task.Run(() => MessagingHelper.SendMessageAndProcessReply(msg, context, SetStatus, ProcessCompletedMessage,
                CurrentMessageGroup));
        }

        private Task ProcessCompletedMessage(ResponseMessage reply, Guid messageGroup, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (CurrentMessageGroup != messageGroup) // Context has changed.  Ignore
            {
                return Task.CompletedTask;
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Success && reply.Data is { Tables.Count: > 0 })
            {
                StopTimer();
                IsMessageInProgress = false;
                try
                {
                    reportDS = reply.Data;
                    this.Invoke(LoadResultsCombo);

                    if (reportDS.Tables.Count > 0)
                    {
                        this.Invoke(ShowTable);
                        SetStatus("Completed", "Success", DashColors.Success);
                    }
                    else
                    {
                        SetStatus("Report didn't return a result set", "Warning", Color.Orange);
                    }
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error running report");
                }
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Failure)
            {
                StopTimer();
                SetStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                IsMessageInProgress = false;
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                StopTimer();
                SetStatus(reply.Message, null, DashColors.Success);
                IsMessageInProgress = false;
            }
            else if (reply.Type == ResponseMessage.ResponseTypes.Progress)
            {
                SetStatus(reply.Message, null, DashColors.Information);
            }

            return Task.CompletedTask;
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            this.Invoke(() =>
            {
                lblDescription.Text = message;
                lblDescription.ForeColor = color;
                lblDescription.ToolTipText = tooltip;
                lblDescription.Visible = true;
                statusStrip1.Visible = true;
            });
        }

        public List<DBADashDataGridView> Grids { get; }
        public List<Control> Charts { get; } = new();
        private string previousSchema;

        private void ClearResults()
        {
            CleanupUI();
        }

        private void LoadResultsIntoExistingGrids()
        {
            foreach (var grid in Grids)
            {
                grid.AutoGenerateColumns = false;
                var table = reportDS.Tables[grid.ResultSetID];
                grid.DataSource = new DataView(table, grid.RowFilter, grid.SortString, DataViewRowState.CurrentRows);
            }
            for (int ci = 0; ci < Charts.Count; ci++)
            {
                var chart = Charts[ci];
                if (chart == null || chart.IsDisposed)
                {
                    continue;
                }
                var chartWrapper = chart.Tag as CustomReportChart;
                if (chartWrapper != null)
                {
                    // If this CustomReportChart references a metric chart type, refresh it via IMetricChart
                    if (chartWrapper.Metric != null)
                    {
                        if (chart is IMetricChart metricChart && context != null)
                        {
                            try
                            {
                                metricChart.SetContext(context);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error refreshing metric chart: {ex}");
                            }
                        }
                        continue;
                    }

                    // configuration-based chart behavior
                    var chartConfig = chartWrapper.Config;
                    // Ensure the configured table exists
                    if (chartConfig == null)
                    {
                        continue;
                    }
                    if (reportDS == null || reportDS.Tables.Count <= chartWrapper.TableIndex)
                    {
                        Debug.WriteLine($"Warning: Chart configured for result set {chartWrapper.TableIndex} but report only returned {reportDS?.Tables.Count ?? 0} result sets.  Chart will be skipped.");
                        continue;
                    }

                    var dt = reportDS.Tables[chartWrapper.TableIndex];

                    // If this is a CartesianChart, update in place
                    if (chart is CartesianChart cchart && chartConfig is ChartConfiguration crc)
                    {
                        ChartHelper.UpdateChart(cchart, dt, crc);
                    }
                    else
                    {
                        // For non-cartesian controls (e.g., PieChart), recreate the control and replace it in the parent panel
                        var parent = chart.Parent;
                        try
                        {
                            var newChart = ChartHelper.GetChartControlFromDataTable(dt, chartConfig);
                            newChart.Tag = chartWrapper;

                            // Replace in the panel's controls keeping the ToolStrip in place
                            if (parent != null)
                            {
                                // Determine where the old chart was located so we can insert the new control at the same position
                                var oldIndex = parent.Controls.IndexOf(chart);
                                var toolStrip = parent.Controls.OfType<ToolStrip>().FirstOrDefault();

                                // Remove the old control from the parent before disposing to avoid leaving a disposed control in the collection
                                try { parent.Controls.Remove(chart); }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Failed removing old chart from parent: {ex}");
                                }

                                // Add the new chart and set its index. Prefer the original index, otherwise place it before the ToolStrip (to preserve layout)
                                parent.Controls.Add(newChart);
                                if (oldIndex >= 0)
                                {
                                    try { parent.Controls.SetChildIndex(newChart, oldIndex); }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Failed setting child index for new chart: {ex}");
                                    }
                                }
                                else if (toolStrip != null)
                                {
                                    try { parent.Controls.SetChildIndex(newChart, parent.Controls.IndexOf(toolStrip)); }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Failed setting child index before toolStrip for new chart: {ex}");
                                    }
                                }
                            }
                            else
                            {
                                // If no parent, simply add to parent (null-safe) - keep previous behavior
                                parent?.Controls.Add(newChart);
                            }

                            // Dispose old chart and update our list reference
                            try { chart.Dispose(); }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed disposing old chart: {ex}");
                            }
                            Charts[ci] = newChart;
                        }
                        catch (Exception ex)
                        {
                            // If replacement fails, log and continue; keep the old chart in place
                            Debug.WriteLine($"Failed replacing chart control: {ex}");
                        }
                    }
                }
            }
        }

        private void GetChartPanels()
        {
            // Always move chartLayout to ChartPanel before any early return.
            // If this is skipped (e.g. different report with no charts), chartLayout can end up in
            // TablePanel from a previous report, covering the grids and making them invisible.
            try
            {
                if (chartLayout.Parent != ChartPanel)
                {
                    chartLayout.Parent?.Controls.Remove(chartLayout);
                    ChartPanel.Controls.Add(chartLayout);
                    chartLayout.Dock = DockStyle.Fill;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetChartPanels: error repositioning chartLayout: {ex}");
            }

            if (Report.Charts == null || Report.Charts.Count == 0) return;
            // Allow metric-chart-only reports to render even if there are no result tables
            if (reportDS == null || reportDS.Tables.Count == 0)
            {
                if (!Report.Charts.Any(c => c.Metric != null))
                {
                    return;
                }
            }
            // Dispose any existing chart controls and clear tracking before rebuilding to avoid leaks
            try { CleanupCharts(); } catch (Exception ex) { Debug.WriteLine($"GetChartPanels: CleanupCharts error: {ex}"); }

            var parentPanel = ChartPanel;
            var enableResize = Report.Charts.Count > 0;

            var layout = ChartHelper.CalculateLayout(Report.Charts.Count, Report.ChartLayoutRows, Report.ChartLayoutColumns);

            // Configure TableLayoutPanel
            chartLayout.Controls.Clear();
            chartLayout.ColumnStyles.Clear();
            chartLayout.ColumnCount = layout.columns;
            for (int c = 0; c < layout.columns; c++)
            {
                chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / layout.columns));
            }

            chartLayout.RowStyles.Clear();
            chartLayout.RowCount = layout.rows;
            for (int r = 0; r < layout.rows; r++)
            {
                chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / layout.rows));
            }

            // Generic spanning logic: compute occupancy grid and let the first chart span to use spare cells
            var totalCells = layout.rows * layout.columns;
            var spare = Math.Max(0, totalCells - Report.Charts.Count);
            var firstSpan = Math.Min(layout.columns, 1 + spare); // first chart occupies 1 + spare cells up to cols

            // occupancy grid
            var occupied = new bool[layout.rows, layout.columns];

            // mark space for first chart
            for (int c0 = 0; c0 < firstSpan && c0 < layout.columns; c0++)
            {
                occupied[0, c0] = true;
            }

            // Ensure the chartLayout is hosted in the ChartPanel
            try
            {
                if (chartLayout.Parent != ChartPanel)
                {
                    chartLayout.Parent?.Controls.Remove(chartLayout);
                    ChartPanel.Controls.Add(chartLayout);
                    chartLayout.Dock = DockStyle.Fill;
                }
            }
            catch { }

            // Create and place panels in row-major order
            for (int idx = 0; idx < Report.Charts.Count; idx++)
            {
                var chartWrapper = Report.Charts[idx];
                var chartConfig = chartWrapper.Config;
                var chartId = idx;

                Control chartControl = null;
                Panel pnl = null;

                // Defensive validation: chart must have either a metric type or a configuration
                // otherwise it can't be rendered.  Log and skip invalid entries to avoid
                // throwing during layout/render.
                if (chartWrapper.Metric == null && chartConfig == null)
                {
                    Debug.WriteLine($"GetChartPanels: skipping invalid CustomReportChart at index {idx} - no MetricType or Config");
                    continue;
                }

                // Metric chart path (instantiated from a type that implements IMetricChart)
                if (chartWrapper.Metric != null)
                {
                    try
                    {
                        IMetricChart metric = chartWrapper.Metric.GetChart();
                        ((Control)metric).Dock = DockStyle.Fill;
                        metric.MoveUpVisible = Report.CanEditReport;
                        metric.CloseVisible = Report.CanEditReport;
                        // Subscribe to metric events to support move/delete actions.
                        // Resolve the chart index at runtime to avoid capturing the loop variable
                        // (which becomes stale if charts are reordered or items removed).
                        metric.Close += (_, __) =>
                        {
                            try
                            {
                                var curIdx = Report.Charts.FindIndex(c => ReferenceEquals(c, chartWrapper));
                                if (curIdx >= 0 && curIdx < Report.Charts.Count)
                                {
                                    Report.Charts.RemoveAt(curIdx);
                                    Report.Update();
                                    previousSchema = null;
                                    ShowTable();
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error removing metric chart: {ex}");
                            }
                        };
                        metric.MoveUp += (_, __) =>
                        {
                            try
                            {
                                var curIdx = Report.Charts.FindIndex(c => ReferenceEquals(c, chartWrapper));
                                if (curIdx > 0)
                                {
                                    RepositionChart(curIdx, curIdx - 1);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error moving metric chart: {ex}");
                            }
                        };

                        // Let the metric control set its context and refresh itself for the current context
                        try
                        {
                            // IMetricChart now implements ISetContext/IRefreshData. Call SetContext so the control
                            // can capture any necessary context (InstanceID/DatabaseID/ObjectID) and refresh as needed.
                            metric.SetContext(context);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error refreshing metric data: {ex}");
                        }

                        chartControl = (Control)metric;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error instantiating metric chart: {ex}");
                    }

                    if (chartControl != null)
                    {
                        chartControl.Tag = chartWrapper;
                        pnl = chartLayoutHelper.CreateResizablePanel(chartControl, chartId, enableResize, string.Empty, panelName: $"chart_{chartId}");
                    }
                    else
                    {
                        // Could not create metric control - skip
                        continue;
                    }
                }
                else if (chartConfig != null)
                {
                    if (reportDS.Tables.Count <= chartWrapper.TableIndex)
                    {
                        Debug.WriteLine($"Warning: Chart configured for result set {chartWrapper.TableIndex} but report only returned {reportDS.Tables.Count} result sets.  Chart will be skipped.");
                        continue;
                    }

                    var dt = reportDS.Tables[chartWrapper.TableIndex];
                    chartControl = ChartHelper.GetChartControlFromDataTable(dt, chartConfig);
                    chartControl.Tag = chartWrapper;
                    pnl = chartLayoutHelper.CreateResizablePanel(chartControl, chartId, enableResize, chartConfig.ChartTitle, panelName: $"chart_{chartId}");
                }
                else
                {
                    // Nothing to render for this chart
                    continue;
                }

                var toolStrip = pnl?.Controls.OfType<ToolStrip>().FirstOrDefault();
                if (toolStrip != null && Report.CanEditReport)
                {
                    var edit = new ToolStripMenuItem("Edit", Properties.Resources.SettingsOutline_16x);

                    var configure = new ToolStripMenuItem("Configure", Properties.Resources.SettingsOutline_16x) { Tag = chartId };
                    configure.Click += ConfigureChart_Click;
                    edit.DropDownItems.Add(configure);

                    if (chartId > 0)
                    {
                        var up = new ToolStripMenuItem("Move Up", Properties.Resources.arrow_Up_16xLG) { Tag = chartId };
                        up.Click += MoveChartUp_Click;
                        edit.DropDownItems.Add(up);
                    }
                    if (chartId < Report.Charts.Count - 1)
                    {
                        var down = new ToolStripMenuItem("Move Down", Properties.Resources.arrow_Down_16xLG) { Tag = chartId };
                        down.Click += MoveChartDown_Click;
                        edit.DropDownItems.Add(down);
                    }

                    var delete = new ToolStripMenuItem("Delete", Properties.Resources.Close_red_16x) { Tag = chartId };
                    delete.Click += DeleteChart_Click;
                    edit.DropDownItems.Add(delete);
                    toolStrip.Items.Add(edit);
                }

                pnl.ApplyTheme();

                Charts.Add(chartControl);

                if (idx == 0)
                {
                    // place first spanning chart at top-left
                    chartLayout.Controls.Add(pnl, 0, 0);
                    if (firstSpan > 1)
                    {
                        try { chartLayout.SetColumnSpan(pnl, firstSpan); } catch { }
                    }
                }
                else
                {
                    // find next free cell scanning row-major
                    bool placed = false;
                    for (int r = 0; r < layout.rows && !placed; r++)
                    {
                        for (int c = 0; c < layout.columns && !placed; c++)
                        {
                            if (!occupied[r, c])
                            {
                                occupied[r, c] = true;
                                chartLayout.Controls.Add(pnl, c, r);
                                placed = true;
                            }
                        }
                    }

                    // Fallback: if not placed (shouldn't happen) put at next sequential position
                    if (!placed)
                    {
                        var frow = idx / layout.columns;
                        var fcol = idx % layout.columns;
                        chartLayout.Controls.Add(pnl, fcol, frow);
                    }
                }
            }
        }

        private void MoveChartDown_Click(object sender, EventArgs e)
        {
            var mnu = sender as ToolStripMenuItem;
            if (mnu == null) return;
            var chartId = (int)mnu.Tag;
            RepositionChart(chartId, chartId + 1);
        }

        private void RepositionChart(int chartId, int newIndex)
        {
            if (chartId >= Report.Charts.Count || newIndex >= Report.Charts.Count || chartId == newIndex || newIndex < 0) return;
            var chart = Report.Charts[chartId];
            Report.Charts.RemoveAt(chartId);
            Report.Charts.Insert(newIndex, chart);
            Report.Update();
            previousSchema = null; // Force chart to be re-generated with new configuration
            ShowTable();
        }

        private void MoveChartUp_Click(object sender, EventArgs e)
        {
            var mnu = sender as ToolStripMenuItem;
            if (mnu == null) return;
            var chartId = (int)mnu.Tag;
            RepositionChart(chartId, chartId - 1);
        }

        private void DeleteChart_Click(object sender, EventArgs e)
        {
            var mnu = sender as ToolStripMenuItem;
            if (mnu == null) return;
            var chartId = (int)mnu.Tag;
            if (chartId >= Report.Charts.Count) return;
            if (MessageBox.Show("Confirm chart deletion?", "Delete Chart", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            Report.Charts.RemoveAt(chartId);
            Report.Update();
            previousSchema = null; // Force chart to be re-generated with new configuration
            ShowTable();
        }

        private async void ConfigureChart_Click(object sender, EventArgs e)
        {
            var mnu = sender as ToolStripMenuItem;
            if (mnu == null) return;
            var chartId = (int)mnu.Tag;
            var frm = new ConfigureChart() { Report = Report, ReportDS = reportDS, ChartId = chartId };
            await frm.ShowDialogAsync();
            if (frm.DialogResult != DialogResult.OK) return;
            Report.Update();
            previousSchema = null; // Force chart to be re-generated with new configuration
            ShowTable();
        }

        private void SetChartTableLayout()
        {
            splitTablesCharts.Orientation = Report.ChartLocation is CustomReport.ChartLocations.Top or CustomReport.ChartLocations.Bottom ? Orientation.Horizontal : Orientation.Vertical;
            splitTablesCharts.SplitterDistance =
                Report.ChartLocation switch
                {
                    CustomReport.ChartLocations.Top => (int)(this.Height * Report.ChartSplitPercentage),
                    CustomReport.ChartLocations.Bottom => (int)(this.Height * (1 - Report.ChartSplitPercentage)),
                    CustomReport.ChartLocations.Right => (int)(this.Width * (1 - Report.ChartSplitPercentage)),
                    CustomReport.ChartLocations.Left => (int)(this.Width * Report.ChartSplitPercentage),
                    _ => splitTablesCharts.SplitterDistance
                };
        }

        private double GetCurrentChartSplitPercentage()
        {
            return Report.ChartLocation switch
            {
                CustomReport.ChartLocations.Top => (double)splitTablesCharts.SplitterDistance / this.Height,
                CustomReport.ChartLocations.Bottom => 1 - ((double)splitTablesCharts.SplitterDistance / this.Height),
                CustomReport.ChartLocations.Right => 1 - ((double)splitTablesCharts.SplitterDistance / this.Width),
                CustomReport.ChartLocations.Left => (double)splitTablesCharts.SplitterDistance / this.Width,
                _ => Report.ChartSplitPercentage
            };
        }

        protected void ShowTable()
        {
            var hasData = reportDS != null && reportDS.Tables.Count > 0;
            var hasCharts = Report?.Charts != null && Report.Charts.Count > 0;

            // If there's no data and no charts to render, nothing to show
            if (!hasData && !hasCharts) return;

            SetChartTableLayout();
            var currentSchema = (reportDS?.GetXmlSchema() ?? string.Empty) + "\n" + Report.Serialize();
            if (currentSchema == previousSchema)
            {
                LoadResultsIntoExistingGrids();
                OnPostGridRefresh();
                return;
            }

            const int minDataGridViewHeight = 100; // Minimum height for a DataGridView
            // Avoid divide by zero when reportDS has no tables (system-only chart reports)
            var maxDataGridViewHeight = hasData ? Math.Max(300, this.Height / Math.Min(3, reportDS.Tables.Count)) : Math.Max(300, this.Height / 3);
            var parentPanel = TablePanel;
            ClearResults();
            List<Panel> panels = new();
            GetChartPanels();
            // Show toggle controls only when charts exist AND there is dataset/table data to toggle
            var showToggles = Charts.Count > 0 && hasData;
            tsToggleGrids.Visible = showToggles;
            tsToggleCharts.Visible = showToggles;
            splitToggle1.Visible = showToggles;
            splitToggle2.Visible = showToggles;
            tsCols.Visible = hasData;
            tsScriptResults.Visible = hasData;
            tsExcel.Visible = hasData;
            tsCopy.Visible = hasData;
            tsClearFilter.Visible = hasData;
            saveSystemChartStateToolStripMenuItem.Visible = Charts.Any(c => c.Tag is CustomReportChart crc && crc.Metric != null);
            SetChartPanelCollapsed(Charts.Count == 0 || !Report.ChartVisible);

            // Determine result tables to show (may be empty for system-only reports)
            var i = 0;
            DataTable[] tables;
            if (hasData)
            {
                i = ShowAllResults ? 0 : cboResults.SelectedIndex;
                tables = ShowAllResults ? reportDS.Tables.Cast<DataTable>().ToArray() : new[] { reportDS.Tables[cboResults.SelectedIndex] };
            }
            else
            {
                tables = Array.Empty<DataTable>();
            }
            // With multiple result sets the panels are stacked (Dock=Top). In a scrollable report the container
            // scrolls so result sets keep a usable height and overflow scrolls off-screen rather than being
            // squashed to fit; in a single-page report the result sets are fitted into the available height.
            // A single result set always fills the panel.
            parentPanel.AutoScroll = tables.Length > 1 && !Report.SinglePageLayout;
            tsToggleSinglePage.Visible = tables.Length > 1;
            tsToggleSinglePage.Checked = Report.SinglePageLayout;
            foreach (var table in tables)
            {
                var pnl = new Panel()
                {
                    Dock = tables.Length == 1 ? DockStyle.Fill : DockStyle.Top, // Dock to the top of the panel
                    Tag = i,
                    Padding = new Padding(0, 0, 0, 5),
                    Width = parentPanel.Width, // Needed to ensure custom auto sizing works correctly which takes into account the width of the grid (otherwise we would need control adding to parent panel before data binding)
                };
                var dgv = new DBADashDataGridView()
                {
                    DataSource = new DataView(table),
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 0, 0, 30),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToOrderColumns = true,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                    RowHeadersVisible = false,
                    ResultSetID = i,
                    ResultSetName = Report.CustomReportResults[i].ResultName,
                    Width = parentPanel.Width, // Needed to ensure custom auto sizing works correctly which takes into account the width of the grid (otherwise we would need control adding to parent panel before data binding)
                };
                dgv.RowsAdded += Dgv_RowsAdded;
                dgv.CellContentClick += Dgv_CellContentClick;
                if (tables.Length > 1)
                {
                    pnl.MouseDown += DataGridPanel_MouseDown;
                    pnl.MouseMove += DataGridPanel_MouseMove;
                    pnl.MouseUp += DataGridPanel_MouseUp;
                    pnl.MouseLeave += DataGridPanel_MouseLeave;
                }

                var customReportResults = Report.CustomReportResults[i];
                dgv.AddColumns(table, customReportResults);
                dgv.DataBindingComplete += Dgv_DataBindingComplete;

                if (Report.CanEditReport) // Add context menu items for editing
                {
                    AddContextMenuItemsForEditing(dgv);
                }
                dgv.ApplyTheme();
                // Adjust height based on row count, with a maximum limit.  Allow for column height being adjusted from default.
                var rowsHeight = ((dgv.RowTemplate.Height) * table.Rows.Count) + dgv.Padding.Top + dgv.Padding.Bottom + (dgv.ColumnHeadersHeight * 3) + pnl.Padding.Top + pnl.Padding.Bottom;
                pnl.Height = Math.Max(Math.Min(rowsHeight, maxDataGridViewHeight), minDataGridViewHeight);

                Grids.Add(dgv);
                panels.Add(pnl);

                if (tables.Length > 1 || panels.Count > 0)
                {
                    var ts = new ToolStrip() { Dock = DockStyle.Top };
                    ts.Items.Add(new ToolStripButton("+", null, Maximize_Click)
                    {
                        Alignment = ToolStripItemAlignment.Right,
                        DisplayStyle = ToolStripItemDisplayStyle.Text,
                        Tag = i
                    });
                    ts.Items.Add(new ToolStripLabel(Report.CustomReportResults[i].ResultName)
                    {
                        Alignment = ToolStripItemAlignment.Right,
                        Font = new Font(this.Font, FontStyle.Bold),
                        Name = ResultSetLabelControlName
                    });
                    ts.Items.Add(new ToolStripButton("Columns", Properties.Resources.Column_16x, Columns_Click)
                    {
                        Alignment = ToolStripItemAlignment.Left,
                        DisplayStyle = ToolStripItemDisplayStyle.Image,
                        Tag = i
                    });
                    ts.Items.Add(new ToolStripButton("Copy", Properties.Resources.ASX_Copy_blue_16x, Copy_Click)
                    {
                        Alignment = ToolStripItemAlignment.Left,
                        DisplayStyle = ToolStripItemDisplayStyle.Image,
                        Tag = i
                    });
                    var clearFilterMenuItem =
                        new ToolStripButton("Clear Filter", Properties.Resources.Eraser_16x, ClearFilter_Click)
                        {
                            Alignment = ToolStripItemAlignment.Left,
                            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                            Enabled = false,
                            Tag = i
                        };
                    ts.Items.Add(clearFilterMenuItem);
                    if (Report.CanEditReport)
                        ts.Items.Add(
                            new ToolStripButton("Rename", Properties.Resources.Rename_16x, RenameResultSet_Click)
                            {
                                Alignment = ToolStripItemAlignment.Right,
                                DisplayStyle = ToolStripItemDisplayStyle.Image,
                                Tag = i
                            });
                    ts.ApplyTheme();
                    pnl.Controls.AddRange(new Control[] { dgv, ts });
                    EventHandler gridFilterHandler = (sender, e) =>
                    {
                        clearFilterMenuItem.Enabled = !string.IsNullOrEmpty(dgv.RowFilter);
                        clearFilterMenuItem.Font = new Font(clearFilterMenuItem.Font, clearFilterMenuItem.Enabled ? FontStyle.Bold : FontStyle.Regular);
                        UpdateClearFilter();
                    };
                    dgv.GridFilterChanged += gridFilterHandler;
                    gridFilterHandlers[dgv] = gridFilterHandler;
                }
                else
                {
                    pnl.Controls.Add(dgv);
                    EventHandler gridFilterHandler = (sender, e) => { UpdateClearFilter(); };
                    dgv.GridFilterChanged += gridFilterHandler;
                    gridFilterHandlers[dgv] = gridFilterHandler;
                }
                i += 1;
            }
            parentPanel.Controls.AddRange(panels.OrderByDescending(p => (int)p.Tag!).Cast<Control>().ToArray());
            OnPostGridRefresh();
            previousSchema = currentSchema;
        }

        private void UpdateClearFilter()
        {
            tsClearFilter.Enabled = Grids.Any(g => (g.DataSource as DataView)?.RowFilter != string.Empty);
            tsClearFilter.Font = new Font(tsClearFilter.Font, tsClearFilter.Enabled ? FontStyle.Bold : FontStyle.Regular);
        }

        private void ClearFilter_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            dgv.SetFilter(string.Empty);
        }

        private void AddContextMenuItemsForEditing(DBADashDataGridView dgv)
        {
            AddCellContextMenuItemsForEditing(dgv);
            AddColumnContextMenuItemsForEditing(dgv);
        }

        private void AddCellContextMenuItemsForEditing(DBADashDataGridView dgv)
        {
            var editReport = new ToolStripMenuItem("Edit Report", Properties.Resources.VBReport_16x);
            var highlight = new ToolStripMenuItem("Highlight Cell Value", Properties.Resources.HighlightHS);
            highlight.Click += (sender, e) => SetCellHighlightingRules(dgv);
            editReport.DropDownItems.Add(highlight);
            dgv.CellContextMenu.Items.Add(editReport);
        }

        private void AddColumnContextMenuItemsForEditing(DBADashDataGridView dgv)
        {
            var editReport = new ToolStripMenuItem("Edit Report", Properties.Resources.VBReport_16x) { Name = "EditReport" };

            var renameColumnMenuItem = new ToolStripMenuItem("Rename Column", Properties.Resources.Rename_16x);
            var setTooltipMenuItem = new ToolStripMenuItem("Set Column Tooltip", Properties.Resources.EditTooltip_16x);
            var setFormatStringMenuItem =
                new ToolStripMenuItem("Set Format String", Properties.Resources.Percentage_16x);
            var addLink = new ToolStripMenuItem("Add Link", Properties.Resources.WebURL_16x);
            var rules = new ToolStripMenuItem("Highlighting Rules", Properties.Resources.HighlightHS);
            var convertLocalMenuItem = new ToolStripMenuItem("Convert to local timezone") { Checked = true, CheckOnClick = true, Name = "ConvertLocal" };
            renameColumnMenuItem.Click += (sender, e) => RenameColumnMenuItem_Click(dgv);
            setTooltipMenuItem.Click += (sender, e) => SetColumnTooltip_Click(dgv);
            convertLocalMenuItem.Click += (sender, e) => ConvertLocalMenuItem_Click(sender, dgv);
            setFormatStringMenuItem.Click += (sender, e) => SetFormatStringMenuItem_Click(dgv);
            addLink.Click += (sender, e) => AddLink_Click(dgv);
            rules.Click += (sender, e) => SetCellHighlightingRules(dgv);

            editReport.DropDownItems.AddRange(new ToolStripItem[]
            {
                renameColumnMenuItem,
                setTooltipMenuItem,
                convertLocalMenuItem,
                setFormatStringMenuItem,
                addLink,
                rules
            });

            dgv.ColumnContextMenu.Items.Add(editReport);
            dgv.ColumnContextMenuOpening += (sender, args) =>
            {
                if (args.ColumnIndex < 0) return;
                convertLocalMenuItem.Checked = dgv.Columns[args.ColumnIndex].ValueType == typeof(DateTime) &&
                                       !Report.CustomReportResults[dgv.ResultSetID].DoNotConvertToLocalTimeZone.Contains(dgv.Columns[args.ColumnIndex].DataPropertyName);
                convertLocalMenuItem.Visible = dgv.Columns[args.ColumnIndex].ValueType == typeof(DateTime);
            };
        }

        private void Maximize_Click(object sender, EventArgs e)
        {
            foreach (var panel in chartLayout.Controls.OfType<Panel>().Union(TablePanel.Controls.OfType<Panel>()))
            {
                var ts = panel.Controls.OfType<ToolStrip>().FirstOrDefault();
                if (ts != null && ts.Items.Cast<object>().Any(itm => itm == sender))
                {
                    panel.Dock = DockStyle.Fill;
                    ts.Items[0].Text = "-";
                    ts.Items[0].Click -= Maximize_Click;
                    ts.Items[0].Click += Minimize_Click;
                    // Collapse the opposite panel so the selected panel fills the view
                    if (chartLayout.Controls.Contains(panel))
                    {
                        SetTablePanelCollapsed(true);
                        SetChartPanelCollapsed(false);
                    }
                    else
                    {
                        SetChartPanelCollapsed(true);
                        SetTablePanelCollapsed(false);
                    }
                }
                else
                {
                    panel.Visible = false;
                }
            }
        }

        private void Minimize_Click(object sender, EventArgs e)
        {
            foreach (var panel in chartLayout.Controls.OfType<Panel>().Union(TablePanel.Controls.OfType<Panel>()))
            {
                var ts = panel.Controls.OfType<ToolStrip>().FirstOrDefault();
                if (ts != null && ts.Items.Cast<object>().Any(itm => itm == sender))
                {
                    panel.Dock = DockStyle.Top;
                    ts.Items[0].Text = "+";
                    ts.Items[0].Click -= Minimize_Click;
                    ts.Items[0].Click += Maximize_Click;
                }
                else
                {
                    panel.Visible = true;
                }
            }
            SetChartPanelCollapsed(Charts.Count == 0);
            SetTablePanelCollapsed(false);
        }

        protected virtual void OnPostGridRefresh()
        {
            ApplyDrillDownGridFilters();
            UpdateClearFilter();
            UpdateBackButtonVisibility();
            PostGridRefresh?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Re-applies programmatic drill-down grid filters after every grid refresh.
        /// </summary>
        private void ApplyDrillDownGridFilters()
        {
            if (DrillDownGridFilters is not { Count: > 0 }) return;
            foreach (var kvp in DrillDownGridFilters)
            {
                var grid = Grids.FirstOrDefault(g => g.ResultSetID == kvp.Key);
                if (grid != null && !string.IsNullOrEmpty(kvp.Value))
                {
                    grid.SetFilter(kvp.Value);
                }
            }
        }

        private void RenameResultSet_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            if (dgv == null) return;
            var name = dgv.ResultSetName;
            if (CommonShared.ShowInputDialog(ref name, "Enter name") != DialogResult.OK) return;
            Report.CustomReportResults[dgv.ResultSetID].ResultName = name;
            suppressCboResultsIndexChanged = true;
            cboResults.Items[dgv.ResultSetID] = name;
            dgv.ResultSetName = name;

            if (sender is ToolStripItem tsRename)
            {
                var tsLabel = tsRename.Owner?.Items.OfType<ToolStripLabel>().Where(lbl => lbl.Name == ResultSetLabelControlName)
                    ?.FirstOrDefault();
                if (tsLabel != null) tsLabel.Text = name;
            }

            suppressCboResultsIndexChanged = false;
            Report.Update();
            ShowTable();
        }

        private DBADashDataGridView GetAssociatedGrid(object sender)
        {
            switch (Grids.Count)
            {
                case 0:
                    return null;

                case 1:
                    return Grids[0];
            }

            if (sender is not ToolStripButton tsb || tsb.Tag == null) return Grids[0];
            var resultSetID = (int)tsb.Tag;
            return Grids.FirstOrDefault(d => d.ResultSetID == resultSetID);
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void Columns_Click(object sender, EventArgs e)
        {
            var dgv = GetAssociatedGrid(sender);
            dgv?.PromptColumnSelection();
        }

        #region DataGridPanelResizing

        private bool isResizing = false;
        private int initialResizeY;
        private Control resizingControl;

        private void DataGridPanel_MouseLeave(object sender, EventArgs e)
        {
            if (!isResizing) // Only reset the cursor if we're not currently resizing
            {
                ((Control)sender).Cursor = Cursors.Default;
            }
        }

        private void DataGridPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Start resizing if the mouse is near the edge of the panel
            if (e.Y < ((Control)sender).Height - 10) return;
            isResizing = true;
            initialResizeY = e.Y;
            resizingControl = (Control)sender;
            resizingControl.Cursor = Cursors.SizeNS; // Change cursor to resizing cursor
        }

        private void DataGridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing && resizingControl != null)
            {
                // Calculate the new height
                var newHeight = resizingControl.Height + (e.Y - initialResizeY);
                if (newHeight <= 100) return; // Minimum height constraint
                resizingControl.Height = newHeight;
                initialResizeY = e.Y;
            }
            else if (e.Y >= ((Control)sender).Height - 10)
            {
                // Change cursor to indicate resizable area
                ((Control)sender).Cursor = Cursors.SizeNS;
            }
            else
            {
                // Change cursor back to default
                ((Control)sender).Cursor = Cursors.Default;
            }
        }

        private void DataGridPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isResizing) return;
            isResizing = false;
            resizingControl.Cursor = Cursors.Default; // Reset cursor
            resizingControl = null;
        }

        #endregion DataGridPanelResizing

        private void SetColumnLayout(DBADashDataGridView dgv)
        {
            if (Report.CustomReportResults.TryGetValue(dgv.ResultSetID, out var value) && value.ColumnLayout.Count > 0)
            {
                dgv.LoadColumnLayout(value.ColumnLayout);
                if (value.ColumnLayout.All(col => col.Value.Width == 0))
                {
                    dgv.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
                    dgv.AutoResizeColumnsWithMaxColumnWidth();
                }
            }
            else
            {
                dgv.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
                dgv.AutoResizeColumnsWithMaxColumnWidth();
            }
        }

        private void Dgv_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not DBADashDataGridView dgv) return;
            SetColumnLayout(dgv);
            dgv.DataBindingComplete -= Dgv_DataBindingComplete;
            // For stacked (multi result set) layouts, size each panel to its natural content height now that the
            // column headers and rows have been measured. A single result set is docked Fill and left untouched.
            if (dgv.Parent is Panel { Dock: DockStyle.Top })
            {
                ResizeResultPanels();
            }
        }

        private const int MinResultPanelHeight = 100;

        /// <summary>
        /// Sizes the stacked result-set panels.
        /// Scrollable report: each panel takes its full natural content height (header + all rows) so every row is
        /// shown without an internal grid scrollbar; the final panel grows to fill any gap and the container
        /// (AutoScroll) scrolls when the combined height exceeds the visible area.
        /// Single-page report: the panels are fitted into the available height, sharing it fairly so a small
        /// result set keeps its natural height while larger ones absorb the shortfall and scroll internally.
        /// </summary>
        private void ResizeResultPanels()
        {
            var parent = TablePanel;
            var panels = parent.Controls.OfType<Panel>()
                .Where(p => p.Tag is int && p.Dock == DockStyle.Top && p.Visible)
                .OrderBy(p => (int)p.Tag!)
                .ToList();
            if (panels.Count == 0) return;

            var viewport = parent.ClientSize.Height;
            var naturals = panels.Select(NaturalPanelHeight).ToList();

            if (Report?.SinglePageLayout == true)
            {
                var heights = AllocateFairHeights(naturals, viewport);
                for (var i = 0; i < panels.Count; i++) panels[i].Height = heights[i];
                return;
            }

            var total = 0;
            for (var i = 0; i < panels.Count; i++)
            {
                panels[i].Height = naturals[i];
                total += naturals[i];
            }
            // Consume any remaining space so the final result set doesn't leave an empty gap below it.
            if (total < viewport)
            {
                panels[^1].Height += viewport - total;
            }
        }

        /// <summary>
        /// Distributes <paramref name="viewport"/> pixels across result-set panels using max-min fair sharing:
        /// a panel whose natural height is no larger than its fair share keeps its natural height, and the space
        /// it leaves is shared among the larger panels (which then scroll internally). The result always sums to
        /// the viewport so the panels exactly fill a single page with no squashed gap at the bottom.
        /// </summary>
        private static int[] AllocateFairHeights(IReadOnlyList<int> naturals, int viewport)
        {
            var n = naturals.Count;
            var heights = new int[n];
            if (n == 0) return heights;
            if (viewport <= 0) // Not laid out yet - fall back to natural heights.
            {
                for (var i = 0; i < n; i++) heights[i] = naturals[i];
                return heights;
            }

            var settled = new bool[n];
            var remaining = viewport;
            var unsettled = n;
            bool progress;
            do
            {
                progress = false;
                var share = remaining / Math.Max(unsettled, 1);
                for (var i = 0; i < n; i++)
                {
                    if (settled[i] || naturals[i] > share) continue;
                    heights[i] = naturals[i];
                    remaining -= naturals[i];
                    settled[i] = true;
                    unsettled--;
                    progress = true;
                }
            } while (progress && unsettled > 0);

            if (unsettled == 0)
            {
                // Everything fits within its share - give any leftover to the last panel so the page is filled.
                if (remaining > 0) heights[n - 1] += remaining;
            }
            else
            {
                // The remaining (larger) panels split what's left equally and scroll internally.
                var share = remaining / unsettled;
                var rem = remaining - (share * unsettled);
                for (var i = 0; i < n; i++)
                {
                    if (settled[i]) continue;
                    heights[i] = share + (rem-- > 0 ? 1 : 0);
                }
            }
            return heights;
        }

        private int NaturalPanelHeight(Panel pnl)
        {
            var grid = pnl.Controls.OfType<DBADashDataGridView>().FirstOrDefault();
            if (grid == null) return MinResultPanelHeight;
            var toolStripHeight = pnl.Controls.OfType<ToolStrip>().FirstOrDefault()?.Height ?? 0;
            // GridBorderAllowance covers the grid's own border (and a little rounding headroom) on top of the
            // horizontal scrollbar allowance. Without it a wide grid that shows a horizontal scrollbar consumes
            // the full scrollbar allowance, leaving the border uncovered and forcing a (spurious) vertical
            // scrollbar too - a nested "double scroll" inside the already-scrollable container.
            const int GridBorderAllowance = 6;
            var content = grid.ColumnHeadersHeight
                          + GridRowsHeight(grid)
                          + grid.Padding.Top + grid.Padding.Bottom
                          + pnl.Padding.Top + pnl.Padding.Bottom
                          + toolStripHeight
                          + SystemInformation.HorizontalScrollBarHeight // room for a horizontal scrollbar on wide grids
                          + GridBorderAllowance;
            return Math.Max(MinResultPanelHeight, content);
        }

        /// <summary>
        /// Returns the summed height of the grid's rows, cached against the row count so frequent resize events
        /// don't re-walk every row (<see cref="DataGridViewRowCollection.GetRowsHeight"/> is O(rowCount)).
        /// </summary>
        private int GridRowsHeight(DBADashDataGridView grid)
        {
            var rowCount = grid.Rows.Count;
            if (rowsHeightCache.TryGetValue(grid, out var cached) && cached.RowCount == rowCount)
            {
                return cached.RowsHeight;
            }
            var rowsHeight = grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible);
            rowsHeightCache[grid] = (rowCount, rowsHeight);
            return rowsHeight;
        }

        /// <summary>
        /// Run the query to get the data for the user custom report
        /// </summary>
        /// <returns></returns>
        protected async Task<DataSet> GetReportDataAsync(CancellationToken cancellationToken)
        {
            // If no procedure is configured for this report (system-only charts), don't attempt to execute a stored procedure
            if (string.IsNullOrWhiteSpace(Report?.QualifiedProcedureName))
            {
                return new DataSet();
            }

            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand(Report.QualifiedProcedureName, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);

            // Add system parameters unless they are overridden by user supplied parameters for drill down reports
            var pInstanceIDs = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@InstanceIDs", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pInstanceIDs != null)
            {
                pInstanceIDs.Param.Value = context.InstanceIDs.AsDataTable();
            }
            var pInstanceID = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@InstanceID", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pInstanceID != null)
            {
                pInstanceID.Param.Value = context.InstanceID > 0 ? context.InstanceID : DBNull.Value;
            }
            var pDatabaseID = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@DatabaseID", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pDatabaseID != null)
            {
                pDatabaseID.Param.Value = context.DatabaseID > 0 ? context.DatabaseID : DBNull.Value;
            }
            // Hidden instances are shown when a single instance is in context or the global ShowHidden preference is enabled.
            var pShowHidden = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ShowHidden", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pShowHidden != null)
            {
                pShowHidden.Param.Value = context.InstanceIDs.Count == 1 || Common.ShowHidden;
            }
            var pFromDate = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@FromDate", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pFromDate != null)
            {
                pFromDate.Param.Value = DateRange.FromUTC;
            }

            var pToDate = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ToDate", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pToDate != null)
            {
                pToDate.Param.Value = DateRange.ToUTC;
            }

            var pObjectID = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals("@ObjectID", StringComparison.InvariantCultureIgnoreCase) && p.UseDefaultValue);
            if (pObjectID != null)
            {
                pObjectID.Param.Value = context.ObjectID > 0 ? context.ObjectID : DBNull.Value;
            }

            if (context.ObjectID > 0)
            {
                var pObjectName = customParams.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals("@ObjectName", StringComparison.InvariantCultureIgnoreCase) &&
                    p.UseDefaultValue);
                if (pObjectName != null && !string.IsNullOrEmpty(context.ObjectName))
                {
                    pObjectName.Param.Value = context.ObjectName;
                    pObjectName.UseDefaultValue = false;
                }

                var pTableName = customParams.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals("@TableName", StringComparison.InvariantCultureIgnoreCase) &&
                    p.UseDefaultValue);
                if (pTableName != null && !string.IsNullOrEmpty(context.ObjectName) &&
                    context.Type == SQLTreeItem.TreeType.Table)
                {
                    pTableName.Param.Value = context.ObjectName;
                    pTableName.UseDefaultValue = false;
                }

                var pSchemaName = customParams.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals("@SchemaName", StringComparison.InvariantCultureIgnoreCase) &&
                    p.UseDefaultValue);
                if (pSchemaName != null && !string.IsNullOrEmpty(context.SchemaName))
                {
                    pSchemaName.Param.Value = context.SchemaName;
                    pSchemaName.UseDefaultValue = false;
                }
            }

            // Add user supplied parameters
            foreach (var p in customParams.Where(p => !p.UseDefaultValue || CustomReport.SystemParamNames.Contains(p.Param.ParameterName, StringComparer.OrdinalIgnoreCase)))
            {
                cmd.Parameters.Add(p.Param.Clone());
            }
            await using var registration = cancellationToken.Register(() =>
            {
                // ReSharper disable once AccessToDisposedClosure
                cmd.Cancel();
            });
            var ds = new DataSet();
            try
            {
                da.Fill(ds);
                ConvertDateTimeColsToLocalTimeZone(ds);
            }
            finally
            {
                registration.Unregister();
            }
            return ds;
        }

        public void SetContext(DBADashContext _context)
        {
            _ = SetContext(_context, null);
        }

        public async Task SetContext(DBADashContext _context, List<CustomSqlParameter> sqlParams)
        {
            try
            {
                if (_context == this.context)
                {
                    // By default refresh is skipped unless context has changed.
                    if (Report.ForceRefreshWithoutContextChange && AutoLoad)
                    {
                        RefreshData();
                    }

                    return;
                }

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => _ = SetContext(_context, sqlParams)));
                    return;
                }

                cboResults.Visible = false;
                lblSelectResults.Visible = false;
                ResetTimer();
                if (IsMessageInProgress)
                {
                    await CancelProcessing();
                }

                ClearResults();
                IsMessageInProgress = false;
                CurrentMessageGroup = Guid.Empty;
                this.context = _context;
                // Only overwrite Report from context when not explicitly prevented by the host.
                if (!PreventReportOverwrite)
                {
                    Report = _context.Report ?? Report;
                }
                // Clear drill-down navigation state on tree navigation (not drill-down).
                // This prevents a stale back button when the user selects a different report
                // in the tree or navigates to a different level.
                if (sqlParams == null)
                {
                    ClearNavigationStack();
                }
                OnContextChanged(isDrillDown: sqlParams != null);
                customParams = sqlParams ?? Report.GetCustomSqlParameters();
                SetContextParametersForDirectExecutionReport();
                EnsureStatusFilter();
                // Reset the filter on tree navigation only; drill-downs supply their own parameter state.
                if (sqlParams == null) ResetStatusFilter();
                // When a status filter is shown it drives the @Include* parameters, so the generic params button is redundant.
                tsParams.Visible = Report.UserParams.Any() && Report.ShowStatusFilter != true;
                tsConfigure.Visible = Report.CanEditReport;
                SetStatus(Report.Description, Report.Description, DBADashUser.SelectedTheme.ForegroundColor);
                lblDescription.Visible = !string.IsNullOrEmpty(Report.Description);
                if (Report.DeserializationException != null)
                {
                    MessageBox.Show(
                        "An error occurred deserializing the report. Preferences have been reset.\n" +
                        Report.DeserializationException.Message, "Warning", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    Report.DeserializationException = null; // Display the message once
                }

                editPickersToolStripMenuItem.Enabled = Report.UserParams.Any();
                tsRefresh.Visible = Report is not DirectExecutionReport;
                tsExecute.Visible = Report is DirectExecutionReport;
                tsExecute.Enabled = Report is DirectExecutionReport rpt && _context.IsReportAllowed(rpt);
                associateCollectionToolStripMenuItem.Visible = Report is not DirectExecutionReport;
                lblURL.Text = Report.URL;
                lblURL.Visible = !string.IsNullOrEmpty(Report.URL);
                AddPickers();
                SetTriggerCollectionVisibility();
                CheckChartLocation();
                SetTablePanelCollapsed(!Report.TableVisible);
                if (AutoLoad)
                {
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error setting context");
            }
        }

        /// <summary>
        /// Called during SetContext after the new context is assigned but before parameters
        /// are built and data is refreshed. Override to reset report state (e.g., switch
        /// back to a default view) without incurring a wasted query.
        /// </summary>
        /// <param name="isDrillDown">True when the caller provided explicit parameters (e.g., drill-down navigation);
        /// false for tree navigation where the report should typically reset to its default view.</param>
        protected virtual void OnContextChanged(bool isDrillDown)
        {
        }

        /// <summary>
        /// Called at the start of every <see cref="RefreshData"/> before parameters are read and the
        /// query is executed. Override to write view-specific parameter values into <see cref="customParams"/>
        /// (for example, a custom status filter). Default implementation does nothing.
        /// </summary>
        protected virtual void OnBeforeRefresh()
        {
        }

        #region Status filter

        private StatusFilterToolStrip statusFilter;

        /// <summary>
        /// The shared status filter created when <see cref="CustomReport.ShowStatusFilter"/> is set.  Exposed so a
        /// derived view (or a drill-down link) can adjust the selection - for example showing all statuses when
        /// drilling into a single instance.  Null when the current report does not use a status filter.
        /// </summary>
        protected StatusFilterToolStrip StatusFilter => statusFilter;

        /// <summary>
        /// Creates the status filter on first use and toggles its visibility (and that of the redundant generic
        /// Parameters button) to match the current report.  Called from SetContext so it tracks report changes,
        /// including drill-downs that swap to a report without a status filter.
        /// </summary>
        private void EnsureStatusFilter()
        {
            var show = Report?.ShowStatusFilter == true;
            if (show && statusFilter == null)
            {
                statusFilter = new StatusFilterToolStrip
                {
                    Name = "tsStatusFilter",
                    AcknowledgedVisible = ReportHasParam("@IncludeACK")
                };
                // StatusFilterToolStrip forces its own icon (FilterCircle), but only when the Image setter runs.
                statusFilter.Image = Properties.Resources.FilterCircle_16x_Colors;
                statusFilter.UserChangedStatusFilter += (_, _) =>
                {
                    if (context != null) RefreshData();
                };
                // Insert just after the Parameters button so the filter sits at the start of the toolbar.
                var insertAt = toolStrip1.Items.IndexOfKey("tsParams");
                insertAt = insertAt >= 0 ? insertAt + 1 : toolStrip1.Items.Count;
                toolStrip1.Items.Insert(insertAt, statusFilter);
            }
            if (statusFilter != null) statusFilter.Visible = show;
        }

        /// <summary>
        /// Resets the status filter on tree navigation: a single instance in context shows all statuses, multiple
        /// instances default to Critical + Warning only.
        /// </summary>
        private void ResetStatusFilter()
        {
            if (statusFilter == null || Report?.ShowStatusFilter != true) return;
            var single = context?.InstanceIDs.Count == 1;
            statusFilter.Critical = true;
            statusFilter.Warning = true;
            statusFilter.NA = single;
            statusFilter.OK = single;
        }

        /// <summary>
        /// Snapshots the current status filter selection for back-navigation, or null when no status filter is
        /// shown.  The captured values are the effective getters (an all-unchecked filter reads as all-true, which
        /// is the same ALL semantics) so restoring them reproduces identical @Include* parameters.
        /// </summary>
        private StatusFilterSelection CaptureStatusFilter() =>
            statusFilter == null
                ? null
                : new StatusFilterSelection(statusFilter.Critical, statusFilter.Warning, statusFilter.NA, statusFilter.OK, statusFilter.Acknowledged);

        /// <summary>
        /// Restores a status filter selection captured by <see cref="CaptureStatusFilter"/>.  Setting the checkbox
        /// properties only updates the control text (it does not raise UserChangedStatusFilter), so this is safe to
        /// call before a refresh without triggering a second one.
        /// </summary>
        private void RestoreStatusFilter(StatusFilterSelection selection)
        {
            if (selection == null || statusFilter == null) return;
            statusFilter.Critical = selection.Critical;
            statusFilter.Warning = selection.Warning;
            statusFilter.NA = selection.NA;
            statusFilter.OK = selection.OK;
            statusFilter.Acknowledged = selection.Acknowledged;
        }

        /// <summary>
        /// Pushes the current status filter selection into the report's @Include* parameters before a refresh.
        /// </summary>
        private void ApplyStatusFilterParams()
        {
            if (statusFilter == null || Report?.ShowStatusFilter != true) return;
            SetBitParam("@IncludeCritical", statusFilter.Critical);
            SetBitParam("@IncludeWarning", statusFilter.Warning);
            SetBitParam("@IncludeNA", statusFilter.NA);
            SetBitParam("@IncludeOK", statusFilter.OK);
            if (ReportHasParam("@IncludeACK")) SetBitParam("@IncludeACK", statusFilter.Acknowledged);
        }

        private bool ReportHasParam(string name) =>
            Report?.Params?.ParamList?.Any(p => p.ParamName.Equals(name, StringComparison.OrdinalIgnoreCase)) == true;

        private void SetBitParam(string name, bool value)
        {
            var p = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (p == null) return;
            p.Param.Value = value;
            p.UseDefaultValue = false;
        }

        #endregion Status filter

        private void SetContextParametersForDirectExecutionReport() // Set DatabaseName
        {
            if (Report is not DirectExecutionReport dxReport) return;
            foreach (var p in customParams.Where(p =>
                         p.Param.ParameterName.TrimStart('@').Equals(dxReport.DatabaseNameParameter?.TrimStart('@'),
                             StringComparison.InvariantCultureIgnoreCase)))
            {
                p.Param.Value = string.IsNullOrEmpty(context.DatabaseName) ? DBNull.Value : context.DatabaseName;
                p.UseDefaultValue = string.IsNullOrEmpty(context.DatabaseName) && Report is not SystemDirectExecutionReport; // SystemDirectExecutionReport are scripts with no parameter default value
            }

            if (context.ObjectID > 0)
            {
                var objectParamName = string.IsNullOrEmpty(dxReport.ObjectParameterName) ? "@ObjectName" : dxReport.ObjectParameterName;
                var schemaParamName = string.IsNullOrEmpty(dxReport.ObjectParameterName) ? "@SchemaName" : dxReport.ObjectParameterName;

                var pObjectName = customParams.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals(objectParamName, StringComparison.InvariantCultureIgnoreCase) &&
                    p.UseDefaultValue);
                if (pObjectName != null && !string.IsNullOrEmpty(context.ObjectName))
                {
                    pObjectName.Param.Value = context.ObjectName;
                    pObjectName.UseDefaultValue = false;
                }

                var pTableName = customParams.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals("@TableName", StringComparison.InvariantCultureIgnoreCase) &&
                    p.UseDefaultValue);
                if (pTableName != null && !string.IsNullOrEmpty(context.ObjectName) &&
                    context.Type == SQLTreeItem.TreeType.Table)
                {
                    pTableName.Param.Value = context.ObjectName;
                    pTableName.UseDefaultValue = false;
                }

                var pSchemaName = customParams.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals(schemaParamName, StringComparison.InvariantCultureIgnoreCase) &&
                    p.UseDefaultValue);
                if (pSchemaName != null && !string.IsNullOrEmpty(context.ObjectName))
                {
                    pSchemaName.Param.Value = context.SchemaName;
                    pSchemaName.UseDefaultValue = false;
                }
            }

            // Some reports have a parameter to get all databases that we need to turn off
            foreach (var p in customParams.Where(p =>
                         string.Equals(p.Param.ParameterName, "@GetAllDatabases") ||
                         string.Equals(p.Param.ParameterName, "@get_all_databases",
                             StringComparison.OrdinalIgnoreCase)))
            {
                p.Param.Value = false;
                p.UseDefaultValue = false;
            }
        }

        public void SetTriggerCollectionVisibility() => tsTrigger.Visible = Report.TriggerCollectionTypes.Count > 0 &&
            (context.CanMessage || (context.InstanceID <= 0 && ContextHasMessagingEnabledInstances()));

        /// <summary>
        /// True if the current (group/folder/root) context contains at least one messaging-enabled
        /// instance, allowing a collection to be triggered across all of them at once.
        /// </summary>
        private bool ContextHasMessagingEnabledInstances()
        {
            if (!DBADashUser.AllowMessaging) return false;
            var ids = GetEffectiveInstanceIDs();
            if (ids.Count == 0) return false;
            return CommonData.Instances.AsEnumerable()
                .Any(r => ids.Contains(r.Field<int>("InstanceID")) && r.Field<bool>("MessagingEnabled"));
        }

        /// <summary>
        /// The set of instances the report is currently scoped to.  Normally this is every instance in the
        /// tree context, but a drill-down narrows the scope by overriding the @InstanceID / @InstanceIDs
        /// parameter (without changing the context object) - in which case a triggered collection should
        /// target only the drilled-into instance(s) rather than the whole original context.
        /// </summary>
        private HashSet<int> GetEffectiveInstanceIDs()
        {
            // Drill-down to a single instance via @InstanceID.  Guard against the parameter carrying an
            // instance outside the current context - fall through to the wider scope rather than target it.
            var pInstanceID = customParams.FirstOrDefault(p => !p.UseDefaultValue &&
                p.Param.ParameterName.Equals("@InstanceID", StringComparison.OrdinalIgnoreCase));
            if (pInstanceID?.Param.Value is int id && id > 0 && context.InstanceIDs.Contains(id))
            {
                return new HashSet<int> { id };
            }

            // Drill-down to a narrowed set via @InstanceIDs (stored as a single-column DataTable).
            if (customParams.FirstOrDefault(p => !p.UseDefaultValue &&
                    p.Param.ParameterName.Equals("@InstanceIDs", StringComparison.OrdinalIgnoreCase))
                    ?.Param.Value is DataTable { Rows.Count: > 0 } dt)
            {
                var ids = dt.AsEnumerable().Select(r => r[0]).OfType<int>().ToHashSet();
                // Guard against the parameter carrying an instance outside the current context.
                ids.IntersectWith(context.InstanceIDs);
                if (ids.Count > 0) return ids;
            }

            return context.InstanceIDs;
        }

        private void AddPickers()
        {
            const string pickerTag = "Picker";
            tsParams.DropDownItems.Clear();
            foreach (var item in toolStrip1.Items.Cast<ToolStripItem>().Where(i => i.Tag?.ToString() == pickerTag).ToList())
            {
                toolStrip1.Items.Remove(item);
                item.Dispose();
            }
            tsParams.Click -= TsParameters_Click;
            if (Report.Pickers == null)
            {
                tsParams.Click += TsParameters_Click;

                return;
            }

            var pickers = Report.Pickers;
            if (customParams.Any(p => p.Param.ParameterName.TrimStart('@').Equals("Top", StringComparison.InvariantCultureIgnoreCase) && p.Param.SqlDbType == SqlDbType.Int) && !pickers.Any(p => p.ParameterName.TrimStart('@').Equals("Top", StringComparison.InvariantCultureIgnoreCase)))
            {
                pickers.Add(Picker.CreateTopPicker());
            }
            var baseIdx = toolStrip1.Items.IndexOf(tsParams);

            foreach (var picker in Report.Pickers.OrderBy(p => p.Name))
            {
                var param = customParams.FirstOrDefault(p =>
                        p.Param.ParameterName.TrimStart('@').Equals(picker.ParameterName.TrimStart('@'),
                            StringComparison.InvariantCultureIgnoreCase));
                if (param == null) continue;
                if (picker.DefaultValue == DBNull.Value)
                {
                    param.UseDefaultValue = false;
                    param.Param.Value = DBNull.Value;
                }
                else if (param.UseDefaultValue && !string.IsNullOrEmpty(picker.DefaultValue?.ToString()))
                {
                    param.Param.Value = picker.DefaultValue;
                    param.UseDefaultValue = false;
                }

                if (picker.IsText)
                {
                    var txtItem = new ToolStripTextBox() { Tag = pickerTag };
                    txtItem.Text = param.Param.Value?.ToString() ?? picker.DefaultValue?.ToString() ?? string.Empty;

                    txtItem.TextChanged += (_, _) =>
                    {
                        param.Param.Value = txtItem.Text;
                        param.UseDefaultValue = false;
                    };
                    txtItem.KeyDown += (_, args) =>
                    {
                        if (args.KeyData == Keys.Enter && AutoLoad)
                        {
                            RefreshData();
                        }
                    };
                    if (picker.MenuBar)
                    {
                        var lbl = new ToolStripLabel(picker.Name + ":") { Tag = pickerTag };
                        toolStrip1.Items.Insert(baseIdx + 1, lbl);
                        toolStrip1.Items.Insert(baseIdx + 2, txtItem);
                    }
                    else
                    {
                        var pickerMenu = new ToolStripMenuItem(picker.Name) { Tag = pickerTag };
                        pickerMenu.DropDownItems.Add(txtItem);
                        tsParams.DropDownItems.Add(pickerMenu);
                    }
                }
                else
                {
                    var pickerMenu = new ToolStripMenuItem(picker.Name) { Tag = pickerTag };
                    foreach (var itm in picker.PickerItems ?? new Dictionary<object, string>())
                    {
                        var item = new ToolStripMenuItem(itm.Value)
                        {
                            Tag = itm.Key,
                            Checked = (param.UseDefaultValue && string.IsNullOrEmpty(itm.Key.ToString())) || (!param.UseDefaultValue && param.Param.Value != null && param.Param.Value.ToString() == itm.Key.ToString())
                        };
                        item.Click += (sender, e) => PickerItem_Click(sender, itm, picker.ParameterName);
                        pickerMenu.DropDownItems.Add(item);
                    }
                    if (picker.MenuBar)
                    {
                        toolStrip1.Items.Insert(baseIdx + 1, pickerMenu);
                    }
                    else
                    {
                        tsParams.DropDownItems.Add(pickerMenu);
                    }
                }
            }

            tsParams.DropDownItems.Add(new ToolStripSeparator());

            var tsParameters = new ToolStripMenuItem("Parameters");
            tsParameters.Click += TsParameters_Click;
            tsParams.DropDownItems.Add(tsParameters);
        }

        private void PickerItem_Click(object sender, KeyValuePair<object, string> itm, string paramName)
        {
            var menu = (ToolStripMenuItem)sender;
            var param = customParams.First(p => p.Param.ParameterName.TrimStart('@').Equals(paramName.TrimStart('@'), StringComparison.InvariantCultureIgnoreCase));
            if (itm.Key != DBNull.Value && (itm.Key == null || string.IsNullOrEmpty(itm.Key.ToString())))
            {
                param.UseDefaultValue = true;
            }
            else
            {
                param.Param.Value = itm.Key;
                param.UseDefaultValue = false;
            }

            if (menu.Owner != null)
            {
                foreach (var item in menu.Owner.Items.Cast<ToolStripMenuItem>())
                {
                    item.Checked = item == menu;
                }
            }
            if (AutoLoad)
            {
                RefreshData();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(Grids.Cast<DataGridView>().ToArray());
        }

        private void SetTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var title = Report.ReportName;
            if (CommonShared.ShowInputDialog(ref title, "Update Title") != DialogResult.OK) return;
            try
            {
                Report.ReportName = title;
                Report.Update();
                OnReportNameChanged(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving title");
            }
        }

        protected virtual void OnReportNameChanged(EventArgs e)
        {
            ReportNameChanged?.Invoke(this, e);
        }

        private void TsParameters_Click(object sender, EventArgs e)
        {
            PromptParams();
        }

        private void LnkParams_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PromptParams();
        }

        private void PromptParams()
        {
            var frm = new ReportParams() { Params = customParams };
            frm.ShowDialog();
            if (frm.DialogResult != DialogResult.OK) return;
            customParams = frm.Params;
            AddPickers();// Update checks on picker items
            if (AutoLoad)
                RefreshData();
        }

        private void SaveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Save Layout (column visibility, order and size)?", "Save", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var dgv in Grids)
            {
                Report.CustomReportResults[dgv.ResultSetID].ColumnLayout = dgv.GetColumnLayout();
            }
            Report.ChartVisible = !IsChartPanelCollapsed();
            Report.TableVisible = !IsTablePanelCollapsed();
            Report.ChartSplitPercentage = GetCurrentChartSplitPercentage();
            Report.Update();
        }

        private void ResetLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset Layout (column visibility, order and size)?", "Reset", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var dgv in Grids)
            {
                Report.CustomReportResults[dgv.ResultSetID].ColumnLayout = new();
            }
            Report.Update();
            RefreshData();
        }

        private void CboResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressCboResultsIndexChanged) return;
            previousSchema = string.Empty; // Force re-generation of grids
            SetTablePanelCollapsed(false);
            ShowTable();
        }

        private void SetDescriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var description = Report.Description;
            if (CommonShared.ShowInputDialog(ref description, "Enter description") != DialogResult.OK) return;
            Report.Description = description;
            Report.Update();
            lblDescription.Text = Report.Description;
            lblDescription.Visible = !string.IsNullOrEmpty(Report.Description);
        }

        private void ScriptReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ScriptReport();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error scripting report");
            }
        }

        private void ScriptReport()
        {
            var options = new ScriptingOptions()
            {
                ScriptForCreateOrAlter = true,
                ScriptBatchTerminator = true,
                EnforceScriptingOptions = true
            }; /* EnforceScriptingOptions = true is required to generate CREATE OR ALTER */

            using var cn = new SqlConnection(Common.ConnectionString);
            var serverCn = new ServerConnection(cn);
            var server = new Server(serverCn);
            var db = server.Databases[cn.Database];

            var sb = new StringBuilder();
            sb.AppendFormat("/*\n\t{0}\n\t{1}\n\n\tCustom report for DBA Dash.\n\thttp://dbadash.com\n\tGenerated: {2:yyyy-MM-dd HH:mm:ss} \n*/\n\n",
                Report.ReportName.Replace("*/", ""), Report.Description?.Replace("*/", ""), DateTime.Now);

            if (Report is not DirectExecutionReport)
            {
                var proc = db.StoredProcedures[Report.ProcedureName, Report.SchemaName];
                if (proc != null)
                {
                    var parts = proc.Script(options);

                    foreach (var part in parts)
                    {
                        sb.AppendLine(part);
                        sb.AppendLine("GO");
                    }
                }
                else
                {
                    sb.AppendLine("/* WARNING: Stored Procedure not found */");
                }
            }
            try
            {
                foreach (var picker in Report.Pickers?.OfType<DBPicker>() ?? Enumerable.Empty<DBPicker>())
                {
                    sb.AppendLine($"/* Script picker {picker.Name.Replace("*", "")} */");

                    var (ObjectId, SchemaName, ObjectName) = CommonData.GetDBObject(picker.StoredProcedureName);
                    if (ObjectName == null || SchemaName == null)
                    {
                        sb.AppendLine($"/* Unable to find object {picker.StoredProcedureName.Replace("*", "")} */");
                        continue;
                    }

                    var pickerProc = db.StoredProcedures[ObjectName, SchemaName];
                    var pickerParts = pickerProc.Script(options);
                    foreach (var part in pickerParts)
                    {
                        sb.AppendLine(part);
                        sb.AppendLine("GO");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"/* Error scripting pickers {ex.Message.Replace("*", "")} */");
            }

            var meta = Report.Serialize();
            sb.AppendLine();
            sb.AppendLine("/* Report customizations in GUI */");
            sb.AppendFormat("DELETE dbo.CustomReport\nWHERE SchemaName = '{0}'\nAND ProcedureName = '{1}'\n\n", Report.SchemaName.SqlSingleQuote(), Report.ProcedureName.SqlSingleQuote());
            sb.AppendLine("INSERT INTO dbo.CustomReport(SchemaName,ProcedureName,MetaData)");
            sb.AppendFormat("VALUES('{0}','{1}','{2}')", Report.SchemaName.SqlSingleQuote(),
                Report.ProcedureName.SqlSingleQuote(), meta.SqlSingleQuote());

            var frm = new CodeViewer() { Code = sb.ToString() };
            frm.ShowDialog();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = (DBADashDataGridView)sender;
            if (e.RowIndex < 0) return;
            var colName = dgv.Columns[e.ColumnIndex].DataPropertyName;
            LinkColumnInfo linkColumnInfo = null;
            Report.CustomReportResults[dgv.ResultSetID].LinkColumns?.TryGetValue(colName, out linkColumnInfo);
            try
            {
                linkColumnInfo?.Navigate(GetContext(), dgv.Rows[e.RowIndex], dgv.ResultSetID, this);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error navigating to link");
            }
        }

        private DBADashContext GetContext()
        {
            if (context.Report != null) return context;
            var tempContext = context.DeepCopy();
            tempContext.Report = Report;
            return tempContext;
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is not DBADashDataGridView dgv) return;
            if (!Report.CustomReportResults.TryGetValue(dgv.ResultSetID, out CustomReportResult value)) return;
            value.CellHighlightingRules.FormatRowsAdded(dgv, e);
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
            foreach (var dgv in Grids)
            {
                dgv.SetFilter(string.Empty);
            }
            tsClearFilter.Enabled = false;
            tsClearFilter.Font = new Font(tsClearFilter.Font, FontStyle.Regular);
            tsClearFilter.ToolTipText = string.Empty;
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            if (context.InstanceID > 0)
            {
                if (context.CollectAgentID == null || context.ImportAgentID == null) return;
                await CollectionMessaging.TriggerCollection(context.ConnectionID, Report.TriggerCollectionTypes, context.CollectAgentID.Value, context.ImportAgentID.Value, this);
            }
            else
            {
                // Higher up the tree (or drilled-down into a subset) - trigger across the instances the
                // report is currently scoped to rather than the whole original context.
                BulkTriggerCollectionForm.Trigger(this, Report.TriggerCollectionTypes, GetEffectiveInstanceIDs(), this);
            }
        }

        private void AssociateCollectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var collectionTypes = string.Join(',', Report.TriggerCollectionTypes);
            if (CommonShared.ShowInputDialog(ref collectionTypes, "Enter collection types to associate with this report", default, "Enter name of collection to be associated with this report.\nThis will allow the collection to be triggered directly from this report.\nMultiple collections can be specified comma-separated.\ne.g.\nUserData.MyCustomCollection") != DialogResult.OK) return;

            Report.TriggerCollectionTypes = collectionTypes.Split(',').Select(c => c.Trim()).ToList();
            Report.Update();
            SetTriggerCollectionVisibility();
        }

        private void EditPickersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var pickers = new Pickers() { Report = Report };
            pickers.ShowDialog();
            if (pickers.DialogResult != DialogResult.OK) return;
            AddPickers();
        }

        private void URL_Click(object sender, EventArgs e)
        {
            CommonShared.OpenURL(Report.URL);
        }

        private DateTime TimerStart;

        private void Timer1_Tick(object sender, EventArgs e)
        {
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(UpdateTimer);
                return;
            }
            lblTimer.Text = (DateTime.Now - TimerStart).ToString(@"hh\:mm\:ss");
        }

        private async void TsCancel_Click(object sender, EventArgs e)
        {
            await CancelProcessing();
        }

        private async Task CancelProcessing()
        {
            if (!string.IsNullOrEmpty(Report.CancellationMessageWarning)
                && MessageBox.Show(Report.CancellationMessageWarning + "\nDo you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            if (Report is DirectExecutionReport)
            {
                if (CurrentMessageGroup != Guid.Empty)
                {
                    var msg = new CancellationMessage()
                    {
                        CollectAgent = context.CollectAgent,
                        ImportAgent = context.ImportAgent,
                        Lifetime = Config.DefaultCommandTimeout,
                        CancelMessageId = CurrentMessageGroup
                    };
                    SetStatus("Cancellation requested", "", DashColors.Warning);
                    await MessagingHelper.SendMessageAndProcessReply(msg, context, SetStatus, ProcessCompletedMessage,
                        CurrentMessageGroup);
                }
                else
                {
                    SetStatus("Nothing to cancel", "", DashColors.Information);
                    IsMessageInProgress = false;
                }
            }
            else
            {
                // Atomically take ownership of the CTS and cancel/dispose it
                var cts = System.Threading.Interlocked.Exchange(ref cancellationTokenSource, null);
                try
                {
                    if (cts != null)
                    {
                        // Only request cancellation here. Do NOT dispose the CTS while the background
                        // operation may still be unregistering callbacks or using the token. Disposal is
                        // performed in the completion path (RefreshDataRepository) to avoid races.
                        await cts.CancelAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CancelProcessing: error cancelling CTS: {ex}");
                }
            }
        }

        private void TsNewWindow_Click(object sender, EventArgs e)
        {
            var frm = new CustomReportViewer();

            var ctx = context.DeepCopy();
            ctx.Report = Report;
            frm.Context = ctx;
            frm.DataSet = reportDS;
            frm.CustomParams = customParams;
            frm.Show();
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            previousSchema = null;
            ShowTable();
        }

        private void ToggleCharts_Click(object sender, EventArgs e)
        {
            if (Charts.Count == 0) return;
            var newCollapsed = !IsChartPanelCollapsed();
            SetChartPanelCollapsed(newCollapsed);
            if (!newCollapsed)
            {
                GetChartPanels();
            }
        }

        private void ToggleGrids(object sender, EventArgs e)
        {
            SetTablePanelCollapsed(!IsTablePanelCollapsed());
        }

        private async void AddChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new ConfigureChart() { Report = Report, ReportDS = reportDS };
            await frm.ShowDialogAsync();
            if (frm.DialogResult != DialogResult.OK) return;
            Report.ChartVisible = true;
            Report.Update();
            previousSchema = null; // Force re-generation of grids and charts
            ShowTable();
        }

        private void SetChartLocation(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            var location = ts.Tag.ToString();
            Report.ChartLocation = Enum.Parse<CustomReport.ChartLocations>(location);
            Report.Update();
            SetTablePanelCollapsed(false); // make table visible
            SetChartPanelCollapsed(!Charts.Any()); // Make chart panel visible if we have charts, otherwise hide it
            previousSchema = null; // Force re-generation of grids and charts
            CheckChartLocation();
            ShowTable();
        }

        private void CheckChartLocation()
        {
            foreach (ToolStripMenuItem ts in chartLocationToolStripMenuItem.DropDownItems)
            {
                var location = ts.Tag.ToString();
                ts.Checked = Report.ChartLocation == Enum.Parse<CustomReport.ChartLocations>(location);
            }
        }

        private void AddSystemChart_Click(object sender, EventArgs e)
        {
            var chartType = (IMetric.MetricTypes)Enum.Parse(typeof(IMetric.MetricTypes), ((ToolStripMenuItem)sender).Tag.ToString());

            // Create a sensible default persisted state for the newly added metric chart.
            IMetric metricState = null;
            switch (chartType)
            {
                case IMetric.MetricTypes.ResourceGovernorWorkloadGroups:
                    metricState = new ResourceGovernorWorkloadGroupsMetric
                    {
                        ShowTable = false,
                        MetricsToDisplay = new List<string> { ResourceGovernorWorkloadGroupsMetric.DefaultMetrics[0] }
                    };
                    break;

                case IMetric.MetricTypes.ResourceGovernorResourcePools:
                    metricState = new ResourceGovernorResourcePoolMetric
                    {
                        ShowTable = false,
                        MetricsToDisplay = new List<string> { ResourceGovernorResourcePoolMetric.DefaultMetrics[0] }
                    };
                    break;

                case IMetric.MetricTypes.PerformanceCounter:
                    metricState = new PerformanceCounterMetric();
                    break;

                case IMetric.MetricTypes.CPU:
                    metricState = new CPUMetric();
                    break;

                case IMetric.MetricTypes.IO:
                    metricState = new IOMetric();
                    break;

                case IMetric.MetricTypes.Blocking:
                    metricState = new BlockingMetric();
                    break;

                case IMetric.MetricTypes.ObjectExecution:
                    metricState = new ObjectExecutionMetric();
                    break;

                case IMetric.MetricTypes.Waits:
                    metricState = new WaitMetric();
                    break;

                default:
                    break;
            }
            if (metricState == null)
            {
                MessageBox.Show($"Unsupported chart type: {chartType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Report.Charts.Add(new CustomReportChart
            {
                Metric = metricState
            });
            Report.Update();
            RefreshData();
        }

        private void SaveSystemChartState(object sender, EventArgs e)
        {
            try
            {
                if (Report?.Charts == null || Report.Charts.Count == 0)
                {
                    MessageBox.Show("No charts to save state for.", "Save State", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var saved = 0;

                foreach (var ctrl in Charts.ToArray())
                {
                    if (ctrl == null || ctrl.IsDisposed) continue;
                    if (ctrl.Tag is not CustomReportChart wrapper) continue;
                    if (wrapper.Metric == null) continue;
                    if (ctrl is IMetricChart metricChart)
                    {
                        IMetric state = null;
                        try
                        {
                            // Persist the metric POCO directly. Controls should expose their
                            // current configuration via the Metric property.
                            state = metricChart.Metric;
                        }
                        catch (Exception ex)
                        {
                            CommonShared.ShowExceptionDialog(ex, "Error getting chart state");
                            return;
                        }

                        wrapper.Metric = state; // IMetric POCO
                        saved++;
                    }
                }

                if (saved > 0)
                {
                    try
                    {
                        Report.Update();
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex, "Error saving report state");
                        return;
                    }
                }

                MessageBox.Show(saved > 0 ? $"Saved state for {saved} system chart(s)." : "No chart state was saved.", "Save State", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving system chart state");
            }
        }

        private async void AddPerformanceCounter(object sender, EventArgs e)
        {
            var frm = new SelectPerformanceCounters() { ShowCurrent = false };
            await frm.ShowDialogAsync();
            if (frm.DialogResult != DialogResult.OK || frm.SelectedCounters == null || frm.SelectedCounters.Count == 0) return;

            var metric = new PerformanceCounterMetric();
            foreach (var kvp in frm.SelectedCounters)
            {
                metric.Counters.Add(kvp.Value);
            }
            var title = metric.GetTitle();
            if (Common.ShowInputDialog(ref title, "Edit Chart Title") == DialogResult.OK)
            {
                metric.Title = title;
            }
            Report.Charts ??= new();
            Report.Charts.Add(new CustomReportChart() { Metric = metric });

            Report.Update();
            RefreshData();
        }

        private void DeleteAllCharts(object sender, EventArgs e)
        {
            if (MessageBox.Show("Delete all charts from this report?", "Delete Charts", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Report.Charts = new();
            Report.Update();
            RefreshData();
        }

        private void ChartLayout_Click(object sender, EventArgs e)
        {
            if (Report?.Charts == null || Report.Charts.Count == 0)
            {
                MessageBox.Show("No charts are configured for this report.", "Chart Layout", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var layout = new ChartLayout() { ChartCount = Report.Charts.Count, Columns = Report.ChartLayoutColumns, Rows = Report.ChartLayoutRows };
            layout.ShowDialog(this);
            if (layout.DialogResult != DialogResult.OK) return;
            Report.ChartLayoutColumns = layout.Columns;
            Report.Update();
            RefreshData();
        }
    }
}