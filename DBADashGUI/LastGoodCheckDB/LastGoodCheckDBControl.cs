using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Interface;
using DBADashGUI.Theme;
using DBADash;
using DBADashGUI.Messaging;

namespace DBADashGUI.LastGoodCheckDB
{
    public partial class LastGoodCheckDBControl : UserControl, ISetContext, ISetStatus
    {
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

        public LastGoodCheckDBControl()
        {
            InitializeComponent();
        }

        private DBADashContext CurrentContext;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = _context.InstanceID > 0;
            IncludeOK = _context.InstanceID > 0;
            CurrentContext = _context;
            lblStatus.Text = string.Empty;
            tsTrigger.Visible = _context.CanMessage;
            RefreshData();
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(RefreshData);
                return;
            }

            var dt = GetLastGoodDBCC();
            dgvLastGoodCheckDB.AutoGenerateColumns = false;
            dgvLastGoodCheckDB.DataSource = new DataView(dt);
            dgvLastGoodCheckDB.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
        }

        private DataTable GetLastGoodDBCC()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.LastGoodCheckDB_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }

        private void Status_Selected(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void DgvLastGoodCheckDB_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvLastGoodCheckDB.Rows[idx].DataBoundItem;
                var status = (DBADashStatus.DBADashStatusEnum)row["Status"];

                var r = dgvLastGoodCheckDB.Rows[idx];
                r.Cells["LastGoodCheckDBTime"].SetStatusColor(status);
                r.Cells["DaysSinceLastGoodCheckDB"].SetStatusColor(status);
                r.Cells["Configure"].Style.Font = (string)row["ConfiguredLevel"] == "Database" ? new Font(dgvLastGoodCheckDB.Font, FontStyle.Bold) : new Font(dgvLastGoodCheckDB.Font, FontStyle.Regular);
            }
        }

        private void DgvLastGoodCheckDB_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgvLastGoodCheckDB.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    var row = (DataRowView)dgvLastGoodCheckDB.Rows[e.RowIndex].DataBoundItem;
                    ConfigureThresholds((int)row["InstanceID"], (int)row["DatabaseID"]);
                }
            }
        }

        public void ConfigureThresholds(int InstanceID, int DatabaseID)
        {
            var frm = new LastGoodCheckDBConfig();
            var threshold = LastGoodCheckDBThreshold.GetLastGoodCheckDBThreshold(InstanceID, DatabaseID);
            frm.Threshold = threshold;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvLastGoodCheckDB);
            Configure.Visible = true;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.PromptSaveDataGridView(ref dgvLastGoodCheckDB);
            Configure.Visible = true;
        }

        private void LastGoodCheckDB_Load(object sender, EventArgs e)
        {
            dgvLastGoodCheckDB.ApplyTheme();
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            await CollectionMessaging.TriggerCollection(CurrentContext.InstanceID, new List<CollectionType>() { CollectionType.LastGoodCheckDB}, this);
        }
    }
}