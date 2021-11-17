using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionDatesThresholds : Form
    {
        public CollectionDatesThresholds()
        {
            InitializeComponent();
        }


        public Int32 InstanceID { get; set; }

        private string _reference;
        public string Reference { get { return _reference; } set { _reference = value; } }

        public bool Inherit
        {
            get
            {
                return optInherit.Checked;
            }
            set
            {
                optInherit.Checked = value;
            }
        }

        public Int32 WarningThreshold
        {
            get
            {
                return (Int32)numWarning.Value;
            }
            set
            {
                numWarning.Value = value;
            }
        }
        public Int32 CriticalThreshold
        {
            get
            {
                return (Int32)numCritical.Value;
            }
            set
            {
                numCritical.Value = value;
            }
        }

        public bool Disabled
        {
            get
            {
                return OptDisabled.Checked;
            }
            set
            {
               OptDisabled.Checked = value;
            }
        }

        private void getThreshold()
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("CollectionDatesThresholds_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                    cn.Open();
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string reference = Convert.ToString(rdr["Reference"]);

                        if (reference == _reference)
                        {
                            chkReferences.Items.Add(reference, CheckState.Checked);
                            if (rdr["WarningThreshold"] != DBNull.Value && rdr["CriticalThreshold"] != DBNull.Value)
                            {
                                WarningThreshold = (Int32)rdr["WarningThreshold"];
                                CriticalThreshold = (Int32)rdr["CriticalThreshold"];
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
                            chkReferences.Items.Add(reference, CheckState.Unchecked);
                        }

                    } 

                    optInherit.Enabled = InstanceID > 0;
                }
            }

        }



        public string ConnectionString { get; set; }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            foreach(string itm in chkReferences.CheckedItems)
            {
                update(itm);
            }
        }

        private void update(string reference)
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("CollectionDatesThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
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
                    this.DialogResult = DialogResult.OK;
                }
            }
        }

        private void CollectionDatesThresholds_Load(object sender, EventArgs e)
        {
            chkCheckAll.Enabled = InstanceID!=-1;
            if (InstanceID == -1)
            {
                this.Text += " (Root)";              
            }
            else
            {
                this.Text += " (Instance)";
            }
            getThreshold();
        }

        private void optInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void optEnabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = true;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void chkCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            for(int i =0;i<chkReferences.Items.Count;i++)
            {
                chkReferences.SetItemChecked(i, chkCheckAll.Checked);
            }
        }
    }
}
