using DBADashGUI.Charts;
using DBADashGUI.Theme;
using DocumentFormat.OpenXml.Math;
using LiveChartsCore.Measure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class ConfigureChart : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CustomReport Report { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataSet ReportDS { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? ChartId { get; set; }

        private bool _suspendPreview;

        // Background preview scheduling
        private long _nextPreviewTicks = long.MaxValue; // UTC ticks when next preview is due

        private int _previewDelayMs = 500; // 0.5 second default debounce
        private CancellationTokenSource _previewCts;
        private Task _previewWorkerTask;
        private ManualResetEventSlim _previewSignal = new(false);
        private ChartLayoutHelper _chartLayoutHelper = new ChartLayoutHelper();

        public ConfigureChart()
        {
            InitializeComponent();
            txtYMax.KeyPress += Common.KeyPressAllowNumericOnly;
            txtYMin.KeyPress += Common.KeyPressAllowNumericOnly;
            txtPointSize.KeyPress += Common.KeyPressAllowNumericOnly;
        }

        private void ConfigureChart_Load(object sender, EventArgs e)
        {
            if (Report?.CustomReportResults == null) return;
            bttnAdd.Text = ChartId.HasValue ? "Update Chart" : "Add Chart";
            var items = Report.CustomReportResults
                .Select(r => new { r.Key, r.Value.ResultName })
                .ToList();

            cboResults.DisplayMember = "ResultName";
            cboResults.ValueMember = "Key";
            cboResults.DataSource = items;

            cboLegend.DataSource = Enum.GetValues(typeof(LegendPosition)).Cast<LegendPosition>().ToList();
            // ValueMember is not set for this simple list binding, so set SelectedItem instead of SelectedValue
            cboLegend.SelectedItem = LegendPosition.Bottom;
            cboChartType.DataSource = Enum.GetValues(typeof(ChartTypes)).Cast<ChartTypes>().ToList();
            // ensure we can react to changes in chart type / series selection to enforce single metric selection when needed
            cboChartType.SelectedIndexChanged += ChartType_Changed;
            cboSeries.SelectedIndexChanged += ChartSeries_Changed;
            chkMetrics.ItemCheck += chkMetrics_ItemCheck;
            EditChart();
            this.ApplyTheme();
            AttachPreviewHandlers(this);
            StartPreviewWorker();
            this.FormClosing += ConfigureChart_FormClosing;
            UpdateTabPages();
            PreviewChart();
        }

        private void StartPreviewWorker()
        {
            _previewCts?.Cancel();
            _previewCts = new CancellationTokenSource();
            var token = _previewCts.Token;
            _previewWorkerTask = Task.Run(() => PreviewWorkerLoop(token), token);
        }

        private void StopPreviewWorker()
        {
            try
            {
                _previewCts?.Cancel();
                _previewSignal.Set();
                _previewWorkerTask?.Wait(500);
            }
            catch { }
            finally
            {
                _previewCts?.Dispose();
                _previewCts = null;
                _previewWorkerTask = null;
            }
        }

        //
        /// <summary>
        /// Debounced chart preview: schedule preview work on a background worker and invoke the UI update on the UI thread
        /// after a short inactivity delay. Each user change resets the delay so the preview is not rebuilt on every keystroke.
        /// </summary>
        /// <param name="token"></param>
        private void PreviewWorkerLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // read next scheduled tick (atomic)
                    var next = Interlocked.Read(ref _nextPreviewTicks);

                    // nothing scheduled - wait until SchedulePreview signals
                    if (next == long.MaxValue)
                    {
                        _previewSignal.Wait(token);
                        _previewSignal.Reset();
                        continue;
                    }

                    // compute remaining milliseconds until scheduled time
                    var now = DateTime.UtcNow.Ticks;
                    var remainMs = (long)((next - now) / TimeSpan.TicksPerMillisecond);
                    if (remainMs > 0)
                    {
                        // wait until either the remaining time elapses or a new
                        // schedule updates the timestamp (signal). Use the token
                        // so cancellation is respected.
                        _previewSignal.Wait((int)Math.Min(remainMs, Int32.MaxValue), token);
                        _previewSignal.Reset();
                        continue;
                    }

                    // time has arrived; clear schedule so we don't immediately
                    // re-run, then marshal the preview to the UI thread
                    Interlocked.Exchange(ref _nextPreviewTicks, long.MaxValue);
                    try
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.BeginInvoke((Action)(() =>
                            {
                                try
                                {
                                    PreviewChart();
                                }
                                catch { }
                            }));
                        }
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ConfigureChart_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopPreviewWorker();
        }

        private void SchedulePreview()
        {
            // schedule preview to run at UtcNow + delay
            var when = DateTime.UtcNow.AddMilliseconds(_previewDelayMs).Ticks;
            Interlocked.Exchange(ref _nextPreviewTicks, when);
            _previewSignal.Set();
        }

        private void AttachPreviewHandlers(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                switch (c)
                {
                    case TextBox tb:
                        tb.TextChanged += (_, _) => { if (!_suspendPreview) SchedulePreview(); };
                        break;

                    case ComboBox cb:
                        cb.SelectedIndexChanged += (_, _) => { if (!_suspendPreview) SchedulePreview(); };
                        break;

                    case CheckBox chk:
                        chk.CheckedChanged += (_, _) => { if (!_suspendPreview) SchedulePreview(); };
                        break;

                    case NumericUpDown num:
                        num.ValueChanged += (_, _) => { if (!_suspendPreview) SchedulePreview(); };
                        break;

                    case CheckedListBox clb:
                        clb.ItemCheck += (_, e) => { if (!_suspendPreview) BeginInvoke((Action)SchedulePreview); };
                        break;
                }

                if (c.HasChildren) AttachPreviewHandlers(c);
            }
        }

        private void EditChart()
        {
            if (!ChartId.HasValue) return;
            if (ChartId >= Report.Charts.Count)
            {
                Common.ShowExceptionDialog(new Exception("Chart not found"), "Error loading chart configuration");
                return;
            }
            var chart = Report.Charts[ChartId.Value];

            // Set UI controls based on chart configuration
            var config = chart.Config;

            // Select the result / table first so dependent controls get populated
            try
            {
                cboResults.SelectedValue = chart.TableIndex;
            }
            catch
            {
                // ignore selection failures
            }

            if (config is PieChartConfiguration pie)
            {
                cboChartType.SelectedItem = ChartTypes.Pie;
                txtTitle.Text = pie.ChartTitle ?? string.Empty;
                cboLegend.SelectedItem = pie.LegendPosition;

                chkDoughnut.Checked = pie.InnerRadius.HasValue && pie.InnerRadius.Value > 0;
                numRadius.Value = chkDoughnut.Checked ? Convert.ToDecimal((pie.InnerRadius ?? 0) * 100) : 0;

                // Try to set pie category/value columns if the data has been loaded
                if (!string.IsNullOrWhiteSpace(pie.CategoryColumn))
                {
                    if (cboPieCategory.Items.Contains(pie.CategoryColumn))
                        cboPieCategory.SelectedItem = pie.CategoryColumn;
                    else
                        cboPieCategory.Text = pie.CategoryColumn;
                }

                if (!string.IsNullOrWhiteSpace(pie.ValueColumn))
                {
                    if (cboPieValueColumn.Items.Contains(pie.ValueColumn))
                        cboPieValueColumn.SelectedItem = pie.ValueColumn;
                    else
                        cboPieValueColumn.Text = pie.ValueColumn;
                }
            }
            else if (config is ChartConfiguration cfg)
            {
                cboChartType.SelectedItem = cfg.ChartType;
                txtTitle.Text = cfg.ChartTitle ?? string.Empty;
                cboLegend.SelectedItem = cfg.LegendPosition;

                txtYLabel.Text = cfg.YAxisLabel ?? string.Empty;
                txtXLabel.Text = cfg.XAxisLabel ?? string.Empty;
                txtYMax.Text = cfg.YAxisMax.HasValue ? cfg.YAxisMax.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                txtYMin.Text = cfg.YAxisMin.HasValue ? cfg.YAxisMin.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                txtYFormat.Text = cfg.YAxisFormat ?? string.Empty;
                chkLineFill.Checked = cfg.LineFill;
                txtPointSize.Text = cfg.GeometrySize.ToString(CultureInfo.InvariantCulture);

                // X column
                if (!string.IsNullOrWhiteSpace(cfg.XColumn))
                {
                    if (cboXCol.Items.Contains(cfg.XColumn))
                        cboXCol.SelectedItem = cfg.XColumn;
                    else
                        cboXCol.Text = cfg.XColumn;
                }

                // Series column
                if (!string.IsNullOrWhiteSpace(cfg.SeriesColumn))
                {
                    if (cboSeries.Items.Contains(cfg.SeriesColumn))
                        cboSeries.SelectedItem = cfg.SeriesColumn;
                    else
                        cboSeries.SelectedItem = string.Empty;
                }

                // Metrics - clear then select configured metrics
                for (int i = 0; i < chkMetrics.Items.Count; i++)
                {
                    chkMetrics.SetItemChecked(i, false);
                }

                if (cfg.MetricColumns != null && cfg.MetricColumns.Length > 0)
                {
                    var set = new HashSet<string>(cfg.MetricColumns, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < chkMetrics.Items.Count; i++)
                    {
                        var item = chkMetrics.Items[i]?.ToString();
                        if (!string.IsNullOrEmpty(item) && set.Contains(item))
                        {
                            chkMetrics.SetItemChecked(i, true);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(cfg.MetricColumn))
                {
                    for (int i = 0; i < chkMetrics.Items.Count; i++)
                    {
                        if (string.Equals(chkMetrics.Items[i]?.ToString(), cfg.MetricColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            chkMetrics.SetItemChecked(i, true);
                            break;
                        }
                    }
                }
            }

            // Enforce single-selection rules after applying configuration
            ChartSeries_Changed(this, EventArgs.Empty);
        }

        private void ChartType_Changed(object sender, EventArgs e)
        {
            UpdateTabPages();
        }

        private bool SingleMetricMode()
        {
            // Single metric when chart is Pie or a series column is selected
            var ct = SelectedChartType;
            if (ct.HasValue && ct.Value == ChartTypes.Pie)
            {
                return true;
            }

            return IsSeriesSelected();
        }

        private ChartTypes? SelectedChartType
        {
            get
            {
                // Try SelectedValue (common when bound), then SelectedItem
                if (cboChartType.SelectedValue is ChartTypes cv)
                {
                    return cv;
                }

                if (cboChartType.SelectedItem is ChartTypes ct)
                {
                    return ct;
                }

                // Fallback: try parse
                if (Enum.TryParse<ChartTypes>(cboChartType.SelectedValue?.ToString(), out var parsed))
                {
                    return parsed;
                }

                return null;
            }
        }

        private bool IsSeriesSelected()
        {
            // cboSeries.DataSource is a simple list where first item is an empty string.
            // Treat any SelectedIndex > 0 or a non-empty SelectedItem as a series being selected.
            if (cboSeries.SelectedIndex > 0) return true;
            var series = cboSeries.SelectedItem?.ToString();
            return !string.IsNullOrEmpty(series);
        }

        private void EnsureSingleMetricSelection()
        {
            var checkedIndices = chkMetrics.CheckedIndices.Cast<int>().ToList();
            if (checkedIndices.Count <= 1) return;

            // keep first checked, uncheck the rest
            for (int i = 0; i < chkMetrics.Items.Count; i++)
            {
                if (i != checkedIndices[0] && chkMetrics.GetItemChecked(i))
                {
                    chkMetrics.SetItemChecked(i, false);
                }
            }
        }

        private void chkMetrics_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // If entering a checked state and single-mode is required, uncheck all other items.
            if (e.NewValue != CheckState.Checked) return;
            if (!SingleMetricMode()) return;

            // Delay action until after the current check state change has been applied
            BeginInvoke((Action)(() =>
            {
                for (int i = 0; i < chkMetrics.Items.Count; i++)
                {
                    if (i != e.Index && chkMetrics.GetItemChecked(i))
                    {
                        chkMetrics.SetItemChecked(i, false);
                    }
                }
            }));
        }

        private void ChartSeries_Changed(object sender, EventArgs e)
        {
            if (!SingleMetricMode()) return;
            EnsureSingleMetricSelection();
        }

        private void UpdateTabPages()
        {
            chkLineFill.Enabled = SelectedChartType == ChartTypes.Line;
            txtPointSize.Enabled = SelectedChartType == ChartTypes.Line;
            lblPointSize.Enabled = SelectedChartType == ChartTypes.Line;
            var ct = SelectedChartType;
            if (!ct.HasValue) return;
            var chartType = ct.Value;

            // Determine desired pages for this chart type (in the order they should appear)
            List<TabPage> desiredPages = chartType == ChartTypes.Pie
                ? new List<TabPage> { tabPieData, tabOptions }
                : new List<TabPage> { tabData, tabAxis, tabOptions };

            // If the current TabPages sequence already matches the desired sequence, do nothing.
            var currentPages = tabConfig.TabPages.Cast<TabPage>().ToList();
            if (currentPages.Count == desiredPages.Count && currentPages.SequenceEqual(desiredPages))
            {
                return;
            }

            // Otherwise clear and re-add desired pages. Clearing/re-adding is acceptable when switching
            // between the two options and keeps the logic simple.
            tabConfig.TabPages.Clear();
            foreach (var p in desiredPages)
            {
                tabConfig.TabPages.Add(p);
            }
        }

        private void Results_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ReportDS == null || cboResults.SelectedIndex < 0) return;

            var table = ReportDS.Tables[cboResults.SelectedIndex];
            var cols = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).OrderBy(col => col);

            var metricCols = table.Columns.Cast<DataColumn>()
                .Where(c => c.DataType.IsNumericType())
                .Select(c => c.ColumnName)
                .OrderBy(col => col)
                .ToArray();

            cboXCol.DataSource = cols.ToList();
            cboPieCategory.DataSource = cols.ToList();
            cboPieValueColumn.DataSource = metricCols.ToList();
            cboSeries.DataSource = (new string[] { "" }).Union(cols).ToList();
            chkMetrics.Items.Clear();
            if (metricCols.Length > 0)
            {
                chkMetrics.Items.AddRange(metricCols);
            }
            // enforce single-selection rules after loading metrics
            ChartSeries_Changed(this, EventArgs.Empty);
            PreviewChart();
        }

        private ChartConfigurationBase GetChartConfiguration()
        {
            if (cboChartType.SelectedValue == null) return null;
            var chartType = (ChartTypes)cboChartType.SelectedValue;
            if (chartType == ChartTypes.Pie)
            {
                return new PieChartConfiguration
                {
                    ValueColumn = cboPieValueColumn.SelectedValue.ToString(),
                    CategoryColumn = cboPieCategory.SelectedValue.ToString(),
                    ChartTitle = txtTitle.Text,
                    LegendPosition = cboLegend.SelectedValue as LegendPosition? ?? LegendPosition.Bottom,
                    InnerRadius = chkDoughnut.Checked ? Convert.ToDouble(numRadius.Value / 100) : 0,
                };
            }
            else
            {
                var metrics = chkMetrics.CheckedItems.Cast<string>().ToArray();
                return new ChartConfiguration
                {
                    ChartType = chartType,
                    MetricColumns = metrics.Count() > 1 ? metrics : null,
                    MetricColumn = metrics.Count() == 1 ? metrics[0] : null,
                    SeriesColumn = cboSeries.SelectedValue?.ToString(),
                    XColumn = cboXCol.SelectedValue.ToString(),
                    ChartTitle = txtTitle.Text,
                    LegendPosition = cboLegend.SelectedValue as LegendPosition? ?? LegendPosition.Bottom,
                    YAxisLabel = txtYLabel.Text,
                    XAxisLabel = txtXLabel.Text,
                    YAxisMax = double.TryParse(txtYMax.Text, out var yMax) ? yMax : (double?)null,
                    YAxisMin = double.TryParse(txtYMin.Text, out var yMin) ? yMin : (double?)null,
                    YAxisFormat = txtYFormat.Text,
                    LineFill = chkLineFill.Checked,
                    GeometrySize = double.TryParse(txtPointSize.Text, out var pointSize) ? pointSize : ChartConfiguration.DefaultGeometrySize,
                };
            }
        }

        private void PreviewChart()
        {
            try
            {
                var config = GetChartConfiguration();
                var chart = ChartHelper.GetChartControlFromDataTable(ReportDS.Tables[cboResults.SelectedIndex], config);
                var pnl = _chartLayoutHelper.CreateResizablePanel(chart, null, false, config.ChartTitle);

                pnlChart.Controls.Clear();
                pnlChart.Controls.Add(pnl);
            }
            catch (Exception ex)
            {
                pnlChart.Controls.Clear();
                var message = ex.Message.Contains(':') ? ex.Message[(ex.Message.IndexOf(':') + 1)..].TrimStart() : ex.Message;
                pnlChart.Controls.Add(new Label { Text = "Waiting for valid chart configuration: " + message, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
                pnlChart.Controls.Add(new Label { Text = "Chart Preview", Font = new Font(pnlChart.Font.FontFamily, 30, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, Dock = DockStyle.Top, Height = 100 });
            }
        }

        private void chkDoughnut_CheckedChanged(object sender, EventArgs e)
        {
            numRadius.Value = chkDoughnut.Checked ? 50 : 0;
            numRadius.Enabled = chkDoughnut.Checked;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            var config = GetChartConfiguration();
            try
            {
                config.Validate();
                var chart = new CustomReportChart() { Config = config, TableIndex = (int)cboResults.SelectedValue };
                if (ChartId.HasValue)
                {
                    Report.Charts[ChartId.Value] = chart;
                }
                else
                {
                    Report.Charts.Add(chart);
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving chart configuration: {ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}