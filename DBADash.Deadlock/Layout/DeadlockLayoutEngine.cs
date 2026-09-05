using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Layout
{
    /// <summary>
    /// Turns a parsed <see cref="DeadlockGraph"/> into positioned nodes and routed edges.
    ///
    /// Deadlocks are cycles, so the nodes are placed on a ring in cycle order, alternating process
    /// and resource.  That makes the cycle itself the shape of the picture: the classic two process
    /// deadlock comes out as a diamond, a three way as a hexagon.  Deadlock graphs are small - a
    /// handful of nodes - so nothing more elaborate than a ring is warranted, and a general graph
    /// layout library would be a large dependency for no gain.
    ///
    /// Contains no drawing code and no drawing dependency: text is sized through
    /// <see cref="IDeadlockTextMeasurer"/> so this stays portable and unit testable.
    /// </summary>
    public sealed class DeadlockLayoutEngine
    {
        private readonly IDeadlockTextMeasurer _measurer;
        private readonly DeadlockLayoutOptions _options;

        /// <summary>
        /// How much of an edge the labels of a shared pair are spread over - the middle 60%, leaving
        /// the ends clear of the node boxes.  Used both to place the labels and to work out how far
        /// apart the pair has to be for them to fit.
        /// </summary>
        private const double LabelSpread = 0.6;

        public DeadlockLayoutEngine(IDeadlockTextMeasurer measurer, DeadlockLayoutOptions? options = null)
        {
            _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
            _options = options ?? new DeadlockLayoutOptions();
        }

        public DeadlockLayout Layout(DeadlockGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var processNodes = new Dictionary<DeadlockProcess, DeadlockProcessNode>();
            for (var i = 0; i < graph.Processes.Count; i++)
            {
                var process = graph.Processes[i];
                processNodes[process] = BuildProcessNode(process, i);
            }

            // A deadlock is nearly always inside one database, and then naming it on every resource
            // box is noise that crowds out the table.  Across databases it is the opposite - which
            // database a table is in becomes the point - so it goes back on.
            var databases = graph.Resources
                .Select(r => r.DatabaseName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var resourceNodes = new Dictionary<DeadlockResource, DeadlockResourceNode>();
            for (var i = 0; i < graph.Resources.Count; i++)
            {
                var resource = graph.Resources[i];
                resourceNodes[resource] = BuildResourceNode(resource, i, showDatabase: databases > 1);
            }

            var (ordered, cycle, cyclePairs) = OrderNodes(graph, processNodes, resourceNodes);

            foreach (var node in cycle)
            {
                node.IsInCycle = true;
            }

            // Collected before placement because what runs between two nodes decides both how far
            // apart they have to be and, for the layered style, which column each one lands in.
            var pending = CollectEdges(graph, processNodes, resourceNodes);
            var separations = MinimumSeparations(pending);
            var adjacency = Adjacency(pending);

            if (_options.Style == DeadlockLayoutStyle.Layered)
            {
                PlaceInColumns(ordered, adjacency, pending);
            }
            else
            {
                PlaceOnRing(ordered, cycle, adjacency, separations, pending);
            }

            var bounds = Normalise(ordered);

            var layout = new DeadlockLayout
            {
                Graph = graph,
                Nodes = ordered,
                Cycle = cycle,
                Bounds = bounds,
                NodePadding = _options.NodePadding,
                LineSpacing = _options.LineSpacing
            };

            layout.Router = this;
            layout.Edges = RouteEdges(pending, cyclePairs, ordered);
            return layout;
        }

        /// <summary>
        /// Route the edges of a layout again, after something has moved its nodes.
        ///
        /// Nothing about a routed edge depends on how the node it joins came to be where it is, so a
        /// moved node needs only this rather than a fresh layout - which would throw away every other
        /// node the reader had arranged.  The inputs are rebuilt from the edges themselves: an edge
        /// carries the pair it joins, its kind and its label, which is all the routing needs, and the
        /// cycle it belongs to is already marked on it.
        ///
        /// The extent is recomputed but <em>not</em> normalised back to the origin: shifting every
        /// node because one was dragged past the left edge would move the whole graph out from under
        /// the reader.  Coordinates may therefore be negative after a move, which is why
        /// <see cref="DeadlockLayout.Bounds"/> carries its own origin.
        /// </summary>
        internal void Reroute(DeadlockLayout layout)
        {
            ArgumentNullException.ThrowIfNull(layout);

            var pending = layout.Edges
                .Select(e => new PendingEdge(e.From, e.To, e.Kind, e.Label))
                .ToList();

            var cyclePairs = new HashSet<(DeadlockNode From, DeadlockNode To)>(
                layout.Edges.Where(e => e.IsInCycle).Select(e => (e.From, e.To)));

            layout.Edges = RouteEdges(pending, cyclePairs, layout.Nodes);
            layout.Bounds = Extent(layout.Nodes);
        }

        // ---------------------------------------------------------------- nodes

        private DeadlockProcessNode BuildProcessNode(DeadlockProcess process, int index)
        {
            var details = new List<string>();

            if (!string.IsNullOrWhiteSpace(process.CurrentDatabaseName)) details.Add(process.CurrentDatabaseName!);
            if (!string.IsNullOrWhiteSpace(process.LoginName)) details.Add(process.LoginName!);
            if (!string.IsNullOrWhiteSpace(process.ClientApp)) details.Add(process.ClientApp!);

            var activity = FormatActivity(process);
            if (activity is not null) details.Add(activity);

            var node = new DeadlockProcessNode
            {
                Process = process,
                Id = string.IsNullOrEmpty(process.Id) ? $"process#{index}" : process.Id,
                Title = process.DisplayName,
                DetailLines = Trim(details),
                StatementPreview = SingleLine(process.PrimaryStatement)
            };

            node.Bounds = LayoutRect.FromCentre(new LayoutPoint(0, 0), MeasureNode(node));
            node.StatementLinkLocalBounds = MeasureStatementLink(node);
            return node;
        }

        /// <param name="showDatabase">
        /// Whether the database name earns a line of its own - true only when the graph spans more
        /// than one.  Neither the title nor the object line carries it otherwise.
        /// </param>
        private DeadlockResourceNode BuildResourceNode(DeadlockResource resource, int index, bool showDatabase)
        {
            var details = new List<string>();

            // A page based lock is titled by its page, so the object name comes down here - it is
            // still worth reading, it just cannot be what identifies the box.  For everything else
            // the title already carries the object name, and the element name is what is missing.
            if (!string.IsNullOrWhiteSpace(resource.ObjectName))
            {
                if (resource.PageKey is not null) details.Add(resource.SchemaQualifiedName!);
                if (showDatabase && resource.DatabaseName is { } database) details.Add(database);
                details.Add(resource.TypeName);
            }
            if (!string.IsNullOrWhiteSpace(resource.Mode)) details.Add($"Mode: {resource.Mode}");

            // Parallelism resources carry their detail in the wait type rather than an object name.
            if (resource.Attributes.TryGetValue("WaitType", out var waitType) && !string.IsNullOrWhiteSpace(waitType))
            {
                details.Add(waitType);
            }

            var node = new DeadlockResourceNode
            {
                Resource = resource,
                Id = string.IsNullOrEmpty(resource.Id) ? $"resource#{index}" : resource.Id,
                Title = resource.DisplayName,
                DetailLines = Trim(details)
            };

            node.Bounds = LayoutRect.FromCentre(new LayoutPoint(0, 0), MeasureNode(node));
            return node;
        }

        private IReadOnlyList<string> Trim(List<string> lines) =>
            lines.Count <= _options.MaxDetailLines
                ? lines
                : lines.Take(_options.MaxDetailLines).ToList();

        /// <summary>
        /// How long the process waited and how much log it burned, on one line.
        ///
        /// They share a line rather than taking one each because between them they say what kind of
        /// participant this is: log used of zero is a reader that is only holding shared locks, while
        /// a large figure is a transaction with a lot to roll back if it is the one chosen as victim.
        /// A node only shows a few detail lines, and this pair earns one between them, not two.
        /// </summary>
        private static string? FormatActivity(DeadlockProcess process)
        {
            var parts = new List<string>();

            if (process.WaitTime is { } wait) parts.Add($"Waited {DeadlockFormat.Duration(wait)}");
            if (process.LogUsed is { } logUsed) parts.Add($"Log Used {DeadlockFormat.Bytes(logUsed)}");

            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }

        /// <summary>
        /// Collapses a statement to a single line for the node preview: runs of whitespace (including
        /// the newlines a multi-statement batch carries) become a single space, so the elided preview
        /// reads as one line rather than showing the first line only.
        /// </summary>
        private static string? SingleLine(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var collapsed = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
            return collapsed.Length == 0 ? null : collapsed;
        }

        private LayoutSize MeasureNode(DeadlockNode node)
        {
            var title = _measurer.Measure(node.Title, DeadlockTextRole.Title);
            var width = title.Width;
            var height = title.Height;

            foreach (var line in node.DetailLines)
            {
                var size = _measurer.Measure(line, DeadlockTextRole.Detail);
                width = Math.Max(width, size.Width);
                height += size.Height + _options.LineSpacing;
            }

            var hasStatement = !string.IsNullOrEmpty(node.StatementPreview);
            if (hasStatement)
            {
                // The statement is the detail people are usually hunting for, so let it widen the
                // box (up to a larger cap) rather than always eliding to whatever width the title
                // and details happened to give the node.  Height is reserved for a single line.
                var size = _measurer.Measure(node.StatementPreview!, DeadlockTextRole.Detail);
                width = Math.Max(width, size.Width);
                height += size.Height + _options.LineSpacing;
            }

            width += 2 * _options.NodePadding;
            height += 2 * _options.NodePadding;

            // Clamped so a long object name cannot stretch the ring.  Statement-bearing nodes get a
            // wider allowance so more SQL fits; the renderer still elides anything past the box.
            var maxWidth = hasStatement ? _options.MaxStatementNodeWidth : _options.MaxNodeWidth;
            width = Math.Clamp(width, _options.MinNodeWidth, maxWidth);

            return new LayoutSize(width, height);
        }

        /// <summary>
        /// The clickable region of the statement preview, relative to the node's top-left corner.
        /// Mirrors the line stacking the renderer draws with (title, then each detail line, then the
        /// statement), so the box lines up with the underlined text.  Null when there is no statement.
        /// </summary>
        private LayoutRect? MeasureStatementLink(DeadlockNode node)
        {
            if (string.IsNullOrEmpty(node.StatementPreview)) return null;

            var top = _options.NodePadding + _measurer.Measure(node.Title, DeadlockTextRole.Title).Height;
            foreach (var line in node.DetailLines)
            {
                top += _options.LineSpacing + _measurer.Measure(line, DeadlockTextRole.Detail).Height;
            }
            top += _options.LineSpacing;

            var statement = _measurer.Measure(node.StatementPreview, DeadlockTextRole.Detail);
            var maxWidth = node.Bounds.Width - (2 * _options.NodePadding);
            var width = Math.Min(statement.Width, maxWidth);

            return new LayoutRect(_options.NodePadding, top, width, statement.Height);
        }

        // ---------------------------------------------------------------- ordering

        /// <summary>
        /// Puts the cycle in ring order - a process, the resource it is waiting on, the process
        /// owning that resource, and so on.  Anything outside the cycle (a process blocked by it but
        /// not part of it, a second lock the same process holds, a resource with no resolvable owner)
        /// is appended afterwards so it is still shown.
        ///
        /// Also returns the node pairs the cycle runs through, so the edges along it can be drawn as
        /// the cycle rather than inferred from ring adjacency: a resource contended by two waiters
        /// appears once on the ring but is entered and left twice.
        /// </summary>
        private static (List<DeadlockNode> Ordered, IReadOnlyList<DeadlockNode> Cycle,
            HashSet<(DeadlockNode From, DeadlockNode To)> CyclePairs) OrderNodes(
                DeadlockGraph graph,
                Dictionary<DeadlockProcess, DeadlockProcessNode> processNodes,
                Dictionary<DeadlockResource, DeadlockResourceNode> resourceNodes)
        {
            var ordered = new List<DeadlockNode>();
            var cycle = new List<DeadlockNode>();
            var cyclePairs = new HashSet<(DeadlockNode From, DeadlockNode To)>();
            var seenProcesses = new HashSet<DeadlockProcess>();
            var seenResources = new HashSet<DeadlockResource>();

            // The cycle itself is a property of the graph rather than of the drawing, so it is found
            // once in DeadlockCycle and used both here and by the analysis.
            foreach (var step in DeadlockCycle.Find(graph))
            {
                var waiter = processNodes[step.Waiter];
                var resource = resourceNodes[step.Resource];

                // The owner closes this step and opens the next one, where it is the waiter - so it
                // is added by that step, or is the start of the cycle and already on the ring.
                if (seenProcesses.Add(step.Waiter))
                {
                    ordered.Add(waiter);
                    cycle.Add(waiter);
                }

                if (seenResources.Add(step.Resource))
                {
                    ordered.Add(resource);
                    cycle.Add(resource);
                }

                cyclePairs.Add((waiter, resource));
                cyclePairs.Add((resource, processNodes[step.Owner]));
            }

            foreach (var process in graph.Processes.Where(p => !seenProcesses.Contains(p)))
            {
                ordered.Add(processNodes[process]);
            }

            foreach (var resource in graph.Resources.Where(r => !seenResources.Contains(r)))
            {
                ordered.Add(resourceNodes[resource]);
            }

            return (ordered, cycle, cyclePairs);
        }

        // ---------------------------------------------------------------- placement

        /// <summary>
        /// The cycle on a ring, and anything else beside whichever node it connects to.
        ///
        /// Only the cycle earns a slot on the ring.  A process that holds a lock the deadlock touches
        /// without being deadlocked itself - a second holder of a shared lock, say - used to take an
        /// equal slot, which stretched the ring by a node and dragged its one edge back across the
        /// middle of the picture to reach the node it belongs to.
        /// </summary>
        private void PlaceOnRing(
            List<DeadlockNode> nodes,
            IReadOnlyList<DeadlockNode> cycle,
            Dictionary<DeadlockNode, List<DeadlockNode>> adjacency,
            Dictionary<(DeadlockNode Resource, DeadlockNode Process), double> separations,
            IReadOnlyList<PendingEdge> pending)
        {
            if (nodes.Count == 0) return;

            if (nodes.Count == 1)
            {
                nodes[0].Bounds = LayoutRect.FromCentre(new LayoutPoint(0, 0), nodes[0].Bounds.Size);
                return;
            }

            // With no cycle traced there is nothing to build the ring around, so everything goes on
            // it - which is all a truncated capture can be shown as anyway.
            var ring = cycle.Count > 1 ? cycle.ToList() : nodes;

            // Start at the top so the first node - the victim, where there is one - leads.
            var angles = new double[ring.Count];
            for (var i = 0; i < ring.Count; i++)
            {
                angles[i] = (-Math.PI / 2) + (i * 2 * Math.PI / ring.Count);
            }

            var radius = RequiredRadius(ring, angles, separations);

            for (var i = 0; i < ring.Count; i++)
            {
                var centre = new LayoutPoint(radius * Math.Cos(angles[i]), radius * Math.Sin(angles[i]));
                ring[i].Bounds = LayoutRect.FromCentre(centre, ring[i].Bounds.Size);
            }

            if (ring.Count < nodes.Count) PlaceSatellites(nodes, ring, adjacency, separations, pending);
        }

        /// <summary>
        /// Places the nodes that are not on the ring: each one just outside the ring, on the far side
        /// of the node it connects to, so its edge is short and points away from the cycle rather than
        /// through it.  Pushed further out until it is clear of everything already placed.
        /// </summary>
        private void PlaceSatellites(
            List<DeadlockNode> nodes,
            List<DeadlockNode> ring,
            Dictionary<DeadlockNode, List<DeadlockNode>> adjacency,
            Dictionary<(DeadlockNode Resource, DeadlockNode Process), double> separations,
            IReadOnlyList<PendingEdge> pending)
        {
            var placed = new List<DeadlockNode>(ring);
            var centre = new LayoutPoint(0, 0);

            foreach (var node in nodes.Where(n => !ring.Contains(n)))
            {
                var anchor = adjacency.TryGetValue(node, out var neighbours)
                    ? neighbours.FirstOrDefault(placed.Contains)
                    : null;

                var from = anchor?.Bounds.Centre ?? centre;
                var direction = UnitVector(centre, from);

                // The gap holds the edge's label, so it has to be wide enough for it - the same rule
                // the ring uses, applied to the one edge tying the satellite on.
                var spacing = Math.Max(
                    anchor is null ? _options.NodeSpacing : Separation(separations, anchor, node),
                    WidestLabel(pending, node, anchor) + _options.EdgeLabelClearance);
                var distance = SupportRadius(anchor?.Bounds, direction) +
                               SupportRadius(node.Bounds, direction) +
                               spacing;

                for (var attempt = 0; attempt < MaxSatelliteAttempts; attempt++)
                {
                    node.Bounds = LayoutRect.FromCentre(
                        new LayoutPoint(from.X + (direction.X * distance), from.Y + (direction.Y * distance)),
                        node.Bounds.Size);

                    if (!placed.Any(p => Overlaps(p.Bounds, node.Bounds, _options.NodeSpacing))) break;
                    distance += _options.NodeSpacing;
                }

                placed.Add(node);
            }
        }

        /// <summary>
        /// Columns left to right by distance from the victim, the way SSMS draws a deadlock: who is
        /// waiting, what they are waiting for, who is holding it, and so on outwards.
        /// </summary>
        private void PlaceInColumns(
            List<DeadlockNode> nodes,
            Dictionary<DeadlockNode, List<DeadlockNode>> adjacency,
            IReadOnlyList<PendingEdge> pending)
        {
            if (nodes.Count == 0) return;

            var layers = LayerNodes(nodes, adjacency);
            var columns = layers.Values.Distinct().OrderBy(layer => layer)
                .Select(layer => nodes.Where(n => layers[n] == layer).ToList())
                .ToList();

            var x = 0.0;
            var previousWidth = 0.0;

            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                var width = column.Max(n => n.Bounds.Width);

                if (c > 0)
                {
                    x += (previousWidth / 2) + ColumnGap(columns[c - 1], column, pending) + (width / 2);
                }

                // Centred on the same axis so the picture reads as one band rather than a staircase.
                var height = column.Sum(n => n.Bounds.Height) + (_options.NodeSpacing * (column.Count - 1));
                var y = -height / 2;

                foreach (var node in column)
                {
                    node.Bounds = LayoutRect.FromCentre(
                        new LayoutPoint(x, y + (node.Bounds.Height / 2)),
                        node.Bounds.Size);
                    y += node.Bounds.Height + _options.NodeSpacing;
                }

                previousWidth = width;
            }
        }

        /// <summary>
        /// Numbers each node by how many hops it is from the victim, which is what puts the waiting
        /// process on the left and works outwards.  Anything the walk cannot reach - a second,
        /// disjoint deadlock in the same graph - is given a column of its own on the end.
        /// </summary>
        private static Dictionary<DeadlockNode, int> LayerNodes(
            List<DeadlockNode> nodes,
            Dictionary<DeadlockNode, List<DeadlockNode>> adjacency)
        {
            var layers = new Dictionary<DeadlockNode, int>();
            var start = nodes.FirstOrDefault(n => n is DeadlockProcessNode { Process.IsVictim: true }) ?? nodes[0];

            var queue = new Queue<DeadlockNode>();
            layers[start] = 0;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!adjacency.TryGetValue(node, out var neighbours)) continue;

                foreach (var neighbour in neighbours.Where(n => !layers.ContainsKey(n)))
                {
                    layers[neighbour] = layers[node] + 1;
                    queue.Enqueue(neighbour);
                }
            }

            var spare = layers.Count == 0 ? 0 : layers.Values.Max() + 1;
            foreach (var node in nodes.Where(n => !layers.ContainsKey(n)))
            {
                layers[node] = spare;
            }

            return layers;
        }

        /// <summary>
        /// How wide the gap between two columns has to be: enough for the labels on the edges that
        /// cross it, which is what fills that space.
        /// </summary>
        private double ColumnGap(
            List<DeadlockNode> left,
            List<DeadlockNode> right,
            IReadOnlyList<PendingEdge> pending)
        {
            var crossing = pending.Where(e =>
                (left.Contains(e.From) && right.Contains(e.To)) ||
                (right.Contains(e.From) && left.Contains(e.To)));

            var widest = crossing
                .Select(e => _measurer.Measure(e.Label, DeadlockTextRole.EdgeLabel).Width)
                .DefaultIfEmpty(0)
                .Max();

            return Math.Max(_options.NodeSpacing * 2, widest + _options.EdgeLabelClearance);
        }

        /// <summary>How many times a satellite is pushed further out before it is left where it is.</summary>
        private const int MaxSatelliteAttempts = 24;

        /// <summary>The widest label on the edges between two nodes, or zero when they are not joined.</summary>
        private double WidestLabel(IReadOnlyList<PendingEdge> pending, DeadlockNode node, DeadlockNode? other) =>
            other is null
                ? 0
                : pending
                    .Where(e => (ReferenceEquals(e.From, node) && ReferenceEquals(e.To, other)) ||
                                (ReferenceEquals(e.From, other) && ReferenceEquals(e.To, node)))
                    .Select(e => _measurer.Measure(e.Label, DeadlockTextRole.EdgeLabel).Width)
                    .DefaultIfEmpty(0)
                    .Max();

        /// <summary>
        /// How far the border of a rectangle reaches from its centre in a direction.  Exact for an
        /// axis aligned rectangle, and what makes a satellite clear of the node it hangs off however
        /// the two are shaped.
        /// </summary>
        private static double SupportRadius(LayoutRect? rect, LayoutPoint direction) =>
            rect is null
                ? 0
                : (Math.Abs(direction.X) * rect.Value.Width / 2) + (Math.Abs(direction.Y) * rect.Value.Height / 2);

        private static LayoutPoint UnitVector(LayoutPoint from, LayoutPoint to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt((dx * dx) + (dy * dy));

            // Nothing to point along - anywhere will do, so out to the right.
            return length < double.Epsilon ? new LayoutPoint(1, 0) : new LayoutPoint(dx / length, dy / length);
        }

        private static bool Overlaps(LayoutRect a, LayoutRect b, double gap)
        {
            var grown = a.Inflate(gap / 2);
            var other = b.Inflate(gap / 2);

            return grown.Left < other.Right && other.Left < grown.Right &&
                   grown.Top < other.Bottom && other.Top < grown.Bottom;
        }

        /// <summary>
        /// The smallest ring radius at which no two node rectangles overlap.
        ///
        /// Chord length alone is not enough: these are axis aligned rectangles, so two nodes are
        /// clear of each other when they are separated on <em>either</em> axis.  For each pair, the
        /// radius needed on each axis is the required gap divided by how far apart the pair's unit
        /// circle positions are on that axis, and the pair needs only the smaller of the two.  The
        /// ring takes the largest requirement across every pair - not just neighbours, since with an
        /// odd node count the widest separation is not always between adjacent nodes.
        /// </summary>
        private double RequiredRadius(
            List<DeadlockNode> nodes,
            double[] angles,
            Dictionary<(DeadlockNode Resource, DeadlockNode Process), double> separations)
        {
            var radius = 0.0;

            for (var i = 0; i < nodes.Count; i++)
            {
                for (var j = i + 1; j < nodes.Count; j++)
                {
                    var dxUnit = Math.Abs(Math.Cos(angles[j]) - Math.Cos(angles[i]));
                    var dyUnit = Math.Abs(Math.Sin(angles[j]) - Math.Sin(angles[i]));

                    var spacing = Separation(separations, nodes[i], nodes[j]);
                    var needX = ((nodes[i].Bounds.Width + nodes[j].Bounds.Width) / 2) + spacing;
                    var needY = ((nodes[i].Bounds.Height + nodes[j].Bounds.Height) / 2) + spacing;

                    var byX = dxUnit < 1e-9 ? double.PositiveInfinity : needX / dxUnit;
                    var byY = dyUnit < 1e-9 ? double.PositiveInfinity : needY / dyUnit;

                    var needed = Math.Min(byX, byY);
                    if (!double.IsInfinity(needed))
                    {
                        radius = Math.Max(radius, needed);
                    }
                }
            }

            return radius;
        }

        /// <summary>
        /// The clear space a pair of nodes needs, whichever way round they are asked about.  Falls
        /// back to <see cref="DeadlockLayoutOptions.NodeSpacing"/>, which is what nearly every pair
        /// gets: the extra only applies where several edges share a pair.
        /// </summary>
        private double Separation(
            Dictionary<(DeadlockNode Resource, DeadlockNode Process), double> separations,
            DeadlockNode a,
            DeadlockNode b)
        {
            if (separations.TryGetValue((a, b), out var forward)) return forward;
            return separations.TryGetValue((b, a), out var reverse) ? reverse : _options.NodeSpacing;
        }

        /// <summary>
        /// Shifts everything so the layout starts at the origin, so no renderer has to deal with
        /// negative coordinates.
        /// </summary>
        private LayoutRect Normalise(List<DeadlockNode> nodes)
        {
            if (nodes.Count == 0) return new LayoutRect(0, 0, 0, 0);

            var extent = Extent(nodes);

            foreach (var node in nodes)
            {
                node.Bounds = node.Bounds.Offset(-extent.X, -extent.Y);
            }

            return new LayoutRect(0, 0, extent.Width, extent.Height);
        }

        /// <summary>Everything the nodes cover, plus the margin.  Not moved to the origin.</summary>
        private LayoutRect Extent(IReadOnlyList<DeadlockNode> nodes)
        {
            if (nodes.Count == 0) return new LayoutRect(0, 0, 0, 0);

            var extent = nodes[0].Bounds;
            for (var i = 1; i < nodes.Count; i++)
            {
                extent = extent.Union(nodes[i].Bounds);
            }

            return extent.Inflate(_options.Margin);
        }

        // ---------------------------------------------------------------- edges

        /// <summary>An edge before it has been routed, while the ones sharing a node pair are counted.</summary>
        private readonly record struct PendingEdge(
            DeadlockNode From,
            DeadlockNode To,
            DeadlockEdgeKind Kind,
            string Label);

        private static List<PendingEdge> CollectEdges(
            DeadlockGraph graph,
            Dictionary<DeadlockProcess, DeadlockProcessNode> processNodes,
            Dictionary<DeadlockResource, DeadlockResourceNode> resourceNodes)
        {
            var pending = new List<PendingEdge>();

            foreach (var resource in graph.Resources)
            {
                var resourceNode = resourceNodes[resource];

                // Owner arrows point at the process holding the resource; waiter arrows point at the
                // resource being waited for.  Participants whose process is missing from the
                // process-list are skipped - a truncated graph has no node to connect them to.
                foreach (var owner in resource.Owners.Where(o => o.Process is not null))
                {
                    pending.Add(new PendingEdge(
                        resourceNode,
                        processNodes[owner.Process!],
                        DeadlockEdgeKind.Owner,
                        Label("Owner", owner.Mode)));
                }

                foreach (var waiter in resource.Waiters.Where(w => w.Process is not null))
                {
                    pending.Add(new PendingEdge(
                        processNodes[waiter.Process!],
                        resourceNode,
                        DeadlockEdgeKind.Waiter,
                        Label("Requested", waiter.Mode)));
                }
            }

            return pending;
        }

        /// <summary>
        /// The clear space each node pair needs, which is <see cref="DeadlockLayoutOptions.NodeSpacing"/>
        /// unless more than one edge runs between them.
        ///
        /// A single label sits at the middle of its edge and reads perfectly well over a short one, so
        /// an ordinary deadlock is left alone.  Several edges between one pair share the gap - their
        /// labels are spread along the line to keep them off each other - so the line has to be long
        /// enough to spread them over, or the arrows come out stubby and the labels pile up.
        /// </summary>
        private Dictionary<(DeadlockNode Resource, DeadlockNode Process), double> MinimumSeparations(
            IReadOnlyList<PendingEdge> pending)
        {
            var widest = new Dictionary<(DeadlockNode Resource, DeadlockNode Process), double>();
            var counts = new Dictionary<(DeadlockNode Resource, DeadlockNode Process), int>();

            foreach (var edge in pending)
            {
                var pair = PairOf(edge);
                var width = _measurer.Measure(edge.Label, DeadlockTextRole.EdgeLabel).Width;

                widest[pair] = Math.Max(widest.GetValueOrDefault(pair), width);
                counts[pair] = counts.GetValueOrDefault(pair) + 1;
            }

            var separations = new Dictionary<(DeadlockNode Resource, DeadlockNode Process), double>();

            foreach (var (pair, count) in counts)
            {
                if (count < 2) continue;

                // The labels occupy LabelSpread of the line, so that portion has to hold them all
                // side by side.
                var needed = (widest[pair] + _options.EdgeLabelClearance) * (count - 1) / LabelSpread;
                separations[pair] = Math.Max(_options.NodeSpacing, needed);
            }

            return separations;
        }

        /// <summary>Which nodes each node is joined to, in both directions.</summary>
        private static Dictionary<DeadlockNode, List<DeadlockNode>> Adjacency(IReadOnlyList<PendingEdge> pending)
        {
            var adjacency = new Dictionary<DeadlockNode, List<DeadlockNode>>();

            void Join(DeadlockNode from, DeadlockNode to)
            {
                if (!adjacency.TryGetValue(from, out var neighbours))
                {
                    adjacency[from] = neighbours = new List<DeadlockNode>();
                }
                if (!neighbours.Contains(to)) neighbours.Add(to);
            }

            foreach (var edge in pending)
            {
                Join(edge.From, edge.To);
                Join(edge.To, edge.From);
            }

            return adjacency;
        }

        private IReadOnlyList<DeadlockEdge> RouteEdges(
            IReadOnlyList<PendingEdge> pending,
            HashSet<(DeadlockNode From, DeadlockNode To)> cyclePairs,
            IReadOnlyList<DeadlockNode> nodes)
        {
            // Edges joining the same two nodes have to be spread apart or they land on top of each
            // other.  Counted first, so an edge that has the pair to itself is routed straight down
            // the middle - which is every edge in an ordinary deadlock.
            var perPair = new Dictionary<(DeadlockNode Resource, DeadlockNode Process), int>();
            foreach (var edge in pending)
            {
                perPair[PairOf(edge)] = perPair.GetValueOrDefault(PairOf(edge)) + 1;
            }

            var placed = new Dictionary<(DeadlockNode Resource, DeadlockNode Process), int>();
            var edges = new List<DeadlockEdge>(pending.Count);

            foreach (var edge in pending)
            {
                var pair = PairOf(edge);
                var total = perPair[pair];
                var index = placed.GetValueOrDefault(pair);
                placed[pair] = index + 1;

                var offset = total == 1
                    ? 0
                    : (index - ((total - 1) / 2.0)) * _options.ParallelEdgeSpacing;

                // Labels sit at the middle of an edge that has its pair to itself.  Edges sharing a
                // pair space their labels out along the line instead: the lines are only far enough
                // apart to be told apart, which is not far enough to keep two labels off each other.
                var labelFraction = total == 1
                    ? 0.5
                    : ((1 - LabelSpread) / 2) + (LabelSpread * index / (total - 1.0));

                edges.Add(BuildEdge(edge, pair, offset, labelFraction, cyclePairs));
            }

            SeparateLabels(edges, nodes);
            return edges;
        }

        /// <summary>
        /// Moves labels off each other.
        ///
        /// Spacing the labels of one node pair apart is not enough: any two edges that cross put their
        /// labels in the same place, and one hides the other.  Which is worse than it sounds - the
        /// label carries the lock mode, so a hidden "Owner: S" next to a visible "Requested: U" reads
        /// as a shared lock being an update lock.
        ///
        /// Each label is tried at its preferred spot first, then progressively further along its own
        /// edge and to either side of it, and takes the first position that collides with nothing.
        /// Cycle edges are placed first so the arrows the reader is following keep the best spots, and
        /// overlapping a node counts against a position but far less than overlapping another label:
        /// labels are drawn last, over a background, so they stay readable over a box.
        /// </summary>
        private void SeparateLabels(List<DeadlockEdge> edges, IReadOnlyList<DeadlockNode> nodes)
        {
            var taken = new List<LayoutRect>();

            foreach (var edge in edges.Where(e => !string.IsNullOrEmpty(e.Label))
                         .OrderByDescending(e => e.IsInCycle))
            {
                var size = LabelSize(edge.Label);
                var preferred = FractionOf(edge, edge.LabelAnchor);
                var crossings = Crossings(edge, edges);

                var best = LabelRect(edge, preferred, 0, size);
                var bestCost = double.MaxValue;

                foreach (var (fraction, sideways) in LabelCandidates(preferred, size.Height))
                {
                    var candidate = LabelRect(edge, fraction, sideways, size);
                    var cost = (taken.Count(r => Intersects(r, candidate)) * LabelCollisionCost) +
                               (crossings.Count(candidate.Contains) * CrossingCost) +
                               nodes.Count(n => Intersects(n.Bounds, candidate));

                    if (cost >= bestCost) continue;

                    best = candidate;
                    bestCost = cost;
                    if (cost == 0) break;
                }

                edge.LabelAnchor = best.Centre;
                taken.Add(best);
            }
        }

        /// <summary>
        /// Positions to try for a label, nearest its preferred spot first: along its own edge, then
        /// stepped to either side of it for the case where the edge is too short to move along.
        /// </summary>
        private static IEnumerable<(double Fraction, double Sideways)> LabelCandidates(
            double preferred,
            double labelHeight)
        {
            var step = labelHeight + 4;

            foreach (var sideways in new[] { 0.0, step, -step, step * 2, -step * 2 })
            {
                foreach (var shift in new[] { 0.0, 0.12, -0.12, 0.24, -0.24, 0.36, -0.36 })
                {
                    var fraction = preferred + shift;
                    if (fraction is >= 0.08 and <= 0.92) yield return (fraction, sideways);
                }
            }
        }

        /// <summary>How much worse covering another label is than covering a node.</summary>
        private const double LabelCollisionCost = 8;

        /// <summary>
        /// How much it costs to sit on the point where the label's own edge crosses another.  Two
        /// labels beside a crossing are ambiguous even when they do not overlap - either could belong
        /// to either arrow - so a label is better off further along its own line.
        /// </summary>
        private const double CrossingCost = 4;

        /// <summary>
        /// Where an edge crosses the others.  Only the crossings on this edge matter: they are the
        /// places its label cannot be read unambiguously.
        /// </summary>
        private static List<LayoutPoint> Crossings(DeadlockEdge edge, IReadOnlyList<DeadlockEdge> edges)
        {
            var points = new List<LayoutPoint>();

            foreach (var other in edges)
            {
                if (ReferenceEquals(other, edge)) continue;
                if (Crossing(edge, other) is { } point) points.Add(point);
            }

            return points;
        }

        /// <summary>Where two segments cross, or null when they do not.</summary>
        private static LayoutPoint? Crossing(DeadlockEdge a, DeadlockEdge b)
        {
            var ax = a.End.X - a.Start.X;
            var ay = a.End.Y - a.Start.Y;
            var bx = b.End.X - b.Start.X;
            var by = b.End.Y - b.Start.Y;

            var denominator = (ax * by) - (ay * bx);
            if (Math.Abs(denominator) < 1e-9) return null; // Parallel

            var dx = b.Start.X - a.Start.X;
            var dy = b.Start.Y - a.Start.Y;

            var t = ((dx * by) - (dy * bx)) / denominator;
            var u = ((dx * ay) - (dy * ax)) / denominator;

            if (t is < 0 or > 1 || u is < 0 or > 1) return null;

            return new LayoutPoint(a.Start.X + (t * ax), a.Start.Y + (t * ay));
        }

        /// <summary>
        /// The space a label takes, allowing for the margin the renderer draws around the text.  Kept
        /// generous: labels that almost touch read as badly as labels that overlap.
        /// </summary>
        private LayoutSize LabelSize(string label)
        {
            var text = _measurer.Measure(label, DeadlockTextRole.EdgeLabel);
            return new LayoutSize(text.Width + 8, text.Height + 4);
        }

        private static LayoutRect LabelRect(DeadlockEdge edge, double fraction, double sideways, LayoutSize size)
        {
            var centre = new LayoutPoint(
                edge.Start.X + ((edge.End.X - edge.Start.X) * fraction),
                edge.Start.Y + ((edge.End.Y - edge.Start.Y) * fraction));

            if (Math.Abs(sideways) > double.Epsilon)
            {
                var offset = Perpendicular(edge.Start, edge.End, sideways);
                centre = new LayoutPoint(centre.X + offset.X, centre.Y + offset.Y);
            }

            return LayoutRect.FromCentre(centre, size);
        }

        /// <summary>Where along an edge a point sits, as a fraction of its length.</summary>
        private static double FractionOf(DeadlockEdge edge, LayoutPoint point)
        {
            var dx = edge.End.X - edge.Start.X;
            var dy = edge.End.Y - edge.Start.Y;
            var lengthSquared = (dx * dx) + (dy * dy);

            return lengthSquared < double.Epsilon
                ? 0.5
                : (((point.X - edge.Start.X) * dx) + ((point.Y - edge.Start.Y) * dy)) / lengthSquared;
        }

        private static bool Intersects(LayoutRect a, LayoutRect b) =>
            a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;

        /// <summary>
        /// The node pair an edge joins, in a fixed order regardless of which way the arrow points, so
        /// the owner and waiter arrows between one process and one resource are recognised as sharing
        /// a pair - and are offset to opposite sides of it rather than both to the same side.
        /// </summary>
        private static (DeadlockNode Resource, DeadlockNode Process) PairOf(PendingEdge edge) =>
            edge.From is DeadlockResourceNode ? (edge.From, edge.To) : (edge.To, edge.From);

        private static string Label(string prefix, string? mode) =>
            string.IsNullOrWhiteSpace(mode) ? prefix : $"{prefix}: {mode}";

        private static DeadlockEdge BuildEdge(
            PendingEdge edge,
            (DeadlockNode Resource, DeadlockNode Process) pair,
            double offset,
            double labelFraction,
            HashSet<(DeadlockNode From, DeadlockNode To)> cyclePairs)
        {
            var start = BorderPoint(edge.From.Bounds, edge.To.Bounds.Centre);
            var end = BorderPoint(edge.To.Bounds, edge.From.Bounds.Centre);

            // The fraction is measured along the pair's own direction, not the arrow's: two arrows
            // between one pair point opposite ways, so taking it from each arrow's own start would
            // land both labels in the same place.
            var fraction = ReferenceEquals(edge.From, pair.Resource) ? labelFraction : 1 - labelFraction;

            if (Math.Abs(offset) > double.Epsilon)
            {
                // Shifting both ends by the same amount keeps the line parallel to the one it shares
                // the pair with.  The ends no longer sit exactly on the node borders - a few units of
                // overlap is the price of two arrows that can be told apart.
                var shift = Perpendicular(pair.Resource.Bounds.Centre, pair.Process.Bounds.Centre, offset);
                start = new LayoutPoint(start.X + shift.X, start.Y + shift.Y);
                end = new LayoutPoint(end.X + shift.X, end.Y + shift.Y);
            }

            return new DeadlockEdge
            {
                Kind = edge.Kind,
                From = edge.From,
                To = edge.To,
                Start = start,
                End = end,
                LabelAnchor = new LayoutPoint(
                    start.X + ((end.X - start.X) * fraction),
                    start.Y + ((end.Y - start.Y) * fraction)),
                Label = edge.Label,
                IsInCycle = cyclePairs.Contains((edge.From, edge.To))
            };
        }

        /// <summary>
        /// A vector of length <paramref name="distance"/> at right angles to the line from
        /// <paramref name="from"/> to <paramref name="to"/>.  Zero when the two points coincide.
        /// </summary>
        private static LayoutPoint Perpendicular(LayoutPoint from, LayoutPoint to, double distance)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt((dx * dx) + (dy * dy));

            return length < double.Epsilon
                ? new LayoutPoint(0, 0)
                : new LayoutPoint(-dy / length * distance, dx / length * distance);
        }

        /// <summary>
        /// Where the line from the centre of <paramref name="rect"/> towards <paramref name="target"/>
        /// crosses the rectangle's border.  Exact for an axis aligned rectangle.
        /// </summary>
        private static LayoutPoint BorderPoint(LayoutRect rect, LayoutPoint target)
        {
            var centre = rect.Centre;
            var dx = target.X - centre.X;
            var dy = target.Y - centre.Y;

            // Concentric nodes give no direction to clip along, so the centre is the only sane answer.
            if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon) return centre;

            var scaleX = Math.Abs(dx) < double.Epsilon
                ? double.PositiveInfinity
                : (rect.Width / 2) / Math.Abs(dx);
            var scaleY = Math.Abs(dy) < double.Epsilon
                ? double.PositiveInfinity
                : (rect.Height / 2) / Math.Abs(dy);

            var scale = Math.Min(scaleX, scaleY);
            return new LayoutPoint(centre.X + (dx * scale), centre.Y + (dy * scale));
        }
    }
}
