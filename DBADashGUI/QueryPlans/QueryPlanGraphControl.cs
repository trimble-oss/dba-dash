using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using DBADash.QueryPlan.Skia;
using DBADashGUI.Theme;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// Hosts the query plan renderer in WinForms.
    ///
    /// This is the only part of the plan viewer tied to a windowing framework: it forwards mouse and
    /// keyboard input into <see cref="PlanViewController"/> and repaints when the controller says
    /// something changed.  Everything else - parsing, layout, view state, drawing - lives in
    /// framework independent projects, so porting the viewer means rewriting this file and nothing
    /// else.
    /// </summary>
    public sealed class QueryPlanGraphControl : UserControl, IThemedControl
    {
        private readonly SKControl _canvas;
        private readonly PlanFonts _fonts;
        private readonly PlanRenderer _renderer;
        private readonly PlanLayoutEngine _engine;

        /// <summary>
        /// The engine's options, held so the operator time choice can be set before each statement
        /// is laid out - which lays it out once in the right mode rather than twice.
        /// </summary>
        private readonly PlanLayoutOptions _layoutOptions = new();

        /// <summary>
        /// The view's own settings, kept rather than made per statement so what a host reads from them
        /// - the zoom range - is the same as what the controller is using.
        /// </summary>
        private readonly PlanViewOptions _viewOptions = new();

        private PlanViewController _controller;

        // Rebuilt for every right click, because what a node offers depends on the node.  Held so it
        // can be disposed - with the items it owns - when the next one replaces it.
        private ContextMenuStrip _contextMenu;

        public QueryPlanGraphControl()
        {
            _fonts = new PlanFonts();
            _renderer = new PlanRenderer(_fonts, QueryPlanPaletteMapper.FromTheme(ThemeExtensions.CurrentTheme));

            // The layout is measured with the same fonts the renderer draws with, so text lands
            // inside the boxes sized for it.
            _engine = new PlanLayoutEngine(new SkiaPlanTextMeasurer(_fonts), _layoutOptions);

            _canvas = new SKControl { Dock = DockStyle.Fill, TabStop = true };
            _canvas.PaintSurface += Canvas_PaintSurface;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseLeave += Canvas_MouseLeave;
            _canvas.MouseWheel += Canvas_MouseWheel;
            _canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            _canvas.KeyDown += Canvas_KeyDown;
            _canvas.PreviewKeyDown += Canvas_PreviewKeyDown;
            _canvas.Resize += Canvas_Resize;

            // Focus on enter so the wheel and keyboard work without clicking first - but only while
            // this window is the active one.  Focusing a control in an inactive window activates it,
            // so a window opened from the plan, such as a missing index's T-SQL, was pulled behind
            // the plan the moment the pointer crossed it.
            _canvas.MouseEnter += (_, _) =>
            {
                if (ReferenceEquals(Form.ActiveForm, FindForm())) _canvas.Focus();
            };

            Controls.Add(_canvas);
        }

        /// <summary>Raised when the selected node changes, so the host can follow it elsewhere.</summary>
        public event EventHandler<PlanNode> SelectionChanged;

        /// <summary>Raised when a node is double clicked, for the host to drill into.</summary>
        public event EventHandler<PlanNode> NodeActivated;

        /// <summary>
        /// Raised while a right click menu is being built, after the control has added the actions it
        /// can answer itself, so the host can append the ones that need the rest of the application.
        /// </summary>
        public event EventHandler<QueryPlanMenuEventArgs> ContextMenuBuilding;

        public PlanStatement Statement { get; private set; }

        /// <summary>
        /// The laid out plan, available after <see cref="LoadStatement"/>.  Exposed because the
        /// synthetic root and the metric maxima are layout results rather than things the parsed
        /// plan knows about, and the host uses both.
        /// </summary>
        public PlanLayout PlanLayout { get; private set; }

        public PlanNode SelectedNode => _controller?.SelectedNode;

        /// <summary>Which metric the node bars measure.  See <see cref="PlanHeatMetric"/>.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public PlanHeatMetric HeatMetric
        {
            get => _controller?.HeatMetric ?? PlanHeatMetric.OperatorCost;
            set
            {
                if (_controller is not null) _controller.HeatMetric = value;
            }
        }

        private PlanEdgeWidthMetric _edgeWidthMetric = PlanEdgeWidthMetric.Rows;

        /// <summary>
        /// What arrow thickness measures - rows or data size.  Held here as well as on the
        /// controller so the choice survives moving to another statement, which builds a new one.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public PlanEdgeWidthMetric EdgeWidthMetric
        {
            get => _edgeWidthMetric;
            set
            {
                _edgeWidthMetric = value;
                if (_controller is not null) _controller.EdgeWidthMetric = value;
            }
        }

        /// <summary>
        /// Whether arrows are drawn by actual or estimated rows.  Held in the layout options so the
        /// choice survives moving to another statement - and survives an estimated plan, which is
        /// drawn by its estimates whatever is chosen.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public PlanEdgeWidthBasis EdgeWidthBasis
        {
            get => _layoutOptions.EdgeWidthBasis;
            set
            {
                _layoutOptions.EdgeWidthBasis = value;
                if (_controller is not null) _controller.EdgeWidthBasis = value;
            }
        }

        /// <summary>What the arrows of the statement shown are actually drawn by.</summary>
        public PlanEdgeWidthBasis EffectiveEdgeWidthBasis =>
            PlanLayout?.EffectiveEdgeWidthBasis ?? PlanEdgeWidthBasis.Estimated;

        /// <summary>True when the statement shown measured rows, so actual arrows are possible.</summary>
        public bool HasActualRows => PlanLayout?.Metrics.HasRuntime == true;

        /// <summary>
        /// Whether node times are each operator's own or as SQL Server reported them.  Held in the
        /// layout options as well as on the controller so the choice survives moving to another
        /// statement.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public OperatorTimeMode OperatorTimeMode
        {
            get => _layoutOptions.OperatorTimeMode;
            set
            {
                _layoutOptions.OperatorTimeMode = value;
                if (_controller is not null) _controller.OperatorTimeMode = value;
            }
        }

        private PlanNodeWidth _nodeWidth = PlanNodeWidth.Normal;

        /// <summary>
        /// How wide operator nodes can be.  Held in the layout options so the choice survives moving
        /// to another statement; the statement shown is laid out again, keeping its selection.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public PlanNodeWidth NodeWidth
        {
            get => _nodeWidth;
            set
            {
                if (_nodeWidth == value) return;

                _nodeWidth = value;
                _layoutOptions.SetNodeWidth(value);
                _controller?.Relayout();
            }
        }

        private PlanColumnSpacing _columnSpacing = PlanColumnSpacing.Normal;

        /// <summary>
        /// How much room is left between columns of nodes.  Like <see cref="NodeWidth"/>, held in the
        /// layout options and applied by laying the statement shown out again.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public PlanColumnSpacing ColumnSpacing
        {
            get => _columnSpacing;
            set
            {
                if (_columnSpacing == value) return;

                _columnSpacing = value;
                _layoutOptions.SetColumnSpacing(value);
                _controller?.Relayout();
            }
        }

        /// <summary>
        /// Wrap object names too long for the widest node rather than cutting them short.  Applied by
        /// laying the statement shown out again, like <see cref="NodeWidth"/>.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool WrapObjectNames
        {
            get => _layoutOptions.WrapObjectNames;
            set
            {
                if (_layoutOptions.WrapObjectNames == value) return;

                _layoutOptions.WrapObjectNames = value;
                _controller?.Relayout();
            }
        }

        /// <summary>
        /// Give every node in a column the width of the widest, so the arrows between two columns are
        /// all the same length.  Off sizes each node to its own content.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool UniformColumnWidths
        {
            get => _layoutOptions.UniformColumnWidths;
            set
            {
                if (_layoutOptions.UniformColumnWidths == value) return;

                _layoutOptions.UniformColumnWidths = value;
                _controller?.Relayout();
            }
        }

        /// <summary>
        /// Say what each operator does on its tooltip.  Kept here as well as on the controller so the
        /// choice survives moving to another statement.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowOperatorDescriptions
        {
            get => _showOperatorDescriptions;
            set
            {
                _showOperatorDescriptions = value;
                if (_controller is not null) _controller.ShowOperatorDescriptions = value;
            }
        }

        private bool _showOperatorDescriptions = true;

        /// <summary>
        /// Fade everything off the selected node's path back to the root, so one path through a wide
        /// plan can be followed.  Off by default - a plan is read whole far more often than one path
        /// through it is.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool FollowDataPath
        {
            get => _renderer.FadeOffPath;
            set
            {
                if (_renderer.FadeOffPath == value) return;

                _renderer.FadeOffPath = value;
                _canvas.Invalidate();
            }
        }

        /// <summary>Whether this plan can be ranked by <paramref name="metric"/> at all.</summary>
        public bool Supports(PlanHeatMetric metric) => PlanLayout?.Metrics.Supports(metric) == true;

        /// <summary>Lay the statement out and show it.</summary>
        public void LoadStatement(PlanStatement statement)
        {
            Statement = statement ?? throw new ArgumentNullException(nameof(statement));

            // The metric choice survives moving between statements of one plan, which is the point
            // of choosing it - but not to a statement that cannot support it, which the controller
            // handles by falling back.
            var metric = _controller?.HeatMetric ?? PlanHeatMetric.OperatorCost;

            PlanLayout = _engine.Layout(statement);
            PlanLayout.SetEdgeWidthMetric(_edgeWidthMetric);

            // A scroll made for the last statement's view means nothing to this one.
            _uncoverScroll = null;
            _viewOptions.HeatMetric = metric;
            _viewOptions.ShowOperatorDescriptions = _showOperatorDescriptions;

            _controller = new PlanViewController(PlanLayout, _viewOptions);
            _controller.Changed += (_, _) =>
            {
                _canvas.Invalidate();
                ViewChanged?.Invoke(this, EventArgs.Empty);
            };
            _controller.SelectionChanged += (_, node) => SelectionChanged?.Invoke(this, node);
            _controller.NodeActivated += (_, node) => NodeActivated?.Invoke(this, node);

            // A freshly loaded plan has a view the user has not touched, so setting the viewport
            // fits it as a side effect.  If the control has no size yet, the first resize does it.
            if (_canvas.ClientSize.Width > 0 && _canvas.ClientSize.Height > 0)
            {
                _controller.SetViewport(new LayoutSize(_canvas.ClientSize.Width, _canvas.ClientSize.Height));
            }

            _canvas.Invalidate();
        }

        /// <summary>
        /// Selects the node for an operator, so somewhere else in the viewer can point at the
        /// picture - a spill warning lights up the operator that spilled.
        /// </summary>
        public void SelectOperator(PlanOperator node) => _controller?.SelectOperator(node);

        public void ZoomToFit() => _controller?.ZoomToFit();

        public void ZoomIn() => _controller?.ZoomIn(CentreOfView());

        public void ZoomOut() => _controller?.ZoomOut(CentreOfView());

        /// <summary>
        /// Raised whenever the view changes - zoomed, panned, selected - so a host showing the zoom
        /// can follow the mouse wheel and the keyboard as well as its own control.
        /// </summary>
        public event EventHandler ViewChanged;

        /// <summary>How far the plan is zoomed in, 1 being actual size.</summary>
        public double Zoom => _controller?.Zoom ?? 1;

        /// <summary>The range <see cref="SetZoom"/> accepts, which is what a zoom control spans.</summary>
        public (double Min, double Max) ZoomRange => (_viewOptions.MinZoom, _viewOptions.MaxZoom);

        /// <summary>
        /// How far out a plan is allowed to be zoomed when it is opened, or the window resized, so a
        /// big plan is shown at a size worth reading rather than fitted at any cost.  The Fit button
        /// always fits the whole plan.  Applied to the statement shown when it changes, unless the
        /// view is one the user set themselves.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double MinAutoFitZoom
        {
            get => _viewOptions.MinAutoFitZoom;
            set
            {
                if (Math.Abs(_viewOptions.MinAutoFitZoom - value) < 0.0001) return;

                _viewOptions.MinAutoFitZoom = value;
                _controller?.AutoFit();
            }
        }

        /// <summary>Zoom about the middle of the view, as the zoom buttons do.</summary>
        public void SetZoom(double zoom) => _controller?.SetZoom(zoom, CentreOfView());

        public void ResetView() => _controller?.ResetView();

        /// <summary>
        /// Show every hidden input.  Returns false when nothing was collapsed.
        ///
        /// Through the controller rather than the layout, which is what carries the selection and the
        /// search over to the nodes the rebuild creates - and repaints, since the view changed.
        /// </summary>
        public bool ExpandAll() => _controller?.ExpandAll() == true;

        public bool HasCollapsedNodes => PlanLayout?.HasCollapsedNodes == true;

        /// <summary>Match nodes against free text, returning the number found.</summary>
        public int Find(string text) => _controller?.Find(text) ?? 0;

        public bool NextMatch(int step = 1) => _controller?.NextMatch(step) == true;

        /// <summary>
        /// The current view as a bitmap, for copying to the clipboard or saving.  Rendered through
        /// the same renderer, so what is copied is what is on screen.
        /// </summary>
        public Bitmap RenderToBitmap()
        {
            if (_controller is null) return null;

            var width = Math.Max(1, _canvas.ClientSize.Width);
            var height = Math.Max(1, _canvas.ClientSize.Height);

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            _renderer.Render(surface.Canvas, _controller);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            // Copy out of the stream: Bitmap keeps the stream alive otherwise, and this one is going.
            using var decoded = new Bitmap(stream);
            return new Bitmap(decoded);
        }

        /// <summary>
        /// The whole plan as a bitmap at 100%, whatever is currently on screen.
        ///
        /// What anyone pasting a plan into a ticket actually wants is the plan, not the part of it
        /// that happened to fit the window - and a wide plan never fits.  Capped so a plan of two
        /// hundred operators cannot ask for a bitmap the machine will not allocate.
        /// </summary>
        public Bitmap RenderWholePlanToBitmap(int maxDimension = 8000)
        {
            if (_controller is null) return null;

            var bounds = PlanLayout.Bounds;
            var scale = Math.Min(1.0, maxDimension / Math.Max(bounds.Width, bounds.Height));

            var width = Math.Max(1, (int)Math.Ceiling(bounds.Width * scale));
            var height = Math.Max(1, (int)Math.Ceiling(bounds.Height * scale));

            // A throwaway controller so the view on screen is not disturbed by the capture.
            var capture = new PlanViewController(PlanLayout, new PlanViewOptions { MaxFitZoom = 1.0 });
            capture.SetViewport(new LayoutSize(width, height));
            capture.ZoomToFit();

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            _renderer.Render(surface.Canvas, capture);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            using var decoded = new Bitmap(stream);
            return new Bitmap(decoded);
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            BackColor = theme.BackgroundColor;

            // Only the palette changes with the theme - the fonts and therefore the layout do not,
            // so there is nothing to re-measure.
            _renderer.Palette = QueryPlanPaletteMapper.FromTheme(theme);
            _canvas.Invalidate();
        }

        // ---------------------------------------------------------------- painting

        private void Canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (_controller is null)
            {
                e.Surface.Canvas.Clear(BackColor.ToSKColor());
                return;
            }

            _renderer.Render(e.Surface.Canvas, _controller);
        }

        private void Canvas_Resize(object sender, EventArgs e) => ApplyViewport(refit: !_keepViewOnResize);

        private void ApplyViewport(bool refit)
        {
            if (_controller is null) return;
            if (_canvas.ClientSize.Width <= 0 || _canvas.ClientSize.Height <= 0) return;

            // The controller re-fits as the viewport changes while the view is still the one it
            // chose, so resizing the window keeps the whole plan visible - but stops doing so once
            // the user has zoomed or panned, rather than yanking the view out from under them.
            _controller.SetViewport(new LayoutSize(_canvas.ClientSize.Width, _canvas.ClientSize.Height), refit);
        }

        /// <summary>Set while <see cref="ResizeKeepingView"/> is running.</summary>
        private bool _keepViewOnResize;

        /// <summary>
        /// The view before and after <see cref="ResizeKeepingView"/> scrolled to uncover the
        /// selection, so the scroll can be undone when the space comes back.  Null when it did not
        /// scroll.
        /// </summary>
        private (PlanViewState Before, PlanViewState After)? _uncoverScroll;

        /// <summary>
        /// Run <paramref name="resize"/> - a panel beside the plan opening or closing - without the
        /// plan re-fitting to its new size, then bring the selection back into view if the panel
        /// covered it.
        ///
        /// A re-fit would rescale the whole plan every time a click opened the panel, moving the
        /// operator just clicked out from under the pointer.  Keeping the view only covers or
        /// uncovers the edge of the plan.
        ///
        /// When the space comes back - the panel closing - a scroll made to uncover the selection is
        /// undone, as long as the reader has not moved the view since, and a view that is still the
        /// fitted one is fitted to the wider space.  Otherwise closing the panel would leave the
        /// plan scrolled off its left edge, or shrunk, with empty space on the right.  Growing moves
        /// nothing the reader is pointing at: they have just clicked away from the plan.
        /// </summary>
        public void ResizeKeepingView(Action resize)
        {
            ArgumentNullException.ThrowIfNull(resize);

            var widthBefore = _controller?.Viewport.Width ?? 0;

            _keepViewOnResize = true;
            try
            {
                resize();
            }
            finally
            {
                _keepViewOnResize = false;
            }

            if (_controller is null) return;

            // Picks up the new size if the resize has not reached the canvas yet; a no-op if it has.
            ApplyViewport(refit: false);

            var scroll = _uncoverScroll;
            _uncoverScroll = null;

            if (_controller.Viewport.Width > widthBefore)
            {
                if (scroll is { } undo && _controller.SaveView() == undo.After) _controller.RestoreView(undo.Before);
                _controller.AutoFit();
                return;
            }

            if (_controller.SelectedNode is not { } selected) return;

            var before = _controller.SaveView();
            if (_controller.ScrollIntoView(selected)) _uncoverScroll = (before, _controller.SaveView());
        }

        // ---------------------------------------------------------------- input

        private static LayoutPoint ToLayoutPoint(MouseEventArgs e) => new(e.X, e.Y);

        private LayoutPoint CentreOfView() => new(_canvas.ClientSize.Width / 2.0, _canvas.ClientSize.Height / 2.0);

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;
            _canvas.Focus();

            if (e.Button == MouseButtons.Left)
            {
                // The collapse control sits on a node, so it has to win the click.
                if (_controller.CollapseToggleAt(ToLayoutPoint(e)) is { } toggle)
                {
                    _controller.ToggleCollapse(toggle);
                    return;
                }

                _controller.SelectAt(ToLayoutPoint(e));
            }

            // Left on empty space or middle anywhere pans.  Nodes are not draggable: the tree is
            // the meaning of the picture, so there is nowhere better for a node to be.
            if (e.Button is MouseButtons.Left or MouseButtons.Middle)
            {
                _controller.BeginPan(ToLayoutPoint(e));
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;

            if (_controller.IsPanning)
            {
                _controller.PanTo(ToLayoutPoint(e));
                _canvas.Cursor = Cursors.SizeAll;
                return;
            }

            _controller.SetHover(ToLayoutPoint(e));
            _canvas.Cursor = CursorAt(ToLayoutPoint(e));
        }

        /// <summary>What the pointer says it will do here.</summary>
        private Cursor CursorAt(LayoutPoint point)
        {
            if (_controller.CollapseToggleAt(point) is not null) return Cursors.Hand;
            return _controller.HitTest(point) is null ? Cursors.Default : Cursors.Hand;
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;

            _controller.EndPan();
            _canvas.Cursor = CursorAt(ToLayoutPoint(e));

            // On mouse-up rather than mouse-down, so a right-drag that started elsewhere does not
            // end in a menu, and so the menu appears where the button was released.
            if (e.Button == MouseButtons.Right) ShowContextMenu(e.Location);
        }

        private void Canvas_MouseLeave(object sender, EventArgs e)
        {
            _controller?.EndPan();
            _controller?.ClearHover();
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;

            // Zoom about the pointer so whatever is under it stays put.
            var anchor = ToLayoutPoint(e);
            if (e.Delta > 0)
            {
                _controller.ZoomIn(anchor);
            }
            else if (e.Delta < 0)
            {
                _controller.ZoomOut(anchor);
            }
        }

        private void Canvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;

            var node = _controller.HitTest(ToLayoutPoint(e));
            if (node is not null) _controller.Activate(node);
        }

        /// <summary>
        /// Claim the arrow keys for the graph.
        ///
        /// WinForms treats them as dialog navigation keys by default - moving focus to the next
        /// control - and never raises KeyDown for them at all, so without this the tree walk in
        /// <see cref="Canvas_KeyDown"/> silently does nothing.
        /// </summary>
        private static void Canvas_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down) e.IsInputKey = true;
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (_controller is null) return;

            switch (e.KeyCode)
            {
                case Keys.Add or Keys.Oemplus:
                    _controller.ZoomIn(CentreOfView());
                    break;

                case Keys.Subtract or Keys.OemMinus:
                    _controller.ZoomOut(CentreOfView());
                    break;

                case Keys.D0 or Keys.NumPad0:
                    _controller.ZoomToFit();
                    break;

                // The arrow keys walk the tree rather than scrolling the view.  A plan is a tree and
                // stepping through it operator by operator is how it is read; panning is what the
                // mouse is for, and is still on the middle button and the scrollwheel.
                case Keys.Left:
                    _controller.MoveSelection(PlanNavigation.Consumer);
                    break;

                case Keys.Right:
                    _controller.MoveSelection(PlanNavigation.Input);
                    break;

                case Keys.Up:
                    _controller.MoveSelection(PlanNavigation.Up);
                    break;

                case Keys.Down:
                    _controller.MoveSelection(PlanNavigation.Down);
                    break;

                case Keys.Space:
                    if (_controller.SelectedNode is { } selected) _controller.ToggleCollapse(selected);
                    break;

                case Keys.F3:
                    _controller.NextMatch(e.Shift ? -1 : 1);
                    break;

                case Keys.Escape:
                    _controller.ClearSelection();
                    break;

                default:
                    return;
            }

            e.Handled = true;
        }

        // ---------------------------------------------------------------- context menu

        /// <summary>
        /// Build and show the right click menu for whatever is under the pointer.  Right clicking a
        /// node selects it first: the menu names the node it acts on only implicitly, so the
        /// selection is what makes it unambiguous.
        /// </summary>
        private void ShowContextMenu(Point location)
        {
            if (_controller is null) return;

            var node = _controller.HitTest(new LayoutPoint(location.X, location.Y));
            if (node is not null) _controller.Select(node);

            var menu = BuildContextMenu(node);
            if (menu.Items.Count == 0)
            {
                menu.Dispose();
                return;
            }

            // The previous menu is closed by now, so this is the safe point to dispose it - doing so
            // from its own Closed event races the click that closed it.
            _contextMenu?.Dispose();
            _contextMenu = menu;
            _contextMenu.Show(_canvas, location);
        }

        private ContextMenuStrip BuildContextMenu(PlanNode node)
        {
            var menu = new ContextMenuStrip();

            if (node?.Operator is { } op)
            {
                if (op.PrimaryObject is { } target)
                {
                    menu.Items.Add(new ToolStripMenuItem("Copy Object Name", Properties.Resources.ASX_Copy_blue_16x,
                        (_, _) => CopyText(target.ToString())));
                }

                if (op.Predicate is not null || op.SeekPredicate is not null)
                {
                    menu.Items.Add(new ToolStripMenuItem("Copy Predicate", Properties.Resources.ASX_Copy_blue_16x,
                        (_, _) => CopyText(op.SeekPredicate is null
                            ? op.Predicate
                            : op.Predicate is null
                                ? op.SeekPredicate
                                : "Seek: " + op.SeekPredicate + Environment.NewLine + "Residual: " + op.Predicate)));
                }

                if (op.Children.Count > 0)
                {
                    menu.Items.Add(new ToolStripMenuItem(
                        node.IsCollapsed ? "Expand Inputs" : "Collapse Inputs",
                        null,
                        (_, _) => _controller.ToggleCollapse(node)));
                }
            }

            if (node is not null)
            {
                // What the tooltip shows, untruncated: already the summary of everything the plan
                // holds about this operator, which makes it the one thing worth pasting elsewhere.
                menu.Items.Add(new ToolStripMenuItem("Copy Details", Properties.Resources.ASX_Copy_blue_16x,
                    (_, _) => CopyText(PlanTooltipBuilder.Build(node, int.MaxValue, OperatorTimeMode).ToString())));
            }

            ContextMenuBuilding?.Invoke(this, new QueryPlanMenuEventArgs(node, menu.Items));

            // Last, and offered even on empty space: once inputs can be hidden, the way back has to
            // be somewhere obvious.
            if (HasCollapsedNodes)
            {
                if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("Expand All", null, (_, _) => ExpandAll()));
            }

            if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Fit to Window", Properties.Resources.ZoomToFit,
                (_, _) => ZoomToFit()));

            // The theme's menu renderer, the same one the toolbars use - a context menu is a ToolStrip.
            menu.ApplyTheme(ThemeExtensions.CurrentTheme);

            // Sub menus are drop downs of their own, which do not take the menu's renderer.
            foreach (var item in menu.Items.OfType<ToolStripMenuItem>().Where(i => i.HasDropDownItems))
            {
                item.DropDown.Renderer = menu.Renderer;
            }

            return menu;
        }

        /// <summary>
        /// Clipboard.SetText throws on an empty string and can fail outright when another
        /// application is holding the clipboard open, neither of which is worth an error dialog over
        /// a copy.
        /// </summary>
        private static void CopyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _contextMenu?.Dispose();
                _renderer.Dispose();

                // The renderer does not own the fonts - the measurer shares them - so they are
                // disposed here, after it.
                _fonts.Dispose();
                _canvas.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Carries the node a right click landed on, and the menu being built for it, so the host can
    /// add the actions that need the rest of the application - looking a table up in the schema
    /// browser, asking the AI assistant about an operator.
    /// </summary>
    public sealed class QueryPlanMenuEventArgs : EventArgs
    {
        internal QueryPlanMenuEventArgs(PlanNode node, ToolStripItemCollection items)
        {
            Node = node;
            Items = items;
        }

        /// <summary>The node under the pointer, or null when the click landed on empty space.</summary>
        public PlanNode Node { get; }

        /// <summary>The operator, or null for the statement root and for empty space.</summary>
        public PlanOperator Operator => Node?.Operator;

        /// <summary>The menu being built.  Add items to it; they are disposed with the menu.</summary>
        public ToolStripItemCollection Items { get; }
    }
}
