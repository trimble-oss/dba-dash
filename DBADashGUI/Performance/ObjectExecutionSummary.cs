using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;
using Humanizer;

namespace DBADashGUI.Performance
{
    public partial class ObjectExecutionSummary : UserControl, ISetContext, IRefreshData
    {
        public ObjectExecutionSummary()
        {
            InitializeComponent();
        }

        private string Instance { get; set; }
        private int InstanceID { get; set; }
        private int DatabaseID { get; set; }
        private long ObjectID { get; set; }

        public string Types
        {
            get
            {
                string types = "";
                foreach (ToolStripMenuItem mnu in tsType.DropDownItems)
                {
                    if (mnu.Checked)
                    {
                        types += (types.Length > 0 ? "," : "") + mnu.Tag;
                    }
                }
                return types;
            }
            set
            {
                var types = value.Split(',');
                foreach (ToolStripMenuItem mnu in tsType.DropDownItems)
                {
                    mnu.Checked = types.Contains(mnu.Tag);
                }
            }
        }

        private int CompareOffset=>int.Parse(SelectedCompareOffsetItem.Tag.ToString() ?? "-1");

        private DateTime _compareTo = DateTime.MinValue;
        private DateTime _compareFrom = DateTime.MinValue;
        private DataTable dt;

        private ToolStripMenuItem SelectedCompareOffsetItem => tsCompare.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(mnu => mnu.Checked,tsNoCompare);

        private DateTime CompareTo
        {
            get
            {
                if (tsPreviousPeriod.Checked)
                {
                    return DateRange.FromUTC;
                }
                return CompareOffset > 0
                    ? DateRange.ToUTC.AddMinutes(-CompareOffset)
                    : _compareTo;
            }
        }

        private DateTime CompareFrom
        {
            get
            {
                if (tsPreviousPeriod.Checked)
                {
                    return DateRange.FromUTC.AddSeconds(-DateRange.TimeSpan.TotalSeconds);
                }
                return CompareOffset > 0 ? DateRange.FromUTC.AddMinutes(-CompareOffset) :
                    _compareFrom;
            }

        }

        private bool HasCompare => CompareFrom != DateTime.MinValue && CompareFrom != DateTime.MaxValue &&
                                   CompareTo != DateTime.MinValue && CompareTo != DateTime.MaxValue;



        private readonly List<DataGridViewColumn> StandardCols = new()
        {
            new DataGridViewTextBoxColumn() { Name = "DB", DataPropertyName = "DB",DisplayIndex=0 },
                                                                        new DataGridViewTextBoxColumn() { Name = "Schema", DataPropertyName = "SchemaName",DisplayIndex=1},
                                                                        new DataGridViewLinkColumn { Name = "Name", DataPropertyName = "ObjectName",DisplayIndex=2, SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                                                                        new DataGridViewTextBoxColumn() { Name = "Type", DataPropertyName = "TypeDescription",DisplayIndex=3}
        };

        private readonly List<DataGridViewColumn> Cols = new()
        { new DataGridViewTextBoxColumn()  { Name= "Total Duration (sec)",Visible=false, DataPropertyName = "total_duration_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.000" } },
                                                                        new DataGridViewTextBoxColumn()  { Name= "Total Duration (ms/sec)", DataPropertyName = "duration_ms_per_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.####" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Duration (sec)", DataPropertyName = "avg_duration_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.000" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Executions", Visible=false, DataPropertyName = "execution_count" },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Executions/min", DataPropertyName = "execs_per_min", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###"} },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Max Executions/min", DataPropertyName = "max_execs_per_min", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###"} },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total CPU (sec)",Visible=false, DataPropertyName = "total_cpu_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total CPU (ms/sec)", DataPropertyName = "cpu_ms_per_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg CPU (sec)", DataPropertyName = "avg_cpu_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total Physical Reads",Visible=false, DataPropertyName = "total_physical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Physical Reads", DataPropertyName = "avg_physical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total Logical Reads", DataPropertyName = "total_logical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Logical Reads",Visible=false, DataPropertyName = "avg_logical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total Writes", Visible=false,DataPropertyName = "total_writes", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Writes",Visible=false, DataPropertyName = "avg_writes", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Period Time (sec)",Visible=false, DataPropertyName = "period_time_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
        };

        private void TsColumn_Click(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(ToolStripMenuItem))
            {
                var mnu = (ToolStripMenuItem)sender;
                foreach (string name in new string[] { mnu.Text, "Compare " + mnu.Text })
                {
                    if (dgv.Columns.Contains(name))
                    {
                        dgv.Columns[name].Visible = mnu.Checked;
                    }
                }
            }
        }

        public List<DataGridViewColumn> Columns
        {
            get
            {
                var _cols = new List<DataGridViewColumn>();
                _cols.AddRange(StandardCols.ToArray());
                var displayIdx = _cols.Count;
                foreach (var col in Cols)
                {
                    col.DisplayIndex = displayIdx;
                    col.Visible = true;
                    _cols.Add(col);
                    displayIdx += 1;
                    if (CompareFrom > DateTime.MinValue)
                    {
                        var compareCol = new DataGridViewTextBoxColumn
                        {
                            Name = "Compare " + col.Name,
                            DataPropertyName = "compare_" + col.DataPropertyName,
                            DisplayIndex = displayIdx,
                            DefaultCellStyle = col.DefaultCellStyle.Clone()
                        };
                        compareCol.DefaultCellStyle.BackColor = Color.BlanchedAlmond;
                        _cols.Add(compareCol);
                        displayIdx += 1;
                        if (col.DataPropertyName is "avg_duration_sec" or "avg_cpu_sec")
                        {
                            var diffCol = new DataGridViewTextBoxColumn() { Name = "Diff " + col.Name.Replace("sec", "%"), DataPropertyName = "diff_" + col.DataPropertyName.Replace("_sec", "_pct"), DisplayIndex = displayIdx };
                            diffCol.DefaultCellStyle.Format = "P2";
                            diffCol.DefaultCellStyle.BackColor = Color.AliceBlue;
                            _cols.Add(diffCol);
                            displayIdx += 1;
                        }
                    }
                }

                return _cols;
            }
        }

        public void SetContext(DBADashContext context)
        {
            Instance = context.InstanceName;
            InstanceID = context.InstanceID;
            DatabaseID = context.DatabaseID;
            ObjectID = (context.Type is SQLTreeItem.TreeType.Database or SQLTreeItem.TreeType.AzureDatabase) ? -1 : context.ObjectID;
            RefreshData();
        }

        public void RefreshData()
        {
            var status = DateRange.FromUTC.ToAppTimeZone() + " - " + DateRange.ToUTC.ToAppTimeZone() + (HasCompare ? " comparing to " + CompareFrom.ToAppTimeZone() + " - " + CompareTo.ToAppTimeZone() : "");
            lblStatus.ForeColor = Color.Black;
            lblStatus.Text = "Refreshing Data...";
            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                if (objectExecutionLineChart1.InstanceID != InstanceID)
                {
                    splitContainer1.Panel1Collapsed = true;
                }
                dgv.DataSource = null;

                var dt = GetObjectExecutionStatsSummary();
                if (dt.Rows.Count == 1)
                {
                    RefreshChart((long)dt.Rows[0]["ObjectID"], (string)dt.Rows[0]["ObjectName"]);
                }


                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = false;
                tsCompare.Font = new Font(tsCompare.Font, HasCompare ? FontStyle.Bold : FontStyle.Regular);
                SetDataSourceWithFilter();
                dgv.Columns.AddRange(Columns.ToArray());
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dgv.ApplyTheme();
                if (splitContainer1.Panel1Collapsed == false)
                {
                    RefreshChart();
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                status = "Error: " + ex.Message;
            }
            finally{
                
                this.Cursor = Cursors.Default;
            }
            lblStatus.Text = status;
        }

        private DataTable GetObjectExecutionStatsSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.ObjectExecutionStatsSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();

                if (cmd.Parameters.AddIfGreaterThanZero("InstanceID", InstanceID) == null && cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", Instance) == null)
                {
                    throw new Exception("Instance not provided to Object Execution Summary");
                }

                cmd.Parameters.AddIfGreaterThanZero("ObjectID", ObjectID);
                cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);

                if (HasCompare)
                {
                    cmd.Parameters.AddWithValue("CompareFrom", CompareFrom);
                    cmd.Parameters.AddWithValue("CompareTo", CompareTo);
                }

                cmd.Parameters.AddStringIfNotNullOrEmpty("Types", Types);

                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }

                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
                dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void SetDataSourceWithFilter()
        {
            if (string.IsNullOrEmpty(txtSearch.Text.Trim()))
            {
                dgv.DataSource = new DataView(dt, null, "total_duration_sec DESC", DataViewRowState.CurrentRows);
                lblSearch.Font = new Font(lblSearch.Font, FontStyle.Regular);
            }
            else
            {
                lblSearch.Font = new Font(lblSearch.Font, FontStyle.Bold);
                try
                {
                    dgv.DataSource = new DataView(dt, string.Format("SchemaName LIKE '%{0}%' OR ObjectName LIKE '%{0}%'", txtSearch.Text.Replace("'", "''")), "total_duration_sec DESC", DataViewRowState.CurrentRows);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Filter Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgv.DataSource = new DataView(dt, null, "total_duration_sec DESC", DataViewRowState.CurrentRows);
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsSetOffset_Click(object sender, EventArgs e)
        {
            CheckCompareOffset((ToolStripMenuItem)sender);
            RefreshData();
        }

        private void CheckCompareOffset(ToolStripItem ts)
        {
            foreach (var itm in tsCompare.DropDownItems.OfType<ToolStripMenuItem>())
            {
                itm.Checked = itm == ts;
            }
            tsCompare.Text = "Compare To: " + ts.Text;
        }

        private void ObjectExecutionSummary_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = true;
        }

        private void TsCustomCompare_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker
            {
                FromDate = CompareFrom > DateTime.MinValue && CompareFrom < DateTime.MaxValue ? CompareFrom.ToAppTimeZone() : DateRange.FromUTC.ToAppTimeZone(),
                ToDate = CompareTo > DateTime.MinValue && CompareTo < DateTime.MaxValue ? CompareTo.ToAppTimeZone() : DateRange.ToUTC.ToAppTimeZone()
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                _compareFrom = frm.FromDate.AppTimeZoneToUtc();
                _compareTo = frm.ToDate.AppTimeZoneToUtc();
                CheckCompareOffset(tsCustomCompare);
                RefreshData();
            }
        }

        private void TsType_Click(object sender, EventArgs e)
        {
            int checkCount = 0;
            foreach (ToolStripMenuItem itm in tsType.DropDownItems)
            {
                itm.Font = new Font(itm.Font, itm.Checked ? FontStyle.Bold : FontStyle.Regular);
                if (itm.Checked)
                {
                    checkCount++;
                }
            }
            tsType.Font = new Font(tsType.Font, checkCount > 0 ? FontStyle.Bold : FontStyle.Regular);
            RefreshData();
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (dgv.Columns.Contains("Diff Avg Duration (%)"))
            {
                for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
                {
                    var r = dgv.Rows[idx];
                    var row = (DataRowView)r.DataBoundItem;
                    double diffAvgDurationPct = row["diff_avg_duration_pct"] == DBNull.Value ? 0d : Convert.ToDouble(row["diff_avg_duration_pct"]);
                    double diffAvgCPUPct = row["diff_avg_cpu_pct"] == DBNull.Value ? 0d : Convert.ToDouble(row["diff_avg_cpu_pct"]);
                    r.Cells["Diff Avg Duration (%)"].SetColor(DiffColorFromDouble(diffAvgDurationPct));
                    r.Cells["Diff Avg CPU (%)"].SetColor(DiffColorFromDouble(diffAvgCPUPct));
                }
            }
        }

        private static Color DiffColorFromDouble(double value)
        {
            return value switch
            {
                > 0.5 => DashColors.Red,
                > 0.3 => DashColors.RedLight,
                < -0.5 => DashColors.Success,
                < -0.3 => DashColors.GreenLight,
                _ => DashColors.BlueLight
            };
        }

        private void RefreshChart()
        {
            if (CompareFrom > DateTime.MinValue)
            {
                splitChart.Panel2Collapsed = false;
                compareObjectExecutionLineChart.FromDate = CompareFrom;
                compareObjectExecutionLineChart.ToDate = CompareTo;
                compareObjectExecutionLineChart.RefreshData();
            }
            else
            {
                splitChart.Panel2Collapsed = true;
            }
            objectExecutionLineChart1.FromDate = DateRange.FromUTC;
            objectExecutionLineChart1.ToDate = DateRange.ToUTC;
            objectExecutionLineChart1.RefreshData();
        }

        private void RefreshChart(long objectID, string title)
        {
            splitContainer1.Panel1Collapsed = false;

            compareObjectExecutionLineChart.InstanceID = InstanceID;
            compareObjectExecutionLineChart.Instance = Instance;
            compareObjectExecutionLineChart.ObjectID = objectID;
            compareObjectExecutionLineChart.Title = title + " (Compare)";

            objectExecutionLineChart1.InstanceID = InstanceID;
            objectExecutionLineChart1.Instance = Instance;
            objectExecutionLineChart1.ObjectID = objectID;
            objectExecutionLineChart1.Title = title;
            RefreshChart();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgv.Columns[e.ColumnIndex].Name == "Name")
                {
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    RefreshChart((long)row["ObjectID"], (string)row["ObjectName"]);
                }
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            tmrSearch.Enabled = true;
            tmrSearch.Stop();
            tmrSearch.Start();
        }

        private void TmrSearch_Tick(object sender, EventArgs e)
        {
            SetDataSourceWithFilter();
            tmrSearch.Enabled = false;
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            if (dgv.PromptColumnSelection() == DialogResult.OK)
            {
                dgv.AutoResizeColumns();
                dgv.AutoResizeRows();
            }
        }

    }
}