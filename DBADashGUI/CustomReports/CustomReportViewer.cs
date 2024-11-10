using System;
using System.Collections.Generic;
using System.Data;
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

        public DataSet DataSet;

        private async void CustomReportViewer_Load(object sender, EventArgs e)
        {
            Text = Context.Report.ReportName;
            await customReportView1.SetContext(Context, CustomParams);
            if(DataSet != null)
            {
                customReportView1.ShowData(DataSet);
            }
        }
    }
}