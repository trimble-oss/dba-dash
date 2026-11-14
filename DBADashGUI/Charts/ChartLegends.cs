using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.WinForms;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Marks a control as a scope for the Legend menu's All Charts - a report view, say, so a child report shown inside
    /// another report has its own.  A tab page is a scope without this.
    /// </summary>
    internal interface IChartLegendScope;

    /// <summary>
    /// Legend on the chart context menu: the user's choice to show or hide the legend, over what each chart is configured
    /// with, for one chart or for every chart in the same view.
    ///
    /// All Charts is there because a view's charts usually share a series column - a line per service, say - so they share
    /// a legend, and showing it one chart at a time would be a click per chart.  The view is the nearest tab page or
    /// <see cref="IChartLegendScope"/> the chart sits in, so charts in another tab or child report are left alone.  A single
    /// chart's choice goes over the view's until All Charts is next used.
    ///
    /// The choices are held against the view, not the chart control: a reload rebuilds charts as new controls, and the
    /// choices have to carry over to them.  A chart is recognised across a rebuild by the key its host gives it
    /// (<see cref="SetKey"/>), on the chart or a control it sits in, else its Tag; a chart with neither keeps its own choice
    /// only as long as the control lives.  Nothing is saved - a new view goes by each chart's configuration again.
    ///
    /// A legend position the user picks some other way (a view's own legend toolbar) is the latest choice for that chart,
    /// so it goes through <see cref="SetPosition"/> and wins over the menu's.
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

            /// <summary>
            /// Single chart choices, by chart key.  True/false from Show on This Chart; null where the user picked a
            /// position some other way, so the chart goes by its configuration whatever All Charts says.
            /// </summary>
            public readonly Dictionary<object, bool?> Charts = new();
        }

        /// <summary>The legend position each chart is configured with, before the user's choice</summary>
        private static readonly ConditionalWeakTable<Control, Configured> ConfiguredPositions = new();

        /// <summary>The user's choices, by view</summary>
        private static readonly ConditionalWeakTable<Control, ViewChoices> Choices = new();

        /// <summary>Keys hosts give their charts, to know them again when they're rebuilt</summary>
        private static readonly ConditionalWeakTable<Control, object> Keys = new();

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

        /// <summary>
        /// A legend position the user picked for the chart some other way - a view's legend toolbar.  It's the latest
        /// choice, so it replaces the Legend menu's for this chart, including All Charts.
        /// </summary>
        public static void SetPosition(Control chart, LegendPosition position)
        {
            if (chart == null) return;
            var choices = Choices.GetOrCreateValue(GetScope(chart) ?? chart);
            SetChartChoice(chart, choices, null);
            SetConfigured(chart, position);
        }

        /// <summary>
        /// Forgets the Legend menu choices for the view <paramref name="within"/> is in - for a view's legend toolbar that
        /// rebuilds every chart with the position picked, which is then the latest choice for all of them.
        /// </summary>
        public static void ClearChoices(Control within)
        {
            if (within == null) return;
            // The views of the charts in it - the control may be a view itself, sit in one, or hold several
            var stack = new Stack<Control>();
            stack.Push(within);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (ConfiguredPositions.TryGetValue(c, out _) && GetScope(c) is { } scope) Choices.Remove(scope);
                foreach (Control child in c.Controls) stack.Push(child);
            }
        }

        /// <summary>
        /// Gives a chart - or a control charts sit in - a key to know it by when it's rebuilt as a new control.  It needs
        /// to be unique in the view: a report's chart index and title, a metric name.
        /// </summary>
        public static void SetKey(Control control, object key)
        {
            if (control == null || key == null) return;
            Keys.AddOrUpdate(control, key);
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
            SetChartChoice(chart, choices, show);
            Apply(chart);
        }

        private static void SetChartChoice(Control chart, ViewChoices choices, bool? show)
        {
            var key = ChartKey(chart);
            // A chart with no key of its own is its key - forget it with the chart, so a view that rebuilds its charts
            // doesn't hold on to every one it's disposed of
            if (ReferenceEquals(key, chart) && !choices.Charts.ContainsKey(key))
            {
                chart.Disposed += (_, _) => choices.Charts.Remove(key);
            }
            choices.Charts[key] = show;
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
        /// What identifies a chart across a rebuild: the key its host gave it or a control it sits in (up to its view),
        /// else its Tag, else the chart itself.
        /// </summary>
        private static object ChartKey(Control chart)
        {
            for (var c = chart; c != null; c = c.Parent)
            {
                if (Keys.TryGetValue(c, out var key)) return key;
                if (c is IChartLegendScope or TabPage) break;
            }
            return chart.Tag ?? chart;
        }

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
                return viewChoices.All;
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
        /// The view the chart belongs to: the nearest <see cref="IChartLegendScope"/> or tab page it sits in, else its
        /// outermost user control.  Null for a chart that isn't in any of these - a chart on its own isn't a view.
        /// </summary>
        private static Control GetScope(Control chart)
        {
            Control outermost = null;
            for (var c = chart?.Parent; c != null; c = c.Parent)
            {
                if (c is IChartLegendScope or TabPage) return c;
                if (c is UserControl) outermost = c;
            }
            return outermost;
        }

        /// <summary>
        /// The charts in <paramref name="scope"/> that this class manages the legend for - not those in a scope nested in
        /// it (a child report, a tab in a tab).
        /// </summary>
        private static IEnumerable<Control> ChartsIn(Control scope)
        {
            var stack = new Stack<Control>();
            foreach (Control child in scope.Controls) stack.Push(child);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (c is IChartLegendScope or TabPage) continue;
                if (ConfiguredPositions.TryGetValue(c, out _)) yield return c;
                foreach (Control child in c.Controls) stack.Push(child);
            }
        }
    }
}
