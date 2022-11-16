using DBADashGUI.Performance;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobStats : UserControl, ISetContext, IRefreshData, INavigation
    {
        public JobStats()
        {
            InitializeComponent();
        }

        private int InstanceID { get; set; }
        private Guid JobID { get; set; }
        private int StepID { get; set; } = 0;

        public bool CanNavigateBack => StepID != context.JobStepID || JobID != context.JobID;

        private int dateGrouping = 60;

        private Guid selectedJobID;
        private int selectedStepID = -1;
        private DBADashContext context;

        public DataTable GetJobStats()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobStats_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("JobID", selectedJobID == Guid.Empty ? JobID : selectedJobID);
                int stepid = selectedStepID >= 0 ? selectedStepID : StepID;
                cmd.Parameters.AddWithValue("StepID", stepid);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                var pDateGrouping = cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                pDateGrouping.Direction = ParameterDirection.InputOutput;
                DataTable dt = new();
                da.Fill(dt);
                tsDateGroup.Text = Common.DateGroups[(int)pDateGrouping.Value];
                return dt;
            }
        }

        public DataTable GetJobStatsSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobStatsSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("JobID", JobID);
                if(JobID == Guid.Empty)
                {
                    cmd.Parameters.AddWithValue("StepID", 0);
                }
                else if (StepID >= 0)
                {
                    cmd.Parameters.AddWithValue("StepID", StepID);
                }
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private readonly Dictionary<string, ColumnMetaData> columns = new() {
              {"SucceededCount", new ColumnMetaData{Alias="Succeeded Count",isVisible=true,axis=1 } },
                {"FailedCount", new ColumnMetaData{Alias="Failed Count",isVisible=true, axis=1  } },
                {"RetryCount", new ColumnMetaData{Alias="Retry Count",isVisible=false,axis=1 } },
                {"AvgDurationSec", new ColumnMetaData{Alias="Avg Duration",isVisible=true } },
                {"MaxDurationSec", new ColumnMetaData{Alias="Max Duration",isVisible=false } },
                {"MinDurationSec", new ColumnMetaData{Alias="Min Duration",isVisible=false } },
                {"TotalDurationSec", new ColumnMetaData{Alias="Total Duration",isVisible=false } }
        };

        public void SetContext(DBADashContext context)
        {
            this.context = context;
            InstanceID = context.InstanceID;
            JobID = context.JobID;
            StepID = context.JobStepID;
            selectedStepID = context.JobStepID >= 0 ? context.JobStepID : 0;
            selectedJobID = context.JobID;
            RefreshData();
        }

        public void RefreshData()
        {
            tsBack.Enabled = CanNavigateBack;
            tsJob.Visible = false;
            dateGrouping = Common.DateGrouping(DateRange.DurationMins, 200);
            tsDateGroup.Text = Common.DateGroupString(dateGrouping);

            dgv.Columns["colRetry"].Visible = JobID != Guid.Empty;

            RefreshSummary();
            RefreshChart();
        }

        private void RefreshSummary()
        {
            var dt = GetJobStatsSummary();
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void RefreshChart()
        {
            if (JobID == Guid.Empty && selectedJobID == Guid.Empty)
            {
                splitContainer1.Panel1Collapsed = true;
                tsDateGroup.Visible = false;
                tsMeasures.Visible = false;
                return;
            }
            tsDateGroup.Visible = true;
            tsMeasures.Visible = true;
            columns["RetryCount"].isVisible = columns["RetryCount"].isVisible && (StepID > 0 || selectedStepID > 0);
            tsMeasures.DropDownItems["RetryCount"].Enabled = StepID > 0 || selectedStepID > 0;

            splitContainer1.Panel1Collapsed = false;
            var dt = GetJobStats();
            var rowCount = dt.Rows.Count;
            if (rowCount > 2000)
            {
                MessageBox.Show(string.Format("Too many data points({0}) to display in chart", rowCount), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            chart1.Series.Clear();
            chart1.AxisX.Clear();
            chart1.AxisY.Clear();
            chart1.AxisY.Add(new Axis
            {
                Title = "Duration",
                LabelFormatter = val => TimeSpan.FromSeconds(val).ToString(),
                MinValue = 0
            });
            chart1.AxisY.Add(new Axis
            {
                Title = "Count",
                LabelFormatter = val => val.ToString("N0"),
                Position = AxisPosition.RightTop,
                MinValue = 0
            });
            chart1.LegendLocation = LegendLocation.Bottom;
            chart1.AddDataTable(dt, columns, "DateGroup", true);
        }

        private void JobStats_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGroup, TsDateGroup_Click);
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colJob", HeaderText = "Job", DataPropertyName = "JobName", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Step", DataPropertyName = "JobStep" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Step ID", DataPropertyName = "step_id" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Failed", DataPropertyName = "FailedCount" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Succeeded", DataPropertyName = "SucceededCount" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colRetry", HeaderText = "Retry", DataPropertyName = "RetryCount" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Avg Duration (sec)", DataPropertyName = "AvgDurationSec" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Duration (sec)", DataPropertyName = "MaxDurationSec" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Min Duration (sec)", DataPropertyName = "MinDurationSec" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Total Duration (sec)", DataPropertyName = "TotalDurationSec" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Avg Duration", DataPropertyName = "AvgDuration" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Duration", DataPropertyName = "MaxDuration" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Min Duration", DataPropertyName = "MinDuration" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Total Duration", DataPropertyName = "TotalDuration" });
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colView", HeaderText = "View", Text = "View", UseColumnTextForLinkValue = true, LinkColor = DashColors.LinkColor });
            foreach (var c in columns)
            {
                var dd = new ToolStripMenuItem(c.Value.Alias)
                {
                    Name = (string)c.Key,
                    CheckOnClick = true
                };
                dd.Checked = dd.Enabled && c.Value.isVisible;
                dd.Click += MeasureDropDown_Click;
                tsMeasures.DropDownItems.Add(dd);
            }
        }

        private void MeasureDropDown_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            columns[ts.Name].isVisible = ts.Checked;
            RefreshChart();
        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = Common.DateGroupString(dateGrouping);
            RefreshChart();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshChart();
            RefreshSummary();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                selectedJobID = (Guid)row["job_id"];
                selectedStepID = (int)row["step_id"];
                if (e.ColumnIndex == dgv.Columns["colView"].Index)
                {
                    tsJob.Text = (string)row["JobName"] + "\\" + (string)row["JobStep"];
                    tsJob.Visible = true;
                    RefreshChart();
                }
                else if (e.RowIndex >= 0 && e.ColumnIndex == dgv.Columns["colJob"].Index)
                {
                    JobID = selectedJobID;
                    RefreshData();
                }
            }
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            _ = NavigateBack();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                if (StepID != context.JobStepID)
                {
                    StepID = context.JobStepID;
                }
                else if (JobID != context.JobID)
                {
                    JobID = context.JobID;
                }
                RefreshData();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
