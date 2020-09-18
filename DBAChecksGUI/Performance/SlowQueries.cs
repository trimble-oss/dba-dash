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
using DBAChecksGUI.Performance;

namespace DBAChecksGUI
{
    public partial class SlowQueries : UserControl
    {
        public SlowQueries()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        public string ConnectionString;
        string groupBy = "ConnectionID";

        Int32 mins = 15;
        private DateTime _from = DateTime.MinValue;
        private DateTime _to = DateTime.MinValue;
        private DateTime fromDate
        {
            get
            {
                if (_from == DateTime.MinValue)
                {
                    return DateTime.UtcNow.AddMinutes(-mins);
                }
                else
                {
                    return _from;
                }
            }
        }

        private DateTime toDate
        {
            get
            {
                if (_to == DateTime.MinValue)
                {
                    return DateTime.UtcNow;
                }
                else
                {
                    return _to;
                }

            }
        }

        public void ResetFilters()
        {
            txtText.Text = "";
            txtClient.Text = "";
            txtDatabase.Text = "";
            txtInstance.Text = "";
            txtObject.Text = "";
            txtText.Text = "";
            txtUser.Text = "";
        }


        public void RefreshData()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Report.SlowQueriesSummary", cn);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("FromDate", fromDate);
                cmd.Parameters.AddWithValue("ToDate", toDate);
                cmd.Parameters.AddWithValue("GroupBy", groupBy);
                if (txtClient.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ClientHostName", txtClient.Text);
                }
                if (txtInstance.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ConnectionID", txtInstance.Text);
                }
                if (txtApp.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ClientAppName", txtApp.Text);
                }
                if (txtDatabase.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseName", txtDatabase.Text);
                }
                if (txtObject.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ObjectName", txtObject.Text);
                }
                if (txtUser.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("UserName", txtUser.Text);
                }
                if (txtText.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Text", txtText.Text);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                dgvSummary.AutoGenerateColumns = false;
                dgvSummary.DataSource = dt;

            }

        }

        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            mins = Int32.Parse((string)itm.Tag);
            _from = DateTime.MinValue;
            _to = DateTime.MinValue;
            RefreshData();
            checkTime();
        }

        private void checkTime()
        {
            foreach (var ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var tsmi = (ToolStripMenuItem)ts;
                    tsmi.Checked = Int32.Parse((string)tsmi.Tag) == mins;
                }
            }
        }

        private void tsCustom_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker();
            frm.FromDate = fromDate;
            frm.ToDate = toDate;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                _from = frm.FromDate;
                _to = frm.ToDate;
                mins = 0;
                checkTime();
            }
            RefreshData();
            tsCustom.Checked = true;
        }

        private void GroupBy_Click(object sender, EventArgs e)
        {
            var selected = (ToolStripMenuItem)sender;
            groupBy = (string)selected.Tag;
            selectGroupBy();
            RefreshData();
        }

        private void selectGroupBy()
        {
            foreach (ToolStripMenuItem mnu in tsGroup.DropDownItems)
            {
                mnu.Checked = (string)mnu.Tag == groupBy;
                if (mnu.Checked)
                {
                    Grp.HeaderText = mnu.Text;
                }
            }
        }


        private void dgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvSummary.Columns[e.ColumnIndex] == Grp)
            {
                var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                string value = row["Grp"] == DBNull.Value ? "" : (string)row["Grp"];
                if (groupBy == "ConnectionID")
                {
                    txtInstance.Text = value;
                }
                else if (groupBy == "client_hostname")
                {
                    txtClient.Text = value;
                }
                else if (groupBy == "client_app_name")
                {
                    txtApp.Text = value;
                }
                else if (groupBy == "DatabaseName")
                {
                    txtDatabase.Text = value;
                }
                else if (groupBy == "object_name")
                {
                    txtObject.Text= value;
                }
                else if(groupBy == "username")
                {
                    txtUser.Text = value;
                }
                else
                {
                    throw new Exception("Invalid group by");
                }

                if (txtInstance.Text.Length == 0)
                {
                    groupBy = "ConnectionID";
                }
                else if (txtDatabase.Text.Length == 0)
                {
                    groupBy = "DatabaseName";
                }
                else if (txtApp.Text.Length == 0)
                {
                    groupBy = "client_app_name";
                }
                else if (txtClient.Text.Length == 0)
                {
                    groupBy = "client_hostname";
                }
                else if (txtObject.Text.Length == 0)
                {
                    groupBy = "object_name";
                }
                else
                {
                    groupBy = "username";
                }
                selectGroupBy();
                RefreshData();
            }
        }

        private void SlowQueries_Load(object sender, EventArgs e)
        {
            selectGroupBy();
        }
    }
}
