using System;
using System.Windows.Forms;

namespace DBADashGUI.Drives
{
    public partial class DriveHistoryView : Form
    {
        public DriveHistoryView()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public Int32 DriveID;

        private void DriveHistoryView_Load(object sender, EventArgs e)
        {
            driveHistory1.DriveID = DriveID;
            driveHistory1.RefreshData();
        }
    }
}
