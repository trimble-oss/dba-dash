using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class ResourceGovernanceViewer : Form
    {
        public ResourceGovernanceViewer()
        {
            InitializeComponent();
        }

        public int InstanceID;
        public string DatabaseName;

        private void ResourceGovernanceViewer_Load(object sender, EventArgs e)
        {
            this.Text = "Resource Governance " + DatabaseName;
            azureDBResourceGovernance1.InstanceIDs = new List<int>() { InstanceID };
            azureDBResourceGovernance1.RefreshData();
        }
    }
}
