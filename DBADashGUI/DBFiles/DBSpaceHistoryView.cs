using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.DBFiles
{
    public partial class DBSpaceHistoryView : Form
    {
        public DBSpaceHistoryView()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public int DatabaseID { get => dbSpaceHistory1.DatabaseID; set => dbSpaceHistory1.DatabaseID = value; }
        public int? DataSpaceID { get => dbSpaceHistory1.DataSpaceID; set => dbSpaceHistory1.DataSpaceID = value; }
        public string InstanceGroupName { get => dbSpaceHistory1.InstanceGroupName; set => dbSpaceHistory1.InstanceGroupName = value; }
        public string DBName { get => dbSpaceHistory1.DBName; set => dbSpaceHistory1.DBName = value; }
        public string FileName { get => dbSpaceHistory1.FileName; set => dbSpaceHistory1.FileName = value; }
        public string NumberFormat { get => dbSpaceHistory1.NumberFormat; set => dbSpaceHistory1.NumberFormat = value; }
        public string Unit { get => dbSpaceHistory1.Unit; set => dbSpaceHistory1.Unit = value; }

        private void DBSpaceHistoryView_Load(object sender, EventArgs e)
        {
            Text = InstanceGroupName;
            if (!string.IsNullOrEmpty(DBName))
            {
                Text += " | " + DBName;
            }
            if (!string.IsNullOrEmpty(FileName))
            {
                Text += " | " + FileName;
            }
            dbSpaceHistory1.RefreshData();
        }
    }
}