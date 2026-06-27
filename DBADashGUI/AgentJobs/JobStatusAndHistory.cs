using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobStatusAndHistory : Form, ISetContext
    {
        public JobStatusAndHistory()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext _context)
        {
            agentJobsControl1.SetContext(_context);
        }
    }
}
