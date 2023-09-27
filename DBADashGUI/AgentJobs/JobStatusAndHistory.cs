using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobStatusAndHistory : Form, ISetContext
    {
        public JobStatusAndHistory()
        {
            InitializeComponent();
        }

        public bool ShowSteps
        {
            get => agentJobsControl1.ShowSteps; set => agentJobsControl1.ShowSteps = value;
        }

        public void SetContext(DBADashContext context)
        {
            agentJobsControl1.SetContext(context);
        }
    }
}