using System;
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

        public DBADashContext CurrentContext;

        private void AzureDBResourceStatsView_Load(object sender, EventArgs e)
        {
            azureDBResourceStats1.SetContext(CurrentContext);
            azureDBResourceStats1.RefreshData();
        }
    }
}