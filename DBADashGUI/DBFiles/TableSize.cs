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

        public void SetContext(DBADashContext _context) => SetContext(_context, false);

        public void SetContext(DBADashContext _context, bool forceRefresh)
        {
            customReportView1.Report = _context.Type == SQLTreeItem.TreeType.Table ? TableSizeHistoryReport.Instance : TableSizeReport.Instance;
            customReportView1.SetContext(_context, forceRefresh);
        }

        private void TableSize_Load(object sender, EventArgs e)
        {
        }
    }
}