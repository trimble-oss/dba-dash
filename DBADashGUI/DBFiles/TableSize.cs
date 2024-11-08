using System;
using System.Windows.Forms;
using DBADashGUI.CustomReports;

namespace DBADashGUI.DBFiles
{
    public partial class TableSize : UserControl, ISetContext
    {
        public TableSize()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext _context)
        {
            customReportView1.Report = _context.Type == SQLTreeItem.TreeType.Table ? SystemReports.TableSizeHistory : SystemReports.TableSizeReport;
            customReportView1.SetContext(_context);
        }

        private void TableSize_Load(object sender, EventArgs e)
        {
        }
    }
}