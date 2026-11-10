using DBADashGUI.Theme;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// Runs' statistics compared: a card with the first against the last, then a Comparison tab - the totals and each
    /// table's I/O side by side - and each run's own statistics on a tab of its own, as they'd show on their own.
    /// </summary>
    public sealed class StatisticsCompareControl : UserControl
    {
        private readonly SubTabControl _tabs = new();

        public StatisticsCompareControl(IReadOnlyList<StatisticsRun> runs, string title)
        {
            Runs = runs;
            Title = title;

            var (headline, details, note, warning) = StatisticsComparison.Summary(runs);
            var card = new StatisticsCard(headline, details, note, warning);

            var totals = StatisticsViewerControl.Section("Totals",
                new GridViewerControl(StatisticsComparison.Totals(runs), $"{title} - Totals"));

            Control comparison = totals;
            if (runs.Any(r => r.Report.HasIO))
            {
                var io = StatisticsViewerControl.Section("Table I/O",
                    new GridViewerControl(StatisticsComparison.TableIO(runs), $"{title} - Table I/O"));
                var container = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    // Sized before the splitter is placed: SplitterDistance throws if it's beyond the container.
                    Size = new Size(1200, 800),
                    SplitterDistance = 300
                };
                container.Panel1.Controls.Add(totals);
                container.Panel2.Controls.Add(io);
                comparison = container;
            }

            _tabs.AddTab("Comparison", comparison);
            for (var i = 0; i < runs.Count; i++)
            {
                _tabs.AddTab(StatisticsComparison.Name(runs, i), new StatisticsViewerControl(runs[i].Report, $"{title} - {runs[i].Label}"));
            }

            Controls.Add(_tabs);
            Controls.Add(card);
            this.ApplyTheme();
        }

        public IReadOnlyList<StatisticsRun> Runs { get; }

        public string Title { get; }

        /// <summary>
        /// The runs compared, short enough for a window title: "A vs B", or the first and last of more - "A vs C (3 runs)".
        /// </summary>
        public string Caption => Runs.Count == 2
            ? $"{Runs[0].Label} vs {Runs[1].Label}"
            : $"{Runs[0].Label} vs {Runs[^1].Label} ({Runs.Count} runs)";

        /// <summary>Comparison, then each run's own tab.</summary>
        public string[] TabNames => _tabs.TabNames;
    }
}
