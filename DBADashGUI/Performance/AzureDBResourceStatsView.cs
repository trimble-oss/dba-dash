using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class AzureDBResourceStatsView : Form
    {
        public AzureDBResourceStatsView()
        {
            InitializeComponent();
        }


        public DateTime FromDate;
        public DateTime ToDate;
        public string ElasticPoolName { get { return azureDBResourceStats1.ElasticPoolName; } set { azureDBResourceStats1.ElasticPoolName = value; } }
        public Int32 InstanceID { get { return azureDBResourceStats1.InstanceID; } set { azureDBResourceStats1.InstanceID = value; } }

        private void AzureDBResourceStatsView_Load(object sender, EventArgs e)
        {
            azureDBResourceStats1.SetDateRange(FromDate, ToDate);
            azureDBResourceStats1.RefreshData();
        }
    }
}
