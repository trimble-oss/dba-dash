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

namespace DBAChecksGUI.Checks
{
    public partial class CustomChecks : UserControl
    {
        public CustomChecks()
        {
            InitializeComponent();
        }
        public List<Int32> InstanceIDs;
        public string ConnectionString;

        public bool IncludeCritical
        {
            get
            {
                return criticalToolStripMenuItem.Checked;
            }
            set
            {
                criticalToolStripMenuItem.Checked = value;
            }
        }

        public bool IncludeWarning
        {
            get
            {
                return warningToolStripMenuItem.Checked;
            }
            set
            {
                warningToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeNA
        {
            get
            {
                return undefinedToolStripMenuItem.Checked;
            }
            set
            {
                undefinedToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeOK
        {
            get
            {
                return OKToolStripMenuItem.Checked;
            }
            set
            {
                OKToolStripMenuItem.Checked = value;
            }
        }

        public string Context
        {
            get
            {
                return context;
            }
            set
            {
                context = value;
                bool found=false;
                var style = context == null ? FontStyle.Regular : FontStyle.Bold;
                contextToolStripMenuItem.Font = new Font(contextToolStripMenuItem.Font, style);
                foreach (ToolStripItem child in contextToolStripMenuItem.DropDownItems)
                {
                    if (child.GetType() == typeof(ToolStripMenuItem))
                    {
                        ((ToolStripMenuItem)child).Checked = child.Text == value;
                        style = child.Text == value ? FontStyle.Bold : FontStyle.Regular;
                        child.Font = new Font(child.Font, style);
                        found = found || child.Text == value;
                    }
                }
               if(context!=null && !found)
                {
                    ddCustomContext.Checked = true;
                    ddCustomContext.Font = new Font(ddCustomContext.Font, FontStyle.Bold);
                }
                else
                {
                    ddCustomContext.Font = new Font(ddCustomContext.Font, FontStyle.Regular);
                }
            }
        }

        public string Test
        {
            get
            {
                return test;
            }
            set
            {
                test = value;
                bool found = false;
                var style = test == null ? FontStyle.Regular : FontStyle.Bold;
                testToolStripMenuItem.Font = new Font(testToolStripMenuItem.Font, style);
                foreach (ToolStripItem child in testToolStripMenuItem.DropDownItems)
                {
                    if (child.GetType() == typeof(ToolStripMenuItem))
                    {
                        ((ToolStripMenuItem)child).Checked = child.Text == value;
                        style = child.Text == value ? FontStyle.Bold : FontStyle.Regular;
                        child.Font = new Font(child.Font, style);
                        found = found || child.Text == value;
                    }
                }
                if (test != null && !found)
                {
                    ddCustomTest.Checked = true;
                    ddCustomTest.Font = new Font(ddCustomTest.Font, FontStyle.Bold);
                }
                else
                {
                    ddCustomTest.Font = new Font(ddCustomTest.Font, FontStyle.Regular);
                }
            }
        }

        private ToolStripMenuItem ddCustomContext=new ToolStripMenuItem("Custom");
        private ToolStripMenuItem ddCustomTest = new ToolStripMenuItem("Custom");
        private string context = null;
        private string test = null;

        private void refreshContext()
        {
            context = null;
            contextToolStripMenuItem.DropDownItems.Clear();
            var cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.CustomCheckContext_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                cmd.CommandType = CommandType.StoredProcedure;

                var rdr = cmd.ExecuteReader();
                Int32 i=0;            
                while (rdr.Read()){
                    if (i >= 30)
                    {
                        break;
                    }
                    var ddContext = new  ToolStripMenuItem((string)rdr[0]);
                    ddContext.Click += DdContext_Click;
                   // ddContext.CheckOnClick = true;
                    contextToolStripMenuItem.DropDownItems.Add(ddContext);
                    i += 1;
                }
                contextToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());             
                contextToolStripMenuItem.DropDownItems.Add(ddCustomContext);
            }
        }

        private void DdCustomContext_Click(object sender, EventArgs e)
        {
            var custom = (ToolStripMenuItem)sender;
            string userContext="";
            if (custom.Checked)
            {
                Context = null;
                refreshCustomChecks();
            }
            else
            {
                if (Common.ShowInputDialog(ref userContext, "Enter Context") == DialogResult.OK)
                {
                    Context = userContext;
                    refreshCustomChecks();
                }
            }
           
        }
        private void DdCustomTest_Click(object sender, EventArgs e)
        {
            var custom = (ToolStripMenuItem)sender;
            string userTest = "";
            if (custom.Checked)
            {
                Test = null;
                refreshCustomChecks();
            }
            else
            {
                if (Common.ShowInputDialog(ref userTest, "Enter Test") == DialogResult.OK)
                {
                   Test = userTest;
                    refreshCustomChecks();
                }
            }
        }

        private void refreshTest()
        {
            test = null;
            testToolStripMenuItem.DropDownItems.Clear();
            var cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.CustomCheckTest_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                cmd.CommandType = CommandType.StoredProcedure;

                var rdr = cmd.ExecuteReader();
                Int32 i = 0;
                while (rdr.Read())
                {
                    if (i >= 30)
                    {
                        break;
                    }
                    var ddTest = new ToolStripMenuItem((string)rdr[0]);
                    ddTest.Click += DdTest_Click; ;
                    ddTest.CheckOnClick = true;
                    testToolStripMenuItem.DropDownItems.Add(ddTest);
                    i += 1;
                }
                testToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                ddCustomTest = new ToolStripMenuItem("Custom");
                ddCustomTest.Click += DdCustomTest_Click;
                testToolStripMenuItem.DropDownItems.Add(ddCustomTest);
            }
        }

    

        private void DdTest_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            if (itm.Checked)
            {
                Test= itm.Text;
                
            }
            else
            {
               Test = null;
            }
            refreshCustomChecks();
        }

        private void DdContext_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            if (itm.Checked)
            {
                Context = null;               
            }
            else
            {
                Context = itm.Text;
            }
            refreshCustomChecks();
        }

        public void RefreshData()
        {
            refreshContext();
            refreshTest();
            refreshCustomChecks();
            Context = context;
            Test = test;
           
        }

        private void refreshCustomChecks()
        {
            var cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.CustomCheck_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                if (context != null)
                {
                    cmd.Parameters.AddWithValue("Context", context);
                }
                if (test != null)
                {
                    cmd.Parameters.AddWithValue("Test", test);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvCustom.AutoGenerateColumns = false;
                dgvCustom.DataSource = dt;
                HistoryView(false);
            }
        }

        private void HistoryView(bool isHistory)
        {
            dgvCustom.Columns["History"].Visible = !isHistory;
            tsBack.Visible = isHistory;
            tsClear.Visible = !isHistory;
            tsFilter.Visible = !isHistory;
            tsRefresh.Visible = !isHistory;
        }

        private void getHistory(Int32 InstanceID,string test,string context)
        {
            var cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.CustomChecksHistory_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);

                if (context != null)
                {
                    cmd.Parameters.AddWithValue("Context", context);
                }
                if (test != null)
                {
                    cmd.Parameters.AddWithValue("Test", test);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvCustom.AutoGenerateColumns = false;
                dgvCustom.DataSource = dt;
                HistoryView(true);
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshCustomChecks();
        }

  
        private void statusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refreshCustomChecks();
        }

        private void dgvCustom_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvCustom.Rows[idx].DataBoundItem;

                dgvCustom.Rows[idx].Cells[colStatus.Index].Style.BackColor = DBAChecksStatus.GetStatusColour((DBAChecksStatus.DBAChecksStatusEnum)Convert.ToInt32(row["Status"]));
                
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvCustom);
        }

        private void CustomChecks_Load(object sender, EventArgs e)
        {
            ddCustomContext.Click += DdCustomContext_Click;
            ddCustomTest.Click += DdCustomTest_Click;
        }

        private void dgvCustom_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvCustom.Rows[e.RowIndex].DataBoundItem;
                if(e.ColumnIndex== colContext.Index)
                {
                    Context = (string)dgvCustom.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    refreshCustomChecks();
                }
                else if(e.ColumnIndex == colTest.Index)
                {
                    Test = (string)dgvCustom.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    refreshCustomChecks();
                }
                else if(e.ColumnIndex == History.Index)
                {
                    getHistory((Int32)row["InstanceID"], (string)row["Test"], (string)row["Context"]);
                }
            }
        }

        private void tsClear_Click(object sender, EventArgs e)
        {
            Context = null;
            Test = null;
            IncludeOK = true;
            IncludeNA = true;
            IncludeCritical = true;
            IncludeWarning = true;
            refreshCustomChecks();
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            refreshCustomChecks();
        }
    }


}
