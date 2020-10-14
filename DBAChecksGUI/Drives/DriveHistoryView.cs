using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBAChecksGUI.Drives
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
