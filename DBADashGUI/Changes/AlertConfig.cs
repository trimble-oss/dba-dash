using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class AlertConfig : Form
    {
        public AlertConfig()
        {
            InitializeComponent();
        }

        private DataRow row;

        public DataRow AlertRow
        {
            set
            {
                row = value;
                numNotificationPeriodHrs.Value = (short)value["ActualNotificationPeriodHrs"];
                chkDefault.Checked = value["NotificationPeriodHrs"] == DBNull.Value;
                if (value["AlertLevel"] == DBNull.Value)
                {
                    optDefault.Checked = true;
                }
                else switch (Convert.ToInt16(value["AlertLevel"]))
                {
                    case 1:
                        optCritical.Checked = true;
                        break;
                    case 2:
                        optWarning.Checked = true;
                        break;
                    default:
                        optNA.Checked = true;
                        break;
                }
                optDefault.Text = Convert.ToInt16(value["DefaultAlertLevel"]) == 1 ? "Default (Critical)" : "Default (Warning)";
            }
        }

        private int InstanceID => (int)row["InstanceID"];
        private int Id => (int)row["id"];

        private short? NotificationPeriodHrs => chkDefault.Checked ? null : Convert.ToInt16(numNotificationPeriodHrs.Value);

        private short? AlertLevel
        {
            get
            {
                if (optCritical.Checked)
                {
                    return 1;
                }
                else if (optWarning.Checked)
                {
                    return 2;
                }
                else if (optNA.Checked)
                {
                    return 3;
                }
                else
                {
                    return null;
                }
            }
        }

        private void ChkDefault_CheckedChanged(object sender, EventArgs e)
        {
            numNotificationPeriodHrs.Enabled = !chkDefault.Checked;
            if (chkDefault.Checked)
            {
                numNotificationPeriodHrs.Value = (int)row["DefaultNotificationPeriodHrs"];
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            UpdateAlertThresholds(InstanceID, Id, AlertLevel, NotificationPeriodHrs);
            this.DialogResult = DialogResult.OK;
        }

        private static void UpdateAlertThresholds(int InstanceID, int id, short? alertLevel, short? notificationPeriodHrs)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.AlertThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithNullableValue("AlertLevel", alertLevel);
                cmd.Parameters.AddWithNullableValue("NotificationPeriodHrs", notificationPeriodHrs);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
