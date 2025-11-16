using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class JobDiff : Form, IThemedControl
    {
        public JobDiff()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID_A { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID_B { get; set; }

        private DataTable GetJobDiff()
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
                var a = new InstanceItem() { Instance = (string)row["InstanceGroupName"], InstanceID = (int)row["InstanceID"] };
                var b = new InstanceItem() { Instance = (string)row["InstanceGroupName"], InstanceID = (int)row["InstanceID"] };
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
            if (cboA.SelectedItem != null) InstanceID_A = ((InstanceItem)cboA.SelectedItem).InstanceID;
        }

        private class InstanceItem
        {
            public int InstanceID { get; init; }
            public string Instance { get; init; }

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
            InstanceID_B = (((InstanceItem)cboB.SelectedItem)!).InstanceID;
        }

        private void DgvJobs_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvJobs.SelectedRows.Count != 1) return;
            var row = (DataRowView)dgvJobs.SelectedRows[0].DataBoundItem;
            var DDLID_A = row["DDLID_A"] == DBNull.Value ? -1 : (long)row["DDLID_A"];
            var DDLID_B = row["DDLID_B"] == DBNull.Value ? -1 : (long)row["DDLID_B"];

            var A = DDLID_A > 0 ? Common.DDL(DDLID_A) : "";
            var B = DDLID_B > 0 ? Common.DDL(DDLID_B) : "";

            diffControl1.OldText = A;
            diffControl1.NewText = B;
            diffControl1.Mode = DiffControl.ViewMode.Diff;
        }

        public void ApplyTheme(BaseTheme theme)
        {
            foreach (Control child in Controls)
            {
                child.ApplyTheme(theme);
            }
            panel1.BackColor = theme.PanelBackColor;
            panel1.ForeColor = theme.ForegroundColor;
        }
    }
}