using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    public partial class CustomChecks : UserControl, ISetContext
    {
        public CustomChecks()
        {
            InitializeComponent();
        }
        private List<Int32> InstanceIDs;

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
                bool found = false;
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
                if (context != null && !found)
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

        private readonly ToolStripMenuItem ddCustomContext = new("Custom");
        private ToolStripMenuItem ddCustomTest = new("Custom");
        private string context = null;
        private string test = null;


        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = context.InstanceID > 0 || context.Type == SQLTreeItem.TreeType.AzureInstance;
            IncludeOK = context.InstanceID > 0 || context.Type == SQLTreeItem.TreeType.AzureInstance;
            RefreshData();
        }

        private void RefreshContext()
        {
            context = null;
            contextToolStripMenuItem.DropDownItems.Clear();

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomCheckContext_Get", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
            using var rdr = cmd.ExecuteReader();
            Int32 i = 0;
            while (rdr.Read())
            {
                if (i >= 30)
                {
                    break;
                }
                var ddContext = new ToolStripMenuItem((string)rdr[0]);
                ddContext.Click += DdContext_Click;
                // ddContext.CheckOnClick = true;
                contextToolStripMenuItem.DropDownItems.Add(ddContext);
                i += 1;
            }
            contextToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            contextToolStripMenuItem.DropDownItems.Add(ddCustomContext);

        }

        private void DdCustomContext_Click(object sender, EventArgs e)
        {
            var custom = (ToolStripMenuItem)sender;
            string userContext = "";
            if (custom.Checked)
            {
                Context = null;
                RefreshCustomChecks();
            }
            else
            {
                if (Common.ShowInputDialog(ref userContext, "Enter Context") == DialogResult.OK)
                {
                    Context = userContext;
                    RefreshCustomChecks();
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
                RefreshCustomChecks();
            }
            else
            {
                if (Common.ShowInputDialog(ref userTest, "Enter Test") == DialogResult.OK)
                {
                    Test = userTest;
                    RefreshCustomChecks();
                }
            }
        }

        private void RefreshTest()
        {
            test = null;
            testToolStripMenuItem.DropDownItems.Clear();

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomCheckTest_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));

            using var rdr = cmd.ExecuteReader();
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



        private void DdTest_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            if (itm.Checked)
            {
                Test = itm.Text;

            }
            else
            {
                Test = null;
            }
            RefreshCustomChecks();
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
            RefreshCustomChecks();
        }

        public void RefreshData()
        {
            RefreshContext();
            RefreshTest();
            RefreshCustomChecks();
            Context = context;
            Test = test;

        }

        private void RefreshCustomChecks()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CustomCheck_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();

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
                DataTable dt = new();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvCustom.AutoGenerateColumns = false;
                dgvCustom.DataSource = dt;
                HistoryView(false);
                dgvCustom.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
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

        private void GetHistory(Int32 InstanceID, string test, string context)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CustomChecksHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);

                if (context != null)
                {
                    cmd.Parameters.AddWithValue("Context", context);
                }
                if (test != null)
                {
                    cmd.Parameters.AddWithValue("Test", test);
                }

                DataTable dt = new();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvCustom.AutoGenerateColumns = false;
                dgvCustom.DataSource = dt;
                HistoryView(true);
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshCustomChecks();
        }


        private void StatusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshCustomChecks();
        }

        private void DgvCustom_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvCustom.Rows[idx].DataBoundItem;
                var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row["Status"]);
                dgvCustom.Rows[idx].Cells[colStatus.Index].SetStatusColor(status);

            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvCustom);
        }

        private void CustomChecks_Load(object sender, EventArgs e)
        {
            ddCustomContext.Click += DdCustomContext_Click;
            ddCustomTest.Click += DdCustomTest_Click;
        }

        private void DgvCustom_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvCustom.Rows[e.RowIndex].DataBoundItem;
                if (e.ColumnIndex == colContext.Index)
                {
                    Context = (string)dgvCustom.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    RefreshCustomChecks();
                }
                else if (e.ColumnIndex == colTest.Index)
                {
                    Test = (string)dgvCustom.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    RefreshCustomChecks();
                }
                else if (e.ColumnIndex == History.Index)
                {
                    GetHistory((Int32)row["InstanceID"], (string)row["Test"], (string)row["Context"]);
                }
            }
        }

        private void TsClear_Click(object sender, EventArgs e)
        {
            Context = null;
            Test = null;
            IncludeOK = true;
            IncludeNA = true;
            IncludeCritical = true;
            IncludeWarning = true;
            RefreshCustomChecks();
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            RefreshCustomChecks();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvCustom);
        }
    }


}
