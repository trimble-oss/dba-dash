using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class Drivers : UserControl
    {
        public Drivers()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        string provider = "";
        string searchText = "";

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
                if (provider != "")
                {
                    cmd.Parameters.AddWithValue("Provider", provider);
                }
                if (searchText != "")
                {
                    cmd.Parameters.AddWithValue("DriverSearch", searchText);
                }

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
            string lastDevice = String.Empty;
            string lastProvider = String.Empty;
            string previousVersion = String.Empty;
            List<DataGridViewRow> rows = new();
            DataGridViewRow row = null;
            foreach (DataRow r in dt.Select("", "DriverProviderName,DeviceName"))
            {
                string device = r["DeviceName"] == DBNull.Value ? "" : (string)r["DeviceName"];
                string provider = r["DriverProviderName"] == DBNull.Value ? "" : (string)r["DriverProviderName"];
                if (provider == String.Empty && device == String.Empty)
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
                row.Cells[idx].ToolTipText = "Valid From: " + validFrom.ToLocalTime().ToString("yyyy-MM-dd");
                if (previousVersion != version && previousVersion != "")
                {
                    row.DefaultCellStyle.BackColor = DashColors.YellowPale;
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
