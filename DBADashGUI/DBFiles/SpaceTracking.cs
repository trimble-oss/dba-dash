using DBADashGUI.DBFiles;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class SpaceTracking : UserControl, INavigation, ISetContext
    {
        public SpaceTracking()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        private List<int> InstanceIDs;
        private int DatabaseID = -1;
        private string DBName = "";
        private string InstanceGroupName = "";
        public bool CanNavigateBack => tsBack.Enabled;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.InstanceIDs.ToList();
            DatabaseID = _context.DatabaseID;
            InstanceGroupName = _context.InstanceName;
            DBName = _context.DatabaseName;
            RefreshData();
        }

        public void RefreshData()
        {
            var drillDownEnabled = DatabaseID > 0;
            tsBack.Enabled = false;
            DisableHyperLinks(drillDownEnabled);
            RefreshDataLocal();
        }

        private DataTable GetDBSpace()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBSpace_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", DatabaseID);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@InstanceGroupName", InstanceGroupName);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@DBName", DBName);
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            DataTable dt = new();
            da.Fill(dt);

            return dt;
        }

        private void RefreshDataLocal()
        {
            tsContext.Text = InstanceGroupName + (string.IsNullOrEmpty(DBName) ? "" : " \\ " + DBName);
            var dt = GetDBSpace();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = new DataView(dt);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            pieChart1.Series.Clear();

            static string labelPoint(ChartPoint chartPoint) =>
                $"{chartPoint.SeriesView.Title} ({chartPoint.Participation:P})";
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
            if (e.RowIndex < 0) return;
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
                    DisableHyperLinks(true);
                }
                else
                {
                    DisableHyperLinks(true);
                    return;
                }
                tsBack.Enabled = true;
                RefreshDataLocal();
            }
            else if (e.ColumnIndex == dgv.Columns["colHistory"].Index)
            {
                var instance = InstanceGroupName;
                var db = DBName;
                var dbid = DatabaseID;
                var fileName = string.Empty;

                if (InstanceIDs.Count > 1 && string.IsNullOrEmpty(InstanceGroupName))
                {
                    instance = selectedGroupValue;
                }
                else if (DBName.Length == 0)
                {
                    db = selectedGroupValue;
                }
                else
                {
                    fileName = selectedGroupValue;
                }
                if (dbid < 1)
                {
                    dbid = CommonData.GetDatabaseID(instance, db);
                }
                LoadDBSpaceHistoryView(dbid, db, instance, fileName);
            }
        }

        private void DisableHyperLinks(bool disable)
        {
            if (disable)
            {
                (((DataGridViewLinkColumn)dgv.Columns["colName"])!).LinkBehavior = LinkBehavior.NeverUnderline;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).LinkColor = DBADashUser.SelectedTheme.ForegroundColor;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).ActiveLinkColor = DBADashUser.SelectedTheme.ForegroundColor;
            }
            else
            {
                (((DataGridViewLinkColumn)dgv.Columns["colName"])!).LinkColor = DBADashUser.SelectedTheme.LinkColor;
                ((DataGridViewLinkColumn)dgv.Columns["colName"]).ActiveLinkColor = DBADashUser.SelectedTheme.LinkColor;
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
                DisableHyperLinks(false);
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
            var dbid = DatabaseID < 1 ? CommonData.GetDatabaseID(InstanceGroupName, DBName) : DatabaseID;
            LoadDBSpaceHistoryView(dbid, DBName, InstanceGroupName, string.Empty);
        }

        private void LoadDBSpaceHistoryView(int databaseID, string dbName, string instanceGroupName, string fileName)
        {
            DBSpaceHistoryView dbSpaceHistoryViewForm = new()
            {
                DatabaseID = databaseID,
                InstanceGroupName = instanceGroupName,
                DBName = dbName,
                NumberFormat = NumberFormat,
                Unit = Unit,
                FileName = fileName
            };
            dbSpaceHistoryViewForm.ShowSingleInstance();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.Columns["colHistory"].Visible = false;
            dgv.ExportToExcel();
            dgv.Columns["colHistory"].Visible = true;
        }

        private void SpaceTracking_Load(object sender, EventArgs e)
        {
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewLinkColumn() { Name = "colName", DataPropertyName = "Grp", HeaderText = "Name", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { Name = "colAllocatedGB", DataPropertyName = "AllocatedGB", HeaderText = "Allocated (GB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = true },
                new DataGridViewTextBoxColumn() { Name = "colUsedGB", DataPropertyName = "UsedGB", HeaderText = "Used (GB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = true },
                new DataGridViewTextBoxColumn() { Name = "colAllocatedMB", DataPropertyName = "AllocatedMB", HeaderText = "Allocated (MB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colUsedMB", DataPropertyName = "UsedMB", HeaderText = "Used (MB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colAllocatedTB", DataPropertyName = "AllocatedTB", HeaderText = "Allocated (TB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colUsedTB", DataPropertyName = "UsedTB", HeaderText = "Used (TB)", DefaultCellStyle = Common.DataGridViewCellStyle("N1"), Visible = false },
                new DataGridViewLinkColumn() { Name = "colHistory", HeaderText = "History", Text = "View", UseColumnTextForLinkValue = true }
                );
            dgv.ApplyTheme();
        }

        private void SetUnit(object sender, EventArgs e)
        {
            var selectedItem = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem itm in tsUnits.DropDownItems)
            {
                itm.Checked = itm == selectedItem;
            }
            foreach (var unit in new[] { "MB", "GB", "TB" })
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
            foreach (var unit in new[] { "MB", "GB", "TB" })
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
            if (e.RowIndex < 0) return;
            var value = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            e.ToolTipText = value == null || value == DBNull.Value ? "Unknown" : value.ToString();
        }
    }
}