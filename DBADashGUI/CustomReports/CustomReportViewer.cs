using DBADash;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class CustomReportViewer : Form
    {
        public CustomReportViewer()
        {
            InitializeComponent();
        }

        public CustomReportView CustomReportView => customReportView1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DBADashContext Context { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CustomSqlParameter> CustomParams { get; set; }

        public DataSet DataSet;

        private async void CustomReportViewer_Load(object sender, EventArgs e)
        {
            Text = Context.Report.ReportName;
            await customReportView1.SetContext(Context, CustomParams);
            if (DataSet != null)
            {
                customReportView1.ShowData(DataSet);
            }
        }
    }
}