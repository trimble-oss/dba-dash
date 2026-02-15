using System;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class ResourceGovernorPerformance : UserControl, ISetContext, IRefreshData
    {
        private DBADashContext CurrentContext;

        public ResourceGovernorPerformance()
        {
            InitializeComponent();
            resourceGovernorWorkloadGroupsMetrics1.CloseVisible = false;
            resourceGovernorWorkloadGroupsMetrics1.MoveUpVisible = false;
            resourceGovernorResourcePools1.CloseVisible = false;
            resourceGovernorResourcePools1.MoveUpVisible = false;
        }

        public void RefreshData()
        {
            if (tabs.SelectedTab == tabPools)
            {
                resourceGovernorResourcePools1.RefreshData();
            }
            else
            {
                resourceGovernorWorkloadGroupsMetrics1.RefreshData();
            }
        }

        public void SetContext(DBADashContext _context)
        {
            CurrentContext = _context;
            SetContext();
        }

        private void SetContext()
        {
            if (tabs.SelectedTab == tabPools)
            {
                resourceGovernorResourcePools1.SetContext(CurrentContext);
            }
            else
            {
                resourceGovernorWorkloadGroupsMetrics1.SetContext(CurrentContext);
            }
        }

        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetContext();
        }
    }
}