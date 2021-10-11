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
using DBADashGUI.Performance;
using Humanizer;
using System.Diagnostics;

namespace DBADashGUI
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
        string _db="";
        public string DBName {
            get {
                return _db;
            }
            set {
                _db = value;
           
                lblDatabase.Visible = _db.Length == 0;
                lblInstance.Visible = _db.Length == 0;
                instanceToolStripMenuItem.Visible = _db.Length == 0;
                databaseNameToolStripMenuItem.Visible = _db.Length == 0;
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
            txtApp.Text = "";
            txtResult.Text = "";
            txtSessionID.Text = "";
            if(_db.Length > 0){
                groupBy = "object_name";
            }
            else if (InstanceIDs.Count == 1)
            {
                groupBy = "DatabaseName";
            }
            else
            {
                groupBy = "ConnectionID";
            }
            selectGroupBy();
        }


        public void RefreshData()
        {
            dgvSlow.DataSource = null;
            lblPageSize.Visible = false;
            toggleSummary(true);
            
            int top = Convert.ToInt32(tsTop.Tag);
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.SlowQueriesSummary_Get", cn);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("GroupBy", groupBy);
                string db = DBName.Length > 0 ? DBName : txtDatabase.Text;
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
                if (db.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseName", db);
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
                if (txtResult.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Result", txtResult.Text);
                }
                if (txtSessionID.Text.Length > 0)
                {
                    if (int.TryParse(txtSessionID.Text, out int sessionID))
                    {
                        cmd.Parameters.AddWithValue("SessionID", sessionID);
                    }
                    else
                    {
                        MessageBox.Show("Invalid SessionID filter.  Please enter an integer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }
                cmd.Parameters.AddWithValue("Top", top);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                dgvSummary.AutoGenerateColumns = false;
                dgvSummary.DataSource = dt;
                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }

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

        public Int32 pageSize = 1000;


        private void dgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                selectedGroupValue = row["Grp"] == DBNull.Value ? "" : Convert.ToString(row["Grp"]);
                if (dgvSummary.Columns[e.ColumnIndex] == Grp)
                {

                    if (groupBy == "ConnectionID")
                    {
                        txtInstance.Text = selectedGroupValue;
                    }
                    else if (groupBy == "client_hostname")
                    {
                        txtClient.Text = selectedGroupValue;
                    }
                    else if (groupBy == "client_app_name")
                    {
                        txtApp.Text = selectedGroupValue;
                    }
                    else if (groupBy == "DatabaseName")
                    {
                        txtDatabase.Text = selectedGroupValue;
                    }
                    else if (groupBy == "object_name")
                    {
                        txtObject.Text = selectedGroupValue;
                    }
                    else if (groupBy == "username")
                    {
                        txtUser.Text = selectedGroupValue;
                    }
                    else if(groupBy == "Result")
                    {
                        txtResult.Text = selectedGroupValue;
                    }
                    else if (groupBy == "text")
                    {
                        txtText.Text = selectedGroupValue;
                    }
                    else if(groupBy== "session_id")
                    {
                        txtSessionID.Text = selectedGroupValue;
                    }
                    else
                    {
                        throw new Exception("Invalid group by");
                    }

                    if (txtInstance.Text.Length == 0 && _db.Length==0)
                    {
                        groupBy = "ConnectionID";
                    }
                    else if (txtDatabase.Text.Length == 0 && _db.Length==0)
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
                    else if(txtUser.Text.Length==0)
                    {
                        groupBy = "username";
                    }
                    else
                    {
                        groupBy = "Result";
                    }
                    selectGroupBy();
                    RefreshData();
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == Total)
                {
                    loadSlowQueriesDetail();
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _1hrPlus)
                {
                    loadSlowQueriesDetail(3600, -1);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _30to60min)
                {
                    loadSlowQueriesDetail(1800, 3600);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _10to30min)
                {
                    loadSlowQueriesDetail(600, 1800);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _5to10min)
                {
                    loadSlowQueriesDetail(300, 600);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _1to5min)
                {
                    loadSlowQueriesDetail(60, 300);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _30to60)
                {
                    loadSlowQueriesDetail(30, 60);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _20to30)
                {
                    loadSlowQueriesDetail(20, 30);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _10to20)
                {
                    loadSlowQueriesDetail(10, 20);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _5to10)
                {
                    loadSlowQueriesDetail(5, 10);
                }
                else if (dgvSummary.Columns[e.ColumnIndex] == _lt5)
                {
                    loadSlowQueriesDetail(0, 5);
                }
            }
        }

        string selectedGroupValue;

        private void SlowQueries_Load(object sender, EventArgs e)
        {
            selectGroupBy();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetFilters();
            RefreshData();
        }

        private void txtInstance_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtInstance, lblInstance);
        }

        private void setFilterHighlight(ToolStripTextBox txt, ToolStripMenuItem lbl)
        {
            if (txt.Text.Length > 0)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            }
            else
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Regular);
            }
        }

        private void txtClient_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtClient, lblClient);
        }

        private void txtApp_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtApp, lblApp);
        }

        private void txtDatabase_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtDatabase, lblDatabase);
        }

        private void txtObject_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtObject, lblObject);
        }

        private void txtUser_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtUser, lblUser);
        }

        private void txtText_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtText, lblText);
        }
        private void txtResult_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtResult, lblResult);
        }

        private void txtSessionID_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(txtSessionID.Text, out _) && !string.IsNullOrEmpty(txtSessionID.Text))
            {
                txtSessionID.BackColor = Color.Red;
            }
            else
            {
                txtSessionID.BackColor = Color.AliceBlue;
            }

            setFilterHighlight(txtSessionID, lblSessionID);
        }

        private void loadSlowQueriesDetail(Int32 durationFrom=-1,Int32 durationTo=-1)
        {
        
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.SlowQueriesDetail_Get", cn);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("Top", pageSize);

                string connectionID = txtInstance.Text;
                string client = txtClient.Text;
                string user = txtUser.Text;
                string db = DBName.Length>0 ?  DBName : txtDatabase.Text;
                string objectname = txtObject.Text;
                string app = txtApp.Text;
                string result = txtResult.Text;
                string text = txtText.Text;
                string sessionid = txtSessionID.Text;
                if (groupBy == "ConnectionID")
                {
                    connectionID = selectedGroupValue;
                }
                else if (groupBy == "client_hostname")
                {
                    client = selectedGroupValue;
                }
                else if (groupBy == "client_app_name")
                {
                    app= selectedGroupValue;
                }
                else if (groupBy == "DatabaseName")
                {
                    db = selectedGroupValue;
                }
                else if (groupBy == "object_name")
                {
                    objectname = selectedGroupValue;
                }
                else if (groupBy == "username")
                {
                   user= selectedGroupValue;
                }
                else if (groupBy == "Result")
                {
                    result = selectedGroupValue;
                }
                else if (groupBy == "text")
                {
                    text = selectedGroupValue;
                }
                else if (groupBy == "session_id")
                {
                    sessionid = selectedGroupValue;
                }
                else
                {
                    throw new Exception("Invalid group by");
                }

                if (client.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ClientHostName", client);
                }
                if (connectionID.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ConnectionID", connectionID);
                }
                if (app.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ClientAppName", app);
                }
                if (db.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseName", db);
                }
                if (objectname.Length > 0)
                {
                    cmd.Parameters.AddWithValue("ObjectName", objectname);
                }
                if (user.Length > 0)
                {
                    cmd.Parameters.AddWithValue("UserName", user);
                }
                if (text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Text", text);
                }
                if (durationFrom > 0)
                {
                    cmd.Parameters.AddWithValue("DurationFromSec", durationFrom);
                }
                if (durationTo > 0)
                {
                    cmd.Parameters.AddWithValue("DurationToSec", durationTo);
                }
                if (result.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Result", result);
                }
                if (sessionid.Length > 0)
                {
                    cmd.Parameters.AddWithValue("SessionID", sessionid);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                dt.Columns.Add("text_trunc",typeof(string));
                foreach(DataRow r in dt.Rows)
                {
                    if (r["text"] != DBNull.Value)
                    {
                        var txt = ((string)r["text"]);
                        r["text_trunc"] = txt.Length > 10000 ? txt.Substring(0, 10000) : txt;
                    }
                }
                Common.ConvertUTCToLocal(ref dt);
                if(dt.Rows.Count== pageSize)
                {
                    lblPageSize.Text = string.Format("Top {0} rows", pageSize);
                    lblPageSize.Visible = true;
                }
                else
                {
                    lblPageSize.Visible = false;
                }
               dgvSlow.AutoGenerateColumns = false;
          
                dgvSlow.DataSource = dt;
                colText.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvSlow.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                colText.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvSlow.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void dgvSlow_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvSlow.Rows[e.RowIndex].DataBoundItem;
                if(dgvSlow.Columns[e.ColumnIndex] == colText){
                    var frm = new CodeViewer
                    {
                        SQL = (string)row["Text"]
                    };
                    frm.ShowDialog();
                }
                else if(dgvSlow.Columns[e.ColumnIndex] == colSessionID)
                {
                    int sessionID = Convert.ToInt32(row["session_id"]);
                    DateTime toDate = Convert.ToDateTime(row["timestamp"]).ToUniversalTime();
                    DateTime fromDate = Convert.ToDateTime(row["start_time"]).ToUniversalTime();
                    int instanceID = Convert.ToInt32(row["InstanceID"]);
                    toggleSummary(false);
                    runningQueries1.SessionID = sessionID;
                    runningQueries1.SnapshotDateFrom = fromDate;
                    runningQueries1.SnapshotDateTo = toDate;
                    runningQueries1.InstanceID = instanceID;
                    runningQueries1.RefreshData();

                }
            }       
        }

           private void Filter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshData();
            }
        }

        private void tsCopySummary_Click(object sender, EventArgs e)
        {

           Common.CopyDataGridViewToClipboard(dgvSummary);
        }

        private void tsCopyDetail_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSlow);
        }

        private void tsTop_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            if(ts == tsTopAll)
            {
                tsTop.Text = "Top *";
            }
            else
            {
                tsTop.Text = "Top " + ts.Tag.ToString();
            }
            tsTop.Tag = ts.Tag;
            RefreshData();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
           Common.PromptSaveDataGridView(ref dgvSummary);    
        }

        private void tsExcelDetail_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSlow);
        }

        private void toggleSummary(bool isSummary)
        {
            tsRunning.Visible = !isSummary;
            tsSummary.Visible = isSummary;
            dgvSummary.Visible = isSummary;
            runningQueries1.Visible = !isSummary;
        }

        private void tsRunningBack_Click(object sender, EventArgs e)
        {
            toggleSummary(true);
        }
    }
}
