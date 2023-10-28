using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class Drivers : UserControl, ISetContext
    {
        public Drivers()
        {
            InitializeComponent();
        }

        private List<int> InstanceIDs;
        private string provider = "";
        private string searchText = "";

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            tsProvider.DropDownItems.Clear();
            txtSearch.Text = "";
            searchText = "";
            RefreshDrivers();
        }

        private DataTable GetDrivers()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Drivers_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddStringIfNotNullOrEmpty("Provider", provider);
                cmd.Parameters.AddStringIfNotNullOrEmpty("DriverSearch", searchText);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private void RefreshDrivers()
        {
            dgvDrivers.Columns.Clear();
            searchText = txtSearch.Text;

            dgvDrivers.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DriverProviderName", HeaderText = "Provider", Frozen = Common.FreezeKeyColumn });
            dgvDrivers.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DeviceName", HeaderText = "Device", Frozen = Common.FreezeKeyColumn });

            DataTable dt = GetDrivers();

            string pivotCol = "InstanceDisplayName";
            if (tsProvider.DropDownItems.Count == 0)
            {
                AddFilters(dt);
            }
            foreach (DataRow r in dt.DefaultView.ToTable(true, pivotCol).Select("", pivotCol))
            {
                if (r[pivotCol] != DBNull.Value)
                {
                    DataGridViewTextBoxColumn col = new() { HeaderText = (string)r[pivotCol], Name = (string)r[pivotCol] };
                    dgvDrivers.Columns.Add(col);
                }
            }
            string lastDevice = string.Empty;
            string lastProvider = string.Empty;
            string previousVersion = string.Empty;
            List<DataGridViewRow> rows = new();
            DataGridViewRow row = null;
            foreach (DataRow r in dt.Select("", "DriverProviderName,DeviceName"))
            {
                string device = r["DeviceName"] == DBNull.Value ? "" : (string)r["DeviceName"];
                string provider = r["DriverProviderName"] == DBNull.Value ? "" : (string)r["DriverProviderName"];
                if (provider == string.Empty && device == string.Empty)
                {
                    continue;
                }
                if (lastDevice != device | lastProvider != provider)
                {
                    row = new DataGridViewRow();
                    row.CreateCells(dgvDrivers);
                    row.Cells[0].Value = provider;
                    row.Cells[1].Value = device;
                    rows.Add(row);
                    previousVersion = "";
                }

                string instance = (string)r[pivotCol];
                string version = r["DriverVersion"] == DBNull.Value ? "" : (string)r["DriverVersion"];
                DateTime validFrom = (DateTime)r["ValidFrom"];
                var idx = dgvDrivers.Columns[instance].Index;

                row.Cells[idx].Value = version;
                row.Cells[idx].ToolTipText = "Valid From: " + validFrom.ToAppTimeZone().ToString("yyyy-MM-dd");
                if (previousVersion != version && previousVersion != "")
                {
                    row.DefaultCellStyle.BackColor = DashColors.YellowPale;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }

                lastDevice = device;
                lastProvider = provider;
                previousVersion = version;
            }
            dgvDrivers.Rows.AddRange(rows.ToArray());
            dgvDrivers.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void AddFilters(DataTable dt)
        {
            tsProvider.DropDownItems.Clear();
            foreach (DataRow r in dt.DefaultView.ToTable(true, "DriverProviderName").Select("", "DriverProviderName"))
            {
                if (r["DriverProviderName"] != DBNull.Value && (string)r["DriverProviderName"] != "")
                {
                    var tsItem = new ToolStripMenuItem((string)r["DriverProviderName"]);
                    tsItem.Checked = tsItem.Text == provider;
                    tsItem.Click += TsItem_Click;
                    tsProvider.DropDownItems.Add(tsItem);
                }
            }
        }

        private void TsItem_Click(object sender, EventArgs e)
        {
            var selectedItm = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem itm in tsProvider.DropDownItems)
            {
                itm.Checked = itm == selectedItm && !selectedItm.Checked;
            }
            provider = selectedItm.Checked ? selectedItm.Text : "";
            RefreshDrivers();
        }

        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (searchText != txtSearch.Text)
                {
                    searchText = txtSearch.Text;
                    RefreshDrivers();
                }
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDrivers();
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            provider = "";
            tsProvider.DropDownItems.Clear();
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvDrivers);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvDrivers);
        }
    }
}