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
            ReplaceViewType();
            Text = Context.Report.ReportName;
            await customReportView1.SetContext(Context, CustomParams);
            if (DataSet != null)
            {
                customReportView1.ShowData(DataSet);
            }
        }

        private void ReplaceViewType()
        {
            var report = Context?.Report;
            if (report == null) return;

            var desiredType = report.ViewType ?? typeof(CustomReportView);
            if (!typeof(CustomReportView).IsAssignableFrom(desiredType)) desiredType = typeof(CustomReportView);

            if (customReportView1 != null && customReportView1.GetType() == desiredType) return;

            CustomReportView desiredView = null;
            var oldView = customReportView1;

            SuspendLayout();
            try
            {
                desiredView = (CustomReportView)Activator.CreateInstance(desiredType);
                desiredView.Report = report;
                desiredView.Dock = DockStyle.Fill;

                if (oldView != null)
                {
                    Controls.Remove(oldView);
                }

                customReportView1 = desiredView;
                Controls.Add(customReportView1);

                // successful swap; dispose old
                oldView?.Dispose();
            }
            catch (Exception ex)
            {
                // cleanup newly created view and restore old one if needed
                try
                {
                    desiredView?.Dispose();
                    if (oldView != null && !Controls.Contains(oldView))
                    {
                        customReportView1 = oldView;
                        Controls.Add(oldView);
                    }
                }
                catch (Exception ex2)
                {
                    CommonShared.ShowExceptionDialog(new AggregateException(ex, ex2), "Error replacing report view");
                    return;
                }
                CommonShared.ShowExceptionDialog(ex, "Error replacing report view");
            }
            finally
            {
                if (!IsDisposed) ResumeLayout(performLayout: true);
            }
        }
    }
}