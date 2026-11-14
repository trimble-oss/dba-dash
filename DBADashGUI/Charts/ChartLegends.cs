using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.WinForms;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Legend on the chart context menu: the user's choice to show or hide the legend, over what each chart is configured
    /// with, for one chart or for every chart in the same view.
    ///
    /// All Charts is there because a view's charts usually share a series column - a line per service, say - so they share
    /// a legend, and showing it one chart at a time would be a click per chart.  The view is the outermost user control the
    /// chart sits in (a report, a tab), so charts in another tab are left alone.  A single chart's choice goes over the
    /// view's until All Charts is next used.
    ///
    /// The choices are held against the view, not the chart control: a report reload rebuilds its charts as new controls,
    /// and the choices have to carry over to them.  A single chart is recognised across a rebuild by its Tag (a report's
    /// chart definition), so a chart without a Tag keeps its own choice only as long as the control lives.  Nothing is
    /// saved - a new view goes by each chart's configuration again.
    /// </summary>
    internal static class ChartLegends
    {
        /// <summary>Where the legend goes when the user turns legends on for a chart configured without one</summary>
        private const LegendPosition DefaultShownPosition = LegendPosition.Bottom;

        private sealed class Configured
        {
            public LegendPosition Position;
        }

        /// <summary>The user's choices for one view</summary>
        private sealed class ViewChoices
        {
            /// <summary>Show Legend on All Charts - null until it's used</summary>
            public bool? All;

            /// <summary>Show Legend on This Chart, by the chart's Tag (or the chart itself where it has none)</summary>
            public readonly Dictionary<object, bool> Charts = new();
        }

        /// <summary>The legend position each chart is configured with, before the user's choice</summary>
        private static readonly ConditionalWeakTable<Control, Configured> ConfiguredPositions = new();

        /// <summary>The user's choices, by view</summary>
        private static readonly ConditionalWeakTable<Control, ViewChoices> Choices = new();

        /// <summary>
        /// Records the legend position the chart is configured with and sets the chart's legend - to that, or to the
        /// user's choice for the view it's in.  Call in place of setting LegendPosition, so the choice survives a redraw.
        /// </summary>
        public static void SetConfigured(Control chart, LegendPosition position)
        {
            if (chart == null) return;
            if (!ConfiguredPositions.TryGetValue(chart, out var configured))
            {
                configured = new Configured();
                ConfiguredPositions.Add(chart, configured);
                // A chart is configured before it's added to its view, so the view's choices can only be applied once
                // it's in it.  VisibleChanged fires when the chart (or a panel it's in) is added to a shown parent;
                // HandleCreated covers a view that isn't shown yet.
                chart.VisibleChanged += (_, _) => { if (chart.Visible) Apply(chart); };
                chart.HandleCreated += (_, _) => Apply(chart);
            }
            configured.Position = position;
            Apply(chart);
        }

        /// <summary>True when the chart is showing its legend</summary>
        public static bool IsShown(Control chart) => GetPosition(chart) is { } p && p != LegendPosition.Hidden;

        /// <summary>True when every chart in the chart's view is showing its legend</summary>
        public static bool AllShown(Control chart)
        {
            var scope = GetScope(chart);
            if (scope == null) return IsShown(chart);
            foreach (var c in ChartsIn(scope))
            {
                if (!IsShown(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Shows or hides the legend on every chart in the chart's view.  Clears the choices made for single charts, so
        /// they all end up the same.
        /// </summary>
        public static void SetShown(Control chart, bool show)
        {
            var scope = GetScope(chart);
            if (scope == null)
            {
                SetChartShown(chart, show);
                return;
            }
            var choices = Choices.GetOrCreateValue(scope);
            choices.All = show;
            choices.Charts.Clear();
            foreach (var c in ChartsIn(scope))
            {
                Apply(c);
            }
        }

        /// <summary>Shows or hides the legend on this chart alone</summary>
        public static void SetChartShown(Control chart, bool show)
        {
            if (chart == null) return;
            // A chart that isn't in a view holds its own choice
            var choices = Choices.GetOrCreateValue(GetScope(chart) ?? chart);
            choices.Charts[ChartKey(chart)] = show;
            Apply(chart);
        }

        /// <summary>Adds the Legend menu - this chart, or every chart in the view - to a chart's context menu</summary>
        public static void AddMenuItem(Control chart, ContextMenuStrip cms)
        {
            if (chart is not (CartesianChart or PieChart) || cms == null) return;
            var thisChart = new ToolStripMenuItem("Show on This Chart") { ToolTipText = "Show or hide this chart's legend" };
            thisChart.Click += (_, _) => SetChartShown(chart, !thisChart.Checked);
            var allCharts = new ToolStripMenuItem("Show on All Charts") { ToolTipText = "Show or hide the legend on every chart in this view" };
            allCharts.Click += (_, _) => SetShown(chart, !allCharts.Checked);
            var legend = new ToolStripMenuItem("Legend");
            legend.DropDownItems.AddRange(thisChart, allCharts);
            cms.Opening += (_, _) =>
            {
                thisChart.Checked = IsShown(chart);
                allCharts.Checked = AllShown(chart);
            };
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(legend);
        }

        /// <summary>
        /// What identifies a chart across a rebuild: a report chart's title (the definition object can be recreated when
        /// the report is loaded again), else its Tag, where the host gives it one.
        /// </summary>
        private static object ChartKey(Control chart) => chart.Tag switch
        {
            CustomReports.CustomReportChart { Title: { Length: > 0 } title } => "chart:" + title,
            CustomReports.CustomReportChart { Config.ChartTitle: { Length: > 0 } title } => "chart:" + title,
            { } tag => tag,
            _ => chart
        };

        private static void Apply(Control chart)
        {
            if (!ConfiguredPositions.TryGetValue(chart, out var configured)) return;
            var position = configured.Position;
            if (GetChoice(chart) is { } show)
            {
                position = !show ? LegendPosition.Hidden
                    : position == LegendPosition.Hidden ? DefaultShownPosition
                    : position;
            }
            switch (chart)
            {
                case CartesianChart c when c.LegendPosition != position: c.LegendPosition = position; break;
                case PieChart p when p.LegendPosition != position: p.LegendPosition = position; break;
            }
        }

        /// <summary>The user's choice for the chart - its own, else its view's - or null to go by its configuration</summary>
        private static bool? GetChoice(Control chart)
        {
            var key = ChartKey(chart);
            var scope = GetScope(chart);
            if (scope != null && Choices.TryGetValue(scope, out var viewChoices))
            {
                if (viewChoices.Charts.TryGetValue(key, out var chartShow)) return chartShow;
                if (viewChoices.All is { } all) return all;
            }
            // A choice made while the chart wasn't in a view
            return Choices.TryGetValue(chart, out var own) && own.Charts.TryGetValue(key, out var ownShow) ? ownShow : null;
        }

        private static LegendPosition? GetPosition(Control chart) => chart switch
        {
            CartesianChart c => c.LegendPosition,
            PieChart p => p.LegendPosition,
            _ => null
        };

        /// <summary>
        /// The view the chart belongs to: its outermost user control, short of the form.  Null for a chart that isn't
        /// in a user control - a chart on its own isn't a view, so the choice would have nothing to be held against.
        /// </summary>
        private static Control GetScope(Control chart)
        {
            Control scope = null;
            for (var c = chart?.Parent; c != null; c = c.Parent)
            {
                if (c is UserControl) scope = c;
            }
            return scope ?? (chart?.Parent != null ? chart.TopLevelControl ?? chart.Parent : null);
        }

        /// <summary>The charts under <paramref name="root"/> that this class manages the legend for</summary>
        private static IEnumerable<Control> ChartsIn(Control root)
        {
            var stack = new Stack<Control>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (ConfiguredPositions.TryGetValue(c, out _)) yield return c;
                foreach (Control child in c.Controls) stack.Push(child);
            }
        }
    }
}
