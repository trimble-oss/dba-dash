using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// Draws a laid out query plan onto an <see cref="SKCanvas"/>.
    ///
    /// Everything inside the view transform is drawn in layout coordinates, so zoom and pan are the
    /// canvas matrix rather than arithmetic repeated at every draw call - which is both faster and
    /// the only way to be sure the picture and the hit testing agree.  The tooltip is the exception:
    /// it is drawn afterwards in screen space, so it stays the same size however far the plan is
    /// zoomed in.
    ///
    /// Holds no framework types beyond Skia, and no state except the palette and style, so a host is
    /// a paint handler and nothing more.
    /// </summary>
    public sealed class PlanRenderer : IDisposable
    {
        private readonly PlanFonts _fonts;
        private readonly PlanRenderStyle _style;

        private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
        private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
        private readonly SKPaint _text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };

        private readonly SKPaint _edge = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Butt
        };

        private readonly SKPaint _glyph = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round
        };

        private readonly SKPaint _marker = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round
        };

        public PlanRenderer(PlanFonts fonts, PlanPalette? palette = null, PlanRenderStyle? style = null)
        {
            _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
            Palette = palette ?? PlanPalette.Light();
            _style = style ?? new PlanRenderStyle();
        }

        /// <summary>Swapped by the host when the application theme changes.</summary>
        public PlanPalette Palette { get; set; }

        /// <summary>
        /// The zoom below which node detail is dropped.  Exposed so a host can match its cursor and
        /// hit testing behaviour to what is actually on screen.
        /// </summary>
        public double MinZoomForDetailLines => _style.MinZoomForDetailLines;

        /// <summary>
        /// Fade everything off the selected node's path to the root.  Off by default - see
        /// <see cref="PlanRenderStyle.FadeOffPath"/>.
        /// </summary>
        public bool FadeOffPath
        {
            get => _style.FadeOffPath;
            set => _style.FadeOffPath = value;
        }

        public void Render(SKCanvas canvas, PlanViewController controller)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            ArgumentNullException.ThrowIfNull(controller);

            canvas.Clear(Palette.Background);

            var layout = controller.Layout;

            // Everything off screen is skipped: a big plan at working zoom is mostly outside the
            // window, and it redraws on every mouse move.
            var visible = controller.VisibleBounds.Inflate(64);
            var detail = controller.Zoom >= _style.MinZoomForDetailLines;
            var content = controller.Zoom >= _style.MinZoomForNodeContent;
            var labels = controller.Zoom >= _style.MinZoomForEdgeLabels;

            canvas.Save();
            canvas.Translate((float)controller.Pan.X, (float)controller.Pan.Y);
            canvas.Scale((float)controller.Zoom);

            foreach (var edge in layout.Edges)
            {
                if (!EdgeBounds(edge).IntersectsWith(visible)) continue;
                DrawEdge(canvas, edge, controller);
            }

            if (labels)
            {
                foreach (var edge in layout.Edges)
                {
                    if (!edge.LabelBounds.IntersectsWith(visible)) continue;
                    DrawEdgeLabel(canvas, edge, controller);
                }
            }

            foreach (var node in layout.Nodes)
            {
                if (!node.Bounds.IntersectsWith(visible)) continue;
                DrawNode(canvas, node, controller, detail, content);
            }

            // Warnings last, over everything, and at every zoom: a plan zoomed out to fit still
            // shows where its problems are, which is when finding them matters most.
            foreach (var node in layout.Nodes)
            {
                if (node.WarningMarkerBounds is not { } marker || !marker.IntersectsWith(visible)) continue;
                DrawWarningMarker(canvas, node, marker, controller);
            }

            canvas.Restore();

            if (controller.HoveredTooltip is { } tooltip && controller.HoverAnchor is { } anchor)
            {
                DrawTooltip(canvas, tooltip, controller, anchor);
            }
        }

        // ---------------------------------------------------------------- nodes

        private void DrawNode(
            SKCanvas canvas,
            PlanNode node,
            PlanViewController controller,
            bool detail,
            bool content)
        {
            var fade = FadeFactor(node, controller);
            var bounds = ToSk(node.Bounds);
            var radius = _style.NodeCornerRadius;

            _fill.Color = Faded(Palette.NodeFill, fade);
            canvas.DrawRoundRect(bounds, radius, radius, _fill);

            // The bar goes on before the border so the border draws over its ends and it reads as
            // part of the card rather than a strip stuck to the bottom of it.
            if (node.MetricBarBounds is { } barBounds)
            {
                DrawMetricBar(canvas, node, barBounds, controller, fade, radius);
            }

            var (borderColour, borderWidth) = BorderFor(node, controller);
            _stroke.Color = Faded(borderColour, fade);
            _stroke.StrokeWidth = borderWidth;
            canvas.DrawRoundRect(bounds, radius, radius, _stroke);

            if (!content) return;

            DrawGlyph(canvas, node, fade);

            var centre = node.CentreText;

            _text.Color = Faded(Palette.TitleText, fade);
            DrawLines(canvas, node.TitleLines, node.Title, node.TitleBounds, _fonts.Title, centre);

            if (detail)
            {
                if (node.Subtitle is not null && node.SubtitleBounds is { } subtitleBounds)
                {
                    _text.Color = Faded(Palette.DetailText, fade);
                    DrawLines(canvas, node.SubtitleLines, node.Subtitle, subtitleBounds, _fonts.Detail, centre);
                }

                if (node.MetricLine is not null && node.MetricBounds is { } metricBounds)
                {
                    _text.Color = Faded(Palette.MetricText, fade);
                    DrawLines(canvas, node.MetricLines, node.MetricLine, metricBounds, _fonts.Metric, centre);
                }

                if (node.TimingLine is not null && node.TimingBounds is { } timingBounds)
                {
                    _text.Color = Faded(Palette.MetricText, fade);
                    DrawLines(canvas, node.TimingLines, node.TimingLine, timingBounds, _fonts.Metric, centre);
                }

                DrawBadges(canvas, node, fade);

                // The control is only shown when it is in play: on every node with inputs it is a
                // row of little dashes down the plan that look like part of the arrows.
                var showToggle = node.IsCollapsed ||
                                 ReferenceEquals(controller.HoveredNode, node) ||
                                 ReferenceEquals(controller.SelectedNode, node);

                if (showToggle && node.CollapseToggleBounds is { } toggle)
                {
                    DrawCollapseToggle(canvas, node, toggle, fade);
                }
            }
        }

        /// <summary>
        /// The bar at the foot of a node, showing its share of the selected metric.
        ///
        /// Clipped to the node's rounded corners rather than drawn as a plain rectangle, so a full
        /// bar does not poke out past the card it belongs to.
        /// </summary>
        private void DrawMetricBar(
            SKCanvas canvas,
            PlanNode node,
            LayoutRect barBounds,
            PlanViewController controller,
            float fade,
            float radius)
        {
            var fraction = node.IsRoot
                ? 1
                : controller.Layout.Metrics.Fraction(node.Operator, controller.HeatMetric);

            var bar = ToSk(barBounds);

            canvas.Save();
            using (var clip = new SKRoundRect(ToSk(node.Bounds), radius, radius))
            {
                canvas.ClipRoundRect(clip, antialias: true);

                _fill.Color = Faded(Palette.MetricBarTrack, fade);
                canvas.DrawRect(bar, _fill);

                if (fraction > 0)
                {
                    var filled = new SKRect(bar.Left, bar.Top, bar.Left + (float)(bar.Width * fraction), bar.Bottom);
                    _fill.Color = Faded(Palette.MetricBarColour(fraction), fade);
                    canvas.DrawRect(filled, _fill);
                }
            }

            canvas.Restore();
        }

        /// <summary>
        /// The operator's icon: a chip in its category colour with the symbol on top.
        ///
        /// The symbol is drawn under a canvas transform that maps its 24 unit grid onto the chip,
        /// rather than by scaling the path, so the cached paths stay immutable and shared.
        /// </summary>
        private void DrawGlyph(SKCanvas canvas, PlanNode node, float fade)
        {
            var chip = ToSk(node.IconBounds);
            var svg = PlanOperatorIcons.SvgFor(node.Kind);

            // A full colour SVG is the whole icon, with no chip behind it - see PlanIconStyle.
            if (svg is { Style: PlanIconStyle.FullColourSvg })
            {
                DrawPicture(canvas, svg.Picture, chip, null, fade);
                return;
            }

            _fill.Color = Faded(Palette.IconFor(node.Category), fade);
            canvas.DrawRoundRect(chip, _style.IconCornerRadius, _style.IconCornerRadius, _fill);

            // The symbol is inset from the chip edge, so the grid it was drawn on maps onto the
            // chip less that inset on each side.
            var inset = _style.IconSymbolInset;

            // A tinted SVG sits where the glyph would, inset the same, in the symbol and accent colours.
            if (svg is not null)
            {
                var area = new SKRect(chip.Left + inset, chip.Top + inset, chip.Right - inset, chip.Bottom - inset);
                using var twoTone = TwoTone(Faded(Palette.IconSymbol, fade), Faded(Palette.IconAccent, fade));
                DrawPicture(canvas, svg.Picture, area, twoTone, 0);
                return;
            }

            var (strokePath, fillPath, accentPath) = PlanOperatorGlyphs.PathsFor(node.Kind);
            var scale = (chip.Width - (inset * 2)) / PlanOperatorGlyphs.GlyphExtent;

            canvas.Save();
            canvas.Translate(chip.Left + inset, chip.Top + inset);
            canvas.Scale(scale);

            if (fillPath is not null)
            {
                _fill.Color = Faded(Palette.IconSymbol, fade);
                canvas.DrawPath(fillPath, _fill);
            }

            _glyph.Color = Faded(Palette.IconSymbol, fade);
            _glyph.StrokeWidth = _style.IconStrokeWidth;
            canvas.DrawPath(strokePath, _glyph);

            // Last, over the rest: it is the part of the symbol that says what happened.
            if (accentPath is not null)
            {
                _glyph.Color = Faded(Palette.IconAccent, fade);
                canvas.DrawPath(accentPath, _glyph);
            }

            canvas.Restore();
        }

        /// <summary>
        /// The recolouring for a tinted SVG icon: its black marks in <paramref name="symbol"/> and its
        /// pure red marks in <paramref name="accent"/> - the two colours a glyph has, so an SVG can say
        /// what a glyph's accent says and still follow the theme.
        ///
        /// A colour matrix, so it is one linear mapping: how red a mark is (its red less its green)
        /// sets how far it goes from the symbol colour to the accent.  Black, white and every grey
        /// have none and come out in the symbol colour; pure red comes out in the accent; the
        /// antialiased edge between the two blends as it should.  Skia applies it to unpremultiplied
        /// colour, so an edge's partial transparency is left alone.
        /// </summary>
        private static SKColorFilter TwoTone(SKColor symbol, SKColor accent)
        {
            static float[] Row(byte from, byte to)
            {
                var start = from / 255f;
                var span = (to - from) / 255f;
                return [span, -span, 0, 0, start];
            }

            return SKColorFilter.CreateColorMatrix(
            [
                .. Row(symbol.Red, accent.Red),
                .. Row(symbol.Green, accent.Green),
                .. Row(symbol.Blue, accent.Blue),
                0, 0, 0, 1, 0
            ]);
        }

        /// <summary>
        /// An SVG icon's picture, fitted into <paramref name="area"/> keeping its shape and centred
        /// in it.
        ///
        /// With a <paramref name="recolour"/> filter it is drawn in the colours that gives - see
        /// <see cref="TwoTone"/> - which is what lets an SVG from a design tool sit on a chip like a
        /// glyph and follow the theme.  Without one it keeps its own colours, and fading blends it
        /// towards the background the way <see cref="Faded"/> does a colour, rather than making it
        /// translucent.
        /// </summary>
        private void DrawPicture(SKCanvas canvas, SKPicture picture, SKRect area, SKColorFilter? recolour, float fade)
        {
            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var scale = Math.Min(area.Width / bounds.Width, area.Height / bounds.Height);
            var matrix = SKMatrix.CreateTranslation(-bounds.Left, -bounds.Top)
                .PostConcat(SKMatrix.CreateScale(scale, scale))
                .PostConcat(SKMatrix.CreateTranslation(
                    area.MidX - (bounds.Width * scale / 2),
                    area.MidY - (bounds.Height * scale / 2)));

            using var paint = new SKPaint { IsAntialias = true };
            // Not disposed with the paint: the caller owns the filter.
            paint.ColorFilter = recolour;

            if (recolour is not null || fade <= 0)
            {
                canvas.DrawPicture(picture, in matrix, paint);
                return;
            }

            // Drawn into a layer and washed with the background over only what it drew.
            canvas.SaveLayer(area, null);
            canvas.DrawPicture(picture, in matrix, paint);

            using var wash = new SKPaint
            {
                Color = Palette.Background.WithAlpha((byte)(Math.Clamp(fade, 0, 1) * 255)),
                BlendMode = SKBlendMode.SrcATop
            };

            canvas.DrawRect(area, wash);
            canvas.Restore();
        }

        /// <summary>
        /// The markers on the node's top edge, most important first, laid out right to left.
        /// </summary>
        private void DrawBadges(SKCanvas canvas, PlanNode node, float fade)
        {
            // The layout placed the strip and sized the node to hold it, so the badges go exactly
            // where it said and nowhere else.
            if (node.BadgeStripBounds is not { } strip) return;

            var size = (float)strip.Height;
            var count = PlanBadges.Count(node.Badges);

            // The step comes out of the strip the layout reserved rather than from a spacing of the
            // renderer's own: the strip is exactly wide enough for these badges at that spacing, so
            // deriving it is both correct and impossible to get out of step with.
            var step = count > 1 ? ((float)strip.Width - size) / (count - 1) : 0;

            var x = (float)strip.Right - size;
            var y = (float)strip.Top;

            foreach (var badge in PlanBadges.Ordered(node.Badges))
            {
                var (colour, symbol, filled, stroke) = BadgeStyle(badge);

                var circle = new SKRect(x, y, x + size, y + size);

                // A ring in the card's own colour, so a badge sitting across the border reads as
                // pinned to the card rather than as a dot the border was drawn through.
                _fill.Color = Faded(Palette.NodeFill, fade);
                canvas.DrawOval(SKRect.Inflate(circle, 1.5f, 1.5f), _fill);

                _fill.Color = Faded(colour, fade);
                canvas.DrawOval(circle, _fill);

                canvas.Save();
                canvas.Translate(circle.Left, circle.Top);
                canvas.Scale(size / BadgeGlyphExtent);

                if (filled)
                {
                    _fill.Color = Faded(Palette.IconSymbol, fade);
                    canvas.DrawPath(symbol, _fill);
                }
                else
                {
                    _glyph.Color = Faded(Palette.IconSymbol, fade);
                    _glyph.StrokeWidth = stroke;
                    canvas.DrawPath(symbol, _glyph);
                }

                canvas.Restore();

                x -= step;
            }
        }

        /// <summary>
        /// The warning triangle over the corner of the glyph: amber, or red when one of the warnings
        /// is critical.
        /// </summary>
        private void DrawWarningMarker(SKCanvas canvas, PlanNode node, LayoutRect marker, PlanViewController controller)
        {
            var fade = FadeFactor(node, controller);
            var bounds = ToSk(marker);

            // Grown about its own centre when it would otherwise be too small to see.
            var onScreen = bounds.Width * (float)controller.Zoom;
            if (onScreen > 0 && onScreen < _style.MinWarningMarkerScreenSize)
            {
                var grow = _style.MinWarningMarkerScreenSize / onScreen;
                var grownWidth = bounds.Width * grow;
                var grownHeight = bounds.Height * grow;
                bounds = SKRect.Create(bounds.MidX - (grownWidth / 2), bounds.MidY - (grownHeight / 2), grownWidth, grownHeight);
            }

            // Everything is proportioned to the triangle's width, so it keeps its shape at any size.
            var unit = bounds.Width / 14f;

            using var triangle = new SKPath();
            triangle.MoveTo(bounds.MidX, bounds.Top);
            triangle.LineTo(bounds.Right, bounds.Bottom);
            triangle.LineTo(bounds.Left, bounds.Bottom);
            triangle.Close();

            // A ring in the card's colour first, so the triangle stands clear of the glyph under it.
            _marker.Color = Faded(Palette.NodeFill, fade);
            _marker.StrokeWidth = 4f * unit;
            canvas.DrawPath(triangle, _marker);

            var colour = node.Badges.HasFlag(PlanNodeBadges.CriticalWarning) ? Palette.Critical : Palette.Warning;

            // Filled, then stroked in the same colour, which is what rounds the corners off.
            _fill.Color = Faded(colour, fade);
            canvas.DrawPath(triangle, _fill);
            _marker.Color = _fill.Color;
            _marker.StrokeWidth = 1.5f * unit;
            canvas.DrawPath(triangle, _marker);

            // The exclamation mark, in whichever of dark or white reads better on the fill: amber
            // is too light for white, red too dark for black.
            var ink = Faded(InkFor(colour), fade);
            var height = bounds.Height;

            _marker.Color = ink;
            _marker.StrokeWidth = 1.7f * unit;
            canvas.DrawLine(bounds.MidX, bounds.Top + (height * 0.36f), bounds.MidX, bounds.Top + (height * 0.63f), _marker);

            _fill.Color = ink;
            canvas.DrawCircle(bounds.MidX, bounds.Top + (height * 0.81f), 1.05f * unit, _fill);
        }

        /// <summary>
        /// Near black or white, whichever contrasts more with <paramref name="background"/>, by
        /// relative luminance as the accessibility guidelines define it.
        /// </summary>
        private static SKColor InkFor(SKColor background)
        {
            static double Linear(byte channel)
            {
                var c = channel / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            var dark = new SKColor(0x1F, 0x1F, 0x1F);
            var luminance = (0.2126 * Linear(background.Red)) +
                            (0.7152 * Linear(background.Green)) +
                            (0.0722 * Linear(background.Blue));

            // Contrast against white is 1.05 / (L + 0.05); against the dark ink, (L + 0.05) / 0.064.
            return 1.05 / (luminance + 0.05) >= (luminance + 0.05) / 0.064 ? SKColors.White : dark;
        }

        /// <summary>
        /// The control on the left edge of a node that hides or shows its inputs, drawn as the plus
        /// or minus box a tree view uses - which is what it is, and so needs no explaining.
        /// </summary>
        private void DrawCollapseToggle(SKCanvas canvas, PlanNode node, LayoutRect toggle, float fade)
        {
            var box = ToSk(toggle);

            _fill.Color = Faded(Palette.NodeFill, fade);
            canvas.DrawRoundRect(box, 3, 3, _fill);

            _stroke.Color = Faded(Palette.NodeBorder, fade);
            _stroke.StrokeWidth = 1;
            canvas.DrawRoundRect(box, 3, 3, _stroke);

            _glyph.Color = Faded(Palette.DetailText, fade);
            _glyph.StrokeWidth = 1.4f;

            var centre = box.MidY;
            var inset = box.Width * 0.26f;
            canvas.DrawLine(box.Left + inset, centre, box.Right - inset, centre, _glyph);

            // A collapsed node shows a plus, because that is what the click will do next.
            if (node.IsCollapsed)
            {
                canvas.DrawLine(box.MidX, box.Top + inset, box.MidX, box.Bottom - inset, _glyph);
            }
        }

        /// <summary>
        /// The border of a node, which carries its state: selected, hovered, matching a search, or
        /// plain.  One at a time and in that order, because two borders on one node is noise.
        /// </summary>
        private (SKColor Colour, float Width) BorderFor(PlanNode node, PlanViewController controller)
        {
            if (ReferenceEquals(controller.SelectedNode, node))
            {
                return (Palette.Selection, _style.EmphasisBorderWidth);
            }

            if (ReferenceEquals(controller.HoveredNode, node))
            {
                return (Palette.Hover, _style.EmphasisBorderWidth);
            }

            if (controller.Matches.Contains(node))
            {
                return (Palette.SearchMatch, _style.EmphasisBorderWidth);
            }

            return (Palette.NodeBorder, _style.NodeBorderWidth);
        }

        // ---------------------------------------------------------------- arrows

        private void DrawEdge(SKCanvas canvas, PlanEdge edge, PlanViewController controller)
        {
            var onPath = controller.PathToRoot.Contains(edge.From) && controller.PathToRoot.Contains(edge.To);
            var fade = Math.Min(FadeFactor(edge.From, controller), FadeFactor(edge.To, controller));

            // The hovered arrow is marked, so it is clear which one the tooltip is describing where
            // several run side by side.  State wins over the estimate: an arrow the reader is
            // pointing at, or one on the path to the root, is being asked about now, and its colour
            // has to answer that rather than something it shares with every other arrow on screen.
            //
            // An estimate that came within ten times keeps the plain actual or estimated colour.
            // Colouring the good ones as well would put a third of the picture in the success colour
            // and leave nothing for the eye to land on, which is the opposite of the point.
            var colour = onPath
                ? Palette.EdgeHighlight
                : ReferenceEquals(controller.HoveredEdge, edge)
                    ? Palette.Hover
                    : MissColour(edge)
                        ?? (edge.IsActual ? Palette.EdgeActual : Palette.Edge);

            var thickness = (float)edge.Thickness;

            // The head is drawn as a triangle, so the body stops short of the node to leave room for
            // it - otherwise the head sits on top of the last stretch of the body and a thin arrow
            // ends up with a visible notch.
            using var body = BuildEdgePath(edge, _style.ArrowLength);

            // A halo in the background colour first, so an arrow crossing another reads as passing
            // over it.  Arrows are routed thickest first, so the thin one crossing a thick one is
            // the one that gets the separation - which is the right way round: a row count of
            // twelve beside one of twelve million is often the whole story.
            _edge.Color = Palette.Background;
            _edge.StrokeWidth = thickness + (_style.EdgeOutlineWidth * 2);
            canvas.DrawPath(body, _edge);

            _edge.Color = Faded(colour, fade);
            _edge.StrokeWidth = thickness;
            canvas.DrawPath(body, _edge);

            DrawArrowHead(canvas, edge, Faded(colour, fade), thickness);

            if (edge.EstimateThickness is { } estimate && edge.EstimateAccuracy is { } accuracy)
            {
                DrawEstimateOutline(canvas, body, (float)estimate, Faded(Palette.ColourFor(accuracy), fade), controller);
            }
        }

        /// <summary>
        /// The warning or critical colour for an arrow whose estimate was ten or a hundred times out
        /// - the same thresholds as the operator's estimate mismatch badge, so the arrow into a
        /// badged operator and the badge itself cannot disagree.  Null where the estimate was close
        /// enough, or where there is no actual figure to compare it against.
        ///
        /// Null as well when the estimate is already drawn as an outline: in
        /// <see cref="PlanEdgeWidthBasis.Both"/> the outline is the accuracy, and colouring the body
        /// underneath it the same would swallow the dashes and lose the width comparison that mode
        /// exists for.
        /// </summary>
        private SKColor? MissColour(PlanEdge edge) =>
            edge.EstimateThickness is null && edge.EstimateAccuracy is { } accuracy and not PlanEstimateAccuracy.Good
                ? Palette.ColourFor(accuracy)
                : null;

        /// <summary>
        /// The estimate, drawn over the actual arrow as a dashed outline of the width it would have
        /// been: inside a fat arrow that was underestimated, a hollow sleeve round a thin one that
        /// was overestimated, and on its edges where the estimate was right.
        ///
        /// Traced from the body's own path at the estimated width, so it follows the rounded corners
        /// without any routing of its own.  The line and the dashes are sized in screen pixels rather
        /// than layout units: in layout units they thin to nothing at the zoom a whole plan fits at,
        /// which is where a reader scans for the red ones.
        /// </summary>
        private void DrawEstimateOutline(SKCanvas canvas, SKPath body, float width, SKColor colour, PlanViewController controller)
        {
            using var sleeve = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = width,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt
            };

            using var outline = new SKPath();
            if (!sleeve.GetFillPath(body, outline)) return;

            var perPixel = 1 / (float)Math.Max(controller.Zoom, 0.01);

            using var dash = SKPathEffect.CreateDash(
                [_style.EstimateDashLength * perPixel, _style.EstimateDashGap * perPixel], 0);

            _marker.Color = colour;
            _marker.StrokeWidth = _style.EstimateOutlineWidth * perPixel;
            _marker.PathEffect = dash;
            canvas.DrawPath(outline, _marker);
            _marker.PathEffect = null;
        }

        /// <summary>
        /// The arrow body as a rounded polyline, stopping <paramref name="trim"/> short of its
        /// destination.
        ///
        /// Corners are rounded because a square corner on a twenty pixel wide arrow reads as a
        /// mistake, and because the radius is what makes a thick arrow look like a pipe carrying
        /// rows rather than a drawn rectangle.
        /// </summary>
        private SKPath BuildEdgePath(PlanEdge edge, float trim)
        {
            var points = edge.Points.Select(ToSk).ToList();

            // Pull the last point back along the final segment by the trim distance.
            if (points.Count >= 2 && trim > 0)
            {
                var last = points[^1];
                var previous = points[^2];
                var dx = last.X - previous.X;
                var dy = last.Y - previous.Y;
                var length = (float)Math.Sqrt((dx * dx) + (dy * dy));

                if (length > trim)
                {
                    points[^1] = new SKPoint(last.X - (dx / length * trim), last.Y - (dy / length * trim));
                }
            }

            var path = new SKPath();
            path.MoveTo(points[0]);

            var radius = (float)Math.Min(_style.NodeCornerRadius * 1.5, 12);

            for (var i = 1; i < points.Count - 1; i++)
            {
                var corner = points[i];
                var previous = points[i - 1];
                var next = points[i + 1];

                // Never round by more than half of either adjacent segment, or the corners of a
                // short dog-leg overlap and the arrow develops a kink.
                var limit = Math.Min(Distance(previous, corner), Distance(corner, next)) / 2;
                var r = Math.Min(radius, limit);

                if (r <= 0.5f)
                {
                    path.LineTo(corner);
                    continue;
                }

                path.LineTo(Towards(corner, previous, r));
                path.QuadTo(corner, Towards(corner, next, r));
            }

            path.LineTo(points[^1]);
            return path;
        }

        private void DrawArrowHead(SKCanvas canvas, PlanEdge edge, SKColor colour, float thickness)
        {
            var tip = ToSk(edge.Points[^1]);
            var from = ToSk(edge.Points[^2]);

            var dx = tip.X - from.X;
            var dy = tip.Y - from.Y;
            var length = (float)Math.Sqrt((dx * dx) + (dy * dy));
            if (length <= 0) return;

            var ux = dx / length;
            var uy = dy / length;

            var baseX = tip.X - (ux * _style.ArrowLength);
            var baseY = tip.Y - (uy * _style.ArrowLength);

            // Wider than the body it caps, so a thin arrow still has a head that reads as one and a
            // thick arrow does not end in a spike.
            var halfWidth = (thickness + _style.ArrowHeadSpread) / 2;

            using var head = new SKPath();
            head.MoveTo(tip);
            head.LineTo(baseX - (uy * -halfWidth), baseY - (ux * halfWidth));
            head.LineTo(baseX + (uy * -halfWidth), baseY + (ux * halfWidth));
            head.Close();

            _fill.Color = colour;
            canvas.DrawPath(head, _fill);
        }

        private void DrawEdgeLabel(SKCanvas canvas, PlanEdge edge, PlanViewController controller)
        {
            var fade = Math.Min(FadeFactor(edge.From, controller), FadeFactor(edge.To, controller));
            var bounds = ToSk(edge.LabelBounds);

            // A pad behind the text, because the label sits over whatever the arrow is crossing.
            _fill.Color = Faded(Palette.EdgeLabelBackground, fade);
            canvas.DrawRoundRect(bounds.Standardized with
            {
                Left = bounds.Left - 3,
                Right = bounds.Right + 3
            }, 3, 3, _fill);

            _text.Color = Faded(Palette.EdgeLabelText, fade);
            canvas.DrawText(
                edge.Label,
                bounds.Left,
                bounds.Top + PlanFonts.Baseline(_fonts.EdgeLabel),
                _fonts.EdgeLabel,
                _text);
        }

        // ---------------------------------------------------------------- tooltip

        private void DrawTooltip(
            SKCanvas canvas,
            PlanTooltip tooltip,
            PlanViewController controller,
            LayoutRect anchor)
        {
            var padding = _style.TooltipPadding;
            var maxWidth = _style.TooltipMaxWidth - (padding * 2);

            var titleHeight = PlanFonts.LineHeight(_fonts.TooltipTitle);
            var rowHeight = PlanFonts.LineHeight(_fonts.TooltipText);

            // The label column is as wide as the widest label, so the values line up - a tooltip of
            // twenty figures is read by scanning down the values, and ragged ones make that slower.
            var labelWidth = tooltip.Rows.Count == 0
                ? 0
                : tooltip.Rows.Max(row => _fonts.TooltipLabel.MeasureText(row.Label, (SKPaint?)null));

            labelWidth = Math.Min(labelWidth, maxWidth * 0.5f);
            var valueWidth = maxWidth - labelWidth - _style.TooltipLabelGap;

            var contentWidth = Math.Max(
                _fonts.TooltipTitle.MeasureText(tooltip.Title, (SKPaint?)null),
                labelWidth + _style.TooltipLabelGap + MaxValueWidth(tooltip, valueWidth));

            if (tooltip.Subtitle is not null)
            {
                contentWidth = Math.Max(
                    contentWidth,
                    Math.Min(_fonts.TooltipText.MeasureText(tooltip.Subtitle, (SKPaint?)null), maxWidth));
            }

            if (tooltip.Description is not null)
            {
                contentWidth = Math.Max(
                    contentWidth,
                    Math.Min(
                        _fonts.TooltipText.MeasureText(tooltip.Description, (SKPaint?)null),
                        _style.TooltipDescriptionMinWidth));
            }

            contentWidth = Math.Min(contentWidth, maxWidth);

            var descriptionLines = tooltip.Description is null
                ? []
                : Wrap(tooltip.Description, contentWidth, _fonts.TooltipText);

            // Each value as the lines it is drawn on: one for most, several for the values that wrap.
            var valueLeft = labelWidth + _style.TooltipLabelGap;
            var valueLines = tooltip.Rows
                .Select(row => row.Wraps
                    ? WrapValue(row.Value, contentWidth - valueLeft, _fonts.TooltipText, _style.TooltipMaxWrappedLines)
                    : [row.Value])
                .ToList();

            var height = padding + titleHeight;
            if (tooltip.Subtitle is not null) height += rowHeight;
            if (descriptionLines.Count > 0) height += _style.TooltipRowSpacing * 2 + (descriptionLines.Count * rowHeight);

            // A rule between what the operator is and what it did, so the figures are not read as more
            // of the paragraph.
            if (descriptionLines.Count > 0 && tooltip.Rows.Count > 0) height += _style.TooltipRowSpacing * 2;
            if (tooltip.Rows.Count > 0) height += _style.TooltipRowSpacing * 2;
            height += valueLines.Sum(lines => (lines.Count * rowHeight) + _style.TooltipRowSpacing);
            height += padding;

            var origin = TooltipOrigin(controller, anchor, contentWidth + (padding * 2), height);
            var box = new SKRect(origin.X, origin.Y, origin.X + contentWidth + (padding * 2), origin.Y + height);

            _fill.Color = Palette.TooltipBackground;
            canvas.DrawRoundRect(box, _style.TooltipCornerRadius, _style.TooltipCornerRadius, _fill);

            _stroke.Color = Palette.TooltipBorder;
            _stroke.StrokeWidth = 1;
            canvas.DrawRoundRect(box, _style.TooltipCornerRadius, _style.TooltipCornerRadius, _stroke);

            var x = box.Left + padding;
            var y = box.Top + padding;

            _text.Color = Palette.TooltipTitleText;
            DrawElided(canvas, tooltip.Title, x, y, contentWidth, _fonts.TooltipTitle);
            y += titleHeight;

            if (tooltip.Subtitle is not null)
            {
                _text.Color = Palette.TooltipLabelText;
                DrawElided(canvas, tooltip.Subtitle, x, y, contentWidth, _fonts.TooltipText);
                y += rowHeight;
            }

            if (descriptionLines.Count > 0)
            {
                y += _style.TooltipRowSpacing * 2;
                _text.Color = Palette.TooltipLabelText;

                foreach (var line in descriptionLines)
                {
                    DrawElided(canvas, line, x, y, contentWidth, _fonts.TooltipText);
                    y += rowHeight;
                }

                if (tooltip.Rows.Count > 0)
                {
                    y += _style.TooltipRowSpacing * 2;
                    _stroke.Color = Palette.TooltipBorder;
                    _stroke.StrokeWidth = 1;
                    canvas.DrawLine(x, y + 0.5f, x + contentWidth, y + 0.5f, _stroke);
                }
            }

            if (tooltip.Rows.Count > 0) y += _style.TooltipRowSpacing * 2;

            for (var i = 0; i < tooltip.Rows.Count; i++)
            {
                var row = tooltip.Rows[i];

                _text.Color = Palette.TooltipLabelText;
                DrawElided(canvas, row.Label, x, y, labelWidth, _fonts.TooltipLabel);

                _text.Color = row.IsEmphasised ? Palette.Warning : Palette.TooltipValueText;

                foreach (var line in valueLines[i])
                {
                    DrawElided(canvas, line, x + valueLeft, y, contentWidth - valueLeft, _fonts.TooltipText);
                    y += rowHeight;
                }

                y += _style.TooltipRowSpacing;
            }
        }

        /// <summary>
        /// Wrap a tooltip value to <paramref name="width"/>, over at most <paramref name="maxLines"/>
        /// lines, the last of which is elided if the value runs on past it.
        ///
        /// Unlike <see cref="Wrap"/>, a word too long for the line is broken rather than given a line
        /// of its own: a predicate is mostly long bracketed names with no spaces in them, and eliding
        /// each one would lose exactly the part being read.
        /// </summary>
        internal static List<string> WrapValue(string text, float width, SKFont font, int maxLines)
        {
            var lines = new List<string>();
            var rest = text.Trim();

            while (rest.Length > 0)
            {
                if (lines.Count >= Math.Max(1, maxLines) - 1 || font.MeasureText(rest, (SKPaint?)null) <= width)
                {
                    lines.Add(Elide(rest, font, width));
                    break;
                }

                var fits = font.BreakText(rest, width);
                if (fits <= 0)
                {
                    lines.Add(Elide(rest, font, width));
                    break;
                }

                // Back to the last space that fits, which may be the one just past the end.
                var space = rest.LastIndexOf(' ', Math.Min(fits, rest.Length - 1));
                var take = space > 0 ? space : fits;

                lines.Add(rest[..take].TrimEnd());
                rest = rest[take..].TrimStart();
            }

            return lines;
        }

        /// <summary>
        /// Word wrap <paramref name="text"/> to <paramref name="width"/>.  A word longer than the
        /// line gets a line to itself and is elided when drawn, rather than broken mid-word.
        /// </summary>
        private static List<string> Wrap(string text, float width, SKFont font)
        {
            var lines = new List<string>();
            var line = new StringBuilder();

            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;

                if (line.Length > 0 && font.MeasureText(candidate, (SKPaint?)null) > width)
                {
                    lines.Add(line.ToString());
                    line.Clear().Append(word);
                }
                else
                {
                    line.Clear().Append(candidate);
                }
            }

            if (line.Length > 0) lines.Add(line.ToString());
            return lines;
        }

        private float MaxValueWidth(PlanTooltip tooltip, float limit)
        {
            var widest = 0f;

            foreach (var row in tooltip.Rows)
            {
                widest = Math.Max(widest, _fonts.TooltipText.MeasureText(row.Value, (SKPaint?)null));
                if (widest >= limit) return limit;
            }

            return widest;
        }

        /// <summary>
        /// Where the tooltip goes: beside what is hovered - a node, or the point on an arrow the
        /// pointer met - flipped to whichever side has room.
        ///
        /// A tooltip that runs off the window is worse than no tooltip, and the node being hovered
        /// is as likely to be at the right hand edge of a wide plan as anywhere else.
        /// </summary>
        private SKPoint TooltipOrigin(
            PlanViewController controller,
            LayoutRect anchor,
            float width,
            float height)
        {
            var node = controller.ToScreen(anchor);
            var viewport = controller.Viewport;

            var x = (float)(node.Right + _style.TooltipOffset);
            if (x + width > viewport.Width) x = (float)(node.Left - _style.TooltipOffset - width);
            x = (float)Math.Clamp(x, 4, Math.Max(4, viewport.Width - width - 4));

            var y = (float)node.Top;
            y = (float)Math.Clamp(y, 4, Math.Max(4, viewport.Height - height - 4));

            return new SKPoint(x, y);
        }

        // ---------------------------------------------------------------- text

        /// <summary>
        /// A node's text item as the lines the layout broke it into, one under the other down
        /// <paramref name="bounds"/>, each elided to fit and centred across it when asked.  With no
        /// lines - a layout that did not break the item - <paramref name="text"/> is drawn as one.
        /// </summary>
        private void DrawLines(
            SKCanvas canvas, IReadOnlyList<string> lines, string text, LayoutRect bounds, SKFont font, bool centre)
        {
            if (lines.Count == 0) lines = [text];

            var width = (float)bounds.Width;
            var step = (float)bounds.Height / lines.Count;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = Elide(lines[i], font, width);
                var x = (float)bounds.X;
                if (centre) x += Math.Max(0, (width - font.MeasureText(line, (SKPaint?)null)) / 2);

                canvas.DrawText(line, x, (float)bounds.Y + (i * step) + PlanFonts.Baseline(font), font, _text);
            }
        }

        private void DrawElided(SKCanvas canvas, string value, float x, float y, float width, SKFont font)
        {
            canvas.DrawText(Elide(value, font, width), x, y + PlanFonts.Baseline(font), font, _text);
        }

        /// <summary>
        /// Shorten text to fit, with an ellipsis.
        ///
        /// Cut from the end rather than the middle: the values being elided here are operator names
        /// and object names, and their beginnings are what distinguishes them.
        /// </summary>
        internal static string Elide(string value, SKFont font, float maxWidth)
        {
            if (string.IsNullOrEmpty(value) || maxWidth <= 0) return string.Empty;
            if (font.MeasureText(value, (SKPaint?)null) <= maxWidth) return value;

            const string ellipsis = "...";
            var ellipsisWidth = font.MeasureText(ellipsis, (SKPaint?)null);
            var available = maxWidth - ellipsisWidth;

            // Not even the ellipsis fits, so drawing anything would only be noise.
            if (available <= 0) return string.Empty;

            var fits = font.BreakText(value, available);
            return fits <= 0 ? string.Empty : value[..fits] + ellipsis;
        }

        // ---------------------------------------------------------------- badges

        /// <summary>The grid the badge symbols are drawn on.</summary>
        private const float BadgeGlyphExtent = 12f;


        private static readonly SKPath MismatchSymbol =
            SKPath.ParseSvgPathData("M2.8 4.8h6.4 M2.8 7.6h6.4 M8.6 2.6l-5.2 7")!;

        // Two arrows running the way the rows travel - right to left, into the root - which is what
        // parallelism is: the same work on more than one stream at once.
        private static readonly SKPath ParallelSymbol =
            SKPath.ParseSvgPathData("M8.9 3.9H3.9 M5.3 2.5 3.9 3.9 5.3 5.3 M8.9 8.1H3.9 M5.3 6.7 3.9 8.1 5.3 9.5")!;

        private static readonly SKPath BatchSymbol =
            SKPath.ParseSvgPathData("M2.4 2.4h3.1v3.1H2.4z M6.5 2.4h3.1v3.1H6.5z M2.4 6.5h3.1v3.1H2.4z M6.5 6.5h3.1v3.1H6.5z")!;

        private static readonly SKPath PlusSymbol =
            SKPath.ParseSvgPathData("M6 2.6v6.8 M2.6 6h6.8")!;

        private static readonly SKPath CrossSymbol =
            SKPath.ParseSvgPathData("M3.2 3.2l5.6 5.6 M8.8 3.2l-5.6 5.6")!;

        /// <summary>The line width a badge's symbol is stroked at, on its 12 unit grid.</summary>
        private const float BadgeStrokeWidth = 1.4f;

        /// <summary>
        /// Thinner, for the parallel badge: two arrows one above the other are twice the line work of
        /// any other badge, and at the weight the rest are drawn they close up into a blob at the size
        /// a badge actually gets.
        /// </summary>
        private const float ParallelStrokeWidth = 1f;

        private (SKColor Colour, SKPath Symbol, bool Filled, float Stroke) BadgeStyle(PlanNodeBadges badge) => badge switch
        {

            PlanNodeBadges.EstimateMismatch => (Palette.Warning, MismatchSymbol, false, BadgeStrokeWidth),
            PlanNodeBadges.RowsDiscarded => (Palette.Warning, CrossSymbol, false, BadgeStrokeWidth),
            PlanNodeBadges.MissingIndex => (Palette.Info, PlusSymbol, false, BadgeStrokeWidth),
            PlanNodeBadges.Parallel => (Palette.Info, ParallelSymbol, false, ParallelStrokeWidth),
            PlanNodeBadges.BatchMode => (Palette.Info, BatchSymbol, true, BadgeStrokeWidth),
            _ => (Palette.Info, PlusSymbol, false, BadgeStrokeWidth)
        };

        // ---------------------------------------------------------------- primitives

        /// <summary>
        /// How far towards the background a node is drawn, from 0 (full colour) to
        /// <see cref="PlanRenderStyle.UnrelatedFade"/>.
        /// </summary>
        private float FadeFactor(PlanNode node, PlanViewController controller)
        {
            if (!_style.FadeOffPath || controller.PathToRoot.Count == 0) return 0;

            return controller.PathToRoot.Contains(node) ? 0 : _style.UnrelatedFade;
        }

        /// <summary>
        /// A colour blended towards the background, which is how fading is done here.
        ///
        /// Blending rather than reducing alpha: alpha over an arrow that is itself over a node
        /// produces three different results for the same nominal fade, and the picture ends up
        /// looking grubby rather than dimmed.
        /// </summary>
        private SKColor Faded(SKColor colour, float amount)
        {
            if (amount <= 0) return colour;

            var t = Math.Clamp(amount, 0, 1);
            var background = Palette.Background;

            return new SKColor(
                (byte)(colour.Red + ((background.Red - colour.Red) * t)),
                (byte)(colour.Green + ((background.Green - colour.Green) * t)),
                (byte)(colour.Blue + ((background.Blue - colour.Blue) * t)),
                colour.Alpha);
        }

        private static LayoutRect EdgeBounds(PlanEdge edge)
        {
            var bounds = new LayoutRect(edge.Points[0].X, edge.Points[0].Y, 0, 0);

            foreach (var point in edge.Points)
            {
                bounds = bounds.Union(new LayoutRect(point.X, point.Y, 0, 0));
            }

            return bounds.Inflate(edge.Thickness);
        }

        private static SKRect ToSk(LayoutRect rect) =>
            new((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);

        private static SKPoint ToSk(LayoutPoint point) => new((float)point.X, (float)point.Y);

        private static float Distance(SKPoint a, SKPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>A point <paramref name="distance"/> from <paramref name="from"/> towards
        /// <paramref name="towards"/>.</summary>
        private static SKPoint Towards(SKPoint from, SKPoint towards, float distance)
        {
            var dx = towards.X - from.X;
            var dy = towards.Y - from.Y;
            var length = (float)Math.Sqrt((dx * dx) + (dy * dy));

            return length <= 0
                ? from
                : new SKPoint(from.X + (dx / length * distance), from.Y + (dy / length * distance));
        }

        public void Dispose()
        {
            _fill.Dispose();
            _stroke.Dispose();
            _text.Dispose();
            _edge.Dispose();
            _glyph.Dispose();
            _marker.Dispose();
        }
    }
}
