using System;
using DBADash.Deadlock.Layout;

namespace DBADash.Deadlock.Interaction
{
    /// <summary>
    /// All the mutable state of a deadlock view: zoom, pan, what is selected and what is hovered,
    /// plus the transforms between layout space and screen space.
    ///
    /// This is deliberately not a control.  It holds no framework types and raises no framework
    /// events, so the host is a thin shim that forwards mouse and keyboard input and repaints when
    /// <see cref="Changed"/> fires - and the whole of the interaction behaviour is unit testable,
    /// including the parts that are awkward to verify by hand, such as zooming about the pointer.
    ///
    /// Both spaces use <see cref="LayoutPoint"/>.  Parameter names say which space is expected;
    /// mixing them is the classic source of zoom and pan bugs, so nothing here takes an ambiguous
    /// point.
    /// </summary>
    public sealed class DeadlockViewController
    {
        private readonly DeadlockViewOptions _options;

        private LayoutSize _viewport;
        private double _zoom = 1.0;
        private LayoutPoint _pan;
        private DeadlockNode? _selectedNode;
        private DeadlockHighlightMap _highlight = DeadlockHighlightMap.Empty;
        private DeadlockNode? _hoveredNode;
        private DeadlockTooltip? _hoveredTooltip;
        private LayoutPoint _panAnchor;
        private DeadlockNode? _draggedNode;
        private LayoutPoint _dragAnchor;
        private bool _dragMoved;

        public DeadlockViewController(DeadlockLayout layout, DeadlockViewOptions? options = null)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _options = options ?? new DeadlockViewOptions();
        }

        /// <summary>Raised whenever something that affects what is drawn has changed.</summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Raised when the statement preview link of a node is activated with a click.  Subscribing
        /// is what turns the preview from plain text into a clickable link (unless
        /// <see cref="DeadlockViewOptions.ShowStatementLinkAlways"/> already forces it), so a host
        /// that offers no way to open the statement shows no misleading link.
        /// </summary>
        public event EventHandler<DeadlockStatementActivatedEventArgs>? StatementActivated;

        public DeadlockLayout Layout { get; }

        /// <summary>The size of the drawing surface, in screen units.</summary>
        public LayoutSize Viewport => _viewport;

        /// <summary>Screen units per layout unit.</summary>
        public double Zoom => _zoom;

        /// <summary>Screen space offset applied after scaling.</summary>
        public LayoutPoint Pan => _pan;

        public DeadlockNode? SelectedNode => _selectedNode;

        /// <summary>
        /// What the selected node holds and what it is waiting for, rebuilt whenever the selection
        /// changes.  <see cref="DeadlockHighlightMap.Empty"/> while nothing is selected, so a caller
        /// never has to null check it.
        /// </summary>
        public DeadlockHighlightMap Highlight => _highlight;

        public DeadlockNode? HoveredNode => _hoveredNode;

        /// <summary>Tooltip for <see cref="HoveredNode"/>, or null when nothing is hovered.</summary>
        public DeadlockTooltip? HoveredTooltip => _hoveredTooltip;

        /// <summary>True between <see cref="BeginPan"/> and <see cref="EndPan"/>.</summary>
        public bool IsPanning { get; private set; }

        /// <summary>The node being dragged, or null.</summary>
        public DeadlockNode? DraggedNode => _draggedNode;

        /// <summary>True between <see cref="BeginNodeDrag"/> and <see cref="EndNodeDrag"/>.</summary>
        public bool IsDraggingNode => _draggedNode is not null;

        /// <summary>
        /// True once the user has zoomed or panned, so automatic re-fitting on resize stops
        /// overriding what they chose.  <see cref="ZoomToFit"/> clears it, which is what makes the
        /// Fit button also mean "start following the window again".
        /// </summary>
        public bool IsViewUserAdjusted { get; private set; }

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

        // ---------------------------------------------------------------- view

        public bool SetViewport(LayoutSize viewport)
        {
            if (_viewport == viewport) return false;

            _viewport = viewport;

            // Keep the whole graph in view as the window is resized - but only while the view is
            // still the one we chose.  Once the user has zoomed or panned, re-fitting under them
            // would throw away what they were looking at.
            if (_options.RefitOnViewportChange && !IsViewUserAdjusted && ZoomToFit()) return true;

            return Notify();
        }

        /// <summary>
        /// Scale and centre the graph so it all fits the viewport.  Does nothing useful before a
        /// viewport is set, or for an empty layout.
        /// </summary>
        public bool ZoomToFit()
        {
            if (_viewport.Width <= 0 || _viewport.Height <= 0) return false;
            if (Layout.Bounds.Width <= 0 || Layout.Bounds.Height <= 0) return false;

            var scale = Math.Min(
                _viewport.Width / Layout.Bounds.Width,
                _viewport.Height / Layout.Bounds.Height);

            scale = Math.Min(scale, _options.MaxFitZoom);
            scale = Math.Clamp(scale, _options.MinZoom, _options.MaxZoom);

            // Centre the leftover space, then pull the layout's own origin back to it: bounds start at
            // zero as laid out, but a node dragged up or left moves them negative, and ignoring that
            // would push the graph off the corner of the window by however far it was dragged.
            var pan = new LayoutPoint(
                ((_viewport.Width - (Layout.Bounds.Width * scale)) / 2) - (Layout.Bounds.X * scale),
                ((_viewport.Height - (Layout.Bounds.Height * scale)) / 2) - (Layout.Bounds.Y * scale));

            // Fitting hands the view back to us, so resizing follows the window again.
            IsViewUserAdjusted = false;
            return Apply(scale, pan);
        }

        /// <summary>
        /// Reset to 100% with the top left of the graph at the top left of the view - which is the
        /// layout's own origin, not zero, once a node has been dragged up or left.
        /// </summary>
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

        // ---------------------------------------------------------------- moving nodes

        /// <summary>
        /// Start dragging the node at <paramref name="screenPoint"/>.  Returns false when there is no
        /// node there, which is the host's cue to pan the view instead.
        ///
        /// A laid out graph is one mechanical arrangement, and on a busy deadlock no single
        /// arrangement answers every question: being able to pull one box clear of a pile is often
        /// the difference between a picture that can be read and one that cannot.
        /// </summary>
        public bool BeginNodeDrag(LayoutPoint screenPoint)
        {
            var node = HitTest(screenPoint);
            if (node is null) return false;

            // Picked up means on top: a box dropped over another has to be the one drawn over it and
            // the one the next press finds.
            Layout.BringToFront(node);

            _draggedNode = node;
            _dragAnchor = ToLayout(screenPoint);
            _dragMoved = false;
            return true;
        }

        /// <summary>
        /// Continue a node drag.  The anchor moves with the pointer, so the node tracks it exactly
        /// however long the drag runs, the same way <see cref="PanTo"/> works.
        /// </summary>
        public bool DragNodeTo(LayoutPoint screenPoint)
        {
            if (_draggedNode is null) return false;

            var point = ToLayout(screenPoint);
            var dx = point.X - _dragAnchor.X;
            var dy = point.Y - _dragAnchor.Y;
            if (dx == 0 && dy == 0) return false;

            _dragAnchor = point;

            // The layout re-routes the edges that meet the node and re-measures its own extent.
            Layout.MoveNode(_draggedNode, dx, dy);
            _dragMoved = true;

            return Notify();
        }

        /// <summary>
        /// End a node drag.  Returns true when the node actually moved, so a host can tell a drag from
        /// a click that happened to land on a node - which is what stops a press on a statement link
        /// opening it after the reader has dragged the box somewhere else.
        /// </summary>
        public bool EndNodeDrag()
        {
            var moved = _dragMoved;

            _draggedNode = null;
            _dragMoved = false;

            return moved;
        }

        // ---------------------------------------------------------------- picking

        /// <summary>The node at a screen position, or null.</summary>
        public DeadlockNode? HitTest(LayoutPoint screenPoint) => Layout.HitTest(ToLayout(screenPoint));

        /// <summary>
        /// The node whose statement preview link is under a screen position, or null.  Lets the host
        /// treat the preview as a hyperlink - a single click on the text opens the full statement -
        /// rather than needing the whole node to be activated.
        /// </summary>
        public DeadlockNode? StatementLinkAt(LayoutPoint screenPoint)
        {
            var point = ToLayout(screenPoint);
            foreach (var node in Layout.Nodes)
            {
                if (node.StatementLinkBounds is { } bounds && bounds.Contains(point)) return node;
            }

            return null;
        }

        /// <summary>
        /// True when statement previews should be drawn as clickable links: either a consumer is
        /// listening on <see cref="StatementActivated"/>, or the option forces it.  The renderer
        /// reads this so a preview only looks clickable when it actually is.
        /// </summary>
        public bool StatementLinksEnabled => StatementActivated is not null || _options.ShowStatementLinkAlways;

        /// <summary>
        /// Activate the statement preview link under a screen position, raising
        /// <see cref="StatementActivated"/> with the full statement.  Returns true when a link was
        /// there and had a statement to raise, so the host can suppress selection or panning.
        /// </summary>
        public bool ActivateStatementAt(LayoutPoint screenPoint) =>
            StatementLinkAt(screenPoint) is { } node && ActivateStatement(node);

        /// <summary>
        /// Raise <see cref="StatementActivated"/> for a node directly, with no pointer position
        /// involved.  A context menu acts on the node it was opened over rather than on whatever
        /// happens to be under the pointer when the item is chosen, so it needs this form.  Returns
        /// false when nothing is listening or the node has no statement.
        /// </summary>
        public bool ActivateStatement(DeadlockNode? node)
        {
            if (StatementActivated is null || node is null) return false;

            var statement = node is DeadlockProcessNode process ? process.Process.PrimaryStatement : null;
            if (string.IsNullOrEmpty(statement)) return false;

            StatementActivated.Invoke(this, new DeadlockStatementActivatedEventArgs(node, statement));
            return true;
        }

        /// <summary>
        /// Update the hovered node from a pointer position.  Returns true only when the hovered node
        /// actually changed, so the host repaints on entering and leaving a node rather than on
        /// every mouse move.
        /// </summary>
        public bool SetHover(LayoutPoint screenPoint) => SetHoveredNode(HitTest(screenPoint));

        public bool ClearHover() => SetHoveredNode(null);

        /// <summary>Select the node at a screen position; clicking empty space clears the selection.</summary>
        public bool SelectAt(LayoutPoint screenPoint) => Select(HitTest(screenPoint));

        public bool Select(DeadlockNode? node)
        {
            if (ReferenceEquals(_selectedNode, node)) return false;

            _selectedNode = node;

            // Built once per selection change rather than on every frame the graph is drawn, the same
            // bargain the hover tooltip makes.
            _highlight = DeadlockHighlightMap.For(Layout, node);

            return Notify();
        }

        public bool ClearSelection() => Select(null);

        // ---------------------------------------------------------------- internals

        private bool SetHoveredNode(DeadlockNode? node)
        {
            if (ReferenceEquals(_hoveredNode, node)) return false;

            _hoveredNode = node;

            // Built once per hover change rather than on every frame the tooltip is drawn.
            _hoveredTooltip = node is null
                ? null
                : DeadlockTooltipBuilder.Build(node, _options.MaxTooltipStatementLength);

            return Notify();
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
