using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace DBAChecksConfigTool
{

    public partial class DBAChecksConfigTool : Form
    {
        public DBAChecksConfigTool()
        {
            InitializeComponent();
        }

        private void DBAChecksConfigTool_Load(object sender, EventArgs e)
        {
            addInstances();
            GetDriveThresholds();
            GetBackupThresholds();
            GetLRThresholds();
        }
        private void addInstances()
        {

            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.Instances_Get", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            using (cn)
            {
                cn.Open();
                var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                cboInstances.DataSource = dt;
                cboInstances.DisplayMember = "ConnectionID";
                cboInstances.ValueMember = "InstanceID";
                var dtDriveInstances = dt.Copy();
                var r = dtDriveInstances.Rows.Add();
                r["InstanceID"] = -1;
                r["Instance"] = "{ALL}";
                cboDrivesInstances.DataSource = dtDriveInstances;
                cboDrivesInstances.DisplayMember = "Instance";
                cboDrivesInstances.ValueMember = "InstanceID";
                var dtBackupinstances = dtDriveInstances.Copy();
                cboBackupInstance.DataSource = dtBackupinstances;
                cboBackupInstance.DisplayMember = "Instance";
                cboBackupInstance.ValueMember = "InstanceID";
                var dtLRinstances = dtDriveInstances.Copy();
                cboLRInstance.DataSource = dtBackupinstances;
                cboLRInstance.DisplayMember = "Instance";
                cboLRInstance.ValueMember = "InstanceID";

            }
            if (cboDrivesInstances.Items.Count > 0)
            {
                cboDrivesInstances.SelectedIndex = 0;
            }
            if (cboInstances.Items.Count > 0)
            {
                cboInstances.SelectedIndex = 0;
            }

        }



        private void chkTags_SelectedIndexChanged(object sender, EventArgs e)
        {


        }

        private void chkTags_ItemCheck(object sender, ItemCheckEventArgs e)
        {

            string tag = chkTags.Items[e.Index].ToString();

            if (e.NewValue == CheckState.Unchecked)
            {
                RemoveTag(tag);
            }
            else
            {
                AddTag(tag);
            }
        }

        private void AddTag(string Tag)
        {

            Int32 instanceID = (Int32)cboInstances.SelectedValue;
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.Tag_Add", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("Tag", Tag);
            using (cn)
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void RemoveTag(string Tag)
        {

            Int32 instanceID = (Int32)cboInstances.SelectedValue;
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.Tag_Remove", cn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("Tag", Tag);
            using (cn)
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void bttAddTag_Click(object sender, EventArgs e)
        {
            AddTag(txtTag.Text);
            refreshTags();
        }

        private void cboInstances_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshTags();
        }

        private void refreshTags()
        {
            chkTags.Items.Clear();

            if (cboInstances.SelectedValue != null)
            {
                Int32 instanceID = (Int32)cboInstances.SelectedValue;
                SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
                var cmd = new SqlCommand("dbo.InstanceTags_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", instanceID);

                using (cn)
                {
                    cn.Open();
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        chkTags.Items.Add((string)rdr["Tag"], (bool)rdr["Checked"]);
                    }
                }
            }
        }

        private void optPercent_CheckedChanged(object sender, EventArgs e)
        {
            if (optPercent.Checked)
            {
                lblDriveCritical.Text = "%";
                lblDriveWarning.Text = "%";
                numDriveCritical.Maximum = 100;
                numDriveWarning.Maximum = 100;
            }
            else
            {
                lblDriveWarning.Text = "GB";
                lblDriveCritical.Text = "GB";
                numDriveCritical.Maximum = Int32.MaxValue;
                numDriveWarning.Maximum = Int32.MaxValue;
            }



        }

        private void GetDriveThresholds()
        {
            Int32 DriveID = (Int32)cboDrives.SelectedValue;
            Int32 InstanceID = (Int32)cboDrivesInstances.SelectedValue;
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.DriveThresholds_Get", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            using (cn)
            {
                cn.Open();
                var da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable("DriveThresholds");
                da.Fill(dt);
                dgvDriveThresholds.DataSource = dt;
                dgvDriveThresholds.Columns["InstanceID"].Visible = false;
                dgvDriveThresholds.Columns["DriveID"].Visible = false;
                dgvDriveThresholds.Columns["DriveWarningThreshold"].HeaderText = "Warning Threshold";
                dgvDriveThresholds.Columns["DriveCriticalThreshold"].HeaderText = "Warning Threshold";
            }
        }

        private void GetBackupThresholds()
        {

            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.BackupThresholds_Get", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            using (cn)
            {
                cn.Open();
                var da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable("BackupThresholds");
                da.Fill(dt);
                dgvBackup.DataSource = dt;
                dgvBackup.Columns["InstanceID"].Visible = false;
                dgvBackup.Columns["DatabaseID"].Visible = false;
                dgvBackup.Columns["LogBackupWarningThreshold"].HeaderText = "Log Warning Threshold";
                dgvBackup.Columns["LogBackupCriticalThreshold"].HeaderText = "Log Critical Threshold";
                dgvBackup.Columns["FullBackupWarningThreshold"].HeaderText = "Full Warning Threshold";
                dgvBackup.Columns["FullBackupCriticalThreshold"].HeaderText = "Full Critical Threshold";
                dgvBackup.Columns["DiffBackupWarningThreshold"].HeaderText = "Diff Warning Threshold";
                dgvBackup.Columns["DiffBackupCriticalThreshold"].HeaderText = "Diff Critical Threshold";
                dgvBackup.Columns["ConsiderPartialBackups"].HeaderText = "Use Partial";
                dgvBackup.Columns["ConsiderFGBackups"].HeaderText = "Use Filegroup";
            }
        }

        private void GetLRThresholds()
        {

            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.LogRestoreThresholds_Get", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            using (cn)
            {
                cn.Open();
                var da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable("LRThresholds");
                da.Fill(dt);
                dgvLR.DataSource = dt;
                dgvLR.Columns["InstanceID"].Visible = false;
                dgvLR.Columns["DatabaseID"].Visible = false;
                dgvLR.Columns["LatencyWarningThreshold"].HeaderText = "Latency Warning Threshold";
                dgvLR.Columns["LatencyCriticalThreshold"].HeaderText = "Latency Critical Threshold";
                dgvLR.Columns["TimeSinceLastWarningThreshold"].HeaderText = "Time Since Last Warning Threshold";
                dgvLR.Columns["TimeSinceLastCriticalThreshold"].HeaderText = "Time Since Last Critical Threshold";

            }
        }

        private void cboDrivesInstances_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshDrives();
        }

        private void refreshDatabases()
        {


            Int32 instanceID = (Int32)cboBackupInstance.SelectedValue;
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.Databases_Get", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", instanceID);

            using (cn)
            {
                cn.Open();
                var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                var r = dt.Rows.Add();
                r["DatabaseID"] = -1;
                r["Name"] = "{ALL}";
                cboBackupDatabase.DataSource = dt;
                cboBackupDatabase.DisplayMember = "Name";
                cboBackupDatabase.ValueMember = "DatabaseID";
                DataTable dtLRDB = dt.Copy();
                cboLRDatabase.DataSource = dtLRDB;
                cboLRDatabase.DisplayMember = "Name";
                cboLRDatabase.ValueMember = "DatabaseID";

            }
            cboLRDatabase.SelectedValue = -1;
            cboBackupDatabase.SelectedValue = -1;
        }


        private void refreshDrives()
        {

            Int32 instanceID = (Int32)cboDrivesInstances.SelectedValue;
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.Drives_Get", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", instanceID);

            using (cn)
            {
                cn.Open();
                var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                var r = dt.Rows.Add();
                r["DriveID"] = -1;
                r["Name"] = "{ALL}";
                cboDrives.DataSource = dt;
                cboDrives.DisplayMember = "Name";
                cboDrives.ValueMember = "DriveID";

            }
            cboDrives.SelectedValue = -1;
        }

        private void dgvDriveThresholds_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dgvDriveThresholds.Columns["DriveWarningThreshold"].Index || e.ColumnIndex == dgvDriveThresholds.Columns["DriveCriticalThreshold"].Index)
            {
                if ((string)dgvDriveThresholds.Rows[e.RowIndex].Cells["DriveCheckType"].Value == "%")
                {
                    e.CellStyle.Format = "P";
                }
                else
                {
                    e.CellStyle.Format = "";
                }
            }
        }

        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Visible = !OptDisabled.Checked;
        }

        private void bttnAddDriveThreshold_Click(object sender, EventArgs e)
        {
            Int32 DriveID = (Int32)cboDrives.SelectedValue;
            Int32 InstanceID = (Int32)cboDrivesInstances.SelectedValue;
            decimal warning = numDriveWarning.Value;
            decimal critical = numDriveCritical.Value;
            char type = 'G';
            if (optPercent.Checked)
            {
                warning = warning / 100;
                critical = critical / 100;
                type = '%';
            }

            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.DriveThresholds_Upd", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("DriveID", DriveID);
            if (OptDisabled.Checked)
            {
                cmd.Parameters.AddWithValue("Warning", DBNull.Value);
                cmd.Parameters.AddWithValue("Critical", DBNull.Value);
                cmd.Parameters.AddWithValue("DriveCheckType", '-');
            }
            else if (optInherit.Checked)
            {
                cmd.Parameters.AddWithValue("Warning", DBNull.Value);
                cmd.Parameters.AddWithValue("Critical", DBNull.Value);
                cmd.Parameters.AddWithValue("DriveCheckType", 'I');
            }
            else
            {
                cmd.Parameters.AddWithValue("Warning", warning);
                cmd.Parameters.AddWithValue("Critical", critical);
                cmd.Parameters.AddWithValue("DriveCheckType", type);
            }

            using (cn)
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            GetDriveThresholds();
        }

        private void dgvDriveThresholds_CurrentCellChaned(object sender, EventArgs e)
        {
            if (dgvDriveThresholds.CurrentCell != null)
            {
                var currentRow = dgvDriveThresholds.CurrentCell.RowIndex;
                decimal critical = 0;
                decimal warning = 0;

                if (dgvDriveThresholds.Rows[currentRow].Cells["DriveCriticalThreshold"].Value != DBNull.Value)
                {
                    critical = (decimal)dgvDriveThresholds.Rows[currentRow].Cells["DriveCriticalThreshold"].Value;
                }
                if (dgvDriveThresholds.Rows[currentRow].Cells["DriveWarningThreshold"].Value != DBNull.Value)
                {
                    warning = (decimal)dgvDriveThresholds.Rows[currentRow].Cells["DriveWarningThreshold"].Value;
                }
                var instanceID = (Int32)dgvDriveThresholds.Rows[currentRow].Cells["InstanceID"].Value;
                var driveID = (Int32)dgvDriveThresholds.Rows[currentRow].Cells["DriveID"].Value;

                string type = dgvDriveThresholds.Rows[currentRow].Cells["DriveCheckType"].Value.ToString();

                cboDrivesInstances.SelectedValue = instanceID;
                cboDrives.SelectedValue = driveID;
                if (type == "%")
                {
                    critical = critical * 100;
                    warning = warning * 100;
                    optPercent.Checked = true;
                }
                else if (type == "G")
                {
                    optGB.Checked = true;
                }
                else
                {
                    OptDisabled.Checked = true;
                }
                numDriveCritical.Value = critical;
                numDriveWarning.Value = warning;
            }
        }

        private void optInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Visible = !optInherit.Checked;
        }

        private void dgvDriveThresholds_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void chkBackupInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlBackupThresholds.Visible = !chkBackupInherit.Checked;
            chkUsePartial.Visible = !chkBackupInherit.Checked;
            chkUseFG.Visible = !chkBackupInherit.Checked;
        }

        private void chkFull_CheckedChanged(object sender, EventArgs e)
        {
            numFullCritical.Enabled = chkFull.Checked;
            numFullWarning.Enabled = chkFull.Checked;
        }

        private void cboBackupInstance_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshDatabases();
        }

        private void dgvBackup_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgvBackup.CurrentCell != null)
            {
                var currentRow = dgvBackup.CurrentCell.RowIndex;
                chkBackupInherit.Checked = false;
                if (dgvBackup.Rows[currentRow].Cells["LogBackupCriticalThreshold"].Value != DBNull.Value && dgvBackup.Rows[currentRow].Cells["LogBackupWarningThreshold"].Value != DBNull.Value)
                {
                    numLogCritical.Value = (Int32)dgvBackup.Rows[currentRow].Cells["LogBackupCriticalThreshold"].Value;
                    numLogWarning.Value = (Int32)dgvBackup.Rows[currentRow].Cells["LogBackupWarningThreshold"].Value;
                    chkLog.Checked = true;
                }
                else
                {
                    chkLog.Checked = false;
                }
                if (dgvBackup.Rows[currentRow].Cells["FullBackupCriticalThreshold"].Value != DBNull.Value && dgvBackup.Rows[currentRow].Cells["FullBackupWarningThreshold"].Value != DBNull.Value)
                {
                    numFullCritical.Value = (Int32)dgvBackup.Rows[currentRow].Cells["FullBackupCriticalThreshold"].Value;
                    numFullWarning.Value = (Int32)dgvBackup.Rows[currentRow].Cells["FullBackupWarningThreshold"].Value;
                    chkFull.Checked = true;
                }
                else
                {
                    chkFull.Checked = false;
                }
                if (dgvBackup.Rows[currentRow].Cells["DiffBackupCriticalThreshold"].Value != DBNull.Value && dgvBackup.Rows[currentRow].Cells["DiffBackupWarningThreshold"].Value != DBNull.Value)
                {
                    numDiffCritical.Value = (Int32)dgvBackup.Rows[currentRow].Cells["DiffBackupCriticalThreshold"].Value;
                    numDiffWarning.Value = (Int32)dgvBackup.Rows[currentRow].Cells["DiffBackupWarningThreshold"].Value;
                    chkDiff.Checked = true;
                }
                else
                {
                    chkDiff.Checked = false;
                }

                var instanceID = (Int32)dgvBackup.Rows[currentRow].Cells["InstanceID"].Value;
                var databaseID = (Int32)dgvBackup.Rows[currentRow].Cells["DatabaseID"].Value;
                chkUseFG.Checked = (bool)dgvBackup.Rows[currentRow].Cells["ConsiderFGBackups"].Value;
                chkUsePartial.Checked = (bool)dgvBackup.Rows[currentRow].Cells["ConsiderPartialBackups"].Value;
                cboBackupInstance.SelectedValue = instanceID;
                cboBackupDatabase.SelectedValue = databaseID;

            }
        }

        private void chkDiff_CheckedChanged(object sender, EventArgs e)
        {
            numDiffCritical.Enabled = chkDiff.Checked;
            numDiffWarning.Enabled = chkDiff.Checked;
        }

        private void chkLog_CheckedChanged(object sender, EventArgs e)
        {
            numLogCritical.Enabled = chkLog.Checked;
            numLogWarning.Enabled = chkLog.Checked;
        }

        private void bttnUpdateBackup_Click(object sender, EventArgs e)
        {
            var instanceID = cboBackupInstance.SelectedValue;
            var databaseID = cboBackupDatabase.SelectedValue;
            if (databaseID == null)
            {
                databaseID = -1;
            }
            if (instanceID == null)
            {
                instanceID = -1;
            }
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.BackupThresholds_Upd", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("DatabaseID", databaseID);
            cmd.Parameters.AddWithValue("Inherit", chkBackupInherit.Checked);
            cmd.Parameters.AddWithValue("UseFG", chkUseFG.Checked);
            cmd.Parameters.AddWithValue("UsePartial", chkUsePartial.Checked);
            if (chkFull.Checked)
            {
                cmd.Parameters.AddWithValue("FullWarning", numFullWarning.Value);
                cmd.Parameters.AddWithValue("FullCritical", numFullCritical.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("FullWarning", DBNull.Value);
                cmd.Parameters.AddWithValue("FullCritical", DBNull.Value);
            }
            if (chkDiff.Checked)
            {
                cmd.Parameters.AddWithValue("DiffWarning", numDiffWarning.Value);
                cmd.Parameters.AddWithValue("DiffCritical", numDiffCritical.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("DiffWarning", DBNull.Value);
                cmd.Parameters.AddWithValue("DiffCritical", DBNull.Value);
            }
            if (chkLog.Checked)
            {
                cmd.Parameters.AddWithValue("LogWarning", numLogWarning.Value);
                cmd.Parameters.AddWithValue("LogCritical", numLogCritical.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("LogWarning", DBNull.Value);
                cmd.Parameters.AddWithValue("LogCritical", DBNull.Value);
            }
            using (cn)
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }

            GetBackupThresholds();

        }

        private void bttnLRAddUpdate_Click(object sender, EventArgs e)
        {
            var instanceID = cboLRInstance.SelectedValue;
            var databaseID = cboLRDatabase.SelectedValue;
            if (databaseID == null)
            {
                databaseID = -1;
            }
            if (instanceID == null)
            {
                instanceID = -1;
            }
            SqlConnection cn = new SqlConnection(Properties.Settings.Default.ConnectionString);
            var cmd = new SqlCommand("dbo.LogRestoreThresholds_Upd", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("DatabaseID", databaseID);
            cmd.Parameters.AddWithValue("Inherit", chkLRInherit.Checked);
            if (chkLRLatency.Checked)
            {
                cmd.Parameters.AddWithValue("LatencyWarning", numLRLatencyWarning.Value);
                cmd.Parameters.AddWithValue("LatencyCritical", numLRLatencyCritical.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("LatencyWarning", DBNull.Value);
                cmd.Parameters.AddWithValue("LatencyCritical", DBNull.Value);
            }
            if (chkLRTimeSinceLast.Checked)
            {
                cmd.Parameters.AddWithValue("TimeSinceLastWarning", numLRTimeSinceLastWarning.Value);
                cmd.Parameters.AddWithValue("TimeSinceLastCritical", numLRTimeSinceLastCritical.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("TimeSinceLastWarning", DBNull.Value);
                cmd.Parameters.AddWithValue("TimeSinceLastCritical", DBNull.Value);
            }

            using (cn)
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            GetLRThresholds();

        }

        private void dgvLR_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgvLR.CurrentCell != null)
            {
                var currentRow = dgvLR.CurrentCell.RowIndex;
                chkLRInherit.Checked = false;
                if (dgvLR.Rows[currentRow].Cells["LatencyCriticalThreshold"].Value != DBNull.Value && dgvLR.Rows[currentRow].Cells["LatencyWarningThreshold"].Value != DBNull.Value)
                {
                    numLRLatencyCritical.Value = (Int32)dgvLR.Rows[currentRow].Cells["LatencyCriticalThreshold"].Value;
                    numLRLatencyWarning.Value = (Int32)dgvLR.Rows[currentRow].Cells["LatencyWarningThreshold"].Value;
                    chkLRLatency.Checked = true;
                }
                else
                {
                    chkLRLatency.Checked = false;
                }
                if (dgvLR.Rows[currentRow].Cells["TimeSinceLastCriticalThreshold"].Value != DBNull.Value && dgvLR.Rows[currentRow].Cells["TimeSinceLastWarningThreshold"].Value != DBNull.Value)
                {
                    numLRTimeSinceLastCritical.Value = (Int32)dgvLR.Rows[currentRow].Cells["TimeSinceLastCriticalThreshold"].Value;
                    numLRTimeSinceLastWarning.Value = (Int32)dgvLR.Rows[currentRow].Cells["TimeSinceLastWarningThreshold"].Value;
                    chkLRTimeSinceLast.Checked = true;
                }
                else
                {
                    chkLRTimeSinceLast.Checked = false;
                }

                var instanceID = (Int32)dgvLR.Rows[currentRow].Cells["InstanceID"].Value;
                var databaseID = (Int32)dgvLR.Rows[currentRow].Cells["DatabaseID"].Value;

                cboLRInstance.SelectedValue = instanceID;
                cboLRDatabase.SelectedValue = databaseID;

            }
        }

        private void chkLRLatency_CheckedChanged(object sender, EventArgs e)
        {
            numLRLatencyCritical.Enabled = chkLRLatency.Checked;
            numLRLatencyWarning.Enabled = chkLRLatency.Checked;
        }

        private void chkLRTimeSinceLast_CheckedChanged(object sender, EventArgs e)
        {
            numLRTimeSinceLastWarning.Enabled = chkLRTimeSinceLast.Checked;
            numLRTimeSinceLastCritical.Enabled = chkLRTimeSinceLast.Checked;
        }
    }
}
