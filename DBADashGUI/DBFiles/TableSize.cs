using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Amazon.Runtime.Internal.Transform;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using Newtonsoft.Json;
using Octokit;

namespace DBADashGUI.DBFiles
{
    public partial class TableSize : UserControl, ISetContext
    {
        public TableSize()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext context)
        {
            context.Report = context.Type == SQLTreeItem.TreeType.Table ? SystemReports.TableSizeHistory : SystemReports.TableSizeReport;
            customReportView1.SetContext(context);
        }

        private void TableSize_Load(object sender, EventArgs e)
        {
        }
    }
}