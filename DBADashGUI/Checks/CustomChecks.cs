using DBADash;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    public partial class CustomChecks : UserControl, ISetContext, ISetStatus
    {
        public CustomChecks()
        {
            InitializeComponent();
        }

        private List<int> InstanceIDs;

        public bool IncludeCritical
        {
            get => statusFilterToolStrip1.Critical; set => statusFilterToolStrip1.Critical = value;
        }

        public bool IncludeWarning
        {
            get => statusFilterToolStrip1.Warning; set => statusFilterToolStrip1.Warning = value;
        }

        public bool IncludeNA
        {
            get => statusFilterToolStrip1.NA; set => statusFilterToolStrip1.NA = value;
        }

        public bool IncludeOK
        {
            get => statusFilterToolStrip1.OK; set => statusFilterToolStrip1.OK = value;
        }

        private DBADashContext CurrentContext;

        public string CheckContext
        {
            get => _checkContext;
            set
            {
                _checkContext = value;
                var found = false;
                var style = _checkContext == null ? FontStyle.Regular : FontStyle.Bold;
                contextToolStripMenuItem.Font = new Font(contextToolStripMenuItem.Font, style);
                foreach (var child in contextToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    child.Checked = child.Text == value;
                    style = child.Text == value ? FontStyle.Bold : FontStyle.Regular;
                    child.Font = new Font(child.Font, style);
                    found = found || child.Text == value;
                }
                if (_checkContext != null && !found)
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
            get => test;
            set
            {
                test = value;
                var found = false;
                var style = test == null ? FontStyle.Regular : FontStyle.Bold;
                testToolStripMenuItem.Font = new Font(testToolStripMenuItem.Font, style);
                foreach (var child in testToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    child.Checked = child.Text == value;
                    style = child.Text == value ? FontStyle.Bold : FontStyle.Regular;
                    child.Font = new Font(child.Font, style);
                    found = found || child.Text == value;
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
        private string _checkContext;
        private string test;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.InstanceIDs.ToList();
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = _context.InstanceID > 0 || _context.Type == SQLTreeItem.TreeType.AzureInstance;
            IncludeOK = _context.InstanceID > 0 || _context.Type == SQLTreeItem.TreeType.AzureInstance;
            CurrentContext = _context;
            tsTrigger.Visible = _context.CanMessage;
            RefreshData();
        }

        private void RefreshContext()
        {
            _checkContext = null;
            contextToolStripMenuItem.DropDownItems.Clear();

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomCheckContext_Get", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));

            using var rdr = cmd.ExecuteReader();
            var i = 0;
            while (rdr.Read())
            {
                if (i >= 30)
                {
                    break;
                }
                var ddContext = new ToolStripMenuItem((string)rdr[0]);
                ddContext.Click += DdContext_Click;
                contextToolStripMenuItem.DropDownItems.Add(ddContext);
                i += 1;
            }
            contextToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            contextToolStripMenuItem.DropDownItems.Add(ddCustomContext);
        }

        private void DdCustomContext_Click(object sender, EventArgs e)
        {
            var custom = (ToolStripMenuItem)sender;
            var userContext = "";
            if (custom.Checked)
            {
                CheckContext = null;
                RefreshCustomChecks();
            }
            else
            {
                if (CommonShared.ShowInputDialog(ref userContext, "Enter Context") != DialogResult.OK) return;
                CheckContext = userContext;
                RefreshCustomChecks();
            }
        }

        private void DdCustomTest_Click(object sender, EventArgs e)
        {
            var custom = (ToolStripMenuItem)sender;
            var userTest = "";
            if (custom.Checked)
            {
                Test = null;
                RefreshCustomChecks();
            }
            else
            {
                if (CommonShared.ShowInputDialog(ref userTest, "Enter Test") != DialogResult.OK) return;
                Test = userTest;
                RefreshCustomChecks();
            }
        }

        private void RefreshTest()
        {
            test = null;
            testToolStripMenuItem.DropDownItems.Clear();

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomCheckTest_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));

            using var rdr = cmd.ExecuteReader();
            var i = 0;
            while (rdr.Read())
            {
                if (i >= 30)
                {
                    break;
                }
                var ddTest = new ToolStripMenuItem((string)rdr[0]);
                ddTest.Click += DdTest_Click;
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
            Test = itm.Checked ? itm.Text : null;
            RefreshCustomChecks();
        }

        private void DdContext_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            CheckContext = itm.Checked ? null : itm.Text;
            RefreshCustomChecks();
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(RefreshData);
                return;
            }
            RefreshContext();
            RefreshTest();
            RefreshCustomChecks();
            CheckContext = _checkContext;
            Test = test;
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }

        private void RefreshCustomChecks()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CustomCheck_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
                cmd.Parameters.AddWithNullableValue("Context", _checkContext);
                cmd.Parameters.AddWithNullableValue("Test", test);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
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

        private void GetHistory(int InstanceID, string test, string context)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CustomChecksHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithNullableValue("Context", context);
                cmd.Parameters.AddWithNullableValue("Test", test);

                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
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
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
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
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvCustom.Rows[e.RowIndex].DataBoundItem;
            if (e.ColumnIndex == colContext.Index)
            {
                CheckContext = (string)dgvCustom.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                RefreshCustomChecks();
            }
            else if (e.ColumnIndex == colTest.Index)
            {
                Test = (string)dgvCustom.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                RefreshCustomChecks();
            }
            else if (e.ColumnIndex == History.Index)
            {
                GetHistory((int)row["InstanceID"], (string)row["Test"], (string)row["Context"]);
            }
        }

        private void TsClear_Click(object sender, EventArgs e)
        {
            CheckContext = null;
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

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            await CollectionMessaging.TriggerCollection(CurrentContext.InstanceID, new List<CollectionType>() { CollectionType.CustomChecks }, this);
        }
    }
}