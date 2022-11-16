using DBADashGUI.DBFiles;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class SpaceTracking : UserControl, INavigation, ISetContext
    {
        public SpaceTracking()
        {
            InitializeComponent();
        }


        private List<Int32> InstanceIDs;
        private Int32 DatabaseID = -1;
        private string DBName = "";
        private string InstanceGroupName = "";
        public bool CanNavigateBack => tsBack.Enabled;

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            DatabaseID = context.DatabaseID;
            InstanceGroupName = context.InstanceName;
            DBName = context.DatabaseName;
            RefreshData();
        }

        public void RefreshData()
        {
            bool drillDownEnabled = DatabaseID > 0;
            tsBack.Enabled = false;
            DiableHyperLinks(drillDownEnabled);
            RefreshDataLocal();
        }

        private DataTable GetDBSpace()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBSpace_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("@DatabaseID", DatabaseID);
                }
                if (InstanceGroupName != null && InstanceGroupName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@InstanceGroupName", InstanceGroupName);
                }
                if (!string.IsNullOrEmpty(DBName))
                {
                    cmd.Parameters.AddWithValue("@DBName", DBName);
                }
                DataTable dt = new();
                da.Fill(dt);

                return dt;
            }
        }

        private void RefreshDataLocal()
        {
            tsContext.Text = InstanceGroupName + (String.IsNullOrEmpty(DBName) ? "" : " \\ " + DBName);
            var dt = GetDBSpace();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            pieChart1.Series.Clear();

            static string labelPoint(ChartPoint chartPoint) =>
            string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation);
            SeriesCollection sc = new();
            var other = (double)0;
            foreach (DataRow r in dt.Rows)
            {
                var pct = (double)(decimal)r["Pct"];
                var allocated = (double)(decimal)r["AllocatedGB"];
                if (pct > 0.02)
                {
                    var s = new PieSeries() { Title = (string)r["Grp"], Values = new ChartValues<double> { allocated }, LabelPoint = labelPoint, DataLabels = true, ToolTip = false };
                    sc.Add(s);
                }
                else
                {
                    other += allocated;
                }
            }
            if (other > 0)
            {
                var s = new PieSeries() { Title = "{Other}", Values = new ChartValues<double> { other }, LabelPoint = labelPoint, DataLabels = true, ToolTip = false };
                sc.Add(s);
            }

            pieChart1.Series = sc;
            pieChart1.LegendLocation = LegendLocation.Bottom;

        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;

                var selectedGroupValue = row["Grp"] == DBNull.Value ? "" : (string)row["Grp"];
                if (e.ColumnIndex == dgv.Columns["colName"].Index)
                {
                    if (InstanceIDs.Count > 1 && string.IsNullOrEmpty(InstanceGroupName))
                    {
                        InstanceGroupName = selectedGroupValue;
                    }
                    else if (DBName.Length == 0)
                    {
                        DBName = selectedGroupValue;
                        DiableHyperLinks(true);
                    }
                    else
                    {
                        DiableHyperLinks(true);
                        return;
                    }
                    tsBack.Enabled = true;
                    RefreshDataLocal();
                }
                else if (e.ColumnIndex == dgv.Columns["colHistory"].Index)
                {
                    var frm = new DBSpaceHistoryView
                    {
                        DatabaseID = DatabaseID,
                        InstanceGroupName = InstanceGroupName,
                        DBName = DBName,
                        NumberFormat = NumberFormat,
                        Unit = Unit
                    };
                    if (InstanceIDs.Count > 1 && string.IsNullOrEmpty(InstanceGroupName))
                    {
                        frm.InstanceGroupName = selectedGroupValue;
                    }
                    else if (DBName.Length == 0)
                    {
                        frm.DBName = selectedGroupValue;
                    }
                    else
                    {
                        frm.FileName = selectedGroupValue;
                    }
                    if (frm.DatabaseID < 1)
                    {
                        frm.DatabaseID = CommonData.GetDatabaseID(frm.InstanceGroupName, frm.DBName);
                    }
                    frm.Show();
                }
            }
        }

        private void DiableHyperLinks(bool disable)
        {
            if (disable)
            {
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).LinkBehavior = LinkBehavior.NeverUnderline;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).LinkColor = Color.Black;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).ActiveLinkColor = Color.Black;
            }
            else
            {
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).LinkColor = DashColors.LinkColor;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).ActiveLinkColor = DashColors.LinkColor;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).LinkBehavior = LinkBehavior.AlwaysUnderline;
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.Columns["colHistory"].Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            dgv.Columns["colHistory"].Visible = true;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataLocal();
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                if (DBName.Length > 0)
                {
                    DBName = "";
                }
                else
                {
                    InstanceGroupName = "";
                    tsBack.Enabled = false;
                }
                DiableHyperLinks(false);
                RefreshDataLocal();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void TsHistory_Click(object sender, EventArgs e)
        {
            var frm = new DBSpaceHistoryView
            {
                DatabaseID = DatabaseID,
                InstanceGroupName = InstanceGroupName,
                DBName = DBName
            };
            if (frm.DatabaseID < 1)
            {
                frm.DatabaseID = CommonData.GetDatabaseID(frm.InstanceGroupName, frm.DBName);
            }
            frm.Show();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.Columns["colHistory"].Visible = false;
            Common.PromptSaveDataGridView(ref dgv);
            dgv.Columns["colHistory"].Visible = true;
        }

        private void SpaceTracking_Load(object sender, EventArgs e)
        {
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewLinkColumn() { Name = "colName", DataPropertyName = "Grp", HeaderText = "Name" },
                new DataGridViewTextBoxColumn() { Name = "colAllocatedGB", DataPropertyName = "AllocatedGB", HeaderText = "Allocated (GB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = true },
                new DataGridViewTextBoxColumn() { Name = "colUsedGB", DataPropertyName = "UsedGB", HeaderText = "Used (GB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = true },
                new DataGridViewTextBoxColumn() { Name = "colAllocatedMB", DataPropertyName = "AllocatedMB", HeaderText = "Allocated (MB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colUsedMB", DataPropertyName = "UsedMB", HeaderText = "Used (MB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colAllocatedTB", DataPropertyName = "AllocatedTB", HeaderText = "Allocated (TB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colUsedTB", DataPropertyName = "UsedTB", HeaderText = "Used (TB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewLinkColumn() { Name = "colHistory", HeaderText = "History", Text = "View", UseColumnTextForLinkValue = true }
                );
            Common.StyleGrid(ref dgv);
        }

        private void SetUnit(object sender, EventArgs e)
        {
            var selectedItem = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem itm in tsUnits.DropDownItems)
            {
                itm.Checked = itm == selectedItem;
            }
            foreach (string unit in new string[] { "MB", "GB", "TB" })
            {
                dgv.Columns["colAllocated" + unit].Visible = Convert.ToString(selectedItem.Tag) == unit;
                dgv.Columns["colUsed" + unit].Visible = Convert.ToString(selectedItem.Tag) == unit;
            }
        }

        private string Unit
        {
            get
            {
                foreach (ToolStripMenuItem itm in tsUnits.DropDownItems)
                {
                    if (itm.Checked)
                    {
                        return Convert.ToString(itm.Tag);
                    }
                }
                return "MB";
            }
        }

        private void SetDecimal(object sender, EventArgs e)
        {
            var selectedItem = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem itm in tsDecimalPlaces.DropDownItems)
            {
                itm.Checked = itm == selectedItem;
            }
            foreach (string unit in new string[] { "MB", "GB", "TB" })
            {
                dgv.Columns["colAllocated" + unit].DefaultCellStyle = Common.DataGridViewCellStyle(Convert.ToString(selectedItem.Tag));
                dgv.Columns["colUsed" + unit].DefaultCellStyle = Common.DataGridViewCellStyle(Convert.ToString(selectedItem.Tag));
            }
        }

        private string NumberFormat
        {
            get
            {
                foreach (ToolStripMenuItem itm in tsDecimalPlaces.DropDownItems)
                {
                    if (itm.Checked)
                    {
                        return Convert.ToString(itm.Tag);
                    }
                }
                return "N1";
            }
        }

        private void Dgv_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                object value = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                e.ToolTipText = value == null || value == DBNull.Value ? "Unknown" : value.ToString();
            }
        }

    }
}
