using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class ServerServices : UserControl, ISetContext
    {
        public ServerServices()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext _context)
        {
            _context.Report = SystemReports.ServerServices;
            customReportView1.SetContext(_context);
        }
    }
}