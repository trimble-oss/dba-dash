using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionDates : UserControl, ISetContext
    {
        public CollectionDates()
        {
            InitializeComponent();
            Common.StyleGrid(ref dgvCollectionDates);
        }

        private List<Int32> InstanceIDs { get; set; }

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

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = context.InstanceID > 0;
            IncludeOK = context.InstanceID > 0;
            RefreshData();
        }

        public void RefreshData()
        {
            UseWaitCursor = true;
            DataTable dt = GetCollectionDates();
            dgvCollectionDates.AutoGenerateColumns = false;
            dgvCollectionDates.DataSource = new DataView(dt);
            dgvCollectionDates.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            UseWaitCursor = false;
        }

        private void Status_Selected(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void ConfigureThresholds(Int32 InstanceID, DataRowView row)
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
                frm.WarningThreshold = (Int32)row["WarningThreshold"];
                frm.CriticalThreshold = (Int32)row["CriticalThreshold"];
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

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[e.RowIndex].DataBoundItem;
                if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Configure Instance")
                {
                    var InstanceID = (Int32)row["InstanceID"];
                    ConfigureThresholds(InstanceID, row);
                }
                else if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Configure Root")
                {
                    ConfigureThresholds(-1, row);
                }
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[idx].DataBoundItem;
                if (row != null)
                {
                    var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];

                    dgvCollectionDates.Rows[idx].Cells["SnapshotAge"].SetStatusColor(Status);

                    if ((string)row["ConfiguredLevel"] == "Instance")
                    {
                        dgvCollectionDates.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvCollectionDates.Font, FontStyle.Bold);
                    }
                    else
                    {
                        dgvCollectionDates.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvCollectionDates.Font, FontStyle.Regular);
                    }
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
    }
}