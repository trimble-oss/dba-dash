using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobStatusAndHistory : Form, ISetContext
    {
        public JobStatusAndHistory()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowSteps
        {
            get => agentJobsControl1.ShowSteps; set => agentJobsControl1.ShowSteps = value;
        }

        public void SetContext(DBADashContext _context)
        {
            agentJobsControl1.SetContext(_context);
        }
    }
}