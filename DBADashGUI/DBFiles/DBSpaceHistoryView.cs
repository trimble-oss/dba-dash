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
    public partial class DBSpaceHistoryView : Form
    {
        public DBSpaceHistoryView()
        {
            InitializeComponent();
        }

        public Int32 DatabaseID { get; set; }
        public Int32? DataSpaceID { get; set; } = null;
        public string Instance { get; set; }
        public string DBName { get; set; }
        public string FileName { get; set; }

        private void DBSpaceHistoryView_Load(object sender, EventArgs e)
        {
            dbSpaceHistory1.DatabaseID = DatabaseID;
            dbSpaceHistory1.DataSpaceID = DataSpaceID;
            dbSpaceHistory1.Instance = Instance;
            dbSpaceHistory1.DBName = DBName;
            dbSpaceHistory1.FileName = FileName;
            this.Text = Instance;
            if(DBName!=null && DBName.Length > 0)
            {
                this.Text += " | " + DBName;
            }
            if(FileName!=null && FileName.Length > 0)
            {
                this.Text += " | " + FileName;
            }
            dbSpaceHistory1.RefreshData();

        }
    }
}
