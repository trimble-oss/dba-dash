using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Interaction
{
    /// <summary>Which way <see cref="PlanViewController.MoveSelection"/> steps.</summary>
    public enum PlanNavigation
    {
        /// <summary>Towards the root, which is leftwards on screen.</summary>
        Consumer,

        /// <summary>Into the first input, which is rightwards on screen.</summary>
        Input,

        /// <summary>The node above, in the same column.</summary>
        Up,

        /// <summary>The node below, in the same column.</summary>
        Down
    }

    /// <summary>
    /// All the mutable state of a plan view: zoom, pan, what is selected, what is hovered, what a
    /// search has matched, and which metric the bars are showing.
    ///
    /// Deliberately not a control.  It holds no framework types and raises no framework events, so
    /// the host is a thin shim that forwards mouse and keyboard input and repaints when
    /// <see cref="Changed"/> fires - and the whole of the interaction behaviour is unit testable,
    /// including the parts that are awkward to verify by hand, such as zooming about the pointer.
    ///
    /// Both spaces use <see cref="LayoutPoint"/>.  Parameter names say which space is expected;
    /// mixing them is the classic source of zoom and pan bugs, so nothing here takes an ambiguous
    /// point.
    /// </summary>
    public sealed class PlanViewController
    {
        private readonly PlanViewOptions _options;

        private LayoutSize _viewport;
        private double _zoom = 1.0;
        private LayoutPoint _pan;
        private PlanNode? _selectedNode;
        private PlanNode? _hoveredNode;
        private PlanEdge? _hoveredEdge;
        private LayoutRect? _hoverAnchor;
        private PlanTooltip? _hoveredTooltip;
        private LayoutPoint _panAnchor;
        private PlanHeatMetric _heatMetric;
        private IReadOnlyList<PlanNode> _matches = [];
        private int _matchIndex = -1;
        private string _searchText = string.Empty;
        private HashSet<PlanNode> _pathToRoot = [];

        public PlanViewController(PlanLayout layout, PlanViewOptions? options = null)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _options = options ?? new PlanViewOptions();
            _heatMetric = _options.HeatMetric;

            // An estimated plan has no CPU or elapsed time to rank by, so a metric carried over from
            // the last plan viewed has to fall back rather than draw an empty picture.
            if (!Layout.Metrics.Supports(_heatMetric)) _heatMetric = PlanHeatMetric.OperatorCost;
        }

        /// <summary>Raised whenever something that affects what is drawn has changed.</summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Raised when the selected node changes, so the host can follow it - a properties panel, a
        /// statement highlight.
        /// </summary>
        public event EventHandler<PlanNode?>? SelectionChanged;

        /// <summary>Raised when a node is activated - double clicked - for the host to drill into.</summary>
        public event EventHandler<PlanNode>? NodeActivated;

        public PlanLayout Layout { get; }

        /// <summary>The size of the drawing surface, in screen units.</summary>
        public LayoutSize Viewport => _viewport;

        /// <summary>Screen units per layout unit.</summary>
        public double Zoom => _zoom;

        /// <summary>Screen space offset applied after scaling.</summary>
        public LayoutPoint Pan => _pan;

        public PlanNode? SelectedNode => _selectedNode;

        public PlanNode? HoveredNode => _hoveredNode;

        /// <summary>The arrow under the pointer, when no node is.</summary>
        public PlanEdge? HoveredEdge => _hoveredEdge;

        /// <summary>
        /// Tooltip for <see cref="HoveredNode"/> or <see cref="HoveredEdge"/>, or null when nothing
        /// is hovered.
        /// </summary>
        public PlanTooltip? HoveredTooltip => _hoveredTooltip;

        /// <summary>
        /// What the tooltip is placed beside, in layout space: the hovered node, or the point where
        /// the pointer met the hovered arrow.
        /// </summary>
        public LayoutRect? HoverAnchor => _hoverAnchor;

        /// <summary>True between <see cref="BeginPan"/> and <see cref="EndPan"/>.</summary>
        public bool IsPanning { get; private set; }

        /// <summary>
        /// True once the user has zoomed or panned, so automatic re-fitting on resize stops
        /// overriding what they chose.  <see cref="ZoomToFit"/> clears it, which is what makes the
        /// Fit button also mean "start following the window again".
        /// </summary>
        public bool IsViewUserAdjusted { get; private set; }

        /// <summary>
        /// Which metric the node bars and the heat colouring measure.  Setting it to one this plan
        /// cannot support - CPU time on an estimated plan - is ignored rather than drawing every bar
        /// empty.
        /// </summary>
        public PlanHeatMetric HeatMetric
        {
            get => _heatMetric;
            set
            {
                if (_heatMetric == value || !Layout.Metrics.Supports(value)) return;

                _heatMetric = value;
                Notify();
            }
        }

        /// <summary>
        /// What arrow thickness measures.  Changing it re-routes only the arrows, so the selection,
        /// the search and the view all stay exactly where they were.
        /// </summary>
        public PlanEdgeWidthMetric EdgeWidthMetric
        {
            get => Layout.EdgeWidthMetric;
            set
            {
                if (!Layout.SetEdgeWidthMetric(value)) return;

                ForgetHoveredEdge();
                Notify();
            }
        }

        /// <summary>
        /// Whether arrows are drawn by actual or estimated rows.  Like <see cref="EdgeWidthMetric"/>,
        /// only the arrows are routed again.
        /// </summary>
        public PlanEdgeWidthBasis EdgeWidthBasis
        {
            get => Layout.EdgeWidthBasis;
            set
            {
                if (!Layout.SetEdgeWidthBasis(value)) return;

                ForgetHoveredEdge();
                Notify();
            }
        }

        /// <summary>
        /// The nodes from the selection back to the root: where the rows this operator produces end
        /// up.  Empty while nothing is selected.
        ///
        /// A plan is read by following the data, and on a wide plan the path from one operator to
        /// the root is genuinely hard to trace by eye through a dozen crossing arrows.
        /// </summary>
        public IReadOnlySet<PlanNode> PathToRoot => _pathToRoot;

        /// <summary>The nodes matching the current search, in layout order.</summary>
        public IReadOnlyList<PlanNode> Matches => _matches;

        /// <summary>Which match <see cref="NextMatch"/> last moved to, or -1.</summary>
        public int MatchIndex => _matchIndex;

        public string SearchText => _searchText;

        // ---------------------------------------------------------------- transforms

        public LayoutPoint ToScreen(LayoutPoint layoutPoint) =>
            new((layoutPoint.X * _zoom) + _pan.X, (layoutPoint.Y * _zoom) + _pan.Y);

        public LayoutRect ToScreen(LayoutRect layoutRect) =>
            new(
                (layoutRect.X * _zoom) + _pan.X,
                (layoutRect.Y * _zoom) + _pan.Y,
                layoutRect.Width * _zoom,
                layoutRect.Height * _zoom);

        public LayoutPoint ToLayout(LayoutPoint screenPoint) =>
            new((screenPoint.X - _pan.X) / _zoom, (screenPoint.Y - _pan.Y) / _zoom);

        /// <summary>
        /// The part of the layout currently on screen, so the renderer can skip everything outside
        /// it.  A big plan at high zoom is mostly off screen, and drawing it all is wasted work on
        /// every frame.
        /// </summary>
        public LayoutRect VisibleBounds
        {
            get
            {
                var topLeft = ToLayout(new LayoutPoint(0, 0));
                var bottomRight = ToLayout(new LayoutPoint(_viewport.Width, _viewport.Height));

                return new LayoutRect(
                    topLeft.X,
                    topLeft.Y,
                    bottomRight.X - topLeft.X,
                    bottomRight.Y - topLeft.Y);
            }
        }

        // ---------------------------------------------------------------- view

        /// <summary>
        /// Tell the controller the drawing surface has changed size.
        ///
        /// Pass <paramref name="refit"/> false when the surface changed because the host opened or
        /// closed a panel beside it rather than because the window was resized.  Re-fitting then
        /// would rescale the whole plan on every click that opens the panel, moving the node just
        /// clicked out from under the pointer; keeping the zoom and pan instead leaves everything
        /// where it was and only covers or uncovers the edge.
        /// </summary>
        public bool SetViewport(LayoutSize viewport, bool refit = true)
        {
            if (_viewport == viewport) return false;

            _viewport = viewport;

            // Keep the whole plan in view as the window is resized - but only while the view is
            // still the one we chose.  Once the user has zoomed or panned, re-fitting under them
            // would throw away what they were looking at.
            if (refit && _options.RefitOnViewportChange && AutoFit()) return true;

            return Notify();
        }

        /// <summary>
        /// Fit the plan to the window the way opening it does: never zoomed out past
        /// <see cref="PlanViewOptions.MinAutoFitZoom"/>, and only while the view is still the one we
        /// chose - once the user has zoomed or panned, re-fitting under them would throw away what
        /// they were looking at.
        /// </summary>
        public bool AutoFit() => !IsViewUserAdjusted && Fit(_options.MinAutoFitZoom);

        /// <summary>
        /// Put the view back where opening the plan would have left it, whatever the user has zoomed
        /// or panned to since - which is what <see cref="AutoFit"/> will not do.
        ///
        /// For the changes that redraw the plan as a different shape rather than adjusting the one on
        /// screen.  Holding the view over one of those is no kindness: the operators the view was
        /// framing have moved, and what is left in frame can easily be the empty margin beside them.
        /// </summary>
        public bool FitAsOpened() => Fit(_options.MinAutoFitZoom);

        /// <summary>
        /// Scale and position the plan so it all fits the viewport, however far out that is.  Does
        /// nothing useful before a viewport is set, or for an empty layout.
        /// </summary>
        public bool ZoomToFit() => Fit(_options.MinZoom);

        private bool Fit(double floor)
        {
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return false;
            if (Layout.Bounds.Width <= 0 || Layout.Bounds.Height <= 0) return false;

            var scale = Math.Min(
                _viewport.Width / Layout.Bounds.Width,
                _viewport.Height / Layout.Bounds.Height);

            scale = Math.Min(scale, _options.MaxFitZoom);
            scale = Math.Clamp(scale, Math.Clamp(floor, _options.MinZoom, _options.MaxZoom), _options.MaxZoom);

            // Pinned to the left rather than centred horizontally: the root is the anchor of a plan,
            // and a plan that does not fit should run off the right hand edge with its root still on
            // screen, not be cropped at both ends.
            //
            // Vertically the whole plan is centred when it fits.  When it does not - which the floor
            // allows - centring the plan's extent, or pinning it to the top, can leave the view on
            // the empty margin beside a tall branch, with no operator in sight: the root sits level
            // with the middle of everything feeding it, which on a tall plan is a long way from
            // either edge.  So the view is centred on the root itself, which is where a plan is read
            // from, and then kept inside the plan's own extent.
            var height = Layout.Bounds.Height * scale;
            var panY = ((_viewport.Height - height) / 2) - (Layout.Bounds.Y * scale);

            if (height > _viewport.Height)
            {
                panY = Math.Clamp(
                    (_viewport.Height / 2) - (Layout.Root.Bounds.Centre.Y * scale),
                    _viewport.Height - (Layout.Bounds.Bottom * scale),
                    -Layout.Bounds.Y * scale);
            }

            var pan = new LayoutPoint(-Layout.Bounds.X * scale, panY);

            // Fitting hands the view back to us, so resizing follows the window again.
            IsViewUserAdjusted = false;
            return Apply(scale, pan);
        }

        /// <summary>Reset to 100% with the head of the plan at the top left of the view.</summary>
        public bool ResetView()
        {
            IsViewUserAdjusted = true;
            return Apply(1.0, new LayoutPoint(-Layout.Bounds.X, -Layout.Bounds.Y));
        }

        /// <summary>
        /// Set the zoom, keeping whatever is under <paramref name="screenAnchor"/> in place.  That
        /// is what makes wheel zoom feel right: the point under the pointer does not drift.
        /// </summary>
        public bool SetZoom(double zoom, LayoutPoint screenAnchor)
        {
            IsViewUserAdjusted = true;

            var clamped = Math.Clamp(zoom, _options.MinZoom, _options.MaxZoom);

            // Resolve the anchor before the scale changes, then re-derive the pan that puts it back
            // under the same screen position afterwards.
            var anchorInLayout = ToLayout(screenAnchor);
            var pan = new LayoutPoint(
                screenAnchor.X - (anchorInLayout.X * clamped),
                screenAnchor.Y - (anchorInLayout.Y * clamped));

            return Apply(clamped, pan);
        }

        public bool ZoomIn(LayoutPoint screenAnchor) => SetZoom(_zoom * _options.ZoomStep, screenAnchor);

        public bool ZoomOut(LayoutPoint screenAnchor) => SetZoom(_zoom / _options.ZoomStep, screenAnchor);

        public bool PanBy(double dx, double dy)
        {
            if (dx == 0 && dy == 0) return false;

            IsViewUserAdjusted = true;
            _pan = _pan.Offset(dx, dy);
            return Notify();
        }

        /// <summary>Start a drag-to-pan gesture at <paramref name="screenPoint"/>.</summary>
        public void BeginPan(LayoutPoint screenPoint)
        {
            IsPanning = true;
            _panAnchor = screenPoint;
        }

        /// <summary>
        /// Continue a drag-to-pan gesture.  The anchor moves with the pointer so the content tracks
        /// it exactly, rather than accelerating away over a long drag.
        /// </summary>
        public bool PanTo(LayoutPoint screenPoint)
        {
            if (!IsPanning) return false;

            var dx = screenPoint.X - _panAnchor.X;
            var dy = screenPoint.Y - _panAnchor.Y;
            _panAnchor = screenPoint;

            return PanBy(dx, dy);
        }

        public void EndPan() => IsPanning = false;

        /// <summary>
        /// Pan so a node is in the middle of the view, without changing the zoom.  Used when
        /// something outside the picture - a search, a warnings list - points at a node that may be
        /// off screen.
        /// </summary>
        public bool CentreOn(PlanNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return false;

            var centre = node.Bounds.Centre;
            IsViewUserAdjusted = true;

            return Apply(_zoom, new LayoutPoint(
                (_viewport.Width / 2) - (centre.X * _zoom),
                (_viewport.Height / 2) - (centre.Y * _zoom)));
        }

        /// <summary>
        /// Bring a node into view only if it is not already there, so stepping through matches that
        /// happen to be on screen together does not jerk the view on every step.
        /// </summary>
        public bool EnsureVisible(PlanNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            var visible = VisibleBounds;
            return !visible.IntersectsWith(node.Bounds) && CentreOn(node);
        }

        /// <summary>
        /// The current zoom and pan, and whether the user chose them, to be put back later with
        /// <see cref="RestoreView"/>.
        /// </summary>
        public PlanViewState SaveView() => new(_zoom, _pan, IsViewUserAdjusted);

        /// <summary>
        /// Put back a view saved with <see cref="SaveView"/> - including whether it was the user's,
        /// so a view that was still fitted goes on following the window.
        /// </summary>
        public bool RestoreView(PlanViewState view)
        {
            IsViewUserAdjusted = view.IsUserAdjusted;
            return Apply(view.Zoom, view.Pan);
        }

        /// <summary>
        /// Pan by the least amount that puts the whole of a node on screen with a margin, or do
        /// nothing when it is already wholly on screen - the margin is only for where it lands, so
        /// a node that is merely close to the edge does not nudge the view.  Returns true when the
        /// view moved.
        ///
        /// For keeping a node the reader is working with in view as the view around it shrinks - a
        /// panel opening over the edge it sits at.  <see cref="EnsureVisible"/> centres instead,
        /// which suits jumping to a node that was out of sight, but would move the whole plan to
        /// bring a node back that was only just covered.
        /// </summary>
        public bool ScrollIntoView(PlanNode node, double margin = 16)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return false;

            var screen = ToScreen(node.Bounds);

            var dx = Shortfall(screen.Left, screen.Right, _viewport.Width, margin);
            var dy = Shortfall(screen.Top, screen.Bottom, _viewport.Height, margin);

            if (dx == 0 && dy == 0) return false;

            // The view is no longer the fitted one, so a resize must not snap it back.
            IsViewUserAdjusted = true;
            return Apply(_zoom, new LayoutPoint(_pan.X + dx, _pan.Y + dy));
        }

        /// <summary>
        /// How far to move a span that runs outside 0..<paramref name="extent"/> to bring it inside
        /// with the margin to spare.  When it cannot fit, its start wins: the left of a node is its
        /// glyph and its name.
        /// </summary>
        private static double Shortfall(double start, double end, double extent, double margin)
        {
            if (start < 0) return margin - start;
            if (end > extent) return Math.Max(extent - margin - end, margin - start);
            return 0;
        }

        // ---------------------------------------------------------------- picking

        /// <summary>The node at a screen position, or null.</summary>
        public PlanNode? HitTest(LayoutPoint screenPoint) => Layout.HitTest(ToLayout(screenPoint));

        /// <summary>
        /// The collapse control at a screen position, or null.  Checked before
        /// <see cref="HitTest"/> by a host, because the control sits on a node and has to win.
        /// </summary>
        public PlanNode? CollapseToggleAt(LayoutPoint screenPoint) =>
            Layout.CollapseToggleAt(ToLayout(screenPoint));

        /// <summary>
        /// Hide or show a node's inputs.  The layout is rebuilt, so every node object is replaced -
        /// the selection is re-established by id, and falls back to the collapsed node when the
        /// selected operator is now hidden underneath it.
        /// </summary>
        public bool ToggleCollapse(PlanNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            var selected = _selectedNode;
            if (!Layout.ToggleCollapse(node)) return false;

            return AfterRelayout(selected);
        }

        /// <summary>
        /// Show every hidden input.  Returns false when nothing was collapsed.
        ///
        /// Here rather than on the layout alone, for the same reason as
        /// <see cref="ToggleCollapse"/>: expanding rebuilds the layout, which replaces every node
        /// object, and the selection, the search and the hover all point at the old ones until they
        /// are carried across.
        /// </summary>
        public bool ExpandAll()
        {
            var selected = _selectedNode;
            if (!Layout.ExpandAll()) return false;

            return AfterRelayout(selected);
        }

        /// <summary>
        /// Lay the plan out again after the layout options changed - the node widths, say - carrying
        /// the selection, the search and the view across to the new nodes.
        /// </summary>
        public bool Relayout()
        {
            var selected = _selectedNode;
            Layout.Relayout();

            return AfterRelayout(selected);
        }

        /// <summary>
        /// Say what each operator does on its tooltip - see <see cref="PlanViewOptions.ShowOperatorDescriptions"/>.
        /// Changing it redraws a tooltip already showing, so the switch is seen to work.
        /// </summary>
        public bool ShowOperatorDescriptions
        {
            get => _options.ShowOperatorDescriptions;
            set
            {
                if (_options.ShowOperatorDescriptions == value) return;

                _options.ShowOperatorDescriptions = value;
                if (_hoveredTooltip is null) return;

                _hoveredTooltip = BuildHoveredTooltip();
                Notify();
            }
        }

        private PlanTooltip? BuildHoveredTooltip() =>
            _hoveredNode is not null
                ? PlanTooltipBuilder.Build(_hoveredNode, _options.MaxTooltipPredicateLength, Layout.OperatorTimeMode, _options.ShowOperatorDescriptions)
                : _hoveredEdge is not null
                    ? PlanTooltipBuilder.BuildForEdge(_hoveredEdge)
                    : null;

        /// <summary>
        /// Whether the node times are each operator's own or as SQL Server reported them.  The plan
        /// is laid out again, since the times are text on the nodes, and the selection, the search
        /// and the view are carried across to the new nodes.
        /// </summary>
        public OperatorTimeMode OperatorTimeMode
        {
            get => Layout.OperatorTimeMode;
            set
            {
                var selected = _selectedNode;
                if (!Layout.SetOperatorTimeMode(value)) return;

                AfterRelayout(selected);
            }
        }

        /// <summary>
        /// Carry the view's state over to the nodes a re-layout built.  The selection is found again
        /// by operator, landing on the collapsed node when the operator is now hidden under it.
        /// </summary>
        private bool AfterRelayout(PlanNode? selected)
        {
            // Nothing selected stays nothing selected.  The statement root has no operator, so it is
            // found again by id.
            _selectedNode = selected is null
                ? null
                : selected.Operator is null
                    ? Layout.FindNodeById(selected.Id)
                    : Layout.FindVisibleNode(selected.Operator);

            // Hover and search results both point at nodes and arrows that no longer exist.
            _hoveredNode = null;
            _hoveredEdge = null;
            _hoverAnchor = null;
            _hoveredTooltip = null;
            RebuildPath();
            if (_searchText.Length > 0) RunSearch();

            SelectionChanged?.Invoke(this, _selectedNode);
            return Notify();
        }

        /// <summary>
        /// Update what is hovered from a pointer position: a node, or failing that an arrow.  Returns
        /// true only when that actually changed, so the host repaints on entering and leaving
        /// something rather than on every mouse move.
        ///
        /// Nodes win, because arrows end on them.  An arrow's tooltip is placed where the pointer
        /// met it - an arrow can be most of the plan's width long, and a tooltip beside one end of
        /// it could be a screen away from the pointer.
        /// </summary>
        public bool SetHover(LayoutPoint screenPoint)
        {
            if (HitTest(screenPoint) is { } node) return SetHovered(node, null, node.Bounds);

            var edge = EdgeAt(screenPoint);
            if (edge is null) return SetHovered(null, null, null);
            if (ReferenceEquals(edge, _hoveredEdge)) return false;

            var point = ToLayout(screenPoint);
            return SetHovered(null, edge, new LayoutRect(point.X, point.Y, 0, 0));
        }

        public bool ClearHover() => SetHovered(null, null, null);

        /// <summary>
        /// The arrow at a screen position, or null.  Given a few pixels either side whatever the
        /// zoom, so a hairline arrow on a plan zoomed out to fit can still be pointed at.
        /// </summary>
        public PlanEdge? EdgeAt(LayoutPoint screenPoint) =>
            Layout.EdgeAt(ToLayout(screenPoint), EdgeHitTolerance / _zoom);

        /// <summary>Screen pixels either side of an arrow that still count as pointing at it.</summary>
        private const double EdgeHitTolerance = 3;

        /// <summary>Select the node at a screen position; clicking empty space clears the selection.</summary>
        public bool SelectAt(LayoutPoint screenPoint) => Select(HitTest(screenPoint));

        public bool Select(PlanNode? node)
        {
            if (ReferenceEquals(_selectedNode, node)) return false;

            _selectedNode = node;
            RebuildPath();

            SelectionChanged?.Invoke(this, node);
            return Notify();
        }

        /// <summary>
        /// Select the node for an operator, so somewhere else in the viewer can point at the
        /// picture - a warning naming a spill lights up the operator that spilled.  Brings it into
        /// view, and lands on the collapsed ancestor when the operator itself is hidden.
        /// </summary>
        public bool SelectOperator(PlanOperator? node)
        {
            var found = Layout.FindVisibleNode(node);
            if (found is null) return false;

            var changed = Select(found);
            EnsureVisible(found);
            return changed;
        }

        public bool ClearSelection() => Select(null);

        /// <summary>Activate a node - what a double click does.</summary>
        public void Activate(PlanNode? node)
        {
            if (node is null) return;
            NodeActivated?.Invoke(this, node);
        }

        /// <summary>
        /// Move the selection one step through the tree.  With nothing selected, any direction
        /// starts at the root, so the keyboard is usable without reaching for the mouse first.
        /// </summary>
        public bool MoveSelection(PlanNavigation direction)
        {
            if (_selectedNode is null) return Select(Layout.Root);

            var target = direction switch
            {
                PlanNavigation.Consumer => _selectedNode.Parent,
                PlanNavigation.Input => _selectedNode.Children.Count > 0 ? _selectedNode.Children[0] : null,
                PlanNavigation.Up => Neighbour(_selectedNode, -1),
                PlanNavigation.Down => Neighbour(_selectedNode, 1),
                _ => null
            };

            if (target is null) return false;

            var changed = Select(target);
            EnsureVisible(target);
            return changed;
        }

        /// <summary>
        /// The next node up or down within the same column, across subtrees.
        ///
        /// Within the column rather than between siblings: the reader is looking at a picture, and
        /// pressing Down means "the box below this one", not "my parent's next child" - which on a
        /// deep plan is often nowhere near below.
        /// </summary>
        private PlanNode? Neighbour(PlanNode node, int offset)
        {
            var column = Layout.Nodes
                .Where(n => n.Depth == node.Depth)
                .OrderBy(n => n.Bounds.Top)
                .ToList();

            var index = column.IndexOf(node) + offset;
            return index >= 0 && index < column.Count ? column[index] : null;
        }

        // ---------------------------------------------------------------- search

        /// <summary>
        /// Match nodes against free text - an operator name, a table, an index, a column in a
        /// predicate.
        ///
        /// A plan of forty operators is searched, not scanned, and the question is nearly always
        /// "where does this plan touch that table".  Returns the number of matches.
        /// </summary>
        public int Find(string? text)
        {
            var search = text?.Trim() ?? string.Empty;
            if (string.Equals(search, _searchText, StringComparison.Ordinal)) return _matches.Count;

            _searchText = search;
            RunSearch();
            Notify();

            return _matches.Count;
        }

        /// <summary>
        /// Select the next match, wrapping round, and bring it into view.  Returns false when
        /// nothing matched.
        /// </summary>
        public bool NextMatch(int step = 1)
        {
            if (_matches.Count == 0) return false;

            _matchIndex = ((_matchIndex + step) % _matches.Count + _matches.Count) % _matches.Count;

            var node = _matches[_matchIndex];
            Select(node);
            CentreOn(node);
            return true;
        }

        public bool ClearSearch() => Find(string.Empty) == 0;

        private void RunSearch()
        {
            if (_searchText.Length == 0)
            {
                _matches = [];
                _matchIndex = -1;
                return;
            }

            // A number is very likely a node id - the cards, the lists and the properties panel all
            // name operators by one - so it finds that node as well as any text that happens to
            // contain the digits.  Parsed once rather than once per node.
            var nodeId = int.TryParse(_searchText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                ? id
                : (int?)null;

            _matches = Layout.Nodes
                .Where(node => IsMatch(node, _searchText, nodeId))
                .OrderBy(node => node.Depth)
                .ThenBy(node => node.Bounds.Top)
                .ToList();

            _matchIndex = -1;
        }

        private static bool IsMatch(PlanNode node, string text, int? nodeId)
        {
            if (nodeId is { } id && node.Operator?.NodeId == id) return true;

            if (Contains(node.Title, text) || Contains(node.Subtitle, text)) return true;

            // The statement root is matched on its caption alone, deliberately not on the statement
            // text.  The text contains the whole query, so matching it would light the root up for
            // every table, column and keyword anyone ever searched for - which is a match on every
            // search, and a result that appears every time is one nobody reads.
            if (node.Operator is not { } op) return false;

            return Contains(op.PhysicalOp, text) ||
                   Contains(op.LogicalOp, text) ||
                   Contains(op.Predicate, text) ||
                   Contains(op.SeekPredicate, text) ||
                   op.Objects.Any(o => Contains(o.ToString(), text)) ||
                   op.OutputList.Any(c => Contains(c.ToString(), text));
        }

        private static bool Contains(string? value, string text) =>
            value is not null && value.Contains(text, StringComparison.OrdinalIgnoreCase);

        // ---------------------------------------------------------------- internals

        private void RebuildPath()
        {
            // Built once per selection change rather than on every frame the plan is drawn, the same
            // bargain the hover tooltip makes.
            if (_selectedNode is null)
            {
                _pathToRoot = [];
                return;
            }

            var path = new HashSet<PlanNode>();
            for (var node = _selectedNode; node is not null; node = node.Parent) path.Add(node);

            _pathToRoot = path;
        }

        private bool SetHovered(PlanNode? node, PlanEdge? edge, LayoutRect? anchor)
        {
            if (ReferenceEquals(_hoveredNode, node) && ReferenceEquals(_hoveredEdge, edge)) return false;

            _hoveredNode = node;
            _hoveredEdge = edge;
            _hoverAnchor = anchor;

            // Built once per hover change rather than on every frame the tooltip is drawn.
            _hoveredTooltip = BuildHoveredTooltip();

            return Notify();
        }

        /// <summary>
        /// Forget a hovered arrow once the arrows have been routed again - it is no longer one of
        /// them, and its tooltip describes a width that is no longer drawn.
        /// </summary>
        private void ForgetHoveredEdge()
        {
            if (_hoveredEdge is null) return;

            _hoveredEdge = null;
            _hoverAnchor = null;
            _hoveredTooltip = null;
        }

        private bool Apply(double zoom, LayoutPoint pan)
        {
            if (_zoom == zoom && _pan == pan) return false;

            _zoom = zoom;
            _pan = pan;
            return Notify();
        }

        private bool Notify()
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
