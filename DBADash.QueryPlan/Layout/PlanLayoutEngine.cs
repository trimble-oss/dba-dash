using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// Turns a parsed <see cref="PlanStatement"/> into positioned nodes and routed arrows.
    ///
    /// A plan is a tree, so it gets a tree layout: the statement root on the left, its inputs to the
    /// right, one column per level.  Rows flow the other way - right to left, into the root - which
    /// is the convention SSMS established and the one every SQL Server DBA already reads fluently.
    /// Going against it to suit the left-to-right habit of every other diagram would be a worse
    /// trade than it sounds.
    ///
    /// Vertical placement is the classic tidy tree: leaves are stacked in order, and every other
    /// node is centred on its inputs.  For plan trees - narrow, deep, and never interleaved - that
    /// gives the same result as a full Reingold-Tilford pass for a fraction of the complexity, with
    /// a relaxation pass afterwards to guarantee what the simple version only almost guarantees.
    ///
    /// <see cref="PlanLayoutOptions.VerticalLayout"/> swaps that for the shape SSMS draws, where a
    /// node sits level with its first input instead of between all of them.  A plan is then as tall
    /// as it has branches rather than as tall as it has leaves, which is the difference between a
    /// wide plan fitting the window and not.
    ///
    /// Contains no drawing code and no drawing dependency: text is sized through
    /// <see cref="IPlanTextMeasurer"/> so this stays portable and unit testable.
    /// </summary>
    public sealed class PlanLayoutEngine
    {
        /// <summary>
        /// How far actual rows have to be from the estimate before the node is badged.
        ///
        /// An order of magnitude.  Estimates are expected to be approximate, and badging a plan that
        /// is merely imprecise trains the reader to ignore the badge - which is the one failure mode
        /// worth designing against, because the badge is there to catch the estimate that caused a
        /// bad plan choice.
        /// </summary>
        internal const double EstimateMismatchThreshold = 10;

        /// <summary>
        /// How far an arrow's estimate has to be out before it is coloured critical rather than a
        /// warning: two orders of magnitude, against the badge's one.
        /// </summary>
        internal const double EstimateCriticalThreshold = 100;

        /// <summary>
        /// Rows thrown away by a residual predicate before it is worth saying so: more discarded
        /// than kept, and enough of them to matter.
        /// </summary>
        internal const long DiscardedRowsThreshold = 100;

        /// <summary>
        /// How many times the overlap relaxation runs before giving up.  Each pass only moves nodes
        /// down, so it converges; the cap is there so a pathological plan cannot spin.
        /// </summary>
        private const int MaxRelaxationPasses = 8;

        private readonly IPlanTextMeasurer _measurer;
        private readonly PlanLayoutOptions _options;

        public PlanLayoutEngine(IPlanTextMeasurer measurer, PlanLayoutOptions? options = null)
        {
            _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
            _options = options ?? new PlanLayoutOptions();
        }

        public PlanLayout Layout(PlanStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);

            var layout = new PlanLayout
            {
                Statement = statement,
                Engine = this,
                EdgeWidthMetric = _options.EdgeWidthMetric,
                EdgeWidthBasis = _options.EdgeWidthBasis,
                OperatorTimeMode = _options.OperatorTimeMode
            };

            Build(layout);
            return layout;
        }

        /// <summary>
        /// Lay the statement out into an existing layout, honouring its collapsed set.  Called again
        /// whenever that set changes, so the caller's reference to the layout stays valid.
        /// </summary>
        internal void Build(PlanLayout layout)
        {
            var statement = layout.Statement;

            var root = BuildRootNode(statement);
            var nodes = new List<PlanNode> { root };

            if (statement.RootOperator is { } rootOperator)
            {
                var child = BuildNode(rootOperator, root, 1, statement, layout.CollapsedIds, layout.OperatorTimeMode, nodes);
                root.Children = new[] { child };
            }

            var text = nodes.ToDictionary(node => node, MeasureNode);

            AssignHorizontal(nodes, text);

            if (_options.VerticalLayout == PlanVerticalLayout.FirstChildAligned)
            {
                AssignVerticalFirstChildAligned(root);
            }
            else
            {
                AssignVertical(root);
                ResolveOverlaps(nodes, root);
            }

            PlaceNodeContents(nodes, text);

            var bounds = Normalise(nodes);

            layout.Root = root;
            layout.Nodes = nodes;
            layout.Metrics = new PlanLayoutMetrics(statement, statement.Operators, layout.OperatorTimeMode);
            layout.Edges = RouteEdges(layout);
            layout.Bounds = bounds;
        }

        // ---------------------------------------------------------------- building nodes

        /// <summary>
        /// The synthetic head of the plan, labelled with the statement type.
        ///
        /// SQL Server does not emit an operator for it, but a plan is a pipeline that ends somewhere
        /// and the reader needs to see where.  It also gives the statement's own figures - the total
        /// cost, the elapsed time - a place on the picture rather than only in a properties panel.
        /// </summary>
        private static PlanNode BuildRootNode(PlanStatement statement)
        {
            var type = string.IsNullOrWhiteSpace(statement.StatementType) ? "Query" : statement.StatementType;

            var metric = statement.StatementSubTreeCost > 0
                ? "Cost " + PlanFormat.Cost(statement.StatementSubTreeCost)
                : null;

            string? subtitle = null;
            if (statement.DegreeOfParallelism is > 1)
            {
                subtitle = "Parallel, DOP " +
                           statement.DegreeOfParallelism.Value.ToString(CultureInfo.InvariantCulture);
            }
            else if (statement.RootOperator is null)
            {
                // Saying so beats an unexplained lone box.
                subtitle = "No plan recorded";
            }

            return new PlanNode
            {
                Id = "root",
                Statement = statement,
                Kind = PlanOperatorKind.StatementRoot,
                Category = PlanOperatorCategory.Root,
                Title = type,
                Subtitle = subtitle,
                MetricLine = metric,
                TimingLine = BuildTimingLine(statement.QueryTimeStats?.ElapsedMs, statement.QueryTimeStats?.CpuMs),
                Depth = 0,
                CostFraction = 1,

                // Plan level warnings - a conversion affecting the plan, a memory grant warning -
                // belong to no operator, so they go on the statement, where SSMS puts them too.
                Badges = WarningBadges(statement.Warnings)
            };
        }

        private PlanNode BuildNode(
            PlanOperator node,
            PlanNode parent,
            int depth,
            PlanStatement statement,
            HashSet<string> collapsed,
            OperatorTimeMode timeMode,
            List<PlanNode> nodes)
        {
            var id = NodeId(node);
            var isCollapsed = collapsed.Contains(id);

            var planNode = new PlanNode
            {
                Id = id,
                Operator = node,
                Statement = statement,
                Kind = node.Kind,
                Category = node.Category,
                Title = node.DisplayName,
                Subtitle = node.PrimaryObject?.ShortName,
                MetricLine = BuildMetricLine(node, statement, _options.ShowNodeIds),
                TimingLine = BuildTimingLine(node.ElapsedMs(timeMode), node.CpuMs(timeMode)),
                Parent = parent,
                Depth = depth,
                CostFraction = node.CostPercent,
                Badges = BuildBadges(node, statement),
                IsCollapsed = isCollapsed
            };

            nodes.Add(planNode);

            if (isCollapsed)
            {
                // Count what is hidden so the node can say so.  Less the operator itself, which the
                // walk returns first.
                var hidden = node.DescendantsAndSelf().Skip(1).ToList();
                planNode.HiddenDescendantCount = hidden.Count;

                // Hidden warnings are raised on the collapsed node, so collapsing part of a plan to
                // read the rest never hides a spill in the part put away.  The plan level warnings
                // traced to a hidden operator count as its own here too, the same as they do on a
                // visible node - see BuildBadges - otherwise collapsing an ancestor loses the marker
                // for a conversion showplan reported against the statement.
                planNode.HiddenWarningCount = hidden.Count(op => op.Warnings.Count > 0 || statement.WarningsFor(op).Any());

                var hiddenWarnings = hidden.SelectMany(op => op.Warnings.Concat(statement.WarningsFor(op))).ToList();

                // Kept apart from the badges, which are the node's own warnings and the hidden ones
                // together: the tooltip row that counts only the hidden ones has to be coloured by
                // only the hidden ones.
                planNode.HiddenWarningsAreCritical = hiddenWarnings.Any(w => w.Severity == PlanWarningSeverity.Critical);
                planNode.Badges |= WarningBadges(hiddenWarnings);
                return planNode;
            }

            if (node.Children.Count > 0)
            {
                planNode.Children = node.Children
                    .Select(child => BuildNode(child, planNode, depth + 1, statement, collapsed, timeMode, nodes))
                    .ToList();
            }

            return planNode;
        }

        private static string NodeId(PlanOperator node) =>
            "op" + node.NodeId.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The figures at the foot of a node: its share of the statement cost, and the rows it
        /// produced.
        ///
        /// Two numbers, not five.  A node is glanced at, not studied - everything else the operator
        /// knows is one click away in the properties panel, and a node crowded with figures is one
        /// nobody reads.  Its node id joins them only when asked for - see
        /// <see cref="PlanLayoutOptions.ShowNodeIds"/>.
        /// </summary>
        private static string? BuildMetricLine(PlanOperator node, PlanStatement statement, bool showNodeId)
        {
            var parts = new List<string>(3);

            // First, where it is looked for: the reader turning this on is holding a node id from a
            // card or a list and looking for that node in the picture.
            if (showNodeId) parts.Add("Node " + node.NodeId.ToString(CultureInfo.InvariantCulture));

            if (statement.StatementSubTreeCost > 0)
            {
                parts.Add(PlanFormat.Percent(node.CostPercent));
            }

            // An actual zero is worth showing; an estimate of zero is not a figure anyone reads.
            var rows = node.RowsForDisplay;
            if (rows > 0 || node.ActualRows is not null)
            {
                parts.Add(PlanFormat.CompactCount(rows) + " rows");
            }

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>
        /// Elapsed and CPU time, each labelled, or null when neither was measured.
        ///
        /// Labelled rather than two bare durations, because which is which is not obvious and the
        /// difference matters: CPU well above elapsed is parallelism doing its job, and elapsed well
        /// above CPU is time spent waiting.  Whether the figures are the operator's own or include its
        /// inputs is the layout's <see cref="PlanLayout.OperatorTimeMode"/>.
        /// </summary>
        private static string? BuildTimingLine(long? elapsedMs, long? cpuMs)
        {
            var parts = new List<string>(2);

            if (elapsedMs is { } elapsed) parts.Add("Elapsed " + PlanFormat.ShortDuration(elapsed));
            if (cpuMs is { } cpu) parts.Add("CPU " + PlanFormat.ShortDuration(cpu));

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>
        /// The warning flags for a set of warnings.
        ///
        /// Any warning at all earns the marker, informational ones included.  The plan chose to
        /// flag them, SSMS marks them all, and a viewer that quietly shows fewer warnings than the
        /// tool the reader already knows is one they stop trusting.  Severity only sets the colour.
        /// </summary>
        private static PlanNodeBadges WarningBadges(IEnumerable<PlanWarning> warnings)
        {
            var badges = PlanNodeBadges.None;

            foreach (var warning in warnings)
            {
                badges |= PlanNodeBadges.Warning;

                if (warning.Severity == PlanWarningSeverity.Critical)
                {
                    return badges | PlanNodeBadges.CriticalWarning;
                }
            }

            return badges;
        }

        private static PlanNodeBadges BuildBadges(PlanOperator node, PlanStatement statement)
        {
            // The plan level warnings traced to this operator count as its own: a conversion showplan
            // reported against the statement happens here, and marking only the statement root leaves
            // the reader to find the operator by opening them one at a time.
            var badges = WarningBadges(node.Warnings.Concat(statement.WarningsFor(node)));

            if (node.RowEstimateError >= EstimateMismatchThreshold)
            {
                badges |= PlanNodeBadges.EstimateMismatch;
            }

            if (node.IsParallel || node.Runtime?.WorkerThreadCount > 1) badges |= PlanNodeBadges.Parallel;
            if (node.Runtime?.IsBatchMode == true) badges |= PlanNodeBadges.BatchMode;

            if (node.RowsDiscarded is { } discarded &&
                discarded >= DiscardedRowsThreshold &&
                (node.Runtime is null || discarded > node.Runtime.ActualRows))
            {
                badges |= PlanNodeBadges.RowsDiscarded;
            }

            // Plan level recommendations are about one table, and the operator reading that table is
            // where the reader needs to be looking.
            if (statement.MissingIndexesFor(node).Any())
            {
                badges |= PlanNodeBadges.MissingIndex;
            }

            return badges;
        }

        // ---------------------------------------------------------------- sizing

        /// <summary>
        /// The measured content of a node, kept so it is measured once rather than per pass.
        ///
        /// A node is a heading - the glyph beside the title and subtitle - over a body of figures
        /// that runs the full width of the node, under the glyph as well.  The figures are the
        /// longest lines on an actual plan, and indenting them past the glyph as well made every
        /// node the width of the glyph wider than it needed to be.
        /// </summary>
        /// <remarks>
        /// The heights of the title and the figures are for all their lines - see
        /// <see cref="PlanNode.TitleLines"/>, which are one each unless the icon is above the text.
        /// </remarks>
        private readonly record struct NodeText(
            double Width,
            double HeadingHeight,
            double TitleHeight,
            double SubtitleHeight,
            double MetricHeight,
            double TimingHeight,
            double ContentHeight,
            double BadgeStripWidth,
            IReadOnlyList<string> TitleLines,
            IReadOnlyList<string> SubtitleLines,
            IReadOnlyList<string> MetricLines,
            IReadOnlyList<string> TimingLines);

        private static IReadOnlyList<string> OneLine(string? text) => text is null ? [] : [text];

        private NodeText MeasureNode(PlanNode node)
        {
            var title = _measurer.Measure(node.Title, PlanTextRole.Title);

            var subtitle = node.Subtitle is null
                ? LayoutSize.Empty
                : _measurer.Measure(node.Subtitle, PlanTextRole.Detail);

            var metric = node.MetricLine is null
                ? LayoutSize.Empty
                : _measurer.Measure(node.MetricLine, PlanTextRole.Metric);

            var timing = node.TimingLine is null
                ? LayoutSize.Empty
                : _measurer.Measure(node.TimingLine, PlanTextRole.Metric);

            // The badges sit on the node's top edge rather than on any line of text, so they only
            // need the node to be as wide as they are, not a title's width wider.
            var badges = BadgeStripWidth(node);

            // The object name beside the icon, or under it with the whole width of the node.
            var subtitleRoom = _options.MaxNodeWidth - (_options.NodePadding * 2) -
                               (_options.IconAboveText ? 0 : _options.IconSize + _options.IconSpacing);
            var subtitleLines = _options.WrapObjectNames
                ? WrapObjectName(node.Subtitle, subtitle.Width, Math.Max(0, subtitleRoom))
                : OneLine(node.Subtitle);

            subtitle = new LayoutSize(
                subtitleLines.Count > 1 ? Widest(subtitleLines, PlanTextRole.Detail) : subtitle.Width,
                subtitleLines.Count * subtitle.Height);

            if (_options.IconAboveText) return MeasureStacked(node, title, subtitle, subtitleLines, metric, timing, badges);

            var headingText = title.Height;
            if (node.Subtitle is not null) headingText += _options.LineSpacing + subtitle.Height;
            var heading = Math.Max(_options.IconSize, headingText);

            var body = 0.0;
            if (node.MetricLine is not null) body += _options.LineSpacing + metric.Height;
            if (node.TimingLine is not null) body += _options.LineSpacing + timing.Height;

            // Inset from both ends, so a node of six badges and a short name still holds them all.
            // The node's own padding is added later, so it is taken off here.
            var badgeRoom = badges <= 0 ? 0 : badges + (_options.BadgeInset * 2) - (_options.NodePadding * 2);

            var width = new[]
            {
                _options.IconSize + _options.IconSpacing + Math.Max(title.Width, subtitle.Width),
                metric.Width,
                timing.Width,
                badgeRoom
            }.Max();

            return new NodeText(
                width, heading, title.Height, subtitle.Height, metric.Height, timing.Height, heading + body, badges,
                OneLine(node.Title), subtitleLines, OneLine(node.MetricLine), OneLine(node.TimingLine));
        }

        /// <summary>
        /// An object name too long for the widest node, wrapped rather than cut short - see
        /// <see cref="PlanLayoutOptions.WrapObjectNames"/>.  Broken after the dot between the parts of
        /// a name, or before its alias, so each line is a whole part where it can be; a part
        /// too long for a line on its own is broken inside it.  At most
        /// <see cref="PlanLayoutOptions.MaxObjectNameLines"/> lines, the last elided when drawn if the
        /// name runs on past it.
        /// </summary>
        private IReadOnlyList<string> WrapObjectName(string? name, double width, double available)
        {
            if (name is null) return [];
            if (width <= available || available <= 0) return [name];

            var lines = new List<string>();
            var maxLines = Math.Max(1, _options.MaxObjectNameLines);
            var rest = name;

            while (rest.Length > 0)
            {
                // The last line allowed takes whatever is left, to be elided when drawn.
                if (lines.Count == maxLines - 1 || Fits(rest))
                {
                    lines.Add(rest);
                    break;
                }

                // The most that fits, ending just after a dot, or before the alias - "AS c" is kept
                // together, since an alias on a line by itself reads as a stray letter.
                var take = 0;
                for (var i = 0; i < rest.Length; i++)
                {
                    var breaks = rest[i] == '.' ||
                                 (rest[i] == ' ' && string.CompareOrdinal(rest, i + 1, "AS ", 0, 3) == 0);
                    if (!breaks) continue;
                    if (!Fits(rest[..(i + 1)].TrimEnd())) break;
                    take = i + 1;
                }

                // No break fits: the first part is too long for a line of its own, so it is broken
                // where the line runs out.
                if (take == 0)
                {
                    take = 1;
                    while (take < rest.Length && Fits(rest[..(take + 1)])) take++;
                }

                lines.Add(rest[..take].TrimEnd());
                rest = rest[take..].TrimStart();
            }

            return lines;

            bool Fits(string text) => _measurer.Measure(text, PlanTextRole.Detail).Width <= available;
        }

        /// <summary>
        /// A node with the icon above the text: the icon, then each line of text under it, all
        /// centred.  The text has the node's whole width, and whatever still does not fit in the
        /// widest node allowed goes onto more lines rather than making the node wider - a long
        /// operator name onto two, and the figures one part to a line.
        /// </summary>
        private NodeText MeasureStacked(
            PlanNode node,
            LayoutSize title,
            LayoutSize subtitle,
            IReadOnlyList<string> subtitleLines,
            LayoutSize metric,
            LayoutSize timing,
            double badges)
        {
            var available = Math.Max(0, _options.MaxNodeWidth - (_options.NodePadding * 2));

            var titleLines = WrapTitle(node.Title, available);
            var metricLines = SplitFigures(node.MetricLine, metric.Width, available);
            var timingLines = SplitFigures(node.TimingLine, timing.Width, available);

            var titleHeight = titleLines.Count * title.Height;
            var metricHeight = metricLines.Count * metric.Height;
            var timingHeight = timingLines.Count * timing.Height;

            var content = _options.IconSize + _options.LineSpacing + titleHeight;
            if (node.Subtitle is not null) content += _options.LineSpacing + subtitle.Height;
            if (metricLines.Count > 0) content += _options.LineSpacing + metricHeight;
            if (timingLines.Count > 0) content += _options.LineSpacing + timingHeight;

            var badgeRoom = badges <= 0 ? 0 : badges + (_options.BadgeInset * 2) - (_options.NodePadding * 2);

            var width = new[]
            {
                _options.IconSize,
                Widest(titleLines, PlanTextRole.Title),
                subtitle.Width,
                Widest(metricLines, PlanTextRole.Metric),
                Widest(timingLines, PlanTextRole.Metric),
                badgeRoom
            }.Max();

            return new NodeText(
                width, _options.IconSize, titleHeight, subtitle.Height, metricHeight, timingHeight, content, badges,
                titleLines, subtitleLines, metricLines, timingLines);
        }

        private double Widest(IReadOnlyList<string> lines, PlanTextRole role) =>
            lines.Count == 0 ? 0 : lines.Max(line => _measurer.Measure(line, role).Width);

        /// <summary>
        /// An operator name that does not fit, split between two words onto two lines - at whichever
        /// break leaves the longer line shortest, so "Clustered Index Seek" becomes "Clustered" over
        /// "Index Seek" rather than two words over one.  A single word stays on one line, to be elided.
        /// </summary>
        private IReadOnlyList<string> WrapTitle(string title, double available)
        {
            if (_measurer.Measure(title, PlanTextRole.Title).Width <= available) return [title];

            string[]? best = null;
            var bestWidth = double.MaxValue;

            for (var i = title.IndexOf(' '); i > 0; i = title.IndexOf(' ', i + 1))
            {
                var first = title[..i];
                var second = title[(i + 1)..].TrimStart();
                if (second.Length == 0) continue;

                var widest = Math.Max(
                    _measurer.Measure(first, PlanTextRole.Title).Width,
                    _measurer.Measure(second, PlanTextRole.Title).Width);

                if (widest < bestWidth)
                {
                    bestWidth = widest;
                    best = [first, second];
                }
            }

            return best ?? [title];
        }

        /// <summary>
        /// A line of figures that does not fit, one figure to a line - split where the figures are
        /// separated, never inside one, so a number and its unit stay together.
        /// </summary>
        private static IReadOnlyList<string> SplitFigures(string? figures, double width, double available)
        {
            if (figures is null) return [];
            if (width <= available) return [figures];

            return figures.Split(" · ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>The width the badges themselves occupy.  Zero when there are none.</summary>
        private double BadgeStripWidth(PlanNode node)
        {
            var count = PlanBadges.Count(node.Badges);

            return count == 0
                ? 0
                : (count * _options.BadgeSize) + ((count - 1) * _options.BadgeSpacing);
        }

        private double NodeHeight(NodeText text) =>
            (_options.NodePadding * 2) + text.ContentHeight + _options.MetricBarHeight;

        private double NodeWidth(NodeText text) =>
            Math.Clamp(text.Width + (_options.NodePadding * 2), _options.MinNodeWidth, _options.MaxNodeWidth);

        // ---------------------------------------------------------------- placement

        /// <summary>
        /// Put every node in the column for its depth.  Columns are as wide as their widest node so
        /// the arrows between two columns are all the same length, which is what makes the plan read
        /// as a sequence of stages.
        ///
        /// Placing each node's inputs against the node itself instead, so one long index name only
        /// widens its own branch, was tried and measured: across a corpus of real plans it saved
        /// about one percent of the width, because the deepest path sets a plan's width and its
        /// nodes are usually the widest anyway.  Not worth giving up aligned columns for.
        /// </summary>
        private void AssignHorizontal(List<PlanNode> nodes, Dictionary<PlanNode, NodeText> text)
        {
            var columnWidths = new Dictionary<int, double>();

            foreach (var node in nodes)
            {
                var width = NodeWidth(text[node]);
                columnWidths[node.Depth] = Math.Max(columnWidths.GetValueOrDefault(node.Depth), width);
            }

            var columnX = new Dictionary<int, double>();
            var x = 0.0;

            foreach (var depth in columnWidths.Keys.OrderBy(d => d))
            {
                columnX[depth] = x;
                x += columnWidths[depth] + _options.ColumnSpacing;
            }

            foreach (var node in nodes)
            {
                var width = _options.UniformColumnWidths ? columnWidths[node.Depth] : NodeWidth(text[node]);
                node.Bounds = new LayoutRect(columnX[node.Depth], 0, width, NodeHeight(text[node]));
            }
        }

        /// <summary>
        /// Stack the leaves in order and centre everything else on its inputs.
        /// </summary>
        private void AssignVertical(PlanNode root)
        {
            var cursor = 0.0;
            Place(root, ref cursor);
        }

        private void Place(PlanNode node, ref double cursor)
        {
            if (node.Children.Count == 0)
            {
                node.Bounds = new LayoutRect(node.Bounds.X, cursor, node.Bounds.Width, node.Bounds.Height);
                cursor += node.Bounds.Height + _options.RowSpacing;
                return;
            }

            foreach (var child in node.Children) Place(child, ref cursor);

            Centre(node);
        }

        /// <summary>
        /// Put a node level with the middle of its inputs, measured from the first and last rather
        /// than by averaging all of them: with an uneven fan-in, the average pulls the parent towards
        /// whichever side has more inputs, and the arrows come out visibly lopsided.
        /// </summary>
        private static void Centre(PlanNode node)
        {
            if (node.Children.Count == 0) return;

            var first = node.Children[0].Bounds;
            var last = node.Children[^1].Bounds;
            var centre = (first.Centre.Y + last.Centre.Y) / 2;

            node.Bounds = new LayoutRect(
                node.Bounds.X,
                centre - (node.Bounds.Height / 2),
                node.Bounds.Width,
                node.Bounds.Height);
        }

        /// <summary>
        /// Push apart any two nodes that ended up overlapping in the same column, then put every
        /// parent back over the middle of its inputs.
        ///
        /// Centring a node on its inputs can push it a little outside the band its own subtree
        /// occupies, and two nodes of very different heights at the same depth can then just touch.
        /// A whole subtree is moved rather than the one node, so the arrows stay untangled, and
        /// re-centring afterwards keeps the parents honest.  Nodes only ever move down, so this
        /// settles rather than oscillating.
        /// </summary>
        private void ResolveOverlaps(List<PlanNode> nodes, PlanNode root)
        {
            for (var pass = 0; pass < MaxRelaxationPasses; pass++)
            {
                var moved = false;

                foreach (var column in nodes.GroupBy(n => n.Depth))
                {
                    var ordered = column.OrderBy(n => n.Bounds.Top).ToList();

                    for (var i = 1; i < ordered.Count; i++)
                    {
                        var previous = ordered[i - 1].Bounds;
                        var current = ordered[i];
                        var required = previous.Bottom + _options.RowSpacing;

                        if (current.Bounds.Top >= required) continue;

                        Shift(current, required - current.Bounds.Top);
                        moved = true;
                    }
                }

                if (!moved) return;

                ReCentre(root);
            }
        }

        private static void Shift(PlanNode node, double dy)
        {
            node.Bounds = node.Bounds.Offset(0, dy);
            foreach (var child in node.Children) Shift(child, dy);
        }

        private static void ReCentre(PlanNode node)
        {
            foreach (var child in node.Children) ReCentre(child);
            Centre(node);
        }

        /// <summary>
        /// Put every node level with its first input and drop the rest below it, the way SSMS draws
        /// a plan.  See <see cref="PlanVerticalLayout.FirstChildAligned"/>.
        ///
        /// Top down, which is the other way round from <see cref="AssignVertical"/>: a node's row is
        /// settled before its inputs are placed, so every input can simply ask to be level with the
        /// node it feeds.  The first one gets its wish - which is what puts the whole first-input
        /// spine, the statement root through to the leaf that ultimately feeds it, on one row - and
        /// the rest are pushed down by the only rule here: nothing may be placed over something
        /// already in its column.
        ///
        /// Which is why a spine is placed all at once rather than a node at a time.  A branch already
        /// hanging off the plan can reach into a column the spine has yet to enter, and a node placed
        /// alone would find that column taken and drop out of its parent's row - leaving the parent
        /// stranded above its own first input, which is the one thing this shape promises never to do.
        /// Asking the whole spine where it may sit before placing any of it moves the parent down to
        /// meet the input instead of the input down away from the parent.
        ///
        /// Keeping that rule per column rather than over the whole plan is what makes this the short
        /// shape.  Two branches that share no column never have to clear each other, so a plan with
        /// a dozen leaves can still be three rows tall - where stacking the leaves is a dozen rows by
        /// definition.  It is also why nothing needs pushing apart afterwards: the rule is enforced
        /// as each node is placed, and relaxing the result the way <see cref="ResolveOverlaps"/>
        /// relaxes the other layout would re-centre the parents and undo the whole thing.
        /// </summary>
        private void AssignVerticalFirstChildAligned(PlanNode root) =>
            PlaceAligned(root, root.Bounds.Height / 2, new Dictionary<int, double>());

        /// <param name="head">The head of a first-input spine, placed here along with the rest of it.</param>
        /// <param name="desiredCentre">The row the spine asks for, which is the one its consumer is on.</param>
        /// <param name="columnBottom">The lowest edge so far in each column, keyed by depth.</param>
        private void PlaceAligned(PlanNode head, double desiredCentre, Dictionary<int, double> columnBottom)
        {
            var spine = new List<PlanNode> { head };

            while (spine[^1].Children.Count > 0)
            {
                spine.Add(spine[^1].Children[0]);
            }

            // The row has to clear every column the spine passes through, not just the head's, or
            // the node that could not have it would be the one to drop instead of all of them.
            var centre = desiredCentre;

            foreach (var node in spine)
            {
                if (columnBottom.TryGetValue(node.Depth, out var occupied))
                {
                    centre = Math.Max(centre, occupied + _options.RowSpacing + (node.Bounds.Height / 2));
                }
            }

            foreach (var node in spine)
            {
                node.Bounds = new LayoutRect(
                    node.Bounds.X,
                    centre - (node.Bounds.Height / 2),
                    node.Bounds.Width,
                    node.Bounds.Height);

                // Always below the last node placed in this column, because of the clamp above, so
                // the column's mark only ever moves down.
                columnBottom[node.Depth] = node.Bounds.Bottom;
            }

            // The inputs the spine did not take, innermost first, which is the order they would have
            // been reached in had the spine been walked a node at a time.  Each asks for its
            // consumer's row and finds the column taken by the input before it, so lands under it.
            for (var i = spine.Count - 1; i >= 0; i--)
            {
                var node = spine[i];

                for (var child = 1; child < node.Children.Count; child++)
                {
                    PlaceAligned(node.Children[child], node.Bounds.Centre.Y, columnBottom);
                }
            }
        }

        /// <summary>
        /// Work out where the glyph, the text lines and the bar sit inside each node, now that the
        /// node itself is placed.
        ///
        /// Done here rather than in the renderer so there is one arrangement rather than two that
        /// have to agree: the node was measured for this layout, and a renderer deriving its own
        /// would eventually disagree with it by a pixel or two and put text outside the box.
        /// </summary>
        private void PlaceNodeContents(List<PlanNode> nodes, Dictionary<PlanNode, NodeText> text)
        {
            foreach (var node in nodes)
            {
                var metrics = text[node];
                var bounds = node.Bounds;

                node.TitleLines = metrics.TitleLines;
                node.SubtitleLines = metrics.SubtitleLines;
                node.MetricLines = metrics.MetricLines;
                node.TimingLines = metrics.TimingLines;
                node.CentreText = _options.IconAboveText;

                if (_options.IconAboveText)
                {
                    PlaceBelowIcon(node, metrics);
                }
                else
                {
                    PlaceBesideIcon(node, metrics);
                }

                // Over the glyph's lower right corner, as SSMS draws it, so anyone who has read a
                // plan before knows what it means without being told.  It overhangs the glyph a
                // little, into the gap before the title, but stops at the glyph's foot so it never
                // reaches the figures underneath.
                var marker = _options.WarningMarkerSize;
                node.WarningMarkerBounds = node.HasWarnings
                    ? new LayoutRect(
                        node.IconBounds.Right + (marker * 0.25) - marker,
                        node.IconBounds.Bottom - (marker * 0.9),
                        marker,
                        marker * 0.9)
                    : null;

                // Straddling the top edge, clear of the rounded corner, where they cost the title
                // nothing.  The padding above the heading is wider than half a badge, so they never
                // reach the title - or, with the icon above the text, the icon - either.
                node.BadgeStripBounds = metrics.BadgeStripWidth > 0
                    ? new LayoutRect(
                        bounds.Right - _options.BadgeInset - metrics.BadgeStripWidth,
                        bounds.Top - (_options.BadgeSize / 2),
                        metrics.BadgeStripWidth,
                        _options.BadgeSize)
                    : null;

                // No bar on the statement root: it is the whole plan, so its share of anything is
                // always everything, and a permanently full bar says nothing at all.
                node.MetricBarBounds = _options.MetricBarHeight > 0 && !node.IsRoot
                    ? new LayoutRect(
                        bounds.Left,
                        bounds.Bottom - _options.MetricBarHeight,
                        bounds.Width,
                        _options.MetricBarHeight)
                    : null;

                // Only offered where there is something to hide or show.
                node.CollapseToggleBounds = node.Operator?.Children.Count > 0
                    ? new LayoutRect(bounds.Left - 7, bounds.Centre.Y - 7, 14, 14)
                    : null;
            }
        }

        /// <summary>
        /// The glyph at the left with the title and subtitle beside it, and the figures under both,
        /// running the node's full width.
        /// </summary>
        private void PlaceBesideIcon(PlanNode node, NodeText metrics)
        {
            var bounds = node.Bounds;
            var padding = _options.NodePadding;

            var contentTop = bounds.Top + padding;
            var contentHeight = bounds.Height - (padding * 2) - _options.MetricBarHeight;
            var headingTop = contentTop + Math.Max(0, (contentHeight - metrics.ContentHeight) / 2);

            node.IconBounds = new LayoutRect(
                bounds.Left + padding,
                headingTop + ((metrics.HeadingHeight - _options.IconSize) / 2),
                _options.IconSize,
                _options.IconSize);

            var textLeft = node.IconBounds.Right + _options.IconSpacing;
            var textWidth = Math.Max(0, bounds.Right - padding - textLeft);

            // The title and subtitle are centred against the glyph rather than pinned to the
            // top, so a title on its own does not sit above the middle of the icon.
            var headingText = metrics.TitleHeight +
                              (node.Subtitle is null ? 0 : _options.LineSpacing + metrics.SubtitleHeight);
            var y = headingTop + ((metrics.HeadingHeight - headingText) / 2);

            node.TitleBounds = new LayoutRect(textLeft, y, textWidth, metrics.TitleHeight);
            y += metrics.TitleHeight;

            if (node.Subtitle is not null)
            {
                y += _options.LineSpacing;
                node.SubtitleBounds = new LayoutRect(textLeft, y, textWidth, metrics.SubtitleHeight);
            }
            else
            {
                node.SubtitleBounds = null;
            }

            // The figures start under the glyph, at the node's own left edge.
            PlaceFigures(node, metrics, headingTop + metrics.HeadingHeight);
        }

        /// <summary>
        /// The glyph centred at the top, and every line of text under it across the node's full
        /// width, each centred by the renderer - see <see cref="PlanNode.CentreText"/>.
        /// </summary>
        private void PlaceBelowIcon(PlanNode node, NodeText metrics)
        {
            var bounds = node.Bounds;
            var padding = _options.NodePadding;

            var contentHeight = bounds.Height - (padding * 2) - _options.MetricBarHeight;
            var top = bounds.Top + padding + Math.Max(0, (contentHeight - metrics.ContentHeight) / 2);

            node.IconBounds = new LayoutRect(
                bounds.Centre.X - (_options.IconSize / 2),
                top,
                _options.IconSize,
                _options.IconSize);

            var textLeft = bounds.Left + padding;
            var textWidth = Math.Max(0, bounds.Width - (padding * 2));
            var y = node.IconBounds.Bottom + _options.LineSpacing;

            node.TitleBounds = new LayoutRect(textLeft, y, textWidth, metrics.TitleHeight);
            y += metrics.TitleHeight;

            if (node.Subtitle is not null)
            {
                y += _options.LineSpacing;
                node.SubtitleBounds = new LayoutRect(textLeft, y, textWidth, metrics.SubtitleHeight);
                y += metrics.SubtitleHeight;
            }
            else
            {
                node.SubtitleBounds = null;
            }

            PlaceFigures(node, metrics, y);
        }

        /// <summary>The metric and timing lines, one under the other below <paramref name="y"/>, across the node.</summary>
        private void PlaceFigures(PlanNode node, NodeText metrics, double y)
        {
            var bodyLeft = node.Bounds.Left + _options.NodePadding;
            var bodyWidth = Math.Max(0, node.Bounds.Width - (_options.NodePadding * 2));

            if (node.MetricLine is not null)
            {
                y += _options.LineSpacing;
                node.MetricBounds = new LayoutRect(bodyLeft, y, bodyWidth, metrics.MetricHeight);
                y += metrics.MetricHeight;
            }
            else
            {
                node.MetricBounds = null;
            }

            if (node.TimingLine is not null)
            {
                y += _options.LineSpacing;
                node.TimingBounds = new LayoutRect(bodyLeft, y, bodyWidth, metrics.TimingHeight);
            }
            else
            {
                node.TimingBounds = null;
            }
        }

        /// <summary>
        /// Shift everything so the layout starts at the origin plus the margin, and report the
        /// extent.
        /// </summary>
        private LayoutRect Normalise(List<PlanNode> nodes)
        {
            if (nodes.Count == 0) return new LayoutRect(0, 0, 0, 0);

            var extent = nodes[0].Bounds;
            foreach (var node in nodes) extent = extent.Union(node.Bounds);

            // The collapse control hangs off the left edge of a node, so the leftmost column needs
            // room for it inside the margin rather than clipped against it.
            var dx = _options.Margin - extent.Left + 8;
            var dy = _options.Margin - extent.Top;

            foreach (var node in nodes)
            {
                node.Bounds = node.Bounds.Offset(dx, dy);
                node.IconBounds = node.IconBounds.Offset(dx, dy);
                node.WarningMarkerBounds = node.WarningMarkerBounds?.Offset(dx, dy);
                node.TitleBounds = node.TitleBounds.Offset(dx, dy);
                node.SubtitleBounds = node.SubtitleBounds?.Offset(dx, dy);
                node.MetricBounds = node.MetricBounds?.Offset(dx, dy);
                node.TimingBounds = node.TimingBounds?.Offset(dx, dy);
                node.MetricBarBounds = node.MetricBarBounds?.Offset(dx, dy);
                node.BadgeStripBounds = node.BadgeStripBounds?.Offset(dx, dy);
                node.CollapseToggleBounds = node.CollapseToggleBounds?.Offset(dx, dy);
            }

            return new LayoutRect(
                0,
                0,
                extent.Width + (_options.Margin * 2) + 8,
                extent.Height + (_options.Margin * 2));
        }

        // ---------------------------------------------------------------- arrows

        /// <summary>
        /// Route every arrow in <paramref name="layout"/>, sized by its current
        /// <see cref="PlanLayout.EdgeWidthMetric"/>.  Also called on its own when only that measure
        /// changes - see <see cref="PlanLayout.SetEdgeWidthMetric"/>.
        /// </summary>
        internal IReadOnlyList<PlanEdge> RouteEdges(PlanLayout layout)
        {
            var edges = new List<PlanEdge>();
            var basis = layout.EffectiveEdgeWidthBasis;
            var scale = EdgeScale(layout);

            foreach (var consumer in layout.Nodes)
            {
                for (var i = 0; i < consumer.Children.Count; i++)
                {
                    edges.Add(RouteEdge(consumer, consumer.Children[i], i, consumer.Children.Count,
                        layout.EdgeWidthMetric, basis, scale));
                }
            }

            // Thick arrows are drawn first so a thin one crossing a thick one stays visible.  The
            // alternative hides the small arrow entirely, and a row count of twelve next to one of
            // twelve million is often the whole story.
            return edges.OrderByDescending(e => e.Thickness).ToList();
        }

        /// <summary>
        /// The value a full-width arrow stands for: the busiest arrow in the plan, or the floor for
        /// the chosen measure, whichever is larger.  See <see cref="PlanLayoutOptions.EdgeWidthRowsFloor"/>
        /// for why there is a floor at all.
        ///
        /// The two floors are what keep the measures in step: thickness depends only on an arrow's
        /// share of the scale, and 100 MB against a million rows is a hundred bytes a row, so an
        /// arrow of typical rows is the same width by either measure.  Switching only moves the
        /// arrows whose rows are unusually wide or narrow, which is what the switch is for.
        ///
        /// Actual and estimated arrows share one scale - the busiest arrow of either - for the same
        /// reason: an arrow's width means the same number of rows whichever is shown, so switching
        /// between them shows how far the estimates were out.  Scaled separately, a plan that
        /// estimated a thousand rows and read a billion would look the same both ways, with only
        /// the labels changing.
        /// </summary>
        private double EdgeScale(PlanLayout layout)
        {
            var metrics = layout.Metrics;

            return layout.EdgeWidthMetric == PlanEdgeWidthMetric.DataSize
                ? Math.Max(Math.Max(metrics.MaxDataSize, metrics.MaxEstimatedDataSize), _options.EdgeWidthDataSizeFloor)
                : Math.Max(Math.Max(metrics.MaxRows, metrics.MaxEstimatedRows), _options.EdgeWidthRowsFloor);
        }

        private PlanEdge RouteEdge(
            PlanNode consumer,
            PlanNode producer,
            int index,
            int siblingCount,
            PlanEdgeWidthMetric metric,
            PlanEdgeWidthBasis basis,
            double scale)
        {
            var source = producer.Operator;
            var actual = basis != PlanEdgeWidthBasis.Estimated;
            var byDataSize = metric == PlanEdgeWidthMetric.DataSize;
            var rowSize = source?.AvgRowSize;

            // Both figures, whatever is drawn, for the tooltip and the colour.
            var actualRows = source?.ActualRows;
            var estimatedRows = source?.EstimatedTotalRows ?? 0;

            var rows = source is null ? 0 : actual ? source.RowsForDisplay : estimatedRows;
            var dataSize = rows * (rowSize ?? 0);

            var thickness = Thickness(byDataSize ? dataSize : rows, scale);

            // The outline is only for an arrow with an actual figure to compare the estimate with.
            double? estimateThickness = basis == PlanEdgeWidthBasis.Both && actualRows is not null
                ? Thickness(byDataSize ? estimatedRows * (rowSize ?? 0) : estimatedRows, scale)
                : null;

            // The label clears whichever is wider, the body or the outline around it.
            var drawnWidth = Math.Max(thickness, estimateThickness ?? 0);

            var error = actualRows is { } measured ? EstimateError(measured, estimatedRows) : (double?)null;

            var exitY = producer.Bounds.Centre.Y;
            var entryY = EntryY(consumer, index, siblingCount);

            var gap = producer.Bounds.Left - consumer.Bounds.Right;
            var cornerX = consumer.Bounds.Right + (gap * _options.EdgeCornerFraction);

            var points = new List<LayoutPoint>
            {
                new(producer.Bounds.Left, exitY),
                new(cornerX, exitY),
                new(cornerX, entryY),
                new(consumer.Bounds.Right, entryY)
            };

            // The label says what the width is measuring, so the number and the thickness agree.
            var label = byDataSize ? PlanFormat.Bytes(dataSize) : PlanFormat.CompactCount(rows);
            var labelSize = _measurer.Measure(label, PlanTextRole.EdgeLabel);

            // Just clear of the producer, where there is always room, whereas the midpoint is where
            // several arrows converge on one consumer.  Above the line, unless the arrow turns
            // upwards at its corner: then its own riser runs through the space above the line, and
            // on a narrow column gap the label would sit on top of it.
            var turnsUp = entryY < exitY - 0.5;
            var labelBounds = new LayoutRect(
                producer.Bounds.Left - labelSize.Width - 6,
                turnsUp ? exitY + (drawnWidth / 2) + 2 : exitY - (drawnWidth / 2) - labelSize.Height - 2,
                labelSize.Width,
                labelSize.Height);

            return new PlanEdge
            {
                From = producer,
                To = consumer,
                Points = points,
                Thickness = thickness,
                Rows = rows,
                DataSize = dataSize,
                Label = label,
                LabelBounds = labelBounds,
                IsActual = actual && actualRows is not null,
                ActualRows = actualRows,
                IsActualRowsInferred = source?.IsActualRowsInferred == true,
                EstimatedRows = estimatedRows,
                RowSize = rowSize,
                EstimateError = error,
                EstimateAccuracy = error is { } e ? Accuracy(e) : null,
                EstimateThickness = estimateThickness
            };
        }

        /// <summary>
        /// How far out an estimate was, as a multiple of at least one either way - see
        /// <see cref="PlanOperator.EstimateErrorMultiple"/>, which the node badge and the heat metric
        /// measure by as well, so an arrow's colour and its operator's badge cannot disagree.
        ///
        /// Measured against the arrow's own figures rather than the operator's
        /// <see cref="PlanOperator.RowEstimateError"/>: an arrow carries the rows of every execution
        /// the optimiser expected, which is what its width has to be comparable with.
        /// </summary>
        private static double EstimateError(double actual, double estimated) =>
            PlanOperator.EstimateErrorMultiple(actual, estimated);

        private static PlanEstimateAccuracy Accuracy(double error) => error switch
        {
            >= EstimateCriticalThreshold => PlanEstimateAccuracy.Critical,
            >= EstimateMismatchThreshold => PlanEstimateAccuracy.Warning,
            _ => PlanEstimateAccuracy.Good
        };

        /// <summary>
        /// Where an arrow meets its consumer.  Several inputs are spread down the consumer's right
        /// edge rather than all meeting at its centre, because at the thicknesses a busy plan
        /// produces, arrows sharing one entry point hide each other completely.
        /// </summary>
        private static double EntryY(PlanNode consumer, int index, int siblingCount)
        {
            var bounds = consumer.Bounds;
            if (siblingCount <= 1) return bounds.Centre.Y;

            return bounds.Top + (bounds.Height * (index + 1) / (siblingCount + 1.0));
        }

        /// <summary>
        /// Arrow thickness from a row count or byte count, on a square root scale against
        /// <paramref name="scale"/> - the busiest arrow, or the floor.
        ///
        /// Linear is too harsh: row counts in one plan routinely span six orders of magnitude, and
        /// a linear scale draws one fat arrow and a set of identical hairlines.  But a log scale is
        /// too generous, and defeats the floor: 5,000 rows is already 62% of the way to a million
        /// on a log scale, so a small plan still came out drawn in thick pipes.  The square root sits
        /// between the two - 5,000 rows against a million is 7% of the width, 100,000 is 32% - so a
        /// small plan looks light, and within a big one the arrows still differ visibly.
        /// </summary>
        private double Thickness(double value, double scale)
        {
            if (value <= 0 || scale <= 0) return _options.MinEdgeThickness;

            var share = Math.Min(1, Math.Sqrt(value / scale));
            return _options.MinEdgeThickness + (share * (_options.MaxEdgeThickness - _options.MinEdgeThickness));
        }
    }
}
