using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBAChecksService;
using Newtonsoft.Json;
using System.IO;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Diagnostics;
using DBAChecks;
namespace DBAChecksServiceConfig
{
    public partial class ServiceConfig : Form
    {
        public ServiceConfig()
        {
            InitializeComponent();
        }

        string originalJson="";
        List<CollectionConfig>collectionConfigs = new List<CollectionConfig>();
        string jsonPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");
        ServiceController svcCtrl;

        private void bttnAdd_Click(object sender, EventArgs e)
        {
            CollectionConfig cfg = new CollectionConfig(cboSource.Text, cboDestination.Text);
            if (pnlAWS.Visible)
            {
                cfg.AWSProfile = txtAWSProfile.Text;
            }
            cfg.NoWMI = chkNoWMI.Checked;
            if (chkCustomizeSchedule.Checked)
            {
                cfg.Schedules = cfg.GetSchedule();
            }
            bool validated = validateSource() && validateDestination();

            if (validated)
            {
                if (cfg.Validate() == false)
                {
                    if (MessageBox.Show("Error connecting to data source/destination.  Are you sure you want to add this to the configuration?","Error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                    {
                        return;
                    }
                }
                if (!(cfg.SourceConnectionType() == CollectionConfig.ConnectionType.SQL || cfg.DestinationConnectionType() == CollectionConfig.ConnectionType.SQL))
                {
                    MessageBox.Show("Error: Invalid source and destination connection combination.  One of these should be a SQL connection string", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (collectionConfigs == null)
                {
                    collectionConfigs = new List<CollectionConfig>();
                }
                foreach (var _cfg in collectionConfigs)
                {
                    if (cfg.Source == _cfg.Source && cfg.Destination == _cfg.Destination)
                    {
                        MessageBox.Show("Error: Item already exists", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                }
                collectionConfigs.Add(cfg);
                txtJson.Text = jsonConfig();
                populateDropDowns();
            }
           // JsonConvert.DeserializeObject<CollectionConfig[]>(jsonConfig);
        }

        private string jsonConfig()
        {
            return JsonConvert.SerializeObject(collectionConfigs, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }

        private void populateDropDowns()
        {
            foreach (var _cfg in collectionConfigs)
            {
               if (!(cboSource.Items.Contains(_cfg.Source)))
                {
                    cboSource.Items.Add(_cfg.Source);
                }
               if (!(cboDestination.Items.Contains(_cfg.Destination)))
                {
                    cboDestination.Items.Add(_cfg.Destination);
                }


            }
        }

        private bool validateSource()
        {
            errorProvider1.SetError(cboSource, null);
            CollectionConfig cfg = new CollectionConfig(cboSource.Text, cboDestination.Text);
            pnlAWS.Visible = (cfg.SourceConnectionType() == CollectionConfig.ConnectionType.AWSS3 || cfg.DestinationConnectionType() == CollectionConfig.ConnectionType.AWSS3);
            if (cboSource.Text == "")
            {
                return false;
            }
            
            if (cfg.SourceConnectionType() == DBAChecksService.CollectionConfig.ConnectionType.Invalid)
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
            errorProvider1.SetError(cboDestination, null);
            CollectionConfig cfg = new CollectionConfig(cboSource.Text, cboDestination.Text);
            pnlAWS.Visible = (cfg.SourceConnectionType() == CollectionConfig.ConnectionType.AWSS3 || cfg.DestinationConnectionType() == CollectionConfig.ConnectionType.AWSS3);
            if (cboDestination.Text == "")
            {
                return false;
            }
            
            if (cfg.DestinationConnectionType() == DBAChecksService.CollectionConfig.ConnectionType.Invalid)
            {
                errorProvider1.SetError(cboDestination, "Invalid connection string, directory or S3 path");
                return false;
            }
            else
            {
                return true;
            }
        }

        private void cboSource_Validating(object sender, CancelEventArgs e)
        {
            bttnAdd.Enabled = validateSource() && validateDestination() ;
        }

        private void cboDestination_Validating(object sender, CancelEventArgs e)
        {
            bttnAdd.Enabled = validateSource() && validateDestination();
        }

        private void bttnSave_Click(object sender, EventArgs e)
        {
            saveChanges();
        }

        private void saveChanges()
        {
            txtJson.Text = jsonConfig();
            System.IO.File.WriteAllText(jsonPath, txtJson.Text);
            originalJson = txtJson.Text;
            MessageBox.Show("Config saved.  Restart service to apply changes.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ServiceConfig_Load(object sender, EventArgs e)
        {
            cboServiceCredentials.SelectedIndex = 3;
            if (File.Exists(jsonPath))
            {
                try
                {
                     originalJson=  System.IO.File.ReadAllText(jsonPath);
                     txtJson.Text = originalJson;
                     collectionConfigs = JsonConvert.DeserializeObject<List<CollectionConfig>>(originalJson);
                    populateDropDowns();
                    if (cboDestination.Items.Count == 1)
                    {
                        cboDestination.SelectedIndex = 0;
                    }
                }
                catch 
                {
                    MessageBox.Show("Error reading ServiceConfig.json", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            refreshServiceStatus();
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
                collectionConfigs = new List<CollectionConfig>();
                return;
            }
            try
            {
                collectionConfigs = JsonConvert.DeserializeObject<List<CollectionConfig>>(txtJson.Text);
                populateDropDowns();
            }
            catch(Exception ex)
            {
                errorProvider1.SetError(txtJson, ex.Message);
            }
        }

        private void ServiceConfig_FromClosing(object sender, FormClosingEventArgs e)
        {
            if (originalJson != txtJson.Text)
            {
                if (MessageBox.Show("Save Changes?","Save", MessageBoxButtons.YesNo , MessageBoxIcon.Question)== DialogResult.Yes)
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
                try { 
                svcCtrl.Start();
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
            if (svcCtrl.Status== ServiceControllerStatus.Running)
            {
                try
                {
                    svcCtrl.Stop();
                }
                catch(Exception ex)
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
                MessageBox.Show("Save configuration file before installing service", "Error",MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
            refreshServiceStatus();
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
