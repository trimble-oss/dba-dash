using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashGUI.DBFiles
{
    public partial class DBSpaceHistoryView : Form
    {
        public DBSpaceHistoryView()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int DatabaseID { get => dbSpaceHistory1.DatabaseID; set => dbSpaceHistory1.DatabaseID = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? DataSpaceID { get => dbSpaceHistory1.DataSpaceID; set => dbSpaceHistory1.DataSpaceID = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InstanceGroupName { get => dbSpaceHistory1.InstanceGroupName; set => dbSpaceHistory1.InstanceGroupName = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DBName { get => dbSpaceHistory1.DBName; set => dbSpaceHistory1.DBName = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FileName { get => dbSpaceHistory1.FileName; set => dbSpaceHistory1.FileName = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string NumberFormat { get => dbSpaceHistory1.NumberFormat; set => dbSpaceHistory1.NumberFormat = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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