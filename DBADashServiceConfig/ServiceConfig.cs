using DBADash;
using Newtonsoft.Json;
using Quartz;
using System;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using static DBADash.DBADashConnection;
using System.Drawing;
using System.Collections.Generic;
using System.Data;

namespace DBADashServiceConfig
{
    public partial class ServiceConfig : Form
    {
        public ServiceConfig()
        {
            InitializeComponent();
        }

        string originalJson = "";
        CollectionConfig collectionConfig = new CollectionConfig();
        readonly string jsonPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");
        ServiceController svcCtrl;
        bool isInstalled = false;


        private void bttnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSource.Text))
            {
                MessageBox.Show("Please click the connect button or enter a list of connection strings for the SQL instances you want to monitor", "Enter Source", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string schemaSnapshotDBs = txtSnapshotDBs.Text.Trim();
            if (!string.IsNullOrEmpty(schemaSnapshotDBs) && collectionConfig.SchemaSnapshotOptions == null)
            {
                collectionConfig.SchemaSnapshotOptions = new SchemaSnapshotDBOptions();
            }
            bool hasUpdateApproval = false;
            bool warnXENotSupported = false;
            bool addUnvalidated = false;
            bool doNotAddUnvalidated = false;
            bool doesNotHaveUpdateApproval = false;
            foreach (string splitSource in txtSource.Text.Split(new[] { "\r\n", "\r", "\n" },StringSplitOptions.None))
            {
                string sourceString = splitSource.Trim();
                if (string.IsNullOrEmpty(splitSource))
                {
                    continue;
                }
                if (!sourceString.Contains(";") && !sourceString.Contains(":") && !sourceString.StartsWith("\\\\") && !sourceString.StartsWith("//"))
                {
                    // Providing the name of the SQL instances - build the connection string automatically
                    var builder = new SqlConnectionStringBuilder
                    {
                        DataSource = sourceString,
                        IntegratedSecurity = true,
                        TrustServerCertificate = true,
                        Encrypt=true,
                        ApplicationName = "DBADash"
                    };
                    sourceString = builder.ConnectionString;
                }
                var src = new DBADashSource(sourceString)
                {
                    NoWMI = chkNoWMI.Checked,
                    UseDualEventSession = chkDualSession.Checked,
                    PersistXESessions = chkPersistXESession.Checked,
                    SlowQueryThresholdMs = chkSlowQueryThreshold.Checked ? (Int32)numSlowQueryThreshold.Value : -1,
                    RunningQueryPlanThreshold = chkCollectPlans.Checked ? new PlanCollectionThreshold() { CountThreshold = int.Parse(txtCountThreshold.Text), CPUThreshold = int.Parse(txtCPUThreshold.Text), DurationThreshold = int.Parse(txtDurationThreshold.Text), MemoryGrantThreshold = int.Parse(txtGrantThreshold.Text) } : null,
                    SchemaSnapshotDBs = schemaSnapshotDBs,
                    CollectSessionWaits = chkCollectSessionWaits.Checked
                };
                bool validated = validateSource(sourceString);

                string validationError = "";
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
                    if (!addUnvalidated)
                    {
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                        try
                        {
                            src.SourceConnection.Validate();
                            validated= true;
                        }
                        catch (Exception ex)
                        {
                            validated= false;
                            validationError = ex.Message;
                        }
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                    }
                    if (!addUnvalidated  && !validated)
                    {
                        if(doNotAddUnvalidated)
                        {
                            continue;
                        }
                        else if (MessageBox.Show("Error connecting to data source.  Do you wish to add anyway?" + Environment.NewLine + Environment.NewLine + validationError, "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                        {
                            doNotAddUnvalidated=true;
                            continue;
                        }
                        else
                        {
                            addUnvalidated = true;
                        }
                    }
                    else if (!addUnvalidated && !src.SourceConnection.IsXESupported() && src.SlowQueryThresholdMs >= 0)
                    {
                        warnXENotSupported=true;
                        src.SlowQueryThresholdMs = -1;
                        src.PersistXESessions = false;
                    }

                    var existingConnection = collectionConfig.GetSourceFromConnectionString(sourceString);
                    if (existingConnection != null)
                    {

                        src.CollectionSchedules = existingConnection.CollectionSchedules;
                        if (doesNotHaveUpdateApproval)
                        {
                            continue;
                        }
                        else if (hasUpdateApproval || MessageBox.Show("Update existing connection(s)?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            collectionConfig.SourceConnections.Remove(existingConnection);
                            hasUpdateApproval = true;
                        }
                        else
                        {
                            doesNotHaveUpdateApproval = true;
                            continue;
                        }

                    }

                    collectionConfig.SourceConnections.Add(src);

                }

            }

            txtJson.Text = collectionConfig.Serialize();
            setConnectionCount();
            setDgv();
            if (warnXENotSupported)
            {
                MessageBox.Show("Warning: Slow query capture is supported for SQL 2012 and later only", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private bool validateSource(string sourceString)
        {
            errorProvider1.SetError(txtSource, null);
            DBADashConnection source = new DBADashConnection(sourceString);
            if (string.IsNullOrEmpty(sourceString))
            {
                return false;
            }

            if (source.Type == ConnectionType.Invalid)
            {
                errorProvider1.SetError(txtSource, "Invalid connection string, directory or S3 path");
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
            DBADashConnection dest = new DBADashConnection(txtDestination.Text);
            lblVersionInfo.ForeColor = Color.Black;
            lblVersionInfo.Text = "";
            lblVersionInfo.Font = new Font(lblVersionInfo.Font, FontStyle.Regular);
            if (txtDestination.Text == "")
            {
                lblVersionInfo.Text = "Please start by setting the destination connection for your DBA Dash repository database.";
                lblVersionInfo.ForeColor = Color.Brown;
                lblVersionInfo.Font = new Font(lblVersionInfo.Font, FontStyle.Bold);
                return false;
            }

            if (dest.Type == ConnectionType.Invalid)
            {
                errorProvider1.SetError(txtDestination, "Invalid connection string, directory or S3 path");
                return false;
            }
            if(dest.Type == ConnectionType.SQL)
            {
                try
                {
                    var status = DBValidations.VersionStatus(dest.ConnectionString);
                    if (status.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                    {
                        lblVersionInfo.Text = "Run Deploy to create database.";
                        lblVersionInfo.ForeColor = Color.Red;
                        return true;
                    }
                    if (status.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                    {
                        lblVersionInfo.Text = "DB upgrade not required. DacVersion/DB Version: " + status.DACVersion.ToString();
                        lblVersionInfo.ForeColor = Color.Green;
                        bttnDeployDatabase.Enabled = true;
                    }
                    else if (status.VersionStatus == DBValidations.DBVersionStatusEnum.AppUpgradeRequired)
                    {
                        lblVersionInfo.Text = "DB version " + status.DBVersion.ToString() + " is newer. Please update the app";
                        lblVersionInfo.ForeColor = Color.Red;
                        bttnDeployDatabase.Enabled = false;
                    }
                    else
                    {
                        lblVersionInfo.Text = "DB version " + status.DBVersion.ToString() + " requires upgrade to " + status.DACVersion.ToString();
                        lblVersionInfo.ForeColor = Color.Red;
                        bttnDeployDatabase.Enabled = true;
                    }
                    return true;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
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
            if (File.Exists(jsonPath))
            {
                File.Move(jsonPath, jsonPath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            System.IO.File.WriteAllText(jsonPath, txtJson.Text);
            originalJson = txtJson.Text;
            updateSaveButton();
            MessageBox.Show("Config saved.  Restart service to apply changes.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ServiceConfig_Load(object sender, EventArgs e)
        {
            dgvConnections.AutoGenerateColumns = false;
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ConnectionString", DataPropertyName = "ConnectionString", HeaderText = "Connection String", Width = 300 });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn() { DataPropertyName = "NoWMI", HeaderText = "No WMI" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "SlowQueryThresholdMs", HeaderText = "Slow Query Threshold (ms)" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn() { DataPropertyName = "UseDualEventSession", HeaderText = "Use Dual Event Session" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn() { DataPropertyName = "PersistXESessions", HeaderText = "Persist XE Sessions" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "SchemaSnapshotDBs", HeaderText = "Schema Snapshot DBs" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "SlowQuerySessionMaxMemoryKB", HeaderText = "Slow Query Session Max Memory (KB)" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn() { DataPropertyName = "CollectSessionWaits", HeaderText = "Collect Session Waits", ToolTipText = "Collect Session Waits for Running Queries" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn() { DataPropertyName = "PlanCollectionEnabled", HeaderText = "Running Query Plan Collection" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "PlanCollectionCPUThreshold", HeaderText = "Plan Collection CPU Threshold" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "PlanCollectionDurationThreshold", HeaderText = "Plan Collection Duration Threshold" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "PlanCollectionCountThreshold", HeaderText = "Plan Collection Count Threshold" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "PlanCollectionMemoryGrantThreshold", HeaderText = "Plan Collection Memory Grant Threshold" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn() { DataPropertyName = "HasCustomSchedule", HeaderText = "Custom Schedule" });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn() { Name = "Schedule", HeaderText = "Schedule", Text = "Schedule", UseColumnTextForLinkValue = true });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn() { Name = "Edit", HeaderText = "Edit", Text = "Edit", UseColumnTextForLinkValue = true });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn() { Name = "Delete", HeaderText = "Delete", Text = "Delete", UseColumnTextForLinkValue = true });

            txtJson.MaxLength = 0;

            if (File.Exists(jsonPath))
            {
                try
                {
                    originalJson = System.IO.File.ReadAllText(jsonPath);
                    txtJson.Text = originalJson;
                    setFromJson(originalJson);
                    if (!originalJson.Like("%Trust%Server%Certificate%") && collectionConfig.SourceConnections.Count>0)
                    {
                        if (MessageBox.Show("Encryption is applied by default to your SQL Server connections with Microsoft.Data.SqlClient 4.0.  Connections might fail if the server certificate is not trusted.  Apply Trust Server Certificate?", "Trust Server Certificate", MessageBoxButtons.YesNo, MessageBoxIcon.Question)== DialogResult.Yes)
                        {
                            applyTrustServerCertificate();
                            saveChanges();
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error reading ServiceConfig.json: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            setConnectionCount();
            refreshServiceStatus();
            validateDestination();
    
        }

        private void applyTrustServerCertificate()
        {
            if (collectionConfig.DestinationConnection.Type == ConnectionType.SQL)
            {
                collectionConfig.DestinationConnection.EncryptedConnectionString = addTrustServerCertificate(collectionConfig.DestinationConnection.EncryptedConnectionString);
                txtDestination.Text = collectionConfig.DestinationConnection.EncryptedConnectionString;
            }
            foreach(var dest in collectionConfig.SecondaryDestinationConnections.Where(dest=> dest.Type == ConnectionType.SQL))
            {
                dest.EncryptedConnectionString = addTrustServerCertificate(dest.EncryptedConnectionString);
            }
                        
            foreach(var src in collectionConfig.SourceConnections.Where(src => src.SourceConnection.Type == ConnectionType.SQL))
            {
                src.SourceConnection.EncryptedConnectionString = addTrustServerCertificate(src.SourceConnection.EncryptedConnectionString);
            }
        }

        private string addTrustServerCertificate(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.TrustServerCertificate = true;
            builder.Encrypt = true;
            return builder.ConnectionString;
        }

      
        private void setConnectionCount()
        {
            int cnt = collectionConfig.SourceConnections.Count;
            lnkSourceConnections.Text = "Source Connections: " + cnt.ToString();
            if (cnt == 0)
            {
                lnkSourceConnections.Text += ".  (Add source connections to monitor)";
                lnkSourceConnections.LinkColor = Color.Brown;
            }
            else
            {
                lnkSourceConnections.LinkColor = Color.Green;
            }
        }

        private void setFromJson(string json)
        {
            collectionConfig = CollectionConfig.Deserialize(json);
            txtDestination.Text = collectionConfig.DestinationConnection.EncryptedConnectionString;
            txtAWSProfile.Text = collectionConfig.AWSProfile;
            txtAccessKey.Text = collectionConfig.AccessKey;
            txtSecretKey.Text = collectionConfig.SecretKey;
            chkScanAzureDB.Checked = collectionConfig.ScanForAzureDBs;
            chkScanEvery.Checked = collectionConfig.ScanForAzureDBsInterval > 0;
            numAzureScanInterval.Value = collectionConfig.ScanForAzureDBsInterval;
            chkAutoUpgradeRepoDB.Checked = collectionConfig.AutoUpdateDatabase;
            chkLogInternalPerfCounters.Checked = collectionConfig.LogInternalPerformanceCounters;
            updateScanInterval();
            setDgv();

        }

        private void setDgv()
        {
            dgvConnections.DataSource = new BindingSource() { DataSource = collectionConfig.SourceConnections };
        }

        private void refreshServiceStatus()
        {
            svcCtrl = ServiceController.GetServices()
    .FirstOrDefault(s => s.ServiceName == collectionConfig.ServiceName);

            if (svcCtrl == null)
            {
                isInstalled = false;
                lnkStart.Enabled = false;
                lnkStop.Enabled = false;
                lnkInstall.Enabled = true;
                lnkInstall.Font = new Font(lnkInstall.Font, FontStyle.Bold);
                lnkInstall.Text = "Install as service";
                toolTip1.SetToolTip(lnkInstall, "Install DBA Dash agent as a Windows service");
                lblServiceStatus.Text = "Service Status: Not Installed";
                lblServiceStatus.ForeColor = Color.Brown;

            }
            else
            {
                isInstalled = true;
                lblServiceStatus.Text ="Service Status: " +  Enum.GetName(typeof(ServiceControllerStatus), svcCtrl.Status);
                if(svcCtrl.Status == ServiceControllerStatus.Running)
                {
                    lblServiceStatus.ForeColor = Color.Green;
                }
                else if(svcCtrl.Status== ServiceControllerStatus.Stopped || svcCtrl.Status == ServiceControllerStatus.StopPending || svcCtrl.Status == ServiceControllerStatus.Paused)
                {
                    lblServiceStatus.ForeColor = Color.Red;
                }
                else
                {
                    lblServiceStatus.ForeColor = Color.Orange;
                }
                lnkStart.Enabled = (svcCtrl.Status == ServiceControllerStatus.Stopped);
                lnkStop.Enabled= (svcCtrl.Status == ServiceControllerStatus.Running);
                lnkInstall.Font = new Font(lnkInstall.Font, FontStyle.Regular);
                toolTip1.SetToolTip(lnkInstall, "Remove Windows service for DBA Dash agent");
                lnkInstall.Text = "Uninstall service";
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
            promptSaveChanges();
        }

        private void promptSaveChanges()
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
            startService();
        }

        private void startService()
        {
            promptSaveChanges();
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
            validateDestination();
        }

        private void bttnStop_Click(object sender, EventArgs e)
        {
            stopService();
        }

        private void stopService()
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
            validateDestination();
        }

        private void bttnUninstall_Click(object sender, EventArgs e)
        {
            uninstallService();
        }

        private void uninstallService()
        {
            if (MessageBox.Show("Are you sure you want to remove the DBA Dash Windows service?", "Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "CMD.EXE",
                    Arguments = "/c DBADashService UnInstall"
                };
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();
                System.Threading.Thread.Sleep(500);
                refreshServiceStatus();
            }
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


        private void txtDestination_Validated(object sender, EventArgs e)
        {
            destinationChanged();
        }

        private void destinationChanged()
        {
            if(collectionConfig.Destination!= txtDestination.Text)
            {
                validateDestination();

                collectionConfig.Destination = txtDestination.Text;
                txtJson.Text = collectionConfig.Serialize();
            }

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
            chkDualSession.Enabled= chkSlowQueryThreshold.Checked;
            chkPersistXESession.Enabled = chkSlowQueryThreshold.Checked;

        }

        private void loadConnectionForEdit(DBADashSource src)
        {
            if (src != null)
            {
                txtSource.Text = src.ConnectionString;
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

                txtSnapshotDBs.Text = src.SchemaSnapshotDBs;
                chkDualSession.Checked = src.UseDualEventSession;
                if (src.RunningQueryPlanThreshold != null)
                {
                    txtCountThreshold.Text = src.RunningQueryPlanThreshold.CountThreshold.ToString();
                    txtDurationThreshold.Text = src.RunningQueryPlanThreshold.DurationThreshold.ToString();
                    txtGrantThreshold.Text = src.RunningQueryPlanThreshold.MemoryGrantThreshold.ToString();
                    txtCPUThreshold.Text = src.RunningQueryPlanThreshold.CPUThreshold.ToString();
                    chkCollectPlans.Checked = src.RunningQueryPlanThreshold.PlanCollectionEnabled;
                }
                else
                {
                    chkCollectPlans.Checked = false;
                }
                
            }
            else
            {
                src.RunningQueryPlanThreshold = null;
            }
            setAvailableOptionsForSource();
        }

        private void bttnDeployDatabase_Click(object sender, EventArgs e)
        {
            var frm = new DBDeploy();
            var cn =  new DBADashConnection(txtDestination.Text);
            if (cn.Type == ConnectionType.SQL)
            {
                frm.ConnectionString = cn.ConnectionString;
            }
            else
            {
                if (setDestination())
                {
                    cn = new DBADashConnection(txtDestination.Text);
                    frm.ConnectionString = cn.ConnectionString;
                }
                else
                {
                    return;
                }

            }

            frm.ShowDialog();
            if (frm.DatabaseName != cn.InitialCatalog())
            {
                if(MessageBox.Show("Update connection string?","Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var builder = new SqlConnectionStringBuilder(txtDestination.Text)
                    {
                        InitialCatalog = frm.DatabaseName
                    };
                    txtDestination.Text = builder.ConnectionString;
                    destinationChanged();
                }
            }
            validateDestination();

        }

        private bool setDestination()
        {
            var frm = new DBConnection();
            var cn = new DBADashConnection(txtDestination.Text);
            if (cn.Type == ConnectionType.SQL)
            {
                frm.ConnectionString = cn.ConnectionString;
            }
            else
            {
                frm.ConnectionString = "Initial Catalog=DBADashDB;Integrated Security=SSPI;Data Source=" + Environment.MachineName;
            }
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {

                var builder = new SqlConnectionStringBuilder(frm.ConnectionString);
                if(builder.InitialCatalog==null || builder.InitialCatalog.Length == 0)
                {
                    builder.InitialCatalog = "DBADashDB";
                }
                cn = new DBADashConnection(builder.ConnectionString) ;

                txtDestination.Text = cn.EncryptedConnectionString;
                destinationChanged();
                return true;
            }
            return false;
        }

        private void bttnConnect_Click(object sender, EventArgs e)
        {
            setDestination();
        }

        private void bttnConnectSource_Click(object sender, EventArgs e)
        {
            var frm = new DBConnection();
            var cn = new DBADashConnection(txtSource.Text);
            if (cn.Type == ConnectionType.SQL)
            {
                frm.ConnectionString = cn.ConnectionString;
            }
            frm.ValidateInitialCatalog = true;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                cn = new DBADashConnection(frm.ConnectionString);
                txtSource.Text = cn.EncryptedConnectionString;
            }
            setAvailableOptionsForSource();
        }

        private void bttnScanNow_Click(object sender, EventArgs e)
        {
           var newConnections= collectionConfig.GetNewAzureDBConnections();
            if (newConnections.Count == 0)
            {
                MessageBox.Show("No new Azure DB connections found", "Scan",  MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if( MessageBox.Show(String.Format("Found {0} new connections.  Add connections to config file?",newConnections.Count), "Scan", MessageBoxButtons.YesNo, MessageBoxIcon.Question)== DialogResult.Yes)
                {
                    collectionConfig.AddConnections(newConnections);
                    txtJson.Text = collectionConfig.Serialize();
                }
            }
        }

        private void chkScanAzureDB_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.ScanForAzureDBs = chkScanAzureDB.Checked;
            txtJson.Text = collectionConfig.Serialize();
        }

        private void chkAutoUpgradeRepoDB_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.AutoUpdateDatabase = chkAutoUpgradeRepoDB.Checked;
            txtJson.Text = collectionConfig.Serialize();
        }

        private void chkScanEvery_CheckedChanged(object sender, EventArgs e)
        {
            if(numAzureScanInterval.Value==0 && chkScanEvery.Checked)
            {
                numAzureScanInterval.Value = 3600;
            }
            if (!chkScanEvery.Checked)
            {
                numAzureScanInterval.Value = 0;
            }
            collectionConfig.ScanForAzureDBsInterval = Convert.ToInt32(numAzureScanInterval.Value);
            updateScanInterval();
            txtJson.Text = collectionConfig.Serialize();
        }

        private void updateScanInterval()
        {
            lblHHmm.Visible = chkScanEvery.Checked;
            lblHHmm.Text = TimeSpan.FromSeconds(Convert.ToInt32(numAzureScanInterval.Value)).ToString();
        }

        private void numAzureScanInterval_ValueChanged(object sender, EventArgs e)
        {
            chkScanEvery.Checked = numAzureScanInterval.Value > 0;
            collectionConfig.ScanForAzureDBsInterval = Convert.ToInt32(numAzureScanInterval.Value);
            updateScanInterval();
            txtJson.Text = collectionConfig.Serialize();
        }

        private void lnkCronBuilder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var psi = new ProcessStartInfo("http://www.cronmaker.com/") { UseShellExecute = true };
            Process.Start(psi);
        }

        private void bttnDestFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDestination.Text = fbd.SelectedPath;
                    destinationChanged();
                }
            }
        }

        private void bttnSrcFolder_Click_1(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtSource.Text  = fbd.SelectedPath;
                }
            }
            setAvailableOptionsForSource();
        }

        private void bttnS3_Click(object sender, EventArgs e)
        {
            var cfg = new CollectionConfig
            {
                AccessKey = txtAccessKey.Text,
                SecretKey = txtSecretKey.Text,
                AWSProfile = txtAWSProfile.Text
            };

            using (var frm = new S3Browser() { AccessKey = cfg.AccessKey, SecretKey = cfg.GetSecretKey(), Folder= "DBADash_" + Environment.MachineName })
            {                
                frm.ShowDialog();
                if(frm.DialogResult== DialogResult.OK)
                {
                    txtDestination.Text = frm.AWSURL;
                    destinationChanged();
                }
            }
        }

        private void bttnS3Src_Click(object sender, EventArgs e)
        {
            var cfg = new CollectionConfig
            {
                AccessKey = txtAccessKey.Text,
                SecretKey = txtSecretKey.Text,
                AWSProfile = txtAWSProfile.Text
            };
            using (var frm = new S3Browser() { AccessKey = cfg.AccessKey, SecretKey = cfg.GetSecretKey(), Folder = "DBADash_{HostName}" })
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                   txtSource.Text= frm.AWSURL;
                }
            }
            setAvailableOptionsForSource();
        }


        private void lnkSourceConnections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tab1.SelectedTab = tabSource;
        }

        private void lnkPermissions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var psi = new ProcessStartInfo("https://github.com/DavidWiseman/DBADash/blob/develop/Docs/Security.md") { UseShellExecute=true};
            Process.Start(psi);
        }

        private void cboSource_Validated(object sender, EventArgs e)
        {
            setAvailableOptionsForSource();
        }

        private void setAvailableOptionsForSource()
        {
            var src = new DBADashSource(txtSource.Text);
            bool isSql = src.SourceConnection.Type == ConnectionType.SQL;

            pnlExtendedEvents.Enabled = isSql;
            chkCollectPlans.Enabled = isSql;
            grpRunningQueryThreshold.Enabled = isSql && chkCollectPlans.Checked;
            chkNoWMI.Enabled = isSql;
        }


        private void chkLogInternalPerfCounters_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.LogInternalPerformanceCounters = chkLogInternalPerfCounters.Checked;
            txtJson.Text = collectionConfig.Serialize();
        }

        private void bttnSchedule_Click(object sender, EventArgs e)
        {
            var frm = new ScheduleConfig()
            {
                ConfiguredSchedule = collectionConfig.CollectionSchedules
            };

            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                collectionConfig.CollectionSchedules = frm.ConfiguredSchedule;
                txtJson.Text = collectionConfig.Serialize();
            }
        }

   
        private void dgv_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            updateFromGrid();
        }

        private void updateFromGrid()
        { 
            collectionConfig.SourceConnections = (List<DBADashSource>)((BindingSource)dgvConnections.DataSource).DataSource;
            txtJson.Text = collectionConfig.Serialize();
            setConnectionCount();

        }

        private void dgvConnections_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["Delete"].Index)
            {
                dgvConnections.Rows.RemoveAt(e.RowIndex);
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["Edit"].Index)
            {
                var src = (DBADashSource)dgvConnections.Rows[e.RowIndex].DataBoundItem;
                loadConnectionForEdit(src);
            }
            else if (e.RowIndex >= 0 && dgvConnections.Columns[e.ColumnIndex].CellType == typeof(DataGridViewCheckBoxCell))
            {
                dgvConnections.EndEdit();
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["Schedule"].Index)
            {
                var src = (DBADashSource)dgvConnections.Rows[e.RowIndex].DataBoundItem;
                if (src.SourceConnection.Type == ConnectionType.SQL)
                {
                    var frm = new ScheduleConfig()
                    {
                        ConfiguredSchedule = src.CollectionSchedules,
                        BaseSchedule = collectionConfig.GetSchedules()
                    };
                    frm.ShowDialog();
                    if (frm.DialogResult == DialogResult.OK)
                    {
                        src.CollectionSchedules = frm.ConfiguredSchedule;
                        dgvConnections.Refresh();
                    }
                }
                else
                {
                    MessageBox.Show("Custom schedule configuration is only available for SQL connections","Schedule", MessageBoxButtons.OK,MessageBoxIcon.Warning);
                }
            }
            updateFromGrid();
        }

        private void dgv_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            updateFromGrid();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            applySearch();
        }

        private void applySearch()
        {
            CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[dgvConnections.DataSource];
            currencyManager1.SuspendBinding();
            foreach (DataGridViewRow row in dgvConnections.Rows)
            {
                if (string.IsNullOrEmpty(txtSearch.Text) || row.Cells["ConnectionString"].Value.ToString().ToLower().Contains(txtSearch.Text.ToLower()))
                {
                    row.Visible = true;
                }
                else
                {
                    row.Visible = false;
                }
            }
            currencyManager1.ResumeBinding();
        }

        private void chkCollectPlans_CheckedChanged(object sender, EventArgs e)
        {
            grpRunningQueryThreshold.Enabled = chkCollectPlans.Checked;
            if (chkCollectPlans.Checked)
            {
                var defaultThreshold = PlanCollectionThreshold.DefaultThreshold;
                txtCPUThreshold.Text = defaultThreshold.CPUThreshold.ToString();
                txtCountThreshold.Text = defaultThreshold.CountThreshold.ToString();
                txtDurationThreshold.Text = defaultThreshold.DurationThreshold.ToString();
                txtGrantThreshold.Text = defaultThreshold.MemoryGrantThreshold.ToString();
            }
        }

        private void lnkALL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSnapshotDBs.Text = "*";
        }

        private void lnkNone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSnapshotDBs.Text = string.Empty;
        }

        private void lnkExample_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSnapshotDBs.Text = "Database1,Database2,Databsase3";
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for(int i =e.RowIndex;i<e.RowIndex + e.RowCount; i++)
            {
                var src = (DBADashSource)dgvConnections.Rows[i].DataBoundItem;
                dgvConnections.Rows[i].ReadOnly = src.SourceConnection.Type != ConnectionType.SQL;
                dgvConnections.Rows[i].DefaultCellStyle.BackColor = src.SourceConnection.Type == ConnectionType.SQL ? Color.White : Color.Silver;
            }
        }

        private void txtJson_TextChanged(object sender, EventArgs e)
        {
            updateSaveButton();
        }

        private void updateSaveButton()
        {
            if (txtJson.Text != originalJson)
            {
                bttnSave.Font = new Font(bttnSave.Font, FontStyle.Bold);
            }
            else
            {
                bttnSave.Font = new Font(bttnSave.Font, FontStyle.Regular);
            }
        }

        private void lnkStart_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            startService();
        }

        private void lnkStop_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            stopService();
        }

        private void lnkRefresh_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            refreshServiceStatus();
            validateDestination(); // DB could be upgraded on service start so refresh destination
        }

        private void lnkInstall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (isInstalled)
            {
                uninstallService();
            }
            else { 
                if(txtJson.Text!= originalJson)
                {
                    if(MessageBox.Show("Save changes to config", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        saveChanges();
                    }
                }
                var frm = new InstallService();
                frm.ServiceName = collectionConfig.ServiceName;
                frm.ShowDialog();
                refreshServiceStatus();
                validateDestination(); // DB could be upgraded on service start so refresh destination
            }
        }
    }
}

public struct CronSelection
{
    public string CronExpression { get; set; }
    public string DisplayValue { get; set; }

    public override string ToString()
    {
        return DisplayValue.ToString();
    }
}