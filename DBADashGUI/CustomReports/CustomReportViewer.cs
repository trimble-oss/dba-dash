using DBADash;
using DBADashGUI.CommunityTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class CustomReportViewer : Form
    {
        public CustomReportViewer()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool LoadDirectExecutionReport { get; set; } = false;

        public CustomReportView CustomReportView => customReportView1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DBADashContext Context { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CustomSqlParameter> CustomParams { get; set; }

        /// <summary>
        /// Optional grid filters to apply after report data loads.
        /// Key = result set index, Value = DataView RowFilter expression.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<int, string> GridFilters { get; set; }

        public DataSet DataSet;

        private async void CustomReportViewer_Load(object sender, EventArgs e)
        {
            ReplaceViewType();
            Text = Context.Report.ReportName;
            if (GridFilters is { Count: > 0 })
            {
                customReportView1.PostGridRefresh += ApplyGridFilters;
            }
            if (DataSet != null)
            {
                customReportView1.SuppressRefresh = true;
            }
            await customReportView1.SetContext(Context, CustomParams);
            if (DataSet != null)
            {
                customReportView1.SuppressRefresh = false;
                customReportView1.ShowData(DataSet);
            }
            else if (LoadDirectExecutionReport && Context.Report is DirectExecutionReport)
            {
                customReportView1.RefreshData();
            }
        }

        private void ApplyGridFilters(object sender, EventArgs e)
        {
            customReportView1.PostGridRefresh -= ApplyGridFilters;
            if (GridFilters == null) return;
            foreach (var kvp in GridFilters)
            {
                var grid = customReportView1.Grids.FirstOrDefault(g => g.ResultSetID == kvp.Key);
                if (grid != null && !string.IsNullOrEmpty(kvp.Value))
                {
                    grid.SetFilter(kvp.Value);
                }
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