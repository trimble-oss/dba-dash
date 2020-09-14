using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBAChecksGUI.CollectionDates
{
    public partial class CollectionDatesThresholds : Form
    {
        public CollectionDatesThresholds()
        {
            InitializeComponent();
        }


        public Int32 InstanceID { get; set; }
        public string Reference { get { return txtReference.Text; } set { txtReference.Text = value; } }

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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("CollectionDatesThresholds_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("Reference", Reference);
                cmd.CommandType = CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
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
                    if ((Int32)rdr["InstanceID"] != InstanceID)
                    {
                        optInherit.Checked = true;
                    }
                    
                }
                else
                {
                    OptDisabled.Checked = true;
                }
                optInherit.Enabled = InstanceID > 0;
            }

        }



        public string ConnectionString { get; set; }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("CollectionDatesThresholds_Upd", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("Reference", Reference);
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
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
                this.DialogResult = DialogResult.OK;
            }
        }

        private void CollectionDatesThresholds_Load(object sender, EventArgs e)
        {
            if (InstanceID == -1)
            {
                this.Text += " (Root)";
                getThreshold();

            }
            else
            {
                this.Text += " (Instance)";
            }
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
    }
}
