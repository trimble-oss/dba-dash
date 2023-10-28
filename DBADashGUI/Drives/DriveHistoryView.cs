using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Drives
{
    public partial class DriveHistoryView : Form
    {
        public DriveHistoryView()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public string ConnectionString;
        public int DriveID;

        private void DriveHistoryView_Load(object sender, EventArgs e)
        {
            driveHistory1.DriveID = DriveID;
            driveHistory1.RefreshData();
        }
    }
}
