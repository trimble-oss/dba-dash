using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace DBAChecksGUI.Changes
{
    public partial class Drivers : UserControl
    {
        public Drivers()
        {
            InitializeComponent();
        }


        public string ConnectionString;
        public List<Int32> InstanceIDs;
        string provider = "";
        string searchText = "";

        public void RefreshData()
        {
            tsProvider.DropDownItems.Clear();
            txtSearch.Text = "";
            searchText = "";
            refreshDrivers();
        }

        private void refreshDrivers()
        {
            dgvDrivers.Columns.Clear();
            searchText = txtSearch.Text;

            dgvDrivers.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DriverProviderName", HeaderText = "Provider" });
            dgvDrivers.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DeviceName", HeaderText = "Device" });

            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Drivers_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                if (provider != "")
                {
                    cmd.Parameters.AddWithValue("Provider", provider);
                }
                if (searchText != "")
                {
                    cmd.Parameters.AddWithValue("DriverSearch", searchText);
                }

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                string pivotCol = "Instance";
                if (tsProvider.DropDownItems.Count==0)
                {
                    addFilters(dt);
                }
                foreach (DataRow r in dt.DefaultView.ToTable(true, pivotCol).Select("", pivotCol))
                {
                    if (r[pivotCol] != DBNull.Value)
                    {
                        DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn() { HeaderText = (string)r[pivotCol], Name = (string)r[pivotCol] };
                        dgvDrivers.Columns.Add(col);
                    }
                }
                string lastDevice = "";
                string lastProvider = "";
                string previousVersion = "";
                List<DataGridViewRow> rows = new List<DataGridViewRow>();
                DataGridViewRow row = null;
                foreach (DataRow r in dt.Select("","DriverProviderName,DeviceName"))
                {
                    string device = r["DeviceName"]==DBNull.Value ? "" : (string)r["DeviceName"];
                    string provider = r["DriverProviderName"]==DBNull.Value ? "" : (string)r["DriverProviderName"];
                    if (lastDevice != device | lastProvider!=provider)
                    {
                        row = new DataGridViewRow();
                        row.CreateCells(dgvDrivers);
                        row.Cells[0].Value = provider;
                        row.Cells[1].Value = device;
                        rows.Add(row);
                        previousVersion = "";
                    }

                    string instance = (string)r[pivotCol];
                    string version = r["DriverVersion"]==DBNull.Value ? "" :(string)r["DriverVersion"];
                    DateTime validFrom = (DateTime)r["ValidFrom"];
                    var idx = dgvDrivers.Columns[instance].Index;

                    row.Cells[idx].Value = version;
                    row.Cells[idx].ToolTipText = "Valid From: " + validFrom.ToLocalTime().ToString("yyyy-MM-dd");
                    if (previousVersion != version && previousVersion != "")
                    {
                        row.DefaultCellStyle.BackColor = Color.Yellow;
                    }
                                                          
                    lastDevice = device;
                    lastProvider = provider;
                    previousVersion = version;
                }
                dgvDrivers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                dgvDrivers.Rows.AddRange(rows.ToArray());
            }
        }

        private void addFilters(DataTable dt)
        {
            tsProvider.DropDownItems.Clear();
            foreach (DataRow r in dt.DefaultView.ToTable(true, "DriverProviderName").Select("", "DriverProviderName"))
            {
                if (r["DriverProviderName"] != DBNull.Value && (string)r["DriverProviderName"]!="")
                {
                   var tsItem= new ToolStripMenuItem((string)r["DriverProviderName"]);
                    tsItem.Checked = tsItem.Text == provider;
                    tsItem.Click += TsItem_Click;
                    tsProvider.DropDownItems.Add(tsItem);
                }
            }
        }

        private void TsItem_Click(object sender, EventArgs e)
        {
            var selectedItm = (ToolStripMenuItem)sender;
            foreach(ToolStripMenuItem itm in tsProvider.DropDownItems)
            {

                itm.Checked = itm == selectedItm ? !selectedItm.Checked : false;
            }
            provider = selectedItm.Checked ? selectedItm.Text : "";
            refreshDrivers();
        }



        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)13)
            {
                if (searchText != txtSearch.Text)
                {
                    searchText = txtSearch.Text;
                    refreshDrivers();
                }
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshDrivers();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            provider = "";
            tsProvider.DropDownItems.Clear();
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            dgvDrivers.SelectAll();
            DataObject dataObj = dgvDrivers.GetClipboardContent();
            Clipboard.SetDataObject(dataObj, true);
        }
    }
}
