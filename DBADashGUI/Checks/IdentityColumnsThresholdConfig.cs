﻿using System;
using System.Data;
using System.Windows.Forms;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.Checks
{
    public partial class IdentityColumnsThresholdConfig : Form
    {
        public IdentityColumnsThresholdConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public string ObjectName { get; set; }

        private void GetThresholds()
        {
            using SqlConnection cn = new(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.IdentityColumnThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("object_name", ObjectName);
            cn.Open();
            using SqlDataReader rdr = cmd.ExecuteReader();
            if (!rdr.Read())
            {
                chkInherit.Checked = chkInherit.Enabled;
                return;
            }
            chkInherit.Checked = rdr.GetBoolean("IsInherited");
            lblObject.Text = rdr.GetString("SelectedObject");
            Warning = GetThresholdValue(rdr, "PctUsedWarningThreshold");
            Critical = GetThresholdValue(rdr, "PctUsedCriticalThreshold");

            chkInherit.Enabled = !(InstanceID == -1 && DatabaseID == -1 && ObjectName == "");
            bttnUp.Enabled= !(InstanceID == -1 && DatabaseID == -1 && ObjectName == "");
        }

        private decimal? Warning
        {
            get=> chkWarning.Checked ? numWarning.Value / 100m : null;
            set
            {
                chkWarning.Checked = value is >= 0m and <= 1m;
                numWarning.Value = value is >= 0m and <= 1m ? (decimal)value * 100m : 0;
            }
        }
        private decimal? Critical
        {
            get => chkCritical.Checked ? numCritical.Value / 100m : null;
            set
            {
                chkCritical.Checked = value is >= 0m and <= 1m;
                numCritical.Value = value is >= 0m and <= 1m ? (decimal)value * 100m : 0;
            }
        }


        private void UpdateThresholds()
        {
            using SqlConnection cn = new(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.IdentityColumnThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("object_name", ObjectName);
            cmd.Parameters.AddWithValue("PctUsedWarningThreshold", Warning==null ? DBNull.Value : (decimal)Warning);
            cmd.Parameters.AddWithValue("PctUsedCriticalThreshold", Critical==null ? DBNull.Value : (decimal)Critical);
            cmd.Parameters.AddWithValue("Inherit", chkInherit.Checked);
            cn.Open();  
            cmd.ExecuteNonQuery();
        }


        private void ChkWarning_CheckedChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = chkWarning.Checked;
            numWarning.Value = chkWarning.Checked ? 50 : 0;
        }

        private void ChkCritical_CheckedChanged(object sender, EventArgs e)
        {
            numCritical.Enabled = chkCritical.Checked;
            numCritical.Value = chkCritical.Checked ? 80 : 0;
        }

        private static decimal GetThresholdValue(SqlDataReader rdr, string columnName)
        {
            var value = rdr[columnName];
            return value == DBNull.Value ? 999 : (decimal)value;
        }

        private void IdentityColumnsThresholdConfig_Load(object sender, EventArgs e)
        {
            GetThresholds();
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateThresholds();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Error updating thresholds:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult= DialogResult.Cancel;
        }

        private void ChkInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlConfig.Enabled=!chkInherit.Checked;
        }

        private void BttnUp_Click(object sender, EventArgs e)
        {
            if(ObjectName!="")
            {
                ObjectName = "";
            }
            else if (DatabaseID!=-1)
            {
                DatabaseID = -1;
            }
            else if (InstanceID !=-1)
            {
                InstanceID = -1;
            }
            GetThresholds();
        }
    }
}
