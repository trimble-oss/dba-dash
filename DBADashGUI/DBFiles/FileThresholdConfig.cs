using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.DBFiles
{
    public partial class FileThresholdConfig : Form
    {
        public FileThresholdConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public int InstanceID;
        public int DatabaseID;
        public int DataSpaceID;
        private bool isLoaded = false;

        public bool IsDataConfig => DataSpaceID is (-1) or > 0;

        public bool IsLogConfig => cboLevel.Text != "Filegroup";

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
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
            LoadThresholds();
            isLoaded = true;
        }

        private void LoadThresholds()
        {
            tab1.TabPages.Clear();
            int _instanceID = cboLevel.Text == "Root" ? -1 : InstanceID;
            int _databaseID = cboLevel.Text is "Root" or "Instance" ? -1 : DatabaseID;
            int _dataSpaceID = cboLevel.Text is "Root" or "Instance" or "Database" ? -1 : DataSpaceID;

            tab1.TabPages.Add(tabData);
            var dataThreshold = FileThreshold.GetFileThreshold(_instanceID, _databaseID, _dataSpaceID);
            dataConfig.FileThreshold = dataThreshold;

            if (IsLogConfig)
            {
                var logThreshold = FileThreshold.GetFileThreshold(_instanceID, _databaseID, 0);
                logConfig.FileThreshold = logThreshold;
                tab1.TabPages.Add(tabLog);
            }
        }

        private void CboLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoaded)
            {
                LoadThresholds();
            }
        }
    }
}