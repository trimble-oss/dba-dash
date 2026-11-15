using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class ObjectExecution : UserControl, IMetricChart, IThemedControl
    {
        private Label lblError = new Label() { Dock = DockStyle.Fill, Visible = false, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

        /// <param name="isError">False for a message that isn't a fault - no stats for the period - shown in the normal text colour</param>
        private void ToggleError(bool show, string message = "", bool isError = true)
        {
            lblError.Text = message;
            lblError.ForeColor = !isError ? DBADashUser.SelectedTheme.ForegroundColor
                : DBADashUser.IsDarkTheme ? DashColors.White : DashColors.Fail;
            lblError.Visible = show;
            objectExecChart.Visible = !show;
        }

        private readonly LoadingOverlay loading;

        public ObjectExecution()
        {
            InitializeComponent();
            this.Controls.Add(lblError);
            lblError.BringToFront();
            loading = new LoadingOverlay(this);
            // Stop a load still running on the server when the control goes away
            Disposed += (_, _) => refreshCts?.Cancel();
        }

        private bool isLoading;

        /// <summary>
        /// Apply settings from the Metric object to the UI controls.
        /// Called during Load after menu items are created.
        /// </summary>
        private void LoadSettingsFromMetric()
        {
            isLoading = true;
            try
            {
                // Apply IncludeOther
                includeOtherToolStripMenuItem.Checked = Metric.IncludeOther;

                // Apply TopRows
                var top = Metric.TopRows;
                if (top > 0)
                {
                    var matching = tsTop.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(i => i != includeOtherToolStripMenuItem && i.Tag != null && i.Tag.ToString() == top.ToString());
                    if (matching != null)
                    {
                        tsTop.Tag = matching.Tag;
                        tsTop.Text = "Top " + matching.Tag;
                        foreach (var itm in tsTop.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item != includeOtherToolStripMenuItem))
                        {
                            itm.Checked = itm == matching;
                        }
                    }
                }

                // Apply Source and Measure
                UpdateSourceMenu();
                PopulateMeasures();
            }
            catch(Exception ex)
            {
                Common.ShowExceptionDialog(ex, "Error loading object execution stats saved settings");
            }
            finally
            {
                isLoading = false;
            }
        }

        public class DateModel
        {
            public DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        private int TopRows => Metric.TopRows;
        private bool IncludeOther => Metric.IncludeOther;

        private int instanceID;

        private int mins;
        private long objectID;
        private int databaseid;
        private int dateGrouping;
        private LegendPosition legendPosition = LegendPosition.Hidden;

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public void SetContext(DBADashContext _context)
        {
            if (_context == null) return;
            instanceID = _context.InstanceID;
            databaseid = _context.DatabaseID;
            objectID = _context.Type.IsQueryStoreObjectType() ? _context.ObjectID : 0;
            RefreshData();
        }

        /// <summary>
        /// Query stats have no object filter, so for an object the chart stays on object execution stats - showing the
        /// whole database's statements on an object's node would read as that object's.  The saved choice is kept for
        /// when the chart is next shown at instance or database level.
        /// </summary>
        private bool ShowQueryStats => Metric.Source == ObjectExecutionMetric.ExecutionStatsSources.Queries && objectID <= 0;

        private readonly ToolStripDropDownButton tsSource = new()
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = "Switch between object execution stats (procedures, functions and triggers) and statement level query stats"
        };

        private readonly ToolStripMenuItem tsSourceObjects = new("Object Execution Stats")
        {
            ToolTipText = "Procedures, functions and triggers, from the ObjectExecutionStats collection"
        };

        private readonly ToolStripMenuItem tsSourceQueries = new("Query Stats (Statements)")
        {
            ToolTipText = "Statements, qualified by the object they belong to, from the QueryStats collection.\nRequires \"Query Stats Top N\" to be set for the connection.\nThe time of day and day of week filters are not applied."
        };

        private void UpdateSourceMenu()
        {
            tsSourceObjects.Checked = !ShowQueryStats;
            tsSourceQueries.Checked = ShowQueryStats;
            tsSource.Text = ShowQueryStats ? "Queries" : "Objects";
            tsSource.Visible = objectID <= 0;
        }

        private void Source_Click(object sender, EventArgs e)
        {
            var source = sender == tsSourceQueries ? ObjectExecutionMetric.ExecutionStatsSources.Queries : ObjectExecutionMetric.ExecutionStatsSources.Objects;
            if (Metric.Source == source) return;
            Metric.Source = source;
            UpdateSourceMenu();
            PopulateMeasures();
            RefreshData();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible;
            set => tsUp.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObjectExecutionMetric Metric { get; set; } = new();

        IMetric IMetricChart.Metric => Metric;

        public void RefreshData(int instanceID, long objectID, int databaseID)
        {
            this.instanceID = instanceID;
            this.objectID = objectID;
            this.databaseid = databaseID;
            RefreshData();
        }

        public void RefreshData(int InstanceID)
        {
            RefreshData(InstanceID, -1, -1);
        }

        private CancellationTokenSource refreshCts;

        public async void RefreshData()
        {
            // A newer refresh supersedes one still loading
            refreshCts?.Cancel();
            var cts = new CancellationTokenSource();
            refreshCts = cts;
            try
            {
                if (mins != DateRange.DurationMins)
                {
                    dateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 65);
                    if (dateGrouping < 1)
                    {
                        dateGrouping = 1;
                    }
                    tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
                    mins = DateRange.DurationMins;
                }
                if (instanceID == 0)
                {
                    ToggleError(true, "No instance selected");
                    return;
                }
                ToggleError(false);
                // The object can change between refreshes, and with it whether query stats are available
                UpdateSourceMenu();
                if (measuresForQueries != ShowQueryStats) PopulateMeasures();
                var showQueryStats = ShowQueryStats;
                lblExecution.Text = (showQueryStats ? "Query Stats" : "Execution Stats") + (databaseid > 0 ? ": Database" : ": Instance");
                toolStrip1.Tag = databaseid > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
                toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);

                if (showQueryStats)
                {
                    await RefreshQueryStatsAsync(cts);
                    return;
                }

                loading.Show();
                var dt = await CommonData.ObjectExecutionStatsAsync(instanceID, databaseid, objectID, dateGrouping, Metric.Measure, DateRange.FromUTC, DateRange.ToUTC, default, TopRows, IncludeOther, cts.Token);
                if (cts.IsCancellationRequested || IsDisposed) return;
                // Before drawing, not after: while the overlay shows, the chart under it is laid out at a different size, and LiveCharts
                // drawn at that size and then resized back could be left blank until the next refresh.  The overlay only shows on loads
                // over its delay - switching instance, rather than a quick refresh - which is why it was intermittent
                loading.Hide();

                if (dt == null || dt.Rows.Count == 0)
                {
                    objectExecChart.Series = Array.Empty<ISeries>();
                    objectExecChart.XAxes = Array.Empty<Axis>();
                    objectExecChart.YAxes = Array.Empty<Axis>();
                    // Said rather than left as an empty box, which looks like the chart failed to draw
                    ToggleError(true, $"No execution stats for this period (instance {instanceID}, {DateRange.FromUTC.ToAppTimeZone():g} to {DateRange.ToUTC.ToAppTimeZone():g})", isError: false);
                    return;
                }

                // Add a computed column for ObjectName (combination of DatabaseName and object_name)
                if (!dt.Columns.Contains("ObjectName"))
                {
                    dt.Columns.Add("ObjectName", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        row["ObjectName"] = row["DatabaseName"] + " | " + row["object_name"];
                    }
                }

                // Use ChartHelper with SeriesColumn to group by ObjectName
                var config = new ChartConfiguration
                {
                    XColumn = "SnapshotDate",
                    MetricColumn = "Measure",
                    SeriesColumn = "ObjectName",  // Group data by ObjectName - each becomes a series
                    ChartType = ChartTypes.StackedColumn,
                    LegendPosition = legendPosition,
                    XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                    XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                    YAxisLabel = measures[Metric.Measure].DisplayName,
                    YAxisFormat = "0.0",
                    YAxisMin = 0,
                    DateUnit = TimeSpan.FromMinutes(dateGrouping)
                };

                ChartHelper.UpdateChart(objectExecChart, dt, config);
            }
            catch (Exception) when (cts.IsCancellationRequested)
            {
                // Superseded by a newer refresh
            }
            catch (Exception ex)
            {
                ToggleError(true, "Error loading data: " + ex.Message);
            }
            finally
            {
                if (refreshCts == cts)
                {
                    refreshCts = null;
                    loading.Hide();
                }
                cts.Dispose();
            }
        }

        /// <summary>
        /// Statement level stats from dbo.QueryStatsCharts_Get - the same data as the Query Stats tab's Over Time
        /// chart: the top N statements, with everything else (the tail past the top N and the collection's own
        /// rollups) as {Other} unless Include Other is unticked.
        /// </summary>
        private async System.Threading.Tasks.Task RefreshQueryStatsAsync(CancellationTokenSource cts)
        {
            var measure = queryMeasures.TryGetValue(Metric.QueryMeasure, out var m) ? m : queryMeasures["CPU_PerSecond"];
            loading.Show();
            var (dt, message) = await QueryStatsChartDataAsync(measure, cts.Token);
            if (cts.IsCancellationRequested || IsDisposed) return;
            // Before drawing - see the note in RefreshData
            loading.Hide();

            if (message != null || dt == null || dt.Rows.Count == 0)
            {
                objectExecChart.Series = Array.Empty<ISeries>();
                objectExecChart.XAxes = Array.Empty<Axis>();
                objectExecChart.YAxes = Array.Empty<Axis>();
                ToggleError(true, message ?? $"No query stats for this period (instance {instanceID}, {DateRange.FromUTC.ToAppTimeZone():g} to {DateRange.ToUTC.ToAppTimeZone():g})", isError: false);
                return;
            }

            if (!IncludeOther)
            {
                // @ExcludeRollups only drops the collection's own rollups - the statements past the top N still come back as {Other}
                foreach (var row in dt.Rows.Cast<DataRow>().Where(r => Convert.ToString(r["Series"]) == "{Other}").ToList())
                {
                    dt.Rows.Remove(row);
                }
                if (dt.Rows.Count == 0)
                {
                    objectExecChart.Series = Array.Empty<ISeries>();
                    objectExecChart.XAxes = Array.Empty<Axis>();
                    objectExecChart.YAxes = Array.Empty<Axis>();
                    ToggleError(true, "No individual statements to show for this period - tick Include Other to see the rest of the workload", isError: false);
                    return;
                }
            }

            DateHelper.ConvertUTCToAppTimeZone(ref dt, new List<string> { "TimeBucket" });
            if (measure.Divisor != 1)
            {
                // A new column rather than writing back to Value, whose type is the proc's decimal
                dt.Columns.Add("ChartValue", typeof(double));
                foreach (DataRow row in dt.Rows)
                {
                    row["ChartValue"] = row["Value"] == DBNull.Value ? DBNull.Value : Convert.ToDouble(row["Value"]) / measure.Divisor;
                }
            }

            var config = new ChartConfiguration
            {
                XColumn = "TimeBucket",
                MetricColumn = measure.Divisor != 1 ? "ChartValue" : "Value",
                SeriesColumn = "Series",
                ChartType = ChartTypes.StackedColumn,
                LegendPosition = legendPosition,
                XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                YAxisLabel = measure.DisplayName,
                YAxisFormat = "0.0",
                YAxisMin = 0,
                DateUnit = TimeSpan.FromMinutes(Math.Max(1, queryDateGrouping))
            };

            ChartHelper.UpdateChart(objectExecChart, dt, config);
        }

        /// <summary>The bucket size the last query stats load used.  The proc rounds anything under a minute up to one.</summary>
        private int queryDateGrouping;

        private async System.Threading.Tasks.Task<(DataTable dt, string message)> QueryStatsChartDataAsync(QueryMeasure measure, CancellationToken token)
        {
            queryDateGrouping = Math.Max(1, dateGrouping);
            await using var cn = new Microsoft.Data.SqlClient.SqlConnection(Common.ConnectionString);
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand("dbo.QueryStatsCharts_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", databaseid);
            cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("@DateGroupingMin", queryDateGrouping);
            cmd.Parameters.AddWithValue("@TopN", TopRows);
            cmd.Parameters.AddWithValue("@Measure", measure.Measure);
            cmd.Parameters.AddWithValue("@Units", measure.Units);
            // Unticking Include Other leaves out the {Other} band - the tail past the top N and the collection's rollups
            cmd.Parameters.AddWithValue("@ExcludeRollups", !IncludeOther);
            await cn.OpenAsync(token).ConfigureAwait(false);
            await using var rdr = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
            var dt = new DataTable();
            // Only the first result set - the over time series.  The pies that follow are for the Query Stats tab.
            dt.Load(rdr);
            // Where query stats aren't collected for the instance the proc returns a message in place of the charts
            if (dt.Columns.Contains("Message"))
            {
                return (null, dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0]["Message"]) : "Query stats are not collected for this instance");
            }
            return (dt, null);
        }

        private sealed class QueryMeasure
        {
            public string DisplayName { get; init; }

            /// <summary>@Measure for dbo.QueryStatsCharts_Get</summary>
            public string Measure { get; init; }

            /// <summary>@Units for dbo.QueryStatsCharts_Get</summary>
            public string Units { get; init; }

            /// <summary>The proc returns CPU and duration in ms - totals are divided down to seconds to match the object stats measures</summary>
            public double Divisor { get; init; } = 1;
        }

        private readonly Dictionary<string, QueryMeasure> queryMeasures = new()
        {
            { "CPU_PerSecond", new QueryMeasure { DisplayName = "CPU (ms/sec)", Measure = "CPU", Units = "PerSecond" } },
            { "Duration_PerSecond", new QueryMeasure { DisplayName = "Duration (ms/sec)", Measure = "Duration", Units = "PerSecond" } },
            { "Executions_PerSecond", new QueryMeasure { DisplayName = "Executions/sec", Measure = "Executions", Units = "PerSecond" } },
            { "Reads_PerSecond", new QueryMeasure { DisplayName = "Logical Reads/sec", Measure = "Reads", Units = "PerSecond" } },
            { "Writes_PerSecond", new QueryMeasure { DisplayName = "Logical Writes/sec", Measure = "Writes", Units = "PerSecond" } },
            { "CPU_Total", new QueryMeasure { DisplayName = "Total CPU", Measure = "CPU", Units = "Total", Divisor = 1000 } },
            { "Duration_Total", new QueryMeasure { DisplayName = "Total Duration", Measure = "Duration", Units = "Total", Divisor = 1000 } },
            { "Executions_Total", new QueryMeasure { DisplayName = "Execution Count", Measure = "Executions", Units = "Total" } },
            { "Reads_Total", new QueryMeasure { DisplayName = "Total Logical Reads", Measure = "Reads", Units = "Total" } },
            { "Writes_Total", new QueryMeasure { DisplayName = "Total Logical Writes", Measure = "Writes", Units = "Total" } }
        };

        /// <summary>Which measure list the Measures menu holds, so a refresh only rebuilds it when that changes</summary>
        private bool measuresForQueries;

        /// <summary>
        /// Fills the Measures menu for the source being shown - object stats and query stats measure different things,
        /// and each keeps its own saved measure.
        /// </summary>
        private void PopulateMeasures()
        {
            var forQueries = ShowQueryStats;
            measuresForQueries = forQueries;
            var items = forQueries
                ? queryMeasures.Select(m => (Key: m.Key, m.Value.DisplayName))
                : measures.Select(m => (Key: m.Key, m.Value.DisplayName));
            var selected = forQueries ? Metric.QueryMeasure : Metric.Measure;

            tsMeasures.DropDownItems.Clear();
            foreach (var (key, displayName) in items)
            {
                ToolStripMenuItem itm = new(displayName) { Name = key, Checked = key == selected };
                if (itm.Checked) tsMeasures.Text = displayName;
                itm.Click += Itm_Click;
                tsMeasures.DropDownItems.Add(itm);
            }
        }

        private class Measure
        {
            public string Name { get; set; }
            public string DisplayName { get; init; }
            public string LabelFormat { get; init; }
        }

        private class Measures : Dictionary<string, Measure>
        {
            public void Add(string Name, string displayName, string labelFormat)
            {
                Add(Name, new Measure() { Name = Name, DisplayName = displayName, LabelFormat = labelFormat });
            }
        }

        private readonly Measures measures = new()
            {
                {"TotalDuration", "Total Duration","#,##0.000 sec"},
                {"duration_ms_per_sec","Duration (ms/sec)","#,##0.000 ms/sec"},
                {"AvgDuration", "Avg Duration","#,##0.000 sec"},
                {"TotalCPU", "Total CPU","#,##0.000 sec"},
                {"cpu_ms_per_sec", "CPU (ms/sec)","#,##0.000 ms/sec"},
                {"AvgCPU","Avg CPU", "#,##0.000  sec"},
                {"ExecutionCount", "Execution Count","N0"},
                {"ExecutionsPerMin", "Executions Per Min","#,##0.000"},
                {"MaxExecutionsPerMin", "Max Executions Per Min","#,##0.000"},
                {"TotalLogicalReads","Total Logical Reads","N0" },
                {"AvgLogicalReads","Avg Logical Reads","N0" },
                {"TotalPhysicalReads","Total Physical Reads" ,"N0"},
                {"AvgPhysicalReads","Avg Physical Reads" ,"N0"},
                {"TotalWrites","Total Writes" ,"N0"},
                {"AvgWrites","Avg Writes","N0" }
            };

        private void ObjectExecution_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGroup, TsDateGrouping_Click);
            tsSourceObjects.Click += Source_Click;
            tsSourceQueries.Click += Source_Click;
            tsSource.DropDownItems.AddRange(new ToolStripItem[] { tsSourceObjects, tsSourceQueries });
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(lblExecution) + 1, tsSource);
            LoadSettingsFromMetric();
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void Itm_Click(object sender, EventArgs e)
        {
            var tsItm = ((ToolStripMenuItem)sender);
            if (measuresForQueries)
            {
                Metric.QueryMeasure = tsItm.Name;
            }
            else
            {
                Metric.Measure = tsItm.Name;
            }
            foreach (ToolStripMenuItem itm in tsMeasures.DropDownItems)
            {
                itm.Checked = itm == tsItm;
            }
            tsMeasures.Text = tsItm.Text;
            RefreshData(instanceID, objectID, databaseid);
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void Top_Select(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            tsTop.Tag = ts.Tag;
            tsTop.Text = "Top " + ts.Tag;
            foreach (var itm in tsTop.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item != includeOtherToolStripMenuItem))
            {
                itm.Checked = itm == ts;
            }
            if (!isLoading)
            {
                Metric.TopRows = Convert.ToInt32(ts.Tag);
            }
            RefreshData();
        }

        private void includeOtherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Metric.IncludeOther = includeOtherToolStripMenuItem.Checked;
            RefreshData();
        }

        private void SetLegendPosition(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            Enum.TryParse(item.Tag.ToString(), out legendPosition);
            foreach (ToolStripMenuItem menuItem in tsLegend.DropDownItems.OfType<ToolStripMenuItem>())
            {
                menuItem.Checked = menuItem == item;
            }
            // Through ChartLegends, so the position picked here wins over the chart menu's Legend choice
            DBADashGUI.Charts.ChartLegends.SetPosition(objectExecChart, legendPosition);
        }

        public void ApplyTheme(BaseTheme theme)
        {
            objectExecChart.ApplyTheme();
            toolStrip1.ApplyTheme();
            lblError.ForeColor = DBADashUser.IsDarkTheme ? DashColors.White : DashColors.Fail;
        }
    }
}