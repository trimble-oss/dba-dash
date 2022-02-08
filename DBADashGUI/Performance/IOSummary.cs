using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class IOSummary : UserControl
    {
        public IOSummary()
        {
            InitializeComponent();
        }

        public enum IOSummaryGroupByOptions
        {
            Database,
            File,
            Drive,
            Filegroup
        }

        public int InstanceID { get; set; }
        public int? DatabaseID { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        private IOSummaryGroupByOptions defaultGroupBy =  IOSummaryGroupByOptions.Database;

        public IOSummaryGroupByOptions GroupBy
        {
            get
            {
                foreach(ToolStripMenuItem itm in tsGroupBy.DropDownItems)
                {
                    if (itm.Checked)
                    {
                        return (IOSummaryGroupByOptions)Enum.Parse(typeof(IOSummaryGroupByOptions),itm.Text);
                    }
                }
                return defaultGroupBy;
            }
            set
            {
                foreach (ToolStripMenuItem itm in tsGroupBy.DropDownItems)
                {
                    itm.Checked= itm.Text == value.ToString();
                }
            }
        }

        public DataTable GetIOSummary(out string InstanceName, out string DatabaseName)
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using( var cmd = new SqlCommand("dbo.IOSummary_Get",cn) {  CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
            using(var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", FromDate);
                cmd.Parameters.AddWithValue("ToDate", ToDate);
                cmd.Parameters.AddWithValue("GroupBy", GroupBy.ToString());
                var pDatabaseName = cmd.Parameters.Add(new SqlParameter() { ParameterName = "DatabaseName", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.NVarChar, Size = 128 });
                var pInstance = cmd.Parameters.Add(new SqlParameter() { ParameterName = "Instance",  Direction = ParameterDirection.Output, SqlDbType = SqlDbType.NVarChar, Size = 128 });
                if (DatabaseID != null)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                da.Fill(dt);
                DatabaseName = Convert.ToString(pDatabaseName.Value);
                InstanceName = Convert.ToString(pInstance.Value);
                return dt;
            }
        }

        private void addGroupByOptions()
        {
            foreach (IOSummaryGroupByOptions val in Enum.GetValues(typeof(IOSummaryGroupByOptions)))
            {
                ToolStripMenuItem item = new ToolStripMenuItem()
                {
                    Text = val.ToString(),
                    Checked = val == defaultGroupBy
                };

                item.Click += tsGroupBy_Click;
                tsGroupBy.DropDownItems.Add(item);
            }
        }

        private void addColsToDGV()
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
            addGroupByOptions();
            addColsToDGV();
            
        }

        public void RefreshData()
        {
            refreshData();
        }

        private void refreshData()
        {
            string instanceName,databaseName;
            var dt = GetIOSummary(out instanceName,out databaseName);
            lblHeader.Text = instanceName + (string.IsNullOrEmpty(databaseName) ? "" : " | " + databaseName);
            dgv.DataSource = dt;
            dgv.Columns["colGroup"].HeaderText = GroupBy.ToString();
            dgv.Sort(dgv.Columns[1], ListSortDirection.Descending);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            lblDateRange.Text = "Date Range : " + FromDate.ToLocalTime().ToString() + " to " + ToDate.ToLocalTime().ToString();
            
        }

        private void tsGroupBy_Click(object sender, EventArgs e)
        {
            GroupBy = (IOSummaryGroupByOptions)Enum.Parse(typeof(IOSummaryGroupByOptions),((ToolStripMenuItem)sender).Text);
            refreshData();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }
    }
}
