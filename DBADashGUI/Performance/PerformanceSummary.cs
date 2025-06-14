using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.Theme;
using static DBADashGUI.Main;

namespace DBADashGUI.Performance
{
    public partial class PerformanceSummary : UserControl, ISetContext, IRefreshData, IThemedControl
    {
        //public List<Int32> InstanceIDs;
        private DBADashContext context;

        //public string TagIDs;
        private readonly List<KeyValuePair<string, PersistedColumnLayout>> standardLayout;

        public Dictionary<int, Counter> SelectedPerformanceCounters = new();
        private PerformanceSummarySavedView selectedView;

        public PerformanceSummary()
        {
            InitializeComponent();
            standardLayout = dgv.GetColumnLayout();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        public void SetContext(DBADashContext _context)
        {
            this.context = _context;
            RefreshData();
        }

        public void RefreshData()
        {
            MigratePerformanceSummaryView();
            savedViewMenuItem1.LoadItemsAndSelectDefault();
            dgv.ApplyTheme(DBADashUser.SelectedTheme);
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgv.DataSource = null;
            var dt = GetPerformanceSummary();
            AddPerformanceCounters(ref dt);
            dgv.AutoGenerateColumns = false;
            GenerateHistogram(ref dt);
            dgv.DataSource ??= new DataView(dt);
            dgv.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
            dgv.AutoResizeColumnsWithMaxColumnWidth();
            dgv.Columns["colCPUHistogram"]!.Width = 200;
            if (selectedView != null)
            {
                LoadPersistedColumnLayout(selectedView.ColumnLayout);
            }
        }

        private void AddPerformanceCounterColsToGrid()
        {
            List<string> pcColNames = new();
            foreach (var ctr in SelectedPerformanceCounters.Values)
            {
                foreach (var agg in ctr.GetAggColumns())
                {
                    var name = agg + "_" + ctr.CounterID;
                    pcColNames.Add(name);
                    if (!dgv.Columns.Contains(name))
                    {
                        dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = name, DataPropertyName = name, HeaderText = agg + " " + ctr.ToString().Replace("\\", " \\ "), Tag = "PC", Width = 70, DefaultCellStyle = Common.DataGridViewNumericCellStyle });
                    }
                }
            }
            var colsToRemove = dgv.Columns.Cast<DataGridViewColumn>().Where(col => (string)col.Tag == "PC" && !pcColNames.Contains(col.Name)).ToList();
            foreach (var col in colsToRemove)
            {
                dgv.Columns.Remove(col);
            }
        }

        private void AddPerformanceCounterColsToTable(ref DataTable dt)
        {
            foreach (var name in from ctr in SelectedPerformanceCounters.Values from agg in ctr.GetAggColumns() select agg + "_" + ctr.CounterID)
            {
                dt.Columns.Add(name, typeof(double));
                dt.Columns.Add(name + "Status", typeof(int));
            }
        }

        private void AddPerformanceCounters(ref DataTable dt)
        {
            AddPerformanceCounterColsToGrid();
            AddPerformanceCounterColsToTable(ref dt);
            if (SelectedPerformanceCounters.Count <= 0) return;
            var pcDT = GetPerformanceCounters();
            DataRow mainRow = null;
            var instanceIdMainRow = -1;
            foreach (DataRow r in pcDT.Rows)
            {
                var instanceID = (int)r["InstanceID"];
                var CounterID = (int)r["CounterID"];
                var cntr = SelectedPerformanceCounters[CounterID];
                if (instanceID != instanceIdMainRow)
                {
                    mainRow = dt.Select("InstanceId=" + (int)r["InstanceID"]).FirstOrDefault();
                    instanceIdMainRow = instanceID;
                }
                if (mainRow != null)
                {
                    if (cntr.Avg)
                    {
                        mainRow[("Avg_" + (int)r["CounterID"])] = r["AvgValue"];
                        mainRow[("Avg_" + (int)r["CounterID"] + "Status")] = r["AvgValueStatus"];
                    }
                    if (cntr.Max)
                    {
                        mainRow[("Max_" + (int)r["CounterID"])] = r["MaxValue"];
                        mainRow[("Max_" + (int)r["CounterID"] + "Status")] = r["MaxValueStatus"];
                    }
                    if (cntr.Total)
                    {
                        mainRow[("Total_" + (int)r["CounterID"])] = r["TotalValue"];
                    }
                    if (cntr.Current)
                    {
                        mainRow[("Current_" + (int)r["CounterID"])] = r["CurrentValue"];
                        mainRow[("Current_" + (int)r["CounterID"] + "Status")] = r["CurrentValueStatus"];
                    }
                    if (cntr.Min)
                    {
                        mainRow[("Min_" + (int)r["CounterID"])] = r["MinValue"];
                        mainRow[("Min_" + (int)r["CounterID"] + "Status")] = r["MinValueStatus"];
                    }
                    if (cntr.SampleCount)
                    {
                        mainRow[("SampleCount_" + (int)r["CounterID"])] = r["SampleCount"];
                    }
                }
            }
        }

        private DataTable GetPerformanceCounters()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();

            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", context.InstanceIDs));

            var counters = string.Join(",", SelectedPerformanceCounters.Values.Select(pc => pc.CounterID));
            cmd.Parameters.AddWithValue("Counters", counters);
            cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            cmd.Parameters.AddWithValue("ShowHidden", context.InstanceIDs.Count == 1 || Common.ShowHidden);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        private DataTable GetPerformanceSummary()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.PerformanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();

            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", context.InstanceIDs));

            cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            cmd.Parameters.AddWithValue("ShowHidden", context.InstanceIDs.Count == 1 || Common.ShowHidden);
            DataTable dt = new();
            var pkCols = new DataColumn[1];
            pkCols[0] = dt.Columns.Add("InstanceID", typeof(int));
            dt.PrimaryKey = pkCols;
            da.Fill(dt);
            return dt;
        }

        private void GenerateHistogram(ref DataTable dt)
        {
            if (dt.Rows.Count > 0 && dgv.Columns["colCPUHistogram"].Visible && (!dt.Columns.Contains("CPUHistogram")))
            {
                dgv.DataSource = null;
                dt.Columns.Add("CPUHistogram", typeof(Bitmap));
                dt.Columns.Add("CPUHistogramTooltip", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    var hist = new List<double>();
                    if (row["CPU10"] != DBNull.Value)
                    {
                        StringBuilder sbToolTip = new();
                        for (var i = 10; i <= 100; i += 10)
                        {
                            var v = Convert.ToDouble(row["CPU" + i]);
                            hist.Add(v);
                            sbToolTip.AppendLine((i - 10) + " to " + i + "% | " + v.ToString("N0"));
                        }
                        row["CPUHistogram"] = Histogram.GetHistogram(hist, 200, 100, true);
                        row["CPUHistogramToolTip"] = sbToolTip.ToString();
                    }
                    else
                    {
                        row["CPUHistogram"] = new Bitmap(1, 1);
                    }
                }
                dgv.DataSource = new DataView(dt);
            }
        }

        private void PerformanceSummary_Load(object sender, EventArgs e)
        {
            dgv.ApplyTheme();
            AddHistCols(dgv, "col");
        }

        /// <summary>
        /// Migrate legacy saved view from Properties.Settings.Default to database
        /// </summary>
        private static void MigratePerformanceSummaryView()
        {
            if (!Properties.Settings.Default.PerformanceSummaryMigrated)
            {
                try
                {
                    var viewName = "Default";
                    if (PerformanceSummarySavedView.GetSavedViews(DBADashUser.UserID).ContainsKey("Default")) // Check if we already have a view called Default and ensure we give it a unique name
                    {
                        viewName += Guid.NewGuid().ToString();
                    }
                    PerformanceSummarySavedView view = new() { Name = viewName };
                    var save = false;
                    var jsonCounters = Properties.Settings.Default.PerformanceSummaryPerformanceCounters;
                    if (!string.IsNullOrEmpty(jsonCounters))
                    {
                        view.SelectedPerformanceCounters = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Counter>>(jsonCounters);
                        save = true;
                    }
                    var jsonLayout = Properties.Settings.Default.PerformanceSummaryCols;
                    if (!string.IsNullOrEmpty(jsonLayout))
                    {
                        view.ColumnLayout = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KeyValuePair<string, PersistedColumnLayout>>>(jsonLayout);
                        save = true;
                    }
                    if (save)
                    {
                        view.Save();
                    }
                    Properties.Settings.Default.PerformanceSummaryCols = string.Empty;
                    Properties.Settings.Default.PerformanceSummaryPerformanceCounters = string.Empty;
                    Properties.Settings.Default.PerformanceSummaryMigrated = true;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error migrating saved view");
                    Properties.Settings.Default.PerformanceSummaryMigrated = true;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private static void AddHistCols(DataGridView dgv, string prefix)
        {
            var histogram = "CPU";

            for (var i = 10; i <= 100; i += 10)
            {
                var col = new DataGridViewTextBoxColumn()
                {
                    Name = prefix + histogram + "Histogram_" + i,
                    DataPropertyName = histogram + i,
                    Visible = false,
                    HeaderText = histogram + " Histogram " + (i - 10) + " to " + i + "%"
                };
                dgv.Columns.Add(col);
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var histogram = ((DataView)dgv.DataSource).Table.Columns.Contains("CPUHistogram");
            var pcCols = dgv.Columns.Cast<DataGridViewColumn>().Where(col => Convert.ToString(col.Tag) == "PC"
                                                                && (col.DataPropertyName.StartsWith("Avg") || col.DataPropertyName.StartsWith("Max") || col.DataPropertyName.StartsWith("Min") || col.DataPropertyName.StartsWith("Current"))
                                                                )
                                                                .Select(col => col.DataPropertyName)
                                                                .ToList();
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                var pAvgCPU = (CustomProgressControl.DataGridViewProgressBarCell)r.Cells["AvgCPU"];
                var pMaxCPU = (CustomProgressControl.DataGridViewProgressBarCell)r.Cells["MaxCPU"];
                var avgCPUstatus = (DBADashStatus.DBADashStatusEnum)row["AvgCPUStatus"];
                var maxCPUstatus = (DBADashStatus.DBADashStatusEnum)row["MaxCPUStatus"];

                DBADashStatus.SetProgressBarColor(avgCPUstatus, ref pAvgCPU);
                DBADashStatus.SetProgressBarColor(maxCPUstatus, ref pMaxCPU);

                foreach (var pcCol in pcCols)
                {
                    var status = row[pcCol + "Status"];
                    if (status != DBNull.Value)
                    {
                        r.Cells[pcCol].SetStatusColor((DBADashStatus.DBADashStatusEnum)Convert.ToInt32(status));
                    }
                    else
                    {
                        r.Cells[pcCol].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                    }
                }

                r.Cells["ReadLatency"].SetStatusColor((DBADashStatus.DBADashStatusEnum)row["ReadLatencyStatus"]);
                r.Cells["WriteLatency"].SetStatusColor((DBADashStatus.DBADashStatusEnum)row["WriteLatencyStatus"]);
                r.Cells["CriticalWaitMsPerSec"].SetStatusColor((DBADashStatus.DBADashStatusEnum)row["CriticalWaitStatus"]);
                if (histogram)
                {
                    r.Height = 100;
                    r.Cells["colCPUHistogram"].ToolTipText = row["CPUHistogramTooltip"] == DBNull.Value ? "" : (string)row["CPUHistogramTooltip"];
                }
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            var cpuHistVisible = colCPUHistogram.Visible;
            if (cpuHistVisible)
            {
                for (var i = 10; i <= 100; i += 10)
                {
                    dgv.Columns["colCPUHistogram_" + i].Visible = true;
                }
            }
            colCPUHistogram.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colCPUHistogram.Visible = cpuHistVisible;
            if (cpuHistVisible)
            {
                for (var i = 10; i <= 100; i += 10)
                {
                    dgv.Columns["colCPUHistogram_" + i].Visible = false;
                }
            }
        }

        public event EventHandler<InstanceSelectedEventArgs> Instance_Selected;

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (int)row["InstanceID"], Tab = "tabPerformance" });
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void LoadPersistedColumnLayout(List<KeyValuePair<string, PersistedColumnLayout>> savedCols)
        {
            if (savedCols == null)
            {
                return;
            }
            dgv.LoadColumnLayout(savedCols);

            if (dgv.DataSource != null)
            {
                var dt = ((DataView)dgv.DataSource).Table;
                GenerateHistogram(ref dt);
            }
            dgv.AutoResizeRows();
        }

        private void StandardColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv.PromptColumnSelection() != DialogResult.OK) return;
            var dt = ((DataView)dgv.DataSource).Table;
            GenerateHistogram(ref dt);
            DeSelectView();
        }

        private void PerformanceCounterColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new SelectPerformanceCounters
            {
                SelectedCounters = SelectedPerformanceCounters
            };
            frm.ApplyTheme();
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                SelectedPerformanceCounters = frm.SelectedCounters;
                DeSelectView();
                RefreshData();
            }
        }

        private void DeSelectView()
        {
            selectedView = null;
            tsDeleteView.Visible = false;
            savedViewMenuItem1.ClearSelectedItem();
        }

        private void TsSave_Click(object sender, EventArgs e)
        {
            using (SaveViewPrompt frm = new())
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    var name = frm.ViewName;

                    if ((savedViewMenuItem1.ContainsUserView(name) && !frm.IsGlobal) || (savedViewMenuItem1.ContainsGlobalView(name) && frm.IsGlobal))
                    {
                        if (MessageBox.Show("Replace existing view : " + name, "Replace View", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                    var savedView = new PerformanceSummarySavedView
                    {
                        Name = name,
                        SelectedPerformanceCounters = SelectedPerformanceCounters,
                        UserID = frm.IsGlobal ? DBADashUser.SystemUserID : DBADashUser.UserID,
                        ColumnLayout = dgv.GetColumnLayout()
                    };

                    savedView.Save();
                    savedViewMenuItem1.RefreshItems();
                    savedViewMenuItem1.SelectItem(name, false);
                    tsDeleteView.Visible = true;
                }
            }
        }

        private void SavedViewSelected(object sender, SavedViewSelectedEventArgs e)
        {
            selectedView = null;
            if (e.SerializedObject != string.Empty)
            {
                try
                {
                    selectedView = PerformanceSummarySavedView.Deserialize(e.SerializedObject);
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error loading the saved view");
                }
            }
            if (selectedView == null)
            {
                SelectedPerformanceCounters.Clear();
                AddPerformanceCounterColsToGrid();
                LoadPersistedColumnLayout(standardLayout);
                tsDeleteView.Visible = e.SerializedObject != string.Empty && (!e.IsGlobal || DBADashUser.HasManageGlobalViews); // Don't show for "None" but allow user to delete a view that failed to deserialize
            }
            else
            {
                SelectedPerformanceCounters = selectedView.SelectedPerformanceCounters;
                LoadPersistedColumnLayout(selectedView.ColumnLayout);
                tsDeleteView.Visible = !e.IsGlobal || DBADashUser.HasManageGlobalViews;
            }
            if (context != null)
            {
                RefreshData();
            }
        }

        private void TsDeleteView_Click(object sender, EventArgs e)
        {
            if (savedViewMenuItem1.SelectedSavedViewIsGlobal && !DBADashUser.HasManageGlobalViews) return;
            if (MessageBox.Show("Delete " + savedViewMenuItem1.SelectedSavedView, "Delete View", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var view = new PerformanceSummarySavedView() { Name = savedViewMenuItem1.SelectedSavedView, UserID = savedViewMenuItem1.SelectedSavedViewIsGlobal ? DBADashUser.SystemUserID : DBADashUser.UserID };
                view.Delete();
                savedViewMenuItem1.RefreshItems();
                tsDeleteView.Visible = false;
            }
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            foreach (Control control in Controls)
            {
                control.ApplyTheme(theme);
            }
        }
    }
}