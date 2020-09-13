using DBAChecks;
using Newtonsoft.Json;
using Quartz;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using static DBAChecks.DBAChecksConnection;

namespace DBAChecksServiceConfig
{
    public partial class ServiceConfig : Form
    {
        public ServiceConfig()
        {
            InitializeComponent();
        }

        string originalJson = "";
        CollectionConfig collectionConfig = new CollectionConfig();
        string jsonPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");
        ServiceController svcCtrl;

        private void bttnAdd_Click(object sender, EventArgs e)
        {
            DBAChecksSource src = new DBAChecksSource(cboSource.Text);
            src.NoWMI = chkNoWMI.Checked;
            if (chkSlowQueryThreshold.Checked)
            {
                src.SlowQueryThresholdMs = (Int32)numSlowQueryThreshold.Value;
            }
            if (chkCustomizeSchedule.Checked)
            {
                src.Schedules = src.GetSchedule();
            }
            if (txtSnapshotDBs.Text.Trim().Length > 0)
            {
                if (!CronExpression.IsValidExpression(txtSnapshotCron.Text))
                {
                    MessageBox.Show("Invalid cron expression", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                src.SchemaSnapshotDBs = txtSnapshotDBs.Text;
                src.SchemaSnapshotCron = txtSnapshotCron.Text;
                if (collectionConfig.SchemaSnapshotOptions == null)
                {
                    collectionConfig.SchemaSnapshotOptions=  new SchemaSnapshotDBOptions();
                }
            }
            src.PersistXESessions = chkPersistXESession.Checked;
            bool validated = validateSource();

            if (validated)
            {
                if (!(src.SourceConnection.Type == ConnectionType.SQL || collectionConfig.DestinationConnection.Type == ConnectionType.SQL))
                {
                    MessageBox.Show("Error: Invalid source and destination connection combination.  One of these should be a SQL connection string", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (collectionConfig == null)
                {
                    collectionConfig = new CollectionConfig();
                }
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                validated = src.SourceConnection.Validate();
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                if (validated == false)
                {
                    if (MessageBox.Show("Error connecting to data source.  Are you sure you want to add this to the configuration?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                    {
                        return;
                    }
                }
                else if (src.SourceConnection.IsAzureDB() && (src.SourceConnection.InitialCatalog() == "" || src.SourceConnection.InitialCatalog() == "master"))
                {
                    if (MessageBox.Show("Add all azure databases as connections?", "Add Connections", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        SqlConnection cn = new SqlConnection(src.SourceConnection.ConnectionString);
                        using (cn)
                        {
                            cn.Open();
                            SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", cn);
                            var rdr = cmd.ExecuteReader();
                            var builder = new SqlConnectionStringBuilder(src.SourceConnection.ConnectionString);
                            while (rdr.Read())
                            {
                                builder.InitialCatalog = rdr.GetString(0);
                                DBAChecksSource dbCn = new DBAChecksSource(builder.ConnectionString);
                                if (ConnectionExists(dbCn.SourceConnection) == false)
                                {
                                    collectionConfig.SourceConnections.Add(dbCn);
                                }
                            }

                        }
                    }
                }

                var existingConnection = getConnection(cboSource.Text);
                if (existingConnection != null)
                {
                    if (chkCustomizeSchedule.Checked && existingConnection.Schedules != null)
                    {
                        src.Schedules = existingConnection.Schedules;
                    }
                    if (MessageBox.Show("Update existing connection?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        collectionConfig.SourceConnections.Remove(existingConnection);
                    }
                    else
                    {
                        return;
                    }

                }

                collectionConfig.SourceConnections.Add(src);
                txtJson.Text = collectionConfig.Serialize();
                populateDropDowns();
            }
            // JsonConvert.DeserializeObject<CollectionConfig[]>(jsonConfig);
        }

        private bool ConnectionExists(DBAChecksConnection newConnection)
        {
            if (getConnection(newConnection.ConnectionString) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        private void populateDropDowns()
        {
            foreach (var _cfg in collectionConfig.SourceConnections)
            {
                if (!(cboSource.Items.Contains(_cfg.ConnectionString)))
                {
                    cboSource.Items.Add(_cfg.ConnectionString);
                }
            }
        }

        private bool validateSource()
        {
            errorProvider1.SetError(cboSource, null);
            DBAChecksConnection source = new DBAChecksConnection(cboSource.Text);
            if (cboSource.Text == "")
            {
                return false;
            }

            if (source.Type == ConnectionType.Invalid)
            {
                errorProvider1.SetError(cboSource, "Invalid connection string, directory or S3 path");
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool validateDestination()
        {
            errorProvider1.SetError(txtDestination, null);
            DBAChecksConnection dest = new DBAChecksConnection(txtDestination.Text);

            if (txtDestination.Text == "")
            {
                return false;
            }

            if (dest.Type == ConnectionType.Invalid)
            {
                errorProvider1.SetError(txtDestination, "Invalid connection string, directory or S3 path");
                return false;
            }
            else
            {
                return true;
            }
        }




        private void bttnSave_Click(object sender, EventArgs e)
        {
            saveChanges();
        }

        private void saveChanges()
        {
            txtJson.Text = collectionConfig.Serialize();
            System.IO.File.WriteAllText(jsonPath, txtJson.Text);
            originalJson = txtJson.Text;
            MessageBox.Show("Config saved.  Restart service to apply changes.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ServiceConfig_Load(object sender, EventArgs e)
        {
            txtJson.MaxLength = 0;
            cboServiceCredentials.SelectedIndex = 3;
            if (File.Exists(jsonPath))
            {
                try
                {
                    originalJson = System.IO.File.ReadAllText(jsonPath);
                    txtJson.Text = originalJson;
                    setFromJson(originalJson);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error reading ServiceConfig.json: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            refreshServiceStatus();
        }

        private void setFromJson(string json)
        {
            collectionConfig = CollectionConfig.Deserialize(json);
            populateDropDowns();
            txtDestination.Text = collectionConfig.DestinationConnection.EncryptedConnectionString;
            txtAWSProfile.Text = collectionConfig.AWSProfile;
            txtAccessKey.Text = collectionConfig.AccessKey;
            txtSecretKey.Text = collectionConfig.SecretKey;
            chkCustomizeMaintenanceCron.Checked = (collectionConfig.MaintenanceScheduleCron != null);

        }

        private void refreshServiceStatus()
        {
            svcCtrl = ServiceController.GetServices()
    .FirstOrDefault(s => s.ServiceName == "DBAChecksService");

            if (svcCtrl == null)
            {
                lblServiceStatus.Text = "Service Status: Not Installed";
                bttnStart.Enabled = false;
                bttnStop.Enabled = false;
                bttnInstall.Enabled = true;
                bttnUninstall.Enabled = false;

            }
            else
            {
                lblServiceStatus.Text = Enum.GetName(typeof(ServiceControllerStatus), svcCtrl.Status);
                bttnStart.Enabled = (svcCtrl.Status == ServiceControllerStatus.Stopped);
                bttnStop.Enabled = (svcCtrl.Status == ServiceControllerStatus.Running);
                bttnInstall.Enabled = false;
                bttnUninstall.Enabled = true;
            }
        }

        private void txtJson_Validating(object sender, CancelEventArgs e)
        {
            errorProvider1.SetError(txtJson, null);
            if (txtJson.Text.Trim() == "")
            {
                collectionConfig = new CollectionConfig();
                return;
            }
            try
            {
                setFromJson(txtJson.Text);
            }
            catch (Exception ex)
            {
                errorProvider1.SetError(txtJson, ex.Message);
            }
        }

        private void ServiceConfig_FromClosing(object sender, FormClosingEventArgs e)
        {
            if (originalJson != txtJson.Text)
            {
                if (MessageBox.Show("Save Changes?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    saveChanges();
                }
            }
        }

        private void bttnStart_Click(object sender, EventArgs e)
        {
            svcCtrl.Refresh();
            if (svcCtrl.Status == ServiceControllerStatus.Stopped)
            {
                try
                {
                    svcCtrl.Start();
                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            refreshServiceStatus();
        }

        private void bttnStop_Click(object sender, EventArgs e)
        {
            svcCtrl.Refresh();
            if (svcCtrl.Status == ServiceControllerStatus.Running)
            {
                try
                {
                    svcCtrl.Stop();
                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            refreshServiceStatus();
        }

        private void bttnRefresh_Click(object sender, EventArgs e)
        {
            refreshServiceStatus();
        }

        private void bttnInstall_Click(object sender, EventArgs e)
        {
            if (!(File.Exists(jsonPath)))
            {
                MessageBox.Show("Save configuration file before installing service", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "CMD.EXE";
            string arg = "";
            switch (cboServiceCredentials.SelectedIndex)
            {
                case 0:
                    arg = "--localsystem";
                    break;
                case 1:
                    arg = "--localservice";
                    break;
                case 2:
                    arg = "--networkservice";
                    break;
                case 3:
                    arg = "--interactive";
                    break;

            }
            psi.Arguments = "/K DBAChecksService Install " + arg;
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();
            System.Threading.Thread.Sleep(500);
            refreshServiceStatus();
        }

        private void bttnUninstall_Click(object sender, EventArgs e)
        {
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "CMD.EXE";
            psi.Arguments = "/K DBAChecksService UnInstall";
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();
            System.Threading.Thread.Sleep(500);
            refreshServiceStatus();
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtAWSProfile_TextChanged(object sender, EventArgs e)
        {
            txtAccessKey.Enabled = (txtAWSProfile.Text.Length == 0);
            txtSecretKey.Enabled = txtAccessKey.Enabled;

        }

        private void txtAccessKey_TextChanged(object sender, EventArgs e)
        {
            txtAWSProfile.Enabled = (txtAccessKey.Text.Length == 0 && txtSecretKey.Text.Length == 0);

        }

        private void txtSecretKey_TextChanged(object sender, EventArgs e)
        {
            txtAWSProfile.Enabled = (txtAccessKey.Text.Length == 0 && txtSecretKey.Text.Length == 0);

        }

        private void cboDestination_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtDestination_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtDestination_Validated(object sender, EventArgs e)
        {
            validateDestination();
            collectionConfig.Destination = txtDestination.Text;
            txtJson.Text = collectionConfig.Serialize();
        }


        private DBAChecksSource getConnection(string connectionString)
        {
            if (collectionConfig == null)
            {
                return null;
            }
            var findConnection = new DBAChecksConnection(connectionString);
            foreach (var s in collectionConfig.SourceConnections)
            {

                if (s.SourceConnection.Type == findConnection.Type)
                {
                    if (s.SourceConnection.Type == ConnectionType.SQL && s.SourceConnection.DataSource() == findConnection.DataSource() && (s.SourceConnection.InitialCatalog() == findConnection.InitialCatalog()))
                    {
                        return s;
                    }
                    else if (s.SourceConnection.ConnectionString == findConnection.ConnectionString)
                    {
                        return s;
                    }
                }
            }
            return null;
        }

        private void bttnRemove_Click(object sender, EventArgs e)
        {
            DBAChecksSource src = null;

            src = getConnection(cboSource.Text);
            if (src != null)
            {
                collectionConfig.SourceConnections.Remove(src);
                MessageBox.Show("Connection removed", "Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Connection not found", "Remove", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            txtJson.Text = collectionConfig.Serialize();
        }

        private void txtAccessKey_Validating(object sender, CancelEventArgs e)
        {
            collectionConfig.AccessKey = (txtAccessKey.Text == "" ? null : txtAccessKey.Text);
            txtJson.Text = collectionConfig.Serialize();
        }

        private void txtSecretKey_Validating(object sender, CancelEventArgs e)
        {
            collectionConfig.SecretKey = (txtSecretKey.Text == "" ? null : txtSecretKey.Text);
            txtJson.Text = collectionConfig.Serialize();
        }

        private void txtAWSProfile_Validating(object sender, CancelEventArgs e)
        {
            collectionConfig.AWSProfile = (txtAWSProfile.Text == "" ? null : txtAWSProfile.Text);
            txtJson.Text = collectionConfig.Serialize();
        }

        private void chkCustomizeMaintenanceCron_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCustomizeMaintenanceCron.Checked)
            {
                collectionConfig.MaintenanceScheduleCron = collectionConfig.GetMaintenanceCron();
            }
            else
            {
                collectionConfig.MaintenanceScheduleCron = null;
            }
            txtJson.Text = collectionConfig.Serialize();
        }

        private void chkSlowQueryThreshold_CheckedChanged(object sender, EventArgs e)
        {
            numSlowQueryThreshold.Enabled = chkSlowQueryThreshold.Checked;
            if (chkSlowQueryThreshold.Checked)
            {
                numSlowQueryThreshold.Value = 1000;
                lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events IS enabled";
            }
            else
            {
                numSlowQueryThreshold.Value = -1;
                lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabled";
            }

        }

        private void cboSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            DBAChecksSource src = null;

            src = getConnection(cboSource.Text);
            if (src != null)
            {
                chkNoWMI.Checked = src.NoWMI;
                chkPersistXESession.Checked = src.PersistXESessions;
                chkSlowQueryThreshold.Checked = (src.SlowQueryThresholdMs != -1);
                if (chkSlowQueryThreshold.Checked)
                {
                    numSlowQueryThreshold.Value = src.SlowQueryThresholdMs;
                }
                else
                {
                    numSlowQueryThreshold.Value = 0;
                }

                chkCustomizeSchedule.Checked = src.Schedules != null;
                txtSnapshotCron.Text = src.SchemaSnapshotCron;
                txtSnapshotDBs.Text = src.SchemaSnapshotDBs;

            }
        }

        private void bttnDeployDatabase_Click(object sender, EventArgs e)
        {
            var frm = new DBDeploy();
            frm.ConnectionString = txtDestination.Text;
            frm.ShowDialog();


        }
    }
}
