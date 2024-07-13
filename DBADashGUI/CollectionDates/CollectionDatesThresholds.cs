using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionDatesThresholds : Form
    {
        public CollectionDatesThresholds()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public int InstanceID { get; set; }

        private string _reference;

        public string Reference
        { get => _reference;
            set => _reference = value;
        }

        public bool Inherit
        {
            get => optInherit.Checked; set => optInherit.Checked = value;
        }

        public int WarningThreshold
        {
            get => (int)numWarning.Value; set => numWarning.Value = value;
        }

        public int CriticalThreshold
        {
            get => (int)numCritical.Value; set => numCritical.Value = value;
        }

        public bool Disabled
        {
            get => OptDisabled.Checked; set => OptDisabled.Checked = value;
        }

        private void GetThreshold()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("CollectionDatesThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var reference = Convert.ToString(rdr["Reference"]);

                if (reference == _reference)
                {
                    chkReferences.Items.Add(reference!, CheckState.Checked);
                    if (rdr["WarningThreshold"] != DBNull.Value && rdr["CriticalThreshold"] != DBNull.Value)
                    {
                        WarningThreshold = (int)rdr["WarningThreshold"];
                        CriticalThreshold = (int)rdr["CriticalThreshold"];
                        optEnabled.Checked = true;
                    }
                    else
                    {
                        OptDisabled.Checked = true;
                    }
                    if (Convert.ToBoolean(rdr["Inherited"]))
                    {
                        optInherit.Checked = true;
                    }
                }
                else
                {
                    chkReferences.Items.Add(reference!, CheckState.Unchecked);
                }
            }

            optInherit.Enabled = InstanceID > 0;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            foreach (string itm in chkReferences.CheckedItems)
            {
                Update(itm);
            }
        }

        private void Update(string reference)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("CollectionDatesThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("Reference", reference);
                if (OptDisabled.Checked)
                {
                    cmd.Parameters.AddWithValue("WarningThreshold", DBNull.Value);
                    cmd.Parameters.AddWithValue("CriticalThreshold", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("WarningThreshold", WarningThreshold);
                    cmd.Parameters.AddWithValue("CriticalThreshold", CriticalThreshold);
                }
                cmd.Parameters.AddWithValue("Inherit", Inherit);
                cmd.ExecuteNonQuery();
                DialogResult = DialogResult.OK;
            }
        }

        private void CollectionDatesThresholds_Load(object sender, EventArgs e)
        {
            chkCheckAll.Enabled = InstanceID != -1;
            if (InstanceID == -1)
            {
                Text += " (Root)";
            }
            else
            {
                Text += " (Instance)";
            }
            GetThreshold();
        }

        private void OptInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void OptEnabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = true;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void ChkCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            for (var i = 0; i < chkReferences.Items.Count; i++)
            {
                chkReferences.SetItemChecked(i, chkCheckAll.Checked);
            }
        }
    }
}