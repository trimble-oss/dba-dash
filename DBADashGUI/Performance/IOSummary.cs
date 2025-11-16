using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class IOSummary : UserControl
    {
        public IOSummary()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        public enum IOSummaryGroupByOptions
        {
            Database,
            File,
            Drive,
            Filegroup
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? DatabaseID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime FromDate { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime ToDate { get; set; }

        private readonly IOSummaryGroupByOptions defaultGroupBy = IOSummaryGroupByOptions.Database;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IOSummaryGroupByOptions GroupBy
        {
            get
            {
                foreach (ToolStripMenuItem itm in tsGroupBy.DropDownItems)
                {
                    if (itm.Checked)
                    {
                        return (IOSummaryGroupByOptions)Enum.Parse(typeof(IOSummaryGroupByOptions), itm.Text!);
                    }
                }
                return defaultGroupBy;
            }
            set
            {
                AddGroupByOptions();
                foreach (ToolStripMenuItem itm in tsGroupBy.DropDownItems)
                {
                    itm.Checked = itm.Text == value.ToString();
                }
            }
        }

        public DataTable GetIOSummary(out string InstanceName, out string DatabaseName)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.IOSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("FromDate", FromDate);
            cmd.Parameters.AddWithValue("ToDate", ToDate);
            cmd.Parameters.AddWithValue("GroupBy", GroupBy.ToString());
            var pDatabaseName = cmd.Parameters.Add(new SqlParameter() { ParameterName = "DatabaseName", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.NVarChar, Size = 128 });
            var pInstance = cmd.Parameters.Add(new SqlParameter() { ParameterName = "Instance", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.NVarChar, Size = 128 });
            cmd.Parameters.AddWithNullableValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            da.Fill(dt);
            DatabaseName = Convert.ToString(pDatabaseName.Value);
            InstanceName = Convert.ToString(pInstance.Value);
            return dt;
        }

        private void AddGroupByOptions()
        {
            if (tsGroupBy.DropDownItems.Count > 0) return;
            foreach (IOSummaryGroupByOptions val in Enum.GetValues(typeof(IOSummaryGroupByOptions)))
            {
                ToolStripMenuItem item = new()
                {
                    Text = val.ToString(),
                    Checked = val == defaultGroupBy
                };

                item.Click += TsGroupBy_Click;
                tsGroupBy.DropDownItems.Add(item);
            }
        }

        private void AddColsToDGV()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { Name = "colGroup", HeaderText = "Group", DataPropertyName = "Grp" },
                new DataGridViewTextBoxColumn() { HeaderText = "IOPs", DataPropertyName = "IOPs", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits },
                new DataGridViewTextBoxColumn() { HeaderText = "Read IOPs", DataPropertyName = "ReadIOPs", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits },
                new DataGridViewTextBoxColumn() { HeaderText = "Write IOPs", DataPropertyName = "WriteIOPs", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits },
                new DataGridViewTextBoxColumn() { HeaderText = "MB/sec", DataPropertyName = "MBsec", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Read MB/sec", DataPropertyName = "ReadMBsec", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Write MB/sec", DataPropertyName = "WriteMBsec", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Latency", DataPropertyName = "Latency", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Read Latency", DataPropertyName = "ReadLatency", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Write Latency", DataPropertyName = "WriteLatency", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max IOPs", DataPropertyName = "MaxIOPs", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Read IOPs", DataPropertyName = "MaxReadIOPs", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Write IOPs", DataPropertyName = "MaxWriteIOPs", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits },
                new DataGridViewTextBoxColumn() { HeaderText = "Max MB/sec", DataPropertyName = "MaxMBsec", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Read MB/sec", DataPropertyName = "MaxReadMBsec", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Write MB/sec", DataPropertyName = "MaxWriteMBsec", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Latency", DataPropertyName = "MaxLatency", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Read Latency", DataPropertyName = "MaxReadLatency", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Write Latency", DataPropertyName = "MaxWriteLatency", DefaultCellStyle = Common.DataGridViewNumericCellStyle }
                );
        }

        private void IOSummary_Load(object sender, EventArgs e)
        {
            AddGroupByOptions();
            AddColsToDGV();
        }

        public void RefreshData()
        {
            var dt = GetIOSummary(out var instanceName, out var databaseName);
            lblHeader.Text = instanceName + (string.IsNullOrEmpty(databaseName) ? "" : " | " + databaseName);
            dgv.DataSource = new DataView(dt);
            dgv.Columns["colGroup"].HeaderText = GroupBy.ToString();
            dgv.Sort(dgv.Columns[1], ListSortDirection.Descending);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            lblDateRange.Text = "Date Range : " + FromDate.ToAppTimeZone().ToString(CultureInfo.CurrentCulture) + " to " + ToDate.ToAppTimeZone().ToString(CultureInfo.CurrentCulture);
        }

        private void TsGroupBy_Click(object sender, EventArgs e)
        {
            GroupBy = (IOSummaryGroupByOptions)Enum.Parse(typeof(IOSummaryGroupByOptions), ((ToolStripMenuItem)sender).Text ?? IOSummaryGroupByOptions.Drive.ToString());
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }
    }
}