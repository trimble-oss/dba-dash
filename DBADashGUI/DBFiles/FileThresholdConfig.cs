using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.DBFiles
{
    public partial class FileThresholdConfig : Form
    {
        public FileThresholdConfig()
        {
            InitializeComponent();
        }

        public int InstanceID;
        public int DatabaseID;
        public int DataSpaceID;

        public bool IsDataConfig
        {
            get
            {
                return DataSpaceID == -1 || DataSpaceID > 0;
            }
        }

        public bool IsLogConfig
        {
            get
            {
                return cboLevel.Text != "Filegroup";
            }
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            if (IsDataConfig)
            {
                dataConfig.FileThreshold.UpdateThresholds();
            }
            if (IsLogConfig)
            {
                logConfig.FileThreshold.UpdateThresholds();
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FileThresholdConfig_Load(object sender, EventArgs e)
        {          
            cboLevel.Items.Add("Root");
            cboLevel.SelectedIndex = 0;
            if (InstanceID != -1)
            {
                cboLevel.Items.Add("Instance");
                cboLevel.SelectedIndex = 1;
            }
            if (DatabaseID != -1)
            {
                cboLevel.Items.Add("Database");
                cboLevel.SelectedIndex = 2;
            }
            if (DataSpaceID > 0)
            {
                cboLevel.Items.Add("Filegroup");
                cboLevel.SelectedIndex = 3;
            }
            if (DataSpaceID == 0)
            {
                tab1.SelectedTab = tabLog;
            }
            loadThresholds();
        }

        private void loadThresholds()
        {
            tab1.TabPages.Clear();
            int _instanceID = cboLevel.Text == "Root" ? -1 : InstanceID;
            int _databaseID = cboLevel.Text == "Root" || cboLevel.Text == "Instance" ? -1 : DatabaseID;
            int _dataSpaceID = cboLevel.Text == "Root" || cboLevel.Text == "Instance" || cboLevel.Text=="Database" ? -1 : DatabaseID;
            int _dataDataSpaceID = _dataSpaceID == 0 ? -1 : _dataSpaceID;

            tab1.TabPages.Add(tabData);
            var dataThreshold = FileThreshold.GetFileThreshold(_instanceID,_databaseID,_dataSpaceID);
            dataConfig.FileThreshold = dataThreshold;
            
            if (IsLogConfig)
            {
                var logThreshold = FileThreshold.GetFileThreshold(_instanceID, _databaseID, 0);
                logConfig.FileThreshold = logThreshold;
                tab1.TabPages.Add(tabLog);
            }
        }

        private void cboLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadThresholds();
        }
    }
}
