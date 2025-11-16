using DBADashGUI.CustomReports;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobInfo : UserControl, ISetContext, IRefreshData
    {
        public JobInfo()
        {
            InitializeComponent();
            AddColumns();
        }

        public void RefreshData()
        {
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string CustomTitle { get; set; }

        public DBADashDataGridView InfoGrid => dgvInfo;
        public DBADashDataGridView ScheduleGrid => dgvSchedules;

        public DBADashDataGridView StepsGrid => dgvSteps;

        public void AddColumns()
        {
            dgvSteps.AutoGenerateColumns = false;
            dgvSteps.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Step ID", DataPropertyName = "StepId", Name = "colStepId" },
                new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "StepName", Name = "colStepName" },
                new DataGridViewTextBoxColumn { HeaderText = "Sub System", DataPropertyName = "SubSystem", Name = "colSubSystem" },
                new DataGridViewLinkColumn() { HeaderText = "Command", DataPropertyName = "Command", Name = "colCommand" },
                new DataGridViewTextBoxColumn() { HeaderText = "Database", DataPropertyName = "DatabaseName" },
                new DataGridViewTextBoxColumn
                { HeaderText = "On Success", DataPropertyName = "OnSuccessActionDescription" },
                new DataGridViewTextBoxColumn
                { HeaderText = "On Failure", DataPropertyName = "OnFailActionDescription" },
                new DataGridViewTextBoxColumn { HeaderText = "Retry Attempts", DataPropertyName = "RetryAttempts" },
                new DataGridViewTextBoxColumn { HeaderText = "Retry Interval", DataPropertyName = "RetryInterval" }
            );
        }

        public void SetContext(DBADashContext _context)
        {
            try
            {
                var info = SqlJobInfo.GetJobInfo(_context);
                LoadJobInfo(info);
            }
            catch (Exception ex)
            {
                var errorInfo = new SqlJobInfo
                {
                    JobName = "Error loading job info for " + _context.JobID.ToString(),
                    Description = ex.ToString()
                };
                LoadJobInfo(errorInfo);
            }
        }

        public void LoadJobInfo(SqlJobInfo info)
        {
            dgvSteps.DataSource = info.Steps;
            dgvSteps.AutoResizeColumnsWithMaxColumnWidth();
            lblJobName.Text = !string.IsNullOrEmpty(CustomTitle) ? CustomTitle : info.JobName;
            var dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Name", typeof(string)),
                new DataColumn("Value", typeof(string))
            });

            dt.Rows.Add("Category", info.Category);
            dt.Rows.Add("Description", info.Description);
            dt.Rows.Add("Enabled", info.Enabled ? "Yes" : "No");
            dt.Rows.Add("Owner", info.Owner);
            dt.Rows.Add("Write to EventLog", info.NotifyLevelEventLogDescription);
            dt.Rows.Add("Email Notification",
                string.IsNullOrEmpty(info.NotifyEmailOperator)
                    ? info.NotifyLevelEmailDescription
                    : $"Email {info.NotifyEmailOperator} {info.NotifyLevelEmailDescription}");
            dt.Rows.Add("Page Notification",
                string.IsNullOrEmpty(info.NotifyPageOperator)
                    ? info.NotifyLevelPageDescription
                    : $"Page {info.NotifyPageOperator} {info.NotifyLevelPageDescription}");
            dt.Rows.Add("Delete Job", info.DeleteLevelDescription);

            dgvInfo.DataSource = new DataView(dt);
            dgvInfo.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgvInfo.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);

            dgvSchedules.DataSource = info.GetSchedulesDataTable();
            dgvSchedules.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            ResizePanels();
        }

        private void ResizePanels()
        {
            const float MinPanelPercentage = 15.0f;
            const float MaxPanelPercentage = 50.0f;
            const int headerRow = 2;
            var totalRowsWithHeaders = (dgvInfo.RowCount + headerRow) +
                                       (dgvSchedules.RowCount + headerRow) +
                                       (dgvSteps.RowCount + headerRow);

            // Calculate percentages based on row counts
            var infoPercentage = CalculatePanelPercentage(dgvInfo.RowCount + 2, totalRowsWithHeaders,
                MinPanelPercentage, MaxPanelPercentage);

            var stepsPercentage = CalculatePanelPercentage(dgvSteps.RowCount + 2, totalRowsWithHeaders,
                MinPanelPercentage, MaxPanelPercentage);

            // Use remaining % for last panel
            var schedulesPercentage = 100.0f - (infoPercentage + stepsPercentage);

            // Apply the calculated percentages to the table layout
            ApplyRowStyle(1, infoPercentage);
            ApplyRowStyle(2, stepsPercentage);
            ApplyRowStyle(3, schedulesPercentage);
        }

        private static float CalculatePanelPercentage(int panelRows, int totalRows, float minPercentage, float maxPercentage)
        {
            var rawPercentage = ((float)panelRows / totalRows) * 100.0f;
            return Math.Clamp(rawPercentage, minPercentage, maxPercentage);
        }

        private void ApplyRowStyle(int rowIndex, float percentage)
        {
            tableLayoutPanel1.RowStyles[rowIndex] = new RowStyle(SizeType.Percent, percentage);
        }

        private void Steps_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvSteps.Columns[e.ColumnIndex].Name == "colCommand")
            {
                var step = (SqlJobStep)dgvSteps.Rows[e.RowIndex].DataBoundItem;

                Common.ShowCodeViewer(step.Command, $"{step.StepName} [{step.StepId}]", step.CodeEditorMode);
            }
        }
    }
}