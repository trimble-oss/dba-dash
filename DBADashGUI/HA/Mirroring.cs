using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.HA
{
    public partial class Mirroring : UserControl, ISetContext
    {
        public Mirroring()
        {
            InitializeComponent();
        }

        private List<int> InstanceIDs;

        public bool SummaryMode
        {
            get => tsDetailSummary.Text != "Summary";
            set => tsDetailSummary.Text = value ? "Detail" : "Summary";
        }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        private void RefreshData()
        {
            if (SummaryMode)
            {
                var dt = GetMirroringSummary();
                dgv.DataSource = null;
                dgv.Columns.Clear();
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name = "Instance", Frozen = Common.FreezeKeyColumn });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Principal Count", DataPropertyName = "PrincipalCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Mirror Count", DataPropertyName = "MirrorCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Synchronized Count", DataPropertyName = "SynchronizedCount", Name = "SynchronizedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Suspended Count", DataPropertyName = "SuspendedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Disconnected Count", DataPropertyName = "DisconnectedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Synchronizing Count", DataPropertyName = "SynchronizingCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Pending Failover Count", DataPropertyName = "PendingFailoverCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Not Synchronized Count", DataPropertyName = "NotSynchronizedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Witness Unknown Count", DataPropertyName = "WitnessUnknownCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Witness Connected Count", DataPropertyName = "WitnessConnectedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Witness Disconnected Count", DataPropertyName = "WitnessDisconnectedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Age (min)", DataPropertyName = "SnapshotAge", Name = "SnapshotAge" });
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            else
            {
                var dt = GetMirroringDetail();
                dgv.DataSource = null;
                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = true;
                dgv.DataSource = dt;
                dgv.Columns.Remove("CollectionDateStatus");
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private DataTable GetMirroringSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseMirroringSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private DataTable GetMirroringDetail()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseMirroring_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsDetailSummary_Click(object sender, EventArgs e)
        {
            SummaryMode = !SummaryMode;
            RefreshData();
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            bool isSummary = SummaryMode;
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["CollectionDateStatus"];
                dgv.Rows[idx].Cells["SnapshotAge"].SetStatusColor(snapshotStatus);
                if (isSummary)
                {
                    var mirroringStatus = (DBADashStatus.DBADashStatusEnum)(row["MirroringStatus"] == DBNull.Value ? 3 : Convert.ToInt16(row["MirroringStatus"]));
                    dgv.Rows[idx].Cells["SynchronizedCount"].SetStatusColor(mirroringStatus);
                }
                else
                {
                    var mirroringState = row["mirroring_state"] == DBNull.Value ? short.MinValue : Convert.ToInt16(row["mirroring_state"]);
                    var witnessState = row["mirroring_witness_state"] == DBNull.Value ? short.MinValue : Convert.ToInt16(row["mirroring_witness_state"]);

                    var mirroringStateStatus = mirroringState switch
                    {
                        4 or 6 => DBADashStatusEnum.OK,
                        2 => DBADashStatusEnum.Warning,
                        _ => DBADashStatusEnum.Critical
                    };

                    var witnessStatus = witnessState switch
                    {
                        1 => DBADashStatusEnum.OK,
                        2 => DBADashStatusEnum.Critical,
                        _ => DBADashStatusEnum.NA
                    };
                    dgv.Rows[idx].Cells["mirroring_witness_state"].SetStatusColor(witnessStatus);
                    dgv.Rows[idx].Cells["mirroring_state"].SetStatusColor(mirroringStateStatus);
                }
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }
}