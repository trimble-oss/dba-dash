using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DBADash;

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

        private async void CustomReportViewer_Load(object sender, EventArgs e)
        {
            Text = Context.Report.ReportName;
            await customReportView1.SetContext(Context, CustomParams);
        }
    }
}