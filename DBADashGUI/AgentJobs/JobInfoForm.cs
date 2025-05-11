using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.AgentJobs
{
    public partial class JobInfoForm : Form
    {
        public JobInfoForm()
        {
            InitializeComponent();
            jobTimeline1.UseGlobalTime = false;
            jobStats1.UseGlobalTime = false;
            this.ApplyTheme();
        }

        public DBADashContext DBADashContext { get; set; }

        public HashSet<int> LoadedTabs = new();

        private void Tab_TabIndexChanged(object sender, EventArgs e)
        {
            if (LoadedTabs.Contains(tabs.SelectedIndex)) return;
            try
            {
                foreach (var ctrl in tabs.SelectedTab.Controls.OfType<ISetContext>())
                {
                    ctrl.SetContext(DBADashContext);
                }

                LoadedTabs.Add(tabs.SelectedIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void JobInfoForm_Load(object sender, EventArgs e)
        {
            this.Text = DBADashContext.ObjectName;
            jobInfo1.SetContext(DBADashContext);
            LoadedTabs.Add(tabs.SelectedIndex);
        }
    }
}