using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounterSummary : UserControl, ISetContext, IRefreshData
    {
        public PerformanceCounterSummary()
        {
            InitializeComponent();
        }

        private int InstanceID { get; set; }

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
            RefreshData();
        }

        public void RefreshData()
        {
            savedViewMenuItem1.LoadItemsAndSelectDefault();
            RefreshSummary();
            RefreshChart();
        }

        private void RefreshSummary()
        {
            if (performanceCounterSummaryGrid1.Visible)
            {
                performanceCounterSummaryGrid1.SearchText = txtSearch.Text;
                performanceCounterSummaryGrid1.InstanceID = InstanceID;
                performanceCounterSummaryGrid1.RefreshData();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshSummary();
            RefreshChart();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(performanceCounterSummaryGrid1);
        }

        private void RefreshChart()
        {
            if (!splitContainer1.Panel1Collapsed)
            {
                foreach (Control c in layout1.Controls)
                {
                    ((IMetricChart)c).RefreshData(InstanceID);
                }
            }
        }

        private void PerformanceCounterSummary_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = true;
            performanceCounterSummaryGrid1.CounterSelected += PerformanceCounterSummaryGrid1_CounterSelected;
            performanceCounterSummaryGrid1.TextSelected += PerformanceCounterSummaryGrid1_TextSelected;
        }

        private void PerformanceCounterSummaryGrid1_TextSelected(object sender, PerformanceCounterSummaryGrid.TextSelectedEventArgs e)
        {
            txtSearch.Text = e.Text;
            RefreshSummary();
        }

        private void PerformanceCounterSummaryGrid1_CounterSelected(object sender, PerformanceCounterSummaryGrid.CounterSelectedEventArgs e)
        {
            AddCounter(e.CounterName, e.CounterID);
        }

        private void AddCounter(string CounterName, int CounterID)
        {
            PerformanceCounters pc = new()
            {
                Metric = new PerformanceCounterMetric()
                {
                    CounterID = CounterID,
                    CounterName = CounterName
                }
            };
            AddChartControl(pc);
        }

        private void AddChartControl(IMetricChart chart)
        {
            ((Control)chart).ApplyTheme();
            chart.Close += Chart_Close;
            chart.MoveUp += Chart_MoveUp;
            chart.MoveUpVisible = layout1.Controls.Count > 0;
            chart.CloseVisible = true;
            ((Control)chart).Dock = DockStyle.Fill;

            if (layout1.RowCount >= 10)
            {
                if (MessageBox.Show("Max number of charts added. Clear existing charts?", "Metrics", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    layout1.Controls.Clear();
                }
                else
                {
                    return;
                }
            }
            if (layout1.Controls.Count == 0)
            {
                layout1.Controls.Clear();
                layout1.RowStyles.Clear();
                layout1.RowCount = 0;
            }
            layout1.RowCount++;
            layout1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / layout1.RowCount));

            SetEvenLayout();

            layout1.Controls.Add((Control)chart);

            splitContainer1.Panel1Collapsed = false;
            DeSelectView();
            RefreshChart();
        }

        private void SetEvenLayout()
        {
            for (int i = 0; i < layout1.RowStyles.Count; i++)
            {
                layout1.RowStyles[i].SizeType = SizeType.Percent;
                layout1.RowStyles[i].Height = 100f / layout1.RowCount;
            }
        }

        private void Chart_MoveUp(object sender, EventArgs e)
        {
            var chart = (Control)sender;
            List<Control> controlOrder = new();
            int i = 0;
            foreach (Control c in layout1.Controls)
            {
                if (c == chart && i == 0)
                {
                    return;
                }
                else if (c == chart)
                {
                    controlOrder.Insert(i - 1, c);
                    ((IMetricChart)c).MoveUpVisible = i > 1;
                    ((IMetricChart)controlOrder[i]).MoveUpVisible = true;
                }
                else
                {
                    controlOrder.Add(c);
                }
                i += 1;
            }
            layout1.Controls.Clear();
            layout1.Controls.AddRange(controlOrder.ToArray());
            DeSelectView();
        }

        private void Chart_Close(object sender, EventArgs e)
        {
            var chart = (Control)sender;
            layout1.Controls.Remove(chart);
            layout1.RowCount--;
            if (layout1.Controls.Count == 0)
            {
                splitContainer1.Panel1Collapsed = true; // No charts left, close panel
            }
            else
            {
                ((IMetricChart)layout1.Controls[0]).MoveUpVisible = false; // First item can't be moved up
            }
            DeSelectView();
        }

        private void TsClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            RefreshSummary();
        }

        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshSummary();
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(performanceCounterSummaryGrid1);
        }

        private void CPUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddChartControl(new CPU());
        }

        private void IOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddChartControl(new IOPerformance());
        }

        private void WaitsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddChartControl(new Waits());
        }

        private void ObjectExecutionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddChartControl(new ObjectExecution());
        }

        private void BlockingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddChartControl(new Blocking());
        }

        private void ShowGrid(bool visible)
        {
            splitContainer1.Panel1Collapsed = layout1.Controls.Count == 0; // Chart.  Both panels can't be collapsed so reset to not visible on show grid
            splitContainer1.Panel2Collapsed = !visible; // Grid
            performanceCounterSummaryGrid1.Visible = visible;
            tsToggleGrid.Text = visible ? "Hide Grid" : "Show Grid";
            tsToggleGrid.Font = visible ? new Font(tsToggleGrid.Font, FontStyle.Regular) : new Font(tsToggleGrid.Font, FontStyle.Bold);
            RefreshSummary(); // will refresh if visible.
        }

        private void TsToggleGrid_Click(object sender, EventArgs e)
        {
            ShowGrid(!performanceCounterSummaryGrid1.Visible);
            DeSelectView();
        }

        private void DeSelectView()
        {
            savedViewMenuItem1.ClearSelectedItem();
            tsDeleteView.Visible = false;
        }

        private void LoadSelectedView(string _selectedView, bool isGlobal, string serializedObject)
        {
            MetricsSavedView view = null;

            if (serializedObject != string.Empty)
            {
                try
                {
                    view = MetricsSavedView.Deserialize(serializedObject);
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error loading the saved view");
                }
            }
            splitContainer1.Visible = false; // Prevent flickering when adding/removing controls
            if (view == null) // User selected None of there was an issue with deserialization
            {
                // Show grid with no charts
                layout1.Controls.Clear();
                splitContainer1.Panel1Collapsed = true;
                ShowGrid(true);
                tsDeleteView.Visible = serializedObject != string.Empty && (!isGlobal || DBADashUser.HasManageGlobalViews); // Don't show for "None", but allow user to delete a view that fails to deserialize
            }
            else
            {
                layout1.Controls.Clear();
                foreach (var chart in view.Metrics)
                {
                    AddChartControl(chart.GetChart());
                }
                splitContainer1.Panel1Collapsed = view.Metrics.Count == 0; // Hide chart panel if no charts
                ShowGrid(view.ShowGrid);
                tsDeleteView.Visible = !isGlobal | DBADashUser.HasManageGlobalViews;
            }
            savedViewMenuItem1.SelectItem(_selectedView, isGlobal);
            splitContainer1.Visible = true;
        }

        private void TsSaveView_Click(object sender, EventArgs e)
        {
            using (SaveViewPrompt frm = new())
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    string name = frm.ViewName;
                    if ((savedViewMenuItem1.ContainsUserView(name) && !frm.IsGlobal) || (savedViewMenuItem1.ContainsGlobalView(name) && frm.IsGlobal))
                    {
                        if (MessageBox.Show("Replace existing view : " + name, "Replace View", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                    var savedView = new MetricsSavedView()
                    {
                        Name = name,
                        ShowGrid = !splitContainer1.Panel2Collapsed,
                        UserID = frm.IsGlobal ? DBADashUser.SystemUserID : DBADashUser.UserID,
                        Metrics = layout1.Controls.Cast<IMetricChart>().Select(chart => chart.Metric).ToList()
                    };

                    savedView.Save();
                    savedViewMenuItem1.RefreshItems();
                    savedViewMenuItem1.SelectItem(name, false);
                    tsDeleteView.Visible = true;
                }
            }
        }

        private void TsDeleteView_Click(object sender, EventArgs e)
        {
            if (savedViewMenuItem1.SelectedSavedView != string.Empty)
            {
                if (MessageBox.Show("Delete " + savedViewMenuItem1.SelectedSavedView, "Delete View", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var sv = new MetricsSavedView { Name = savedViewMenuItem1.SelectedSavedView, UserID = savedViewMenuItem1.SelectedSavedViewIsGlobal ? DBADashUser.SystemUserID : DBADashUser.UserID };
                    sv.Delete();
                    savedViewMenuItem1.RefreshItems();
                    tsDeleteView.Visible = false;
                }
            }
        }

        private void SavedViewSelected(object sender, SavedViewSelectedEventArgs e)
        {
            LoadSelectedView(e.Name, e.IsGlobal, e.SerializedObject);
        }

        private void WorkloadGroupAnalysis_Click(object sender, EventArgs e)
        {
            AddChartControl(new ResourceGovernorWorkloadGroupsMetrics() { Metric = new ResourceGovernorWorkloadGroupsMetric() { ShowTable = false, MetricsToDisplay = [.. ResourceGovernorWorkloadGroupsMetric.DefaultMetrics.Take<string>(1)] } });
        }
    }
}