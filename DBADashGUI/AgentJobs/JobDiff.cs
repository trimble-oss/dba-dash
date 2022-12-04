using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class JobDiff : Form
    {
        public JobDiff()
        {
            InitializeComponent();
        }

        public int InstanceID_A { get; set; }
        public int InstanceID_B { get; set; }

        DataTable GetJobDiff()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Job_Diff", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID_A", InstanceID_A);
                cmd.Parameters.AddWithValue("InstanceID_B", InstanceID_B);
                var dt = new DataTable();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        private void JobDiff_Load(object sender, EventArgs e)
        {
            var dt = CommonData.GetInstances(default, default, false);
            foreach (DataRow row in dt.Rows)
            {
                var a = new InstanceItem() { Instance = (string)row["InstanceGroupName"], InstanceID = (Int32)row["InstanceID"] };
                var b = new InstanceItem() { Instance = (string)row["InstanceGroupName"], InstanceID = (Int32)row["InstanceID"] };
                cboA.Items.Add(a);
                cboB.Items.Add(b);
                if (InstanceID_A == a.InstanceID)
                {
                    cboA.SelectedItem = a;
                }
                if (InstanceID_B == b.InstanceID)
                {
                    cboB.SelectedItem = b;
                }
            }
        }

        private void CboA_SelectedIndexChanged(object sender, EventArgs e)
        {
            InstanceID_A = ((InstanceItem)cboA.SelectedItem).InstanceID;
        }

        class InstanceItem
        {
            public int InstanceID { get; set; }
            public string Instance { get; set; }
            public override string ToString()
            {
                return Instance;
            }
        }

        private void BttnCompare_Click(object sender, EventArgs e)
        {
            var diff = GetJobDiff();
            dgvJobs.AutoGenerateColumns = false;
            dgvJobs.DataSource = diff;
            dgvJobs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void CboB_SelectedIndexChanged(object sender, EventArgs e)
        {
            InstanceID_B = ((InstanceItem)cboB.SelectedItem).InstanceID;
        }

        private void DgvJobs_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvJobs.SelectedRows.Count == 1)
            {
                var row = (DataRowView)dgvJobs.SelectedRows[0].DataBoundItem;
                long DDLID_A = row["DDLID_A"] == DBNull.Value ? -1 : (long)row["DDLID_A"];
                long DDLID_B = row["DDLID_B"] == DBNull.Value ? -1 : (long)row["DDLID_B"];

                string A = DDLID_A > 0 ? Common.DDL(DDLID_A) : "";
                string B = DDLID_B > 0 ? Common.DDL(DDLID_B) : "";

                diffControl1.OldText = A;
                diffControl1.NewText = B;
                diffControl1.Mode = DiffControl.ViewMode.Diff;
            }
        }
    }
}
