using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            dgvCollectionDates.RegisterClearFilter(tsClearFilter);
            dgvCollectionDates.GridFilterChanged += ((_, _) => PersistFilter = dgvCollectionDates.RowFilter);
        }

        private string PersistFilter;
        private string PersistSort;

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
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CollectionDates_Get", cn) { CommandType = CommandType.StoredProcedure };
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

        private DBADashContext CurrentContext;

        public void SetContext(DBADashContext _context)
        {
            if (CurrentContext == _context) return;
            CurrentContext = _context;
            if (InstanceIDs == _context.InstanceIDs.ToList()) return;
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
            dgvCollectionDates.DataSource = new DataView(dt, PersistFilter, PersistSort, DataViewRowState.CurrentRows);
            dgvCollectionDates.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            UseWaitCursor = false;
            dgvCollectionDates.Columns["colRun"]!.Visible = DBADashUser.AllowMessaging && dt.Rows.Cast<DataRow>().Any(r => r.Field<bool>("MessagingEnabled"));
            tsTriggerMenu.Visible = DBADashUser.AllowMessaging && dt.Rows.Cast<DataRow>().Any(r => r.Field<bool>("MessagingEnabled"));
            tsTriggerWarningAndCritical.Enabled = dt.Rows.Cast<DataRow>().Any(r => r.Field<bool>("MessagingEnabled") && r["Status"] is 1 or 2);
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
            if (e.RowIndex < 0) return;
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
                PersistFilter = dgvCollectionDates.RowFilter;
                PersistSort = dgvCollectionDates.SortString;
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

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[idx].DataBoundItem;
                if (row == null) continue;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];

                dgvCollectionDates.Rows[idx].Cells["SnapshotAge"].SetStatusColor(Status);

                dgvCollectionDates.Rows[idx].Cells["Configure"].Style.Font = (string)row["ConfiguredLevel"] == "Instance" ? new Font(dgvCollectionDates.Font, FontStyle.Bold) : new Font(dgvCollectionDates.Font, FontStyle.Regular);
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            PersistFilter = dgvCollectionDates.RowFilter;
            PersistSort = dgvCollectionDates.SortString;
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            ConfigureRoot.Visible = false;
            dgvCollectionDates.CopyGrid();
            Configure.Visible = true;
            ConfigureRoot.Visible = true;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvCollectionDates.ExportToExcel();
        }

        private async void tsTriggerWarningAndCritical_Click(object sender, EventArgs e)
        {
            RefreshData();
            var rows = dgvCollectionDates.Rows.Cast<DataGridViewRow>().Select(r => ((DataRowView)r.DataBoundItem).Row).Where(row => row.Field<int>("Status") is 1 or 2 && CanRowBeTriggered(row)).ToList();
            await TriggerCollections(rows);
        }

        private static bool CanRowBeTriggered(DataRow row)
        {
            return row.Field<bool>("MessagingEnabled") && !NoTriggerCollectionTypes.Any(r =>
                string.Equals(r, row.Field<string>("Reference"), StringComparison.OrdinalIgnoreCase));
        }

        private async Task TriggerCollections(List<DataRow> rows)
        {
            var filteredAndGrouped = rows.Where(CanRowBeTriggered)
                .GroupBy(row => row.Field<string>("ConnectionID"))
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        References = group.Select(row => row.Field<string>("Reference")).ToList(),
                        ImportAgentID = group.First().Field<int>("ImportAgentID"),
                        CollectionAgentID = group.First().Field<int>("CollectAgentID")
                    }
                );
            if (rows.Count == 0)
            {
                MessageBox.Show("Nothing to trigger.", "Trigger Collections", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            if (rows.Count > Config.CollectionTriggerLimit)
            {
                MessageBox.Show(
                    $"Unable to trigger {rows.Count} collections.  Collection Trigger limit is set to {Config.CollectionTriggerLimit}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show($"Trigger {rows.Count} collection(s) across {filteredAndGrouped.Count} instance(s)?", "Trigger Collections", MessageBoxButtons.YesNo,
                 rows.Count > Config.CollectionTriggerWarningLimit ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes) return;
            PersistFilter = dgvCollectionDates.RowFilter;
            PersistSort = dgvCollectionDates.SortString;
            foreach (var group in filteredAndGrouped)
            {
                try
                {
                    await CollectionMessaging.TriggerCollection(group.Key, group.Value.References,
                        group.Value.CollectionAgentID, group.Value.ImportAgentID, this);
                }
                catch (Exception ex)
                {
                    SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                }
            }
        }

        private async void tsTriggerSelected_Click(object sender, EventArgs e)
        {
            var rows = dgvCollectionDates.SelectedCells.Cast<DataGridViewCell>().Select(cell => ((DataRowView)cell.OwningRow.DataBoundItem).Row).Where(CanRowBeTriggered).Distinct();
            await TriggerCollections(rows.ToList());
        }

        private async void tsTriggerAll_Click(object sender, EventArgs e)
        {
            var rows = dgvCollectionDates.Rows.Cast<DataGridViewRow>().Select(r => ((DataRowView)r.DataBoundItem).Row).Where(CanRowBeTriggered).ToList();
            await TriggerCollections(rows);
        }
    }
}