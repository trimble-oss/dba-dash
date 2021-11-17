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
using Humanizer;
namespace DBADashGUI.Checks
{
    public partial class UptimeThresholdConfig : Form
    {
        public UptimeThresholdConfig()
        {
            InitializeComponent();
        }

        public int InstanceID { get; set; }


        private void getThreshold()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceUptimeThresholds_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        chkInherit.Checked = Convert.ToBoolean(rdr["IsInherited"]);
                        this.Text += " " + Convert.ToString(rdr["Instance"]);
                        if(rdr["WarningThreshold"] == DBNull.Value || rdr["CriticalThreshold"] == DBNull.Value)
                        {
                            chkEnabled.Checked = false;
                        }
                        else
                        {
                            chkEnabled.Checked = true;
                            numCritical.Value = Convert.ToInt32(rdr["CriticalThreshold"]);
                            numWarning.Value = Convert.ToInt32(rdr["WarningThreshold"]);
                        }
                        if (rdr["sqlserver_start_time"] != DBNull.Value)
                        {
                            DateTime startTimeUtc = Convert.ToDateTime(rdr["sqlserver_start_time_utc"]);
                            DateTime startTimeServer = Convert.ToDateTime(rdr["sqlserver_start_time"]);
                            DateTime ackDate = Convert.ToDateTime(rdr["UptimeAckDate"]==DBNull.Value ? DateTime.MinValue : rdr["UptimeAckDate"]);

                            lblStartTime.Text = "Start Time: " + startTimeUtc.ToLocalTime();
                            if (startTimeUtc.ToLocalTime() != startTimeServer)
                            {
                                lblStartTime.Text += " (" + startTimeServer.ToString() + " on server)";
                            }
                            lblUptime.Text = "Uptime: " + DateTime.UtcNow.Subtract(startTimeUtc).Humanize(3);
                           if(ackDate> startTimeUtc)
                            {
                                lblUptime.Text += " (Acknowledged)";
                                bttnClearAlert.Enabled = false;
                            }
                        }
                    }
                    else
                    {
                        chkInherit.Checked = true;
                    }
                }
            }
        }

        private void chkInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = !chkInherit.Checked;
        }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = chkEnabled.Checked;
            numCritical.Enabled = chkEnabled.Checked;
        }

        private void UptimeThresholdConfig_Load(object sender, EventArgs e)
        {           
            getThreshold();
            lnkConfigureRoot.Visible = InstanceID != -1;
            chkInherit.Enabled = InstanceID != -1;
            bttnClearAlert.Text = InstanceID == -1 ? "Clear ALL Alerts" : "Clear Alert";
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceUptimeThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("Inherit", chkInherit.Checked);
                if(!chkInherit.Checked && chkEnabled.Checked)
                {
                    cmd.Parameters.AddWithValue("WarningThreshold", numWarning.Value);
                    cmd.Parameters.AddWithValue("CriticalThreshold", numCritical.Value);
                }
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Threshold updated", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void chkConfigureRoot_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var frm = new UptimeThresholdConfig() { InstanceID = -1 })
            {
                frm.ShowDialog();
                if(frm.DialogResult== DialogResult.OK)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void bttnClearAlert_Click(object sender, EventArgs e)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceUptimeAck", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);            
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Alert cleared", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
