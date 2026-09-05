using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using SkiaSharp;

namespace DBADash.Deadlock.Skia
{
    /// <summary>
    /// Draws a laid out deadlock graph onto an <see cref="SKCanvas"/>.
    ///
    /// Everything above this - parsing, layout, view state - is already decided by the time we get
    /// here; this class only paints.  It touches no windowing framework, so the same code runs under
    /// a WinForms SKControl today and an Avalonia Skia surface later.
    ///
    /// The graph is drawn under the view transform so zooming scales text and strokes with it.  The
    /// tooltip is drawn afterwards in screen space, so it stays a readable, constant size no matter
    /// how far the graph is zoomed out.
    /// </summary>
    public sealed class DeadlockRenderer : IDisposable
    {
        private readonly DeadlockFonts _fonts;
        private readonly DeadlockRenderStyle _style;

        private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
        private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
        private readonly SKPaint _text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };

        /// <param name="fonts">
        /// Must be the same instance the layout was measured with - see <see cref="SkiaTextMeasurer"/>.
        /// Not owned: the caller disposes it.
        /// </param>
        public DeadlockRenderer(
            DeadlockFonts fonts,
            DeadlockPalette? palette = null,
            DeadlockRenderStyle? style = null)
        {
            _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
            _style = style ?? new DeadlockRenderStyle();
            Palette = palette ?? DeadlockPalette.Light();
        }

        /// <summary>Swappable so the host can follow a theme change without rebuilding the renderer.</summary>
        public DeadlockPalette Palette { get; set; }

        /// <summary>
        /// The zoom at or above which node detail lines - and therefore the statement preview link -
        /// are drawn.  Exposed so the host only treats the preview as clickable when it is visible.
        /// </summary>
        public double MinZoomForDetailLines => _style.MinZoomForDetailLines;

        public void Render(SKCanvas canvas, DeadlockViewController controller)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            ArgumentNullException.ThrowIfNull(controller);

            canvas.Clear(Palette.Background);

            var layout = controller.Layout;
            if (layout.Nodes.Count == 0) return;

            canvas.Save();
            canvas.Translate((float)controller.Pan.X, (float)controller.Pan.Y);
            canvas.Scale((float)controller.Zoom);

            // Everything the selected node holds and is waiting for.  Empty while nothing is
            // selected, in which case the graph is drawn plainly with nothing faded.
            var highlight = controller.Highlight;

            DrawEdges(canvas, layout, highlight);
            DrawNodes(canvas, layout, controller, highlight);

            // Labels go on last.  They sit at the midpoint of an edge, which for two nodes close
            // together is a gap narrower than the label, so drawing them under the nodes left them
            // half hidden behind a box.  Their background pill keeps them readable over one.
            if (controller.Zoom >= _style.MinZoomForEdgeLabels) DrawEdgeLabels(canvas, layout, highlight);

            canvas.Restore();

            DrawTooltip(canvas, controller);
        }

        // ---------------------------------------------------------------- edges

        private void DrawEdges(SKCanvas canvas, DeadlockLayout layout, DeadlockHighlightMap highlight)
        {
            // Faded edges first, so the handful that answer the question are drawn over the dozens
            // that do not.  With nothing selected everything falls into the first pass and the order
            // is exactly what it was.
            foreach (var edge in layout.Edges)
            {
                if (highlight.RoleOf(edge) == DeadlockHighlightRole.None) DrawEdge(canvas, edge, highlight);
            }

            if (!highlight.IsActive) return;

            foreach (var edge in layout.Edges)
            {
                if (highlight.RoleOf(edge) != DeadlockHighlightRole.None) DrawEdge(canvas, edge, highlight);
            }
        }

        private void DrawEdge(SKCanvas canvas, DeadlockEdge edge, DeadlockHighlightMap highlight)
        {
            var (colour, width) = EdgeAppearance(edge, highlight);

            _stroke.Color = colour;
            _stroke.StrokeWidth = width;

            var start = ToPoint(edge.Start);
            var end = ToPoint(edge.End);
            canvas.DrawLine(start, end, _stroke);

            _fill.Color = colour;
            DrawArrowHead(canvas, start, end);
        }

        /// <summary>
        /// The colour and weight an edge is drawn with.  An edge joining the selection to something it
        /// holds or wants takes the colour of that relationship; everything else fades back once
        /// anything is selected.
        /// </summary>
        private (SKColor Colour, float Width) EdgeAppearance(DeadlockEdge edge, DeadlockHighlightMap highlight)
        {
            var colour = edge.IsInCycle ? Palette.CycleEdge : Palette.Edge;
            var width = edge.IsInCycle ? _style.CycleEdgeWidth : _style.EdgeWidth;

            return highlight.RoleOf(edge) switch
            {
                DeadlockHighlightRole.Owns => (Palette.OwnerHighlight, _style.OwnerEdgeWidth),
                DeadlockHighlightRole.Wants => (Palette.WaiterHighlight, _style.WaitEdgeWidth),
                _ when highlight.IsActive => (Fade(colour), width),
                _ => (colour, width)
            };
        }

        /// <summary>A filled triangle with its tip on the target node's border.</summary>
        private void DrawArrowHead(SKCanvas canvas, SKPoint from, SKPoint to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = MathF.Sqrt((dx * dx) + (dy * dy));

            // Coincident endpoints give no direction to orient the head along.
            if (length < 1e-4f) return;

            var ux = dx / length;
            var uy = dy / length;
            var baseX = to.X - (ux * _style.ArrowLength);
            var baseY = to.Y - (uy * _style.ArrowLength);
            var halfWidth = _style.ArrowWidth / 2f;

            using var path = new SKPath();
            path.MoveTo(to);
            path.LineTo(baseX - (uy * halfWidth), baseY + (ux * halfWidth));
            path.LineTo(baseX + (uy * halfWidth), baseY - (ux * halfWidth));
            path.Close();

            canvas.DrawPath(path, _fill);
        }

        private void DrawEdgeLabels(SKCanvas canvas, DeadlockLayout layout, DeadlockHighlightMap highlight)
        {
            var lineHeight = DeadlockFonts.LineHeight(_fonts.EdgeLabel);

            // Faded labels first, for the same reason as the edges they belong to.  OrderBy is stable,
            // so with nothing selected the order is untouched.
            var labelled = layout.Edges
                .Where(e => !string.IsNullOrEmpty(e.Label))
                .OrderBy(e => highlight.RoleOf(e) == DeadlockHighlightRole.None ? 0 : 1);

            foreach (var edge in labelled)
            {
                var width = _fonts.EdgeLabel.MeasureText(edge.Label, (SKPaint?)null);
                var anchor = ToPoint(edge.LabelAnchor);

                // The label takes its outline from the edge, so a highlighted mode reads as belonging
                // to the highlighted arrow and a faded one recedes with it.
                var edgeColour = EdgeAppearance(edge, highlight).Colour;
                var faded = highlight.IsActive && highlight.RoleOf(edge) == DeadlockHighlightRole.None;

                // A pill behind the text so a label crossing an edge stays readable.
                var box = new SKRect(
                    anchor.X - (width / 2f) - 3f,
                    anchor.Y - (lineHeight / 2f) - 1f,
                    anchor.X + (width / 2f) + 3f,
                    anchor.Y + (lineHeight / 2f) + 1f);

                // A label that had to move aside for another one gets a leader back to its own line,
                // drawn first so the pill covers the end of it.
                DrawLabelLeader(canvas, edge, anchor, edgeColour);

                _fill.Color = Palette.EdgeLabelBackground;
                canvas.DrawRoundRect(box, 3f, 3f, _fill);

                // Outlined in the edge's own colour: where two labels end up near a crossing, the
                // outline is what says which arrow each one belongs to.
                _stroke.Color = edgeColour;
                _stroke.StrokeWidth = _style.EdgeLabelBorderWidth;
                canvas.DrawRoundRect(box, 3f, 3f, _stroke);

                _text.Color = faded ? Fade(Palette.EdgeLabelText) : Palette.EdgeLabelText;
                canvas.DrawText(
                    edge.Label,
                    anchor.X,
                    box.Top + 1f + DeadlockFonts.Baseline(_fonts.EdgeLabel),
                    SKTextAlign.Center,
                    _fonts.EdgeLabel,
                    _text);
            }
        }

        /// <summary>
        /// Joins a label back to the edge it belongs to when it has been moved off the line, which the
        /// layout does when two labels would otherwise land on top of each other.  Nothing is drawn
        /// while the label still sits on its line - which is most of them.
        /// </summary>
        private void DrawLabelLeader(SKCanvas canvas, DeadlockEdge edge, SKPoint anchor, SKColor colour)
        {
            var onLine = ClosestPointOnSegment(ToPoint(edge.Start), ToPoint(edge.End), anchor);
            var dx = anchor.X - onLine.X;
            var dy = anchor.Y - onLine.Y;

            if ((dx * dx) + (dy * dy) < _style.EdgeLabelLeaderDistance * _style.EdgeLabelLeaderDistance) return;

            _stroke.Color = colour;
            _stroke.StrokeWidth = _style.EdgeLabelBorderWidth;
            canvas.DrawLine(onLine, anchor, _stroke);
        }

        private static SKPoint ClosestPointOnSegment(SKPoint start, SKPoint end, SKPoint point)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var lengthSquared = (dx * dx) + (dy * dy);

            if (lengthSquared < 1e-6f) return start;

            var t = (((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / lengthSquared;
            t = Math.Clamp(t, 0f, 1f);

            return new SKPoint(start.X + (t * dx), start.Y + (t * dy));
        }

        // ---------------------------------------------------------------- nodes

        private void DrawNodes(
            SKCanvas canvas,
            DeadlockLayout layout,
            DeadlockViewController controller,
            DeadlockHighlightMap highlight)
        {
            var showDetail = controller.Zoom >= _style.MinZoomForDetailLines;

            foreach (var node in layout.Nodes)
            {
                var role = highlight.RoleOf(node);

                // Other tasks of the selected session keep their ordinary colours rather than fading:
                // eight threads of one parallel query are one participant, and dimming seven of them
                // makes the picture harder to read, not easier.
                var faded = highlight.IsActive && role == DeadlockHighlightRole.None;

                var rect = ToRect(node.Bounds);
                var (fillColour, borderColour) = ColoursFor(node);

                _fill.Color = faded ? Fade(fillColour) : fillColour;
                canvas.DrawRoundRect(rect, _style.NodeCornerRadius, _style.NodeCornerRadius, _fill);

                var isSelected = ReferenceEquals(node, controller.SelectedNode);
                var isHovered = ReferenceEquals(node, controller.HoveredNode);
                var related = role is DeadlockHighlightRole.Owns or DeadlockHighlightRole.Wants;

                _stroke.Color = isSelected ? Palette.Selection
                    : isHovered ? Palette.Hover
                    : role switch
                    {
                        DeadlockHighlightRole.Owns => Palette.OwnerHighlight,
                        DeadlockHighlightRole.Wants => Palette.WaiterHighlight,
                        _ => faded ? Fade(borderColour) : borderColour
                    };
                _stroke.StrokeWidth = isSelected || isHovered || related
                    ? _style.EmphasisBorderWidth
                    : _style.NodeBorderWidth;
                canvas.DrawRoundRect(rect, _style.NodeCornerRadius, _style.NodeCornerRadius, _stroke);

                DrawNodeText(canvas, node, rect, layout, showDetail, controller.StatementLinksEnabled, faded);
            }
        }

        private (SKColor Fill, SKColor Border) ColoursFor(DeadlockNode node) => node switch
        {
            DeadlockProcessNode { IsVictim: true } => (Palette.VictimFill, Palette.VictimBorder),
            DeadlockProcessNode => (Palette.ProcessFill, Palette.ProcessBorder),
            _ => (Palette.ResourceFill, Palette.ResourceBorder)
        };

        private void DrawNodeText(
            SKCanvas canvas,
            DeadlockNode node,
            SKRect rect,
            DeadlockLayout layout,
            bool showDetail,
            bool statementLinksEnabled,
            bool faded)
        {
            SKColor Ink(SKColor colour) => faded ? Fade(colour) : colour;

            // The layout carries the padding it measured with, so text lands inside the box that was
            // sized for it rather than wherever this renderer happens to think the padding is.
            var padding = (float)layout.NodePadding;
            var spacing = (float)layout.LineSpacing;
            var left = rect.Left + padding;
            var maxWidth = rect.Width - (2 * padding);
            var y = rect.Top + padding;

            _text.Color = Ink(Palette.TitleText);
            canvas.DrawText(
                Elide(node.Title, _fonts.Title, maxWidth),
                left,
                y + DeadlockFonts.Baseline(_fonts.Title),
                SKTextAlign.Left,
                _fonts.Title,
                _text);

            if (!showDetail) return;

            y += DeadlockFonts.LineHeight(_fonts.Title);
            _text.Color = Ink(Palette.DetailText);

            foreach (var line in node.DetailLines)
            {
                y += spacing;
                canvas.DrawText(
                    Elide(line, _fonts.Detail, maxWidth),
                    left,
                    y + DeadlockFonts.Baseline(_fonts.Detail),
                    SKTextAlign.Left,
                    _fonts.Detail,
                    _text);
                y += DeadlockFonts.LineHeight(_fonts.Detail);
            }

            if (!string.IsNullOrEmpty(node.StatementPreview))
            {
                y += spacing;
                var statement = Elide(node.StatementPreview, _fonts.Detail, maxWidth);
                var baseline = y + DeadlockFonts.Baseline(_fonts.Detail);

                if (statementLinksEnabled)
                {
                    _text.Color = Ink(Palette.LinkText);
                    canvas.DrawText(statement, left, baseline, SKTextAlign.Left, _fonts.Detail, _text);

                    // Underline so the statement reads as a link the way the rest of the UI does.  Only
                    // the statement text is the link - clicking it opens the full text - so this styling
                    // only appears when a consumer can actually handle the click.
                    var underlineWidth = _fonts.Detail.MeasureText(statement, (SKPaint?)null);
                    _stroke.Color = Ink(Palette.LinkText);
                    _stroke.StrokeWidth = 1f;
                    canvas.DrawLine(left, baseline + 1.5f, left + underlineWidth, baseline + 1.5f, _stroke);
                }
                else
                {
                    // Nothing can open the statement, so show it as plain detail text with no link
                    // affordance rather than a hyperlink that goes nowhere.
                    _text.Color = Ink(Palette.DetailText);
                    canvas.DrawText(statement, left, baseline, SKTextAlign.Left, _fonts.Detail, _text);
                }

                y += DeadlockFonts.LineHeight(_fonts.Detail);
            }
        }

        // ---------------------------------------------------------------- tooltip

        private void DrawTooltip(SKCanvas canvas, DeadlockViewController controller)
        {
            var tooltip = controller.HoveredTooltip;
            var node = controller.HoveredNode;
            if (tooltip is null || node is null) return;

            var padding = _style.TooltipPadding;
            var spacing = _style.TooltipRowSpacing;
            var titleHeight = DeadlockFonts.LineHeight(_fonts.TooltipTitle);
            var rowHeight = DeadlockFonts.LineHeight(_fonts.TooltipText);

            var labels = tooltip.Rows.Select(r => r.Label + ":").ToArray();
            var labelWidth = labels.Length == 0
                ? 0f
                : labels.Max(l => _fonts.TooltipText.MeasureText(l, (SKPaint?)null));
            var gap = labelWidth > 0 ? 8f : 0f;

            var maxContentWidth = _style.TooltipMaxWidth - (2 * padding);
            var valueWidth = maxContentWidth - labelWidth - gap;

            var contentWidth = _fonts.TooltipTitle.MeasureText(tooltip.Title, (SKPaint?)null);
            if (tooltip.Subtitle is not null)
            {
                contentWidth = Math.Max(
                    contentWidth, _fonts.TooltipText.MeasureText(tooltip.Subtitle, (SKPaint?)null));
            }

            foreach (var row in tooltip.Rows)
            {
                var width = labelWidth + gap +
                            Math.Min(valueWidth, _fonts.TooltipText.MeasureText(row.Value, (SKPaint?)null));
                contentWidth = Math.Max(contentWidth, width);
            }

            contentWidth = Math.Min(contentWidth, maxContentWidth);

            var rowCount = tooltip.Rows.Count + (tooltip.Subtitle is null ? 0 : 1);
            var height = (2 * padding) + titleHeight + (rowCount * (spacing + rowHeight));
            var size = new SKSize(contentWidth + (2 * padding), height);

            var origin = PlaceTooltip(controller, node, size);
            var box = SKRect.Create(origin, size);

            _fill.Color = Palette.TooltipBackground;
            canvas.DrawRoundRect(box, _style.TooltipCornerRadius, _style.TooltipCornerRadius, _fill);
            _stroke.Color = Palette.TooltipBorder;
            _stroke.StrokeWidth = 1f;
            canvas.DrawRoundRect(box, _style.TooltipCornerRadius, _style.TooltipCornerRadius, _stroke);

            var y = box.Top + padding;

            _text.Color = Palette.TooltipTitleText;
            canvas.DrawText(
                Elide(tooltip.Title, _fonts.TooltipTitle, contentWidth),
                box.Left + padding,
                y + DeadlockFonts.Baseline(_fonts.TooltipTitle),
                SKTextAlign.Left,
                _fonts.TooltipTitle,
                _text);
            y += titleHeight;

            if (tooltip.Subtitle is not null)
            {
                y += spacing;
                _text.Color = Palette.TooltipLabelText;
                canvas.DrawText(
                    Elide(tooltip.Subtitle, _fonts.TooltipText, contentWidth),
                    box.Left + padding,
                    y + DeadlockFonts.Baseline(_fonts.TooltipText),
                    SKTextAlign.Left,
                    _fonts.TooltipText,
                    _text);
                y += rowHeight;
            }

            for (var i = 0; i < tooltip.Rows.Count; i++)
            {
                y += spacing;
                var baseline = y + DeadlockFonts.Baseline(_fonts.TooltipText);

                _text.Color = Palette.TooltipLabelText;
                canvas.DrawText(
                    labels[i], box.Left + padding, baseline, SKTextAlign.Left, _fonts.TooltipText, _text);

                _text.Color = Palette.TooltipValueText;
                canvas.DrawText(
                    Elide(tooltip.Rows[i].Value, _fonts.TooltipText, valueWidth),
                    box.Left + padding + labelWidth + gap,
                    baseline,
                    SKTextAlign.Left,
                    _fonts.TooltipText,
                    _text);

                y += rowHeight;
            }
        }

        /// <summary>
        /// Beside the hovered node by preference - right, then left - and below or above it when
        /// neither side has room.
        ///
        /// Falling back to below rather than simply clamping matters: clamping a tooltip that is
        /// wider than the space beside it drops it straight on top of the node being pointed at,
        /// which hides the thing the reader is asking about.
        /// </summary>
        private SKPoint PlaceTooltip(DeadlockViewController controller, DeadlockNode node, SKSize size)
        {
            var nodeRect = ToRect(controller.ToScreen(node.Bounds));
            var viewport = controller.Viewport;

            if (viewport.Width <= 0 || viewport.Height <= 0)
            {
                return new SKPoint(nodeRect.Right + _style.TooltipOffset, nodeRect.Top);
            }

            var maxX = Math.Max(0, (float)viewport.Width - size.Width);
            var maxY = Math.Max(0, (float)viewport.Height - size.Height);
            var alongside = Math.Clamp(nodeRect.Top, 0, maxY);

            var right = nodeRect.Right + _style.TooltipOffset;
            if (right + size.Width <= viewport.Width) return new SKPoint(right, alongside);

            var left = nodeRect.Left - _style.TooltipOffset - size.Width;
            if (left >= 0) return new SKPoint(left, alongside);

            var x = Math.Clamp(nodeRect.Left, 0, maxX);

            var below = nodeRect.Bottom + _style.TooltipOffset;
            if (below + size.Height <= viewport.Height) return new SKPoint(x, below);

            var above = nodeRect.Top - _style.TooltipOffset - size.Height;
            if (above >= 0) return new SKPoint(x, above);

            // Nothing fits anywhere - a very small window - so keep it on screen and accept that it
            // covers something.
            return new SKPoint(x, alongside);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// Trims to fit and appends an ellipsis.  Node widths are clamped by the layout engine, so a
        /// long object name or login has to be shortened somewhere, and here is where the real font
        /// metrics are known.
        /// </summary>
        internal static string Elide(string text, SKFont font, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (maxWidth <= 0) return string.Empty;
            if (font.MeasureText(text, (SKPaint?)null) <= maxWidth) return text;

            const string ellipsis = "…";
            var ellipsisWidth = font.MeasureText(ellipsis, (SKPaint?)null);
            if (ellipsisWidth > maxWidth) return string.Empty;

            var length = text.Length;
            while (length > 0)
            {
                length--;
                if (font.MeasureText(text[..length], (SKPaint?)null) + ellipsisWidth <= maxWidth)
                {
                    return text[..length] + ellipsis;
                }
            }

            return ellipsis;
        }

        /// <summary>
        /// Blends a colour towards the background, which is how everything unconnected to the selected
        /// node steps back without disappearing.
        /// </summary>
        private SKColor Fade(SKColor colour)
        {
            var amount = Math.Clamp(_style.UnrelatedFade, 0f, 1f);

            return new SKColor(
                Blend(colour.Red, Palette.Background.Red, amount),
                Blend(colour.Green, Palette.Background.Green, amount),
                Blend(colour.Blue, Palette.Background.Blue, amount),
                colour.Alpha);
        }

        private static byte Blend(byte from, byte to, float amount) =>
            (byte)Math.Round(from + ((to - from) * amount));

        private static SKPoint ToPoint(LayoutPoint point) => new((float)point.X, (float)point.Y);

        private static SKRect ToRect(LayoutRect rect) =>
            new((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);

        public void Dispose()
        {
            // The fonts belong to the caller, since the measurer shares them; only our paints are ours.
            _fill.Dispose();
            _stroke.Dispose();
            _text.Dispose();
        }
    }
}
