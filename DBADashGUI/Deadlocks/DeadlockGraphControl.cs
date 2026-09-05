using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using DBADash.Deadlock.Model;
using DBADash.Deadlock.Skia;
using DBADashGUI.Theme;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Hosts the deadlock graph renderer in WinForms.
    ///
    /// This is the only part of the deadlock viewer tied to a windowing framework: it forwards mouse
    /// and keyboard input into <see cref="DeadlockViewController"/> and repaints when the controller
    /// says something changed.  Everything else - parsing, layout, view state, drawing - lives in
    /// framework independent projects, so porting the viewer means rewriting this file and nothing
    /// else.
    /// </summary>
    public sealed class DeadlockGraphControl : UserControl, IThemedControl
    {
        private readonly SKControl _canvas;
        private readonly DeadlockFonts _fonts;
        private readonly DeadlockRenderer _renderer;

        private DeadlockViewController _controller;

        // Held so the StatementActivated subscription survives graphs being loaded and reloaded: the
        // controller is rebuilt per graph, so subscribers are moved onto each new one.
        private EventHandler<DeadlockStatementActivatedEventArgs> _statementActivated;

        // Set on mouse-down over a statement link and consumed on mouse-up, so the preview opens on a
        // click - press and release on the same link - the way a hyperlink does, without panning.
        private DeadlockNode _pendingLinkNode;

        // Rebuilt for every right click, because what a node offers depends on the node.  Held so it
        // can be disposed - with the items it owns - when the next one replaces it.
        private ContextMenuStrip _contextMenu;

        public DeadlockGraphControl()
        {
            _fonts = new DeadlockFonts();
            _renderer = new DeadlockRenderer(_fonts, DeadlockPaletteMapper.FromTheme(ThemeExtensions.CurrentTheme));

            _canvas = new SKControl { Dock = DockStyle.Fill, TabStop = true };
            _canvas.PaintSurface += Canvas_PaintSurface;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseLeave += Canvas_MouseLeave;
            _canvas.MouseWheel += Canvas_MouseWheel;
            _canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            _canvas.KeyDown += Canvas_KeyDown;
            _canvas.Resize += Canvas_Resize;

            // Focus on enter so the wheel and keyboard work without clicking first.  There is nothing
            // else on this tab that wants the keyboard, so nothing is taken away by doing so.
            _canvas.MouseEnter += (_, _) => _canvas.Focus();

            Controls.Add(_canvas);
        }

        /// <summary>Raised when a node is double clicked, for the host to drill into.</summary>
        public event EventHandler<DeadlockNode> NodeActivated;

        /// <summary>
        /// Raised when a process node's statement preview link is clicked, carrying the full
        /// statement text.  Subscribing is what turns the preview into a clickable link - if nothing
        /// is listening (and <see cref="ShowStatementLinkAlways"/> is off) the statement is drawn as
        /// plain text with no link affordance, so no link is offered that goes nowhere.
        /// </summary>
        public event EventHandler<DeadlockStatementActivatedEventArgs> StatementActivated
        {
            add
            {
                _statementActivated += value;
                if (_controller is not null) _controller.StatementActivated += value;
            }
            remove
            {
                _statementActivated -= value;
                if (_controller is not null) _controller.StatementActivated -= value;
            }
        }

        /// <summary>
        /// Draw the statement preview as a link even when nothing subscribes to
        /// <see cref="StatementActivated"/>.  Off by default so the link only shows when it works.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowStatementLinkAlways { get; set; }

        /// <summary>Raised when the selected node changes, so the host can follow it elsewhere.</summary>
        public event EventHandler<DeadlockNode> SelectionChanged;

        /// <summary>
        /// Raised while a right click menu is being built, after the control has added the actions it
        /// can answer itself, so the host can append the ones that need the rest of the application.
        /// </summary>
        public event EventHandler<DeadlockGraphMenuEventArgs> ContextMenuBuilding;

        public DeadlockGraph Graph { get; private set; }

        /// <summary>
        /// The laid out graph, available after <see cref="LoadGraph"/>.  Exposed because the traced
        /// deadlock cycle is a layout result rather than something the parsed graph knows about, and
        /// the host summarises it.
        /// </summary>
        public DeadlockLayout GraphLayout { get; private set; }

        private DeadlockLayoutStyle _layoutStyle = DeadlockLayoutStyle.Ring;

        /// <summary>
        /// How the graph is arranged.  Changing it lays the loaded graph out again from scratch, which
        /// also refits the view - the picture is a different shape, so the old zoom and pan mean
        /// nothing.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public DeadlockLayoutStyle LayoutStyle
        {
            get => _layoutStyle;
            set
            {
                if (_layoutStyle == value) return;
                _layoutStyle = value;
                if (Graph is not null) LoadGraph(Graph);
            }
        }

        /// <summary>
        /// Put every node back where the layout engine placed it, discarding a hand arrangement, and
        /// refit the view.  Nodes can be dragged, so there has to be a way back.
        /// </summary>
        public void ResetLayout()
        {
            if (Graph is not null) LoadGraph(Graph);
        }

        /// <summary>
        /// Lay the graph out and show it.  The layout is measured with the same fonts the renderer
        /// draws with, so text lands inside the boxes sized for it.
        /// </summary>
        public void LoadGraph(DeadlockGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));

            GraphLayout = new DeadlockLayoutEngine(
                    new SkiaTextMeasurer(_fonts),
                    new DeadlockLayoutOptions { Style = _layoutStyle })
                .Layout(graph);
            _controller = new DeadlockViewController(GraphLayout, new DeadlockViewOptions { MaxFitZoom = 2.0, ShowStatementLinkAlways = ShowStatementLinkAlways });
            _controller.Changed += (_, _) => _canvas.Invalidate();

            // Carry any statement subscribers onto the freshly built controller so the preview stays
            // a working link across graph changes.
            if (_statementActivated is not null) _controller.StatementActivated += _statementActivated;

            // A freshly loaded graph has a view the user has not touched, so setting the viewport
            // fits it as a side effect.  If the control has no size yet, the first resize does it.
            if (_canvas.ClientSize.Width > 0 && _canvas.ClientSize.Height > 0)
            {
                _controller.SetViewport(new LayoutSize(_canvas.ClientSize.Width, _canvas.ClientSize.Height));
            }

            _canvas.Invalidate();
        }

        /// <summary>
        /// What the selected node holds and what it is waiting for, so the host can say in words what
        /// the graph is saying in colour.  Empty while nothing is selected.
        /// </summary>
        public DeadlockHighlightMap Highlight => _controller?.Highlight ?? DeadlockHighlightMap.Empty;

        /// <summary>
        /// Selects the node for a process, so somewhere else in the viewer can point at the graph -
        /// a finding naming SPID 61 lights up SPID 61 when the reader switches to the picture.
        /// </summary>
        public void SelectProcess(DeadlockProcess process)
        {
            if (_controller is null || process is null) return;

            var node = GraphLayout?.Nodes
                .OfType<DeadlockProcessNode>()
                .FirstOrDefault(n => ReferenceEquals(n.Process, process));

            if (node != null) _controller.Select(node);
        }

        public void ZoomToFit()
        {
            _controller?.ZoomToFit();
        }

        public void ZoomIn() => _controller?.ZoomIn(CentreOfView());

        public void ZoomOut() => _controller?.ZoomOut(CentreOfView());

        public void ResetView() => _controller?.ResetView();

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
        /// Put the current view on the clipboard.  Returns false when there is no graph loaded, so
        /// the caller can stay quiet rather than reporting a copy that did not happen.
        /// </summary>
        public bool CopyImageToClipboard()
        {
            using var bitmap = RenderToBitmap();
            if (bitmap is null) return false;

            Clipboard.SetImage(bitmap);
            return true;
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            BackColor = theme.BackgroundColor;

            // Only the palette changes with the theme - the fonts and therefore the layout do not, so
            // there is nothing to re-measure.
            _renderer.Palette = DeadlockPaletteMapper.FromTheme(theme);
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

        private void Canvas_Resize(object sender, EventArgs e)
        {
            if (_controller is null) return;
            if (_canvas.ClientSize.Width <= 0 || _canvas.ClientSize.Height <= 0) return;

            // The controller re-fits as the viewport changes while the view is still the one it
            // chose, so resizing the window keeps the whole graph visible - but stops doing so once
            // the user has zoomed or panned, rather than yanking the view out from under them.
            _controller.SetViewport(new LayoutSize(_canvas.ClientSize.Width, _canvas.ClientSize.Height));
        }

        // ---------------------------------------------------------------- input

        private static LayoutPoint ToLayoutPoint(MouseEventArgs e) => new(e.X, e.Y);

        private LayoutPoint CentreOfView() => new(_canvas.ClientSize.Width / 2.0, _canvas.ClientSize.Height / 2.0);

        /// <summary>
        /// The statement link under the pointer, or null.  Only live when the preview is drawn as a
        /// link - detail lines are shown and something can handle the click - so the pointer is not
        /// treated as being over a link where none is offered.
        /// </summary>
        private DeadlockNode LinkAt(LayoutPoint point) =>
            _controller.StatementLinksEnabled && _controller.Zoom >= _renderer.MinZoomForDetailLines
                ? _controller.StatementLinkAt(point)
                : null;

        /// <summary>
        /// What the pointer says it will do here: open the statement, move the box, or nothing.
        /// </summary>
        private Cursor CursorAt(LayoutPoint point)
        {
            if (LinkAt(point) is not null) return Cursors.Hand;

            return _controller.HitTest(point) is null ? Cursors.Default : Cursors.SizeAll;
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;
            _canvas.Focus();

            if (e.Button == MouseButtons.Left)
            {
                // A click on the statement link opens it rather than selecting - the decision is
                // deferred to mouse-up so a press-and-drag off the link cancels cleanly.  The press
                // still starts a drag: a link is part of its node, and a box whose statement happens
                // to be under the pointer should still be draggable.
                _pendingLinkNode = LinkAt(ToLayoutPoint(e));

                if (_pendingLinkNode is null && _controller.SelectAt(ToLayoutPoint(e)))
                {
                    SelectionChanged?.Invoke(this, _controller.SelectedNode);
                }
            }

            if (e.Button is MouseButtons.Left or MouseButtons.Middle)
            {
                // Left on a node moves the node; left on empty space, or middle anywhere, pans the
                // view - the mapping SSMS uses, and the one that needs no modifier key.
                if (e.Button == MouseButtons.Left && _controller.BeginNodeDrag(ToLayoutPoint(e))) return;

                _controller.BeginPan(ToLayoutPoint(e));
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;

            if (_controller.IsDraggingNode)
            {
                _controller.DragNodeTo(ToLayoutPoint(e));
                _canvas.Cursor = Cursors.SizeAll;
                return;
            }

            if (_controller.IsPanning)
            {
                _controller.PanTo(ToLayoutPoint(e));
                _canvas.Cursor = Cursors.SizeAll;
                return;
            }

            _controller.SetHover(ToLayoutPoint(e));
            _canvas.Cursor = CursorAt(ToLayoutPoint(e));
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_controller is null) return;

            var moved = _controller.EndNodeDrag();
            _controller.EndPan();

            // Fire the link only when the release lands on the same link the press started on, and the
            // node did not move on the way - dragging a box by its statement is a drag, not a click.
            // The controller owns activation and raises StatementActivated with the full statement.
            if (_pendingLinkNode is not null)
            {
                var node = _pendingLinkNode;
                _pendingLinkNode = null;
                if (!moved && ReferenceEquals(LinkAt(ToLayoutPoint(e)), node))
                {
                    _controller.ActivateStatementAt(ToLayoutPoint(e));
                }
            }

            _canvas.Cursor = CursorAt(ToLayoutPoint(e));

            // On mouse-up rather than mouse-down, so a right-drag that started elsewhere does not end
            // in a menu, and so the menu appears where the button was released.
            if (e.Button == MouseButtons.Right) ShowContextMenu(e.Location);
        }

        private void Canvas_MouseLeave(object sender, EventArgs e)
        {
            _pendingLinkNode = null;
            _controller?.EndNodeDrag();
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
            if (node is not null) NodeActivated?.Invoke(this, node);
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (_controller is null) return;

            const double step = 40;
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

                case Keys.Left:
                    _controller.PanBy(step, 0);
                    break;

                case Keys.Right:
                    _controller.PanBy(-step, 0);
                    break;

                case Keys.Up:
                    _controller.PanBy(0, step);
                    break;

                case Keys.Down:
                    _controller.PanBy(0, -step);
                    break;

                case Keys.Escape:
                    _controller.ClearSelection();
                    SelectionChanged?.Invoke(this, null);
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
        /// selection is what makes it unambiguous - and it keeps the grids pointed at the same
        /// process the menu is about.  Right clicking empty space leaves the selection alone.
        /// </summary>
        private void ShowContextMenu(Point location)
        {
            if (_controller is null) return;

            var node = _controller.HitTest(new LayoutPoint(location.X, location.Y));
            if (node is not null && _controller.Select(node))
            {
                SelectionChanged?.Invoke(this, node);
            }

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

        private ContextMenuStrip BuildContextMenu(DeadlockNode node)
        {
            var menu = new ContextMenuStrip();

            switch (node)
            {
                case DeadlockProcessNode process:
                    AddProcessItems(menu, process);
                    break;

                case DeadlockResourceNode resource:
                    AddResourceItems(menu, resource);
                    break;
            }

            ContextMenuBuilding?.Invoke(this, new DeadlockGraphMenuEventArgs(node, menu.Items));

            // Last, and offered even on empty space: once boxes can be dragged, the way back to the
            // arrangement the engine chose has to be somewhere obvious.
            if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Reset Layout", Properties.Resources.ZoomToFit,
                (_, _) => ResetLayout()));

            // The theme's menu renderer, the same one the toolbars use - a context menu is a ToolStrip.
            menu.ApplyTheme(ThemeExtensions.CurrentTheme);
            return menu;
        }

        private void AddProcessItems(ContextMenuStrip menu, DeadlockProcessNode node)
        {
            var statement = node.Process.PrimaryStatement;

            if (!string.IsNullOrWhiteSpace(statement))
            {
                // Only offered when something is listening, for the same reason the preview is only
                // drawn as a link then: an action that opens nothing is worse than no action.
                if (_controller.StatementLinksEnabled)
                {
                    menu.Items.Add(new ToolStripMenuItem("View Statement", Properties.Resources.SQLScript_16x,
                        (_, _) => _controller.ActivateStatement(node)));
                }

                menu.Items.Add(new ToolStripMenuItem("Copy Statement", Properties.Resources.ASX_Copy_blue_16x,
                    (_, _) => CopyText(statement)));
            }

            if (node.Process.Spid is { } spid)
            {
                // The bare number, not "SPID 61": what this is for is pasting into a query.
                menu.Items.Add(new ToolStripMenuItem("Copy SPID", Properties.Resources.ASX_Copy_blue_16x,
                    (_, _) => CopyText(spid.ToString(CultureInfo.InvariantCulture))));
            }

            AddCopyDetailsItem(menu, node);
        }

        private void AddResourceItems(ContextMenuStrip menu, DeadlockResourceNode node)
        {
            var objectName = node.Resource.ObjectName;
            if (!string.IsNullOrWhiteSpace(objectName))
            {
                menu.Items.Add(new ToolStripMenuItem("Copy Object Name", Properties.Resources.ASX_Copy_blue_16x,
                    (_, _) => CopyText(objectName)));
            }

            AddCopyDetailsItem(menu, node);
        }

        /// <summary>
        /// Copies what the node's tooltip shows, untruncated.  The tooltip is already the summary of
        /// everything the graph captured about a node, so pasting it into a ticket or a chat is the
        /// one action that carries the whole picture across.
        /// </summary>
        private void AddCopyDetailsItem(ContextMenuStrip menu, DeadlockNode node)
        {
            menu.Items.Add(new ToolStripMenuItem("Copy Details", Properties.Resources.ASX_Copy_blue_16x,
                (_, _) => CopyText(DeadlockTooltipBuilder.Build(node, int.MaxValue).ToString())));
        }

        /// <summary>
        /// Clipboard.SetText throws on an empty string and can fail outright when another application
        /// is holding the clipboard open, neither of which is worth an error dialog over a copy.
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
}