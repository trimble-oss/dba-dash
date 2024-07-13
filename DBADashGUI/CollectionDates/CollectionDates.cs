using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;
using DBADash;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionDates : UserControl, ISetContext, ISetStatus
    {
        public CollectionDates()
        {
            InitializeComponent();
            dgvCollectionDates.ApplyTheme();
        }

        private List<int> InstanceIDs { get; set; }

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

        private static readonly string[] NoTriggerCollectionTypes = new[] { "QueryPlans", "QueryText", "SlowQueriesStats", "InternalPerformanceCounters", "SessionWaits" };

        private DataTable GetCollectionDates()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CollectionDates_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.InstanceIDs.ToList();
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = _context.InstanceID > 0;
            IncludeOK = _context.InstanceID > 0;
            lblStatus.Visible = false;
            RefreshData();
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(RefreshData);
                return;
            }
            UseWaitCursor = true;
            DataTable dt = GetCollectionDates();
            dgvCollectionDates.AutoGenerateColumns = false;
            dgvCollectionDates.DataSource = new DataView(dt);
            dgvCollectionDates.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            UseWaitCursor = false;
            dgvCollectionDates.Columns["colRun"]!.Visible = DBADashUser.AllowMessaging && dt.Rows.Cast<DataRow>().Any(r => r["MessagingEnabled"] != DBNull.Value && (bool)r["MessagingEnabled"]);
            tsTrigger.Visible = DBADashUser.AllowMessaging && dt.Rows.Cast<DataRow>().Any(r => r["MessagingEnabled"] != DBNull.Value && (bool)r["MessagingEnabled"] && r["Status"] is 1 or 2);
        }

        private void Status_Selected(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void ConfigureThresholds(int InstanceID, DataRowView row)
        {
            using var frm = new CollectionDatesThresholds
            {
                InstanceID = InstanceID
            };
            if (row["WarningThreshold"] == DBNull.Value || row["CriticalThreshold"] == DBNull.Value)
            {
                frm.Disabled = true;
            }
            else
            {
                frm.WarningThreshold = (int)row["WarningThreshold"];
                frm.CriticalThreshold = (int)row["CriticalThreshold"];
            }
            if ((string)row["ConfiguredLevel"] != "Instance")
            {
                frm.Inherit = true;
            }
            frm.Reference = (string)row["Reference"];
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private async void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[e.RowIndex].DataBoundItem;
                if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Configure Instance")
                {
                    var InstanceID = (int)row["InstanceID"];
                    ConfigureThresholds(InstanceID, row);
                }
                else if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Configure Root")
                {
                    ConfigureThresholds(-1, row);
                }
                else if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Run")
                {
                    var instance = (string)row["ConnectionID"];
                    var importAgentID = (int)row["ImportAgentID"];
                    var collectAgentID = (int)row["CollectAgentID"];
                    var supportsMessaging = (bool)row["MessagingEnabled"];
                    if (!supportsMessaging)
                    {
                        SetStatus("The associated DBA Dash service is not enabled for messaging", null, DashColors.Fail);
                        return;
                    }

                    var collection = (string)row["Reference"];
                    if (NoTriggerCollectionTypes.Contains(collection, StringComparer.OrdinalIgnoreCase))
                    {
                        SetStatus("This collection type cannot be triggered manually", null, DashColors.Warning);
                        return;
                    }
                    await CollectionMessaging.TriggerCollection(instance, collection, collectAgentID, importAgentID, this);
                }
            }
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[idx].DataBoundItem;
                if (row != null)
                {
                    var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];

                    dgvCollectionDates.Rows[idx].Cells["SnapshotAge"].SetStatusColor(Status);

                    dgvCollectionDates.Rows[idx].Cells["Configure"].Style.Font = (string)row["ConfiguredLevel"] == "Instance" ? new Font(dgvCollectionDates.Font, FontStyle.Bold) : new Font(dgvCollectionDates.Font, FontStyle.Regular);
                }
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            ConfigureRoot.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvCollectionDates);
            Configure.Visible = true;
            ConfigureRoot.Visible = true;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvCollectionDates);
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            RefreshData();
            var dt = (dgvCollectionDates.DataSource as DataView)?.Table;
            if (dt == null) return;
            List<CollectionType> collectionTypes = new();
            foreach (var row in dt.Rows.Cast<DataRow>().Where(r => r["Status"] is 1 or 2 && (bool)r["MessagingEnabled"]).GroupBy((r => r["ConnectionID"])))
            {
                var instance = (string)row.Key;
                await TriggerCriticalAndWarningForConnectionID(instance, dt);
            }
        }

        private async Task TriggerCriticalAndWarningForConnectionID(string connectionID, DataTable dt)
        {
            List<string> collectionTypes = new();
            var importAgentID = 0;
            var collectAgentID = 0;
            foreach (var row in dt.Rows.Cast<DataRow>()
                         .Where(r => r["Status"] is 1 or 2
                                     && (bool)r["MessagingEnabled"]
                                     && string.Equals((string)r["ConnectionID"], connectionID, StringComparison.OrdinalIgnoreCase)
                                     && !NoTriggerCollectionTypes.Any(s => string.Equals((string)r["Reference"], s, StringComparison.OrdinalIgnoreCase)))
                         .OrderBy(r => r["ConnectionID"]))
            {
                importAgentID = (int)row["ImportAgentID"];
                collectAgentID = (int)row["CollectAgentID"];
                collectionTypes.Add((string)row["Reference"]);
            }
            if (importAgentID == 0 || collectionTypes.Count == 0) return;
            try
            {
                await CollectionMessaging.TriggerCollection(connectionID, collectionTypes, collectAgentID, importAgentID, this);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
        }
    }
}