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
    public partial class CustomReportViewer : Form
    {
        public CustomReportViewer()
        {
            InitializeComponent();
        }

        public DBADashContext Context { get; set; }

        public List<CustomSqlParameter> CustomParams { get; set; }

        private void CustomReportViewer_Load(object sender, EventArgs e)
        {
            this.Text = Context.Report.ReportName;
            customReportView1.SetContext(Context, CustomParams);
        }
    }
}