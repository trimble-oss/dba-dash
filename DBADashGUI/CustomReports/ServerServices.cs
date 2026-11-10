using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class ServerServices : UserControl, ISetContext
    {
        public ServerServices()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext _context) => SetContext(_context, false);

        public void SetContext(DBADashContext _context, bool forceRefresh)
        {
            _context.Report = ServerServicesReport.Instance;
            customReportView1.SetContext(_context, forceRefresh);
        }
    }
}