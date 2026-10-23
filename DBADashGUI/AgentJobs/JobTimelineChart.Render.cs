using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.AgentJobs
{
    internal sealed partial class JobTimelineChart
    {
        /// <summary>Set while rendering for an image, which is taller than the control so every row fits.</summary>
        private int? _exportHeight;

        private float CanvasHeight => _exportHeight ?? _canvas.Height;

        private SKTypeface _typeface;
        private SKTypeface _boldTypeface;
        private SKFont _font;
        private SKFont _boldFont;
        private SKFont _smallFont;

        private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
        private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
        private readonly SKPaint _text = new() { IsAntialias = true };

        /// <summary>Legend chips drawn in the last frame, for clicking.</summary>
        private readonly List<(SKRect Rect, RunStatus Status)> _legendRects = new();

        private static readonly RunStatus[] LegendOrder = { RunStatus.Succeeded, RunStatus.Failed, RunStatus.Retry, RunStatus.Cancelled, RunStatus.InProgress };

        private static readonly TimeSpan[] TickSteps =
        {
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(3), TimeSpan.FromHours(6), TimeSpan.FromHours(12),
            TimeSpan.FromDays(1), TimeSpan.FromDays(2), TimeSpan.FromDays(7), TimeSpan.FromDays(14), TimeSpan.FromDays(28)
        };

        private void InitFonts()
        {
            // FromFamilyName falls back to a default face when the family is missing.
            _typeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
            _boldTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? _typeface;
            _font = new SKFont(_typeface, 12);
            _boldFont = new SKFont(_boldTypeface, 12);
            _smallFont = new SKFont(_typeface, 11);
        }

        private void DisposeFonts()
        {
            _overviewImage?.Dispose();
            _font?.Dispose();
            _boldFont?.Dispose();
            _smallFont?.Dispose();
            _fill.Dispose();
            _stroke.Dispose();
            _text.Dispose();
            // SKTypeface.Default is shared and the bold lookup can return the regular face, so dispose each at most once.
            if (_typeface != null && !ReferenceEquals(_typeface, SKTypeface.Default)) _typeface.Dispose();
            if (_boldTypeface != null && !ReferenceEquals(_boldTypeface, _typeface) && !ReferenceEquals(_boldTypeface, SKTypeface.Default)) _boldTypeface.Dispose();
        }

        private sealed class Palette
        {
            public SKColor Back;
            public SKColor Fore;
            public SKColor Dim;
            public SKColor Panel;
            public SKColor Header;
            public SKColor AltRow;
            public SKColor HoverRow;
            public SKColor Grid;
            public SKColor GridStrong;
            public SKColor Accent;
            public SKColor TipBack;
            public SKColor TipFore;
        }

        private static Palette GetPalette()
        {
            var theme = DBADashUser.SelectedTheme;
            var back = theme.TimelineChartBackColor.ToSKColor();
            var fore = theme.TimelineLabelColor.ToSKColor();
            var grid = theme.TimelineGridColor.ToSKColor();
            return new Palette
            {
                Back = back,
                Fore = fore,
                Dim = Blend(back, fore, 0.6f),
                Panel = Blend(back, fore, 0.035f),
                Header = Blend(back, fore, 0.07f),
                AltRow = Blend(back, fore, 0.025f),
                HoverRow = Blend(back, fore, 0.08f),
                Grid = grid.WithAlpha(35),
                GridStrong = grid.WithAlpha(90),
                Accent = DashColors.Information.ToSKColor(),
                TipBack = theme.TimelineToolTipBackColor.ToSKColor(),
                TipFore = theme.TimelineToolTipForeColor.ToSKColor()
            };
        }

        private static SKColor Blend(SKColor a, SKColor b, float t) => new(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));

        private void Render(SKCanvas canvas, int width, int height, bool interactive)
        {
            var p = GetPalette();
            _font.Size = 12 * Dpi;
            _boldFont.Size = 12 * Dpi;
            _smallFont.Size = 11 * Dpi;
            canvas.Clear(p.Back);

            var (step, ticks) = ComputeTicks();
            // Only the rows scrolled into view, found by binary search - there can be thousands on the multi-instance timeline
            var firstVisible = FirstRowEndingAfter(_scrollY);
            var visibleRows = new List<TimelineRowModel>();
            for (var i = firstVisible; i < _rows.Count && _rows[i].Top <= _scrollY + RowsHeight; i++) visibleRows.Add(_rows[i]);

            // Rows: background bands across label and chart
            canvas.Save();
            canvas.ClipRect(new SKRect(0, RowsTop, width, RowsBottom));
            for (var i = 0; i < visibleRows.Count; i++)
            {
                var row = visibleRows[i];
                var top = RowScreenTop(row);
                var band = new SKRect(0, top, ChartRight, top + row.Height);
                SKColor? color = row == _hoverRow && interactive ? p.HoverRow
                    : row.IsGroup ? p.Header
                    : (firstVisible + i) % 2 == 1 ? p.AltRow : null;
                if (color.HasValue)
                {
                    _fill.Color = color.Value;
                    canvas.DrawRect(band, _fill);
                }
                if (row.IsGroup || row.Parent == null)
                {
                    _stroke.Color = p.Grid;
                    _stroke.StrokeWidth = 1;
                    canvas.DrawLine(0, top, ChartRight, top, _stroke);
                }
            }

            DrawGrid(canvas, p, step, ticks);
            DrawNowLine(canvas, p);

            canvas.Save();
            canvas.ClipRect(new SKRect(ChartLeft, RowsTop, ChartRight, RowsBottom));
            // And of those, only the bars in the time in view
            foreach (var row in visibleRows)
            {
                foreach (var bar in row.BarsBetween(_viewFrom, _viewTo))
                {
                    if (IsVisible(bar)) DrawBar(canvas, p, bar);
                }
            }
            canvas.Restore();

            DrawLabels(canvas, p, visibleRows);
            canvas.Restore();

            DrawHeader(canvas, p, width, step, ticks);
            if (ShowConcurrency) DrawConcurrencyBand(canvas, p, ticks);
            if (!string.IsNullOrEmpty(Banner)) DrawBanner(canvas, p, Banner);
            if (interactive) DrawCursor(canvas, p);
            DrawOverview(canvas, p);
            DrawScrollBar(canvas, p);

            if (_rows.Count == 0)
            {
                var text = _model.Bars.Count == 0 ? EmptyText : "No jobs match the filter";
                _text.Color = p.Dim;
                canvas.DrawText(text, ChartLeft + ChartWidth / 2, RowsTop + 40 * Dpi, SKTextAlign.Center, _font, _text);
            }

            if (interactive && _drag == DragMode.None && _hoverBar != null && _mouseInside)
            {
                DrawTooltip(canvas, p, _hoverBar, width, height);
            }
        }

        #region Axis

        private (TimeSpan Step, List<DateTime> Ticks) ComputeTicks()
        {
            var minSpacing = 80 * Dpi;
            var step = TickSteps.FirstOrDefault(s => s.Ticks / ViewTicks * ChartWidth >= minSpacing);
            if (step == default) step = TickSteps[^1];

            var ticks = new List<DateTime>();
            if (step < TimeSpan.FromDays(1))
            {
                // Restart at each midnight so ticks stay aligned to the day even when the step doesn't divide it.
                for (var day = _viewFrom.Date; day <= _viewTo; day = day.AddDays(1))
                {
                    var first = day < _viewFrom ? day.AddTicks((_viewFrom - day).Ticks / step.Ticks * step.Ticks) : day;
                    for (var t = first; t < day.AddDays(1) && t <= _viewTo; t = t.Add(step))
                    {
                        if (t >= _viewFrom) ticks.Add(t);
                    }
                }
            }
            else
            {
                for (var t = _viewFrom.Date; t <= _viewTo; t = t.Add(step))
                {
                    if (t >= _viewFrom) ticks.Add(t);
                }
            }
            return (step, ticks);
        }

        private static string TickLabel(DateTime t, TimeSpan step) =>
            step >= TimeSpan.FromDays(1) ? t.ToString("dd MMM")
            : step < TimeSpan.FromMinutes(1) ? t.ToString("HH:mm:ss")
            : t.ToString("HH:mm");

        private void DrawGrid(SKCanvas canvas, Palette p, TimeSpan step, List<DateTime> ticks)
        {
            canvas.Save();
            canvas.ClipRect(new SKRect(ChartLeft, RowsTop, ChartRight, RowsBottom));
            foreach (var t in ticks)
            {
                var x = TimeToX(t);
                var midnight = t.TimeOfDay == TimeSpan.Zero && step < TimeSpan.FromDays(1);
                _stroke.Color = midnight ? p.GridStrong : p.Grid;
                _stroke.StrokeWidth = midnight ? 1.5f * Dpi : 1;
                canvas.DrawLine(x, RowsTop, x, RowsBottom, _stroke);
            }
            canvas.Restore();
        }

        private void DrawHeader(SKCanvas canvas, Palette p, int width, TimeSpan step, List<DateTime> ticks)
        {
            _fill.Color = p.Header;
            canvas.DrawRect(new SKRect(0, 0, width, HeaderHeight), _fill);
            _stroke.Color = p.GridStrong;
            _stroke.StrokeWidth = 1;
            canvas.DrawLine(0, HeaderHeight, width, HeaderHeight, _stroke);

            // Title and legend above the labels
            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0, LabelWidth - 4 * Dpi, HeaderHeight));
            _text.Color = p.Fore;
            canvas.DrawText(Title ?? string.Empty, 8 * Dpi, 17 * Dpi, _boldFont, _text);
            DrawLegend(canvas, p);
            canvas.Restore();

            // Axis
            canvas.Save();
            canvas.ClipRect(new SKRect(ChartLeft, 0, ChartRight, HeaderHeight));
            var tickTop = HeaderHeight - 5 * Dpi;
            foreach (var t in ticks)
            {
                var x = TimeToX(t);
                _stroke.Color = p.GridStrong;
                canvas.DrawLine(x, tickTop, x, HeaderHeight, _stroke);
                // Skip a label that would be cut in half by the edge of the chart
                var tickLabel = TickLabel(t, step);
                var half = _smallFont.MeasureText(tickLabel, (SKPaint)null) / 2;
                if (x - half < ChartLeft || x + half > ChartRight) continue;
                _text.Color = p.Fore;
                canvas.DrawText(tickLabel, x, HeaderHeight - 10 * Dpi, SKTextAlign.Center, _smallFont, _text);
            }

            // Upper tier: the date, kept at the left edge until the next one pushes it along
            var upperY = 17 * Dpi;
            _text.Color = p.Fore;
            if (step < TimeSpan.FromDays(1))
            {
                var days = new List<DateTime> { _viewFrom.Date };
                for (var d = _viewFrom.Date.AddDays(1); d <= _viewTo; d = d.AddDays(1)) days.Add(d);
                for (var i = 0; i < days.Count; i++)
                {
                    var label = days[i].ToString("ddd dd MMM yyyy");
                    var labelWidth = _boldFont.MeasureText(label, (SKPaint)null);
                    var x = Math.Max(ChartLeft + 6 * Dpi, TimeToX(days[i]) + 6 * Dpi);
                    if (i + 1 < days.Count) x = Math.Min(x, TimeToX(days[i + 1]) - labelWidth - 12 * Dpi);
                    canvas.DrawText(label, x, upperY, _boldFont, _text);
                }
            }
            else
            {
                canvas.DrawText(_viewFrom.ToString("MMMM yyyy"), ChartLeft + 6 * Dpi, upperY, _boldFont, _text);
            }

            // The span in view, on the right
            var span = FormatDuration(_viewTo - _viewFrom);
            var zoomed = _viewFrom != _periodFrom || _viewTo != _periodTo;
            _text.Color = zoomed ? p.Accent : p.Dim;
            canvas.DrawText(zoomed ? $"Zoomed: {span}" : span, ChartRight - 6 * Dpi, upperY, SKTextAlign.Right, _smallFont, _text);
            canvas.Restore();

            // Rubber band while dragging out a range to zoom to
            if (_drag == DragMode.ZoomRange && _dragMoved)
            {
                var x1 = Math.Clamp(Math.Min(_dragStart.X, _mouse.X), ChartLeft, ChartRight);
                var x2 = Math.Clamp(Math.Max(_dragStart.X, _mouse.X), ChartLeft, ChartRight);
                var band = new SKRect(x1, 0, x2, RowsBottom);
                _fill.Color = p.Accent.WithAlpha(45);
                canvas.DrawRect(band, _fill);
                _stroke.Color = p.Accent;
                _stroke.StrokeWidth = 1;
                canvas.DrawRect(band, _stroke);
                var label = FormatDuration(XToTime(x2) - XToTime(x1));
                DrawPill(canvas, label, (x1 + x2) / 2, RowsTop + 14 * Dpi, p.Accent, SKColors.White);
            }
        }

        private void DrawLegend(SKCanvas canvas, Palette p)
        {
            _legendRects.Clear();
            var x = 8 * Dpi;
            var y = 36 * Dpi;
            // A hidden status stays in the legend even when nothing of it matches, so it can be shown again
            foreach (var status in LegendOrder.Where(s => _legendCounts.ContainsKey(s) || _hiddenStatuses.Contains(s)))
            {
                var hidden = _hiddenStatuses.Contains(status);
                var text = _legendCounts.GetValueOrDefault(status).ToString("N0");
                var textWidth = _smallFont.MeasureText(text, (SKPaint)null);
                var rect = new SKRect(x - 2 * Dpi, y - 10 * Dpi, x + 14 * Dpi + textWidth + 4 * Dpi, y + 4 * Dpi);
                var color = TimelineBar.StatusColor(status).ToSKColor();
                _fill.Color = hidden ? color.WithAlpha(60) : color;
                canvas.DrawRoundRect(new SKRect(x, y - 8 * Dpi, x + 10 * Dpi, y + 2 * Dpi), 2 * Dpi, 2 * Dpi, _fill);
                _text.Color = hidden ? p.Dim.WithAlpha(120) : p.Fore;
                canvas.DrawText(text, x + 14 * Dpi, y, _smallFont, _text);
                if (hidden)
                {
                    _stroke.Color = p.Dim;
                    _stroke.StrokeWidth = 1;
                    canvas.DrawLine(rect.Left, rect.MidY, rect.Right, rect.MidY, _stroke);
                }
                _legendRects.Add((rect, status));
                x = rect.Right + 8 * Dpi;
            }
        }

        private RunStatus? LegendHit(Point pt)
        {
            foreach (var (rect, status) in _legendRects)
            {
                if (rect.Contains(pt.X, pt.Y)) return status;
            }
            return null;
        }

        #endregion Axis

        #region Rows

        private void DrawBar(SKCanvas canvas, Palette p, TimelineBar bar)
        {
            var rect = BarRect(bar);
            if (rect.Right < ChartLeft || rect.Left > ChartRight) return;

            var color = TimelineBar.StatusColor(ShownStatus(bar)).ToSKColor();
            var selectionActive = _selectedBar != null;
            var sameJob = selectionActive && SameJob(bar, _selectedBar);
            if (selectionActive && !sameJob) color = color.WithAlpha(90);
            if (bar == _hoverBar) color = Blend(color, SKColors.White, 0.2f);

            var radius = 3 * Dpi;
            _fill.Color = color;
            canvas.DrawRoundRect(rect, radius, radius, _fill);

            if (bar.IsRunning)
            {
                // Diagonal stripes for work still in progress
                canvas.Save();
                canvas.ClipRoundRect(new SKRoundRect(rect, radius), antialias: true);
                _stroke.Color = SKColors.White.WithAlpha(60);
                _stroke.StrokeWidth = 3 * Dpi;
                var gap = 8 * Dpi;
                // Only across the part in view: a long running job zoomed in can be millions of stripes wide, every frame
                var from = Math.Max(rect.Left, ChartLeft - rect.Height);
                var stripesFrom = rect.Left - rect.Height + (float)Math.Floor((from - rect.Left) / gap) * gap;
                var stripesTo = Math.Min(rect.Right, ChartRight);
                for (var x = stripesFrom; x < stripesTo; x += gap)
                {
                    canvas.DrawLine(x, rect.Bottom, x + rect.Height, rect.Top, _stroke);
                }
                canvas.Restore();
            }

            // Fade the end that runs past the selected period
            var fadeWidth = Math.Min(14 * Dpi, rect.Width / 2);
            if (bar.Start < _periodFrom) FadeEdge(canvas, p, rect, fadeWidth, true);
            if (bar.End > _periodTo && !bar.IsRunning) FadeEdge(canvas, p, rect, fadeWidth, false);

            if (bar == _selectedBar || bar == _hoverBar)
            {
                _stroke.Color = bar == _selectedBar ? p.Fore : color;
                _stroke.StrokeWidth = 2 * Dpi;
                var outline = rect;
                outline.Inflate(1.5f * Dpi, 1.5f * Dpi);
                canvas.DrawRoundRect(outline, radius + 1, radius + 1, _stroke);
            }

            // Duration inside the bar when there's room
            var label = bar.IsRunning ? "Running " + FormatDuration(TimeSpan.FromSeconds(bar.DurationSec))
                : FormatDuration(TimeSpan.FromSeconds(bar.DurationSec)) + (bar.Executions > 1 ? $" ×{bar.Executions:N0}" : "");
            var visibleLeft = Math.Max(rect.Left, ChartLeft);
            var visibleRight = Math.Min(rect.Right, ChartRight);
            var textWidth = _smallFont.MeasureText(label, (SKPaint)null);
            if (visibleRight - visibleLeft >= textWidth + 8 * Dpi)
            {
                _text.Color = selectionActive && !sameJob ? SKColors.White.WithAlpha(150) : SKColors.White;
                canvas.DrawText(label, visibleLeft + 4 * Dpi, CenterBaseline(rect, _smallFont), _smallFont, _text);
            }
        }

        private static void FadeEdge(SKCanvas canvas, Palette p, SKRect rect, float width, bool left)
        {
            var edge = left ? new SKRect(rect.Left, rect.Top, rect.Left + width, rect.Bottom) : new SKRect(rect.Right - width, rect.Top, rect.Right, rect.Bottom);
            var start = left ? new SKPoint(edge.Left, 0) : new SKPoint(edge.Right, 0);
            var end = left ? new SKPoint(edge.Right, 0) : new SKPoint(edge.Left, 0);
            using var shader = SKShader.CreateLinearGradient(start, end, new[] { p.Back.WithAlpha(200), p.Back.WithAlpha(0) }, SKShaderTileMode.Clamp);
            using var paint = new SKPaint { Shader = shader, IsAntialias = true };
            canvas.DrawRect(edge, paint);
        }

        private void DrawLabels(SKCanvas canvas, Palette p, List<TimelineRowModel> rows)
        {
            _fill.Color = p.Panel;
            canvas.DrawRect(new SKRect(0, RowsTop, LabelWidth, RowsBottom), _fill);
            var hasGroups = _model.Roots.Any(r => r.IsGroup);
            var focusRow = _canvas.Focused && _exportHeight == null ? FocusRow : null;

            foreach (var row in rows)
            {
                var top = RowScreenTop(row);
                var lane = new SKRect(0, top + RowPadding, LabelWidth, top + RowPadding + LaneHeight);
                if (row == _hoverRow)
                {
                    _fill.Color = p.HoverRow;
                    canvas.DrawRect(new SKRect(0, top, LabelWidth, top + row.Height), _fill);
                }
                // The keyboard's row, while the chart has focus (not in a copied image)
                if (row == focusRow)
                {
                    _stroke.Color = p.Accent;
                    _stroke.StrokeWidth = 1.5f * Dpi;
                    canvas.DrawRect(new SKRect(1.5f * Dpi, top + 1.5f * Dpi, LabelWidth - 1.5f * Dpi, top + row.Height - 1.5f * Dpi), _stroke);
                }
                var x = 8 * Dpi + (hasGroups ? 18 * Dpi : 0) + row.Depth * 14 * Dpi;

                if (row.IsGroup) DrawChevron(canvas, p, 12 * Dpi, lane.MidY, row.Collapsed);

                // Failures badge: for a collapsed group, include its steps.  An instance's own bars are summary spans, so
                // it counts its executions when collapsed and leaves the count to its jobs when expanded.
                var failures = _hiddenStatuses.Contains(RunStatus.Failed) ? 0
                    : row.Kind == TimelineRowKind.Instance ? (row.Collapsed || row.CanLoadChildren ? row.StatusCounts.GetValueOrDefault(RunStatus.Failed) : 0)
                    : RowFailures(row);
                var right = LabelWidth - 8 * Dpi;
                if (failures > 0)
                {
                    var badge = failures.ToString("N0");
                    var w = _smallFont.MeasureText(badge, (SKPaint)null) + 10 * Dpi;
                    var pill = new SKRect(right - w, lane.MidY - 8 * Dpi, right, lane.MidY + 8 * Dpi);
                    _fill.Color = TimelineBar.StatusColor(RunStatus.Failed).ToSKColor();
                    canvas.DrawRoundRect(pill, pill.Height / 2, pill.Height / 2, _fill);
                    _text.Color = SKColors.White;
                    canvas.DrawText(badge, pill.MidX, CenterBaseline(pill, _smallFont), SKTextAlign.Center, _smallFont, _text);
                    right = pill.Left - 6 * Dpi;
                }

                // Loading or failed to load the jobs of an expanded instance
                var status = row.Loading ? "loading…" : row.LoadError != null ? "failed to load" : null;
                if (status != null)
                {
                    var w = _smallFont.MeasureText(status, (SKPaint)null);
                    _text.Color = row.LoadError != null ? TimelineBar.StatusColor(RunStatus.Failed).ToSKColor() : p.Dim;
                    canvas.DrawText(status, right - w, CenterBaseline(lane, _smallFont), _smallFont, _text);
                    right -= w + 6 * Dpi;
                }

                var font = row.IsGroup || (row.Parent == null && hasGroups) ? _boldFont : _font;
                _text.Color = row.Parent != null ? Blend(p.Fore, p.Back, 0.15f) : p.Fore;
                canvas.DrawText(Ellipsize(row.Label, font, right - x), x, CenterBaseline(lane, font), font, _text);

                if (row.Collapsed)
                {
                    _text.Color = p.Dim;
                    var stepCount = row.Children.Count;
                    if (row.Height >= LaneHeight * 2)
                        canvas.DrawText($"{stepCount} steps hidden", x, CenterBaseline(lane, _smallFont) + LaneHeight, _smallFont, _text);
                }
            }

            // Splitter
            _stroke.Color = _drag == DragMode.LabelSplitter || IsOnSplitter(_mouse) ? p.Accent : p.GridStrong;
            _stroke.StrokeWidth = _drag == DragMode.LabelSplitter ? 2 * Dpi : 1;
            canvas.DrawLine(LabelWidth, RowsTop, LabelWidth, RowsBottom, _stroke);
        }

        private void DrawChevron(SKCanvas canvas, Palette p, float cx, float cy, bool collapsed)
        {
            var s = 4 * Dpi;
            using var path = new SKPath();
            if (collapsed)
            {
                path.MoveTo(cx - s / 2, cy - s);
                path.LineTo(cx + s / 2 + 1, cy);
                path.LineTo(cx - s / 2, cy + s);
            }
            else
            {
                path.MoveTo(cx - s, cy - s / 2);
                path.LineTo(cx, cy + s / 2 + 1);
                path.LineTo(cx + s, cy - s / 2);
            }
            _stroke.Color = p.Fore;
            _stroke.StrokeWidth = 1.6f * Dpi;
            _stroke.StrokeCap = SKStrokeCap.Round;
            canvas.DrawPath(path, _stroke);
            _stroke.StrokeCap = SKStrokeCap.Butt;
        }

        private static string Ellipsize(string text, SKFont font, float maxWidth)
        {
            if (maxWidth <= 0) return string.Empty;
            if (font.MeasureText(text, (SKPaint)null) <= maxWidth) return text;
            var count = (int)font.BreakText(text, maxWidth - font.MeasureText("…", (SKPaint)null), (SKPaint)null);
            return count <= 0 ? "…" : text[..Math.Min(count, text.Length)] + "…";
        }

        private static float CenterBaseline(SKRect rect, SKFont font) => rect.MidY - (font.Metrics.Ascent + font.Metrics.Descent) / 2;

        #endregion Rows

        #region Overlays

        private void DrawNowLine(SKCanvas canvas, Palette p)
        {
            var now = DateTime.UtcNow.ToAppTimeZone();
            if (now < _viewFrom || now > _viewTo) return;
            var x = TimeToX(now);
            using var dash = SKPathEffect.CreateDash(new[] { 4 * Dpi, 3 * Dpi }, 0);
            _stroke.Color = DashColors.Red.ToSKColor();
            _stroke.StrokeWidth = 1.5f * Dpi;
            _stroke.PathEffect = dash;
            canvas.DrawLine(x, RowsTop, x, RowsBottom, _stroke);
            _stroke.PathEffect = null;
        }

        private void DrawCursor(SKCanvas canvas, Palette p)
        {
            if (!_mouseInside || _drag is DragMode.LabelSplitter or DragMode.ScrollThumb or DragMode.Overview) return;
            if (_mouse.X < ChartLeft || _mouse.X > ChartRight || _mouse.Y < 0 || _mouse.Y > RowsBottom) return;

            using var dash = SKPathEffect.CreateDash(new[] { 2 * Dpi, 3 * Dpi }, 0);
            _stroke.Color = p.Fore.WithAlpha(110);
            _stroke.StrokeWidth = 1;
            _stroke.PathEffect = dash;
            canvas.DrawLine(_mouse.X, HeaderHeight, _mouse.X, RowsBottom, _stroke);
            _stroke.PathEffect = null;

            var t = XToTime(_mouse.X);
            var format = ViewTicks < TimeSpan.FromHours(6).Ticks ? "HH:mm:ss" : "dd MMM HH:mm";
            DrawPill(canvas, t.ToString(format), _mouse.X, HeaderHeight - 14 * Dpi, p.Fore, p.Back);

            // How many were running then, on the band
            if (ShowConcurrency && ConcurrencyAt(t) is int running)
            {
                DrawPill(canvas, running == 1 ? "1 job running" : $"{running:N0} jobs running", _mouse.X, HeaderHeight + BandHeight / 2, p.Accent, SKColors.White);
            }
        }

        /// <summary>
        /// How many jobs were running at once: a column per slice, as tall as the most running at any moment in it, scaled
        /// to the highest in view so the shape stays readable when zoomed in.
        /// </summary>
        private void DrawConcurrencyBand(SKCanvas canvas, Palette p, List<DateTime> ticks)
        {
            var top = HeaderHeight;
            var bottom = RowsTop;
            _fill.Color = p.Panel;
            canvas.DrawRect(new SKRect(0, top, _canvas.Width, bottom), _fill);
            _stroke.Color = p.GridStrong;
            _stroke.StrokeWidth = 1;
            canvas.DrawLine(0, bottom, _canvas.Width, bottom, _stroke);

            // The slices in view, and the highest of them for the scale
            var first = _bandPeaks.Length == 0 ? 0 : Math.Max(0, BandSliceAt(_viewFrom < _bandEdges[0] ? _bandEdges[0] : _viewFrom));
            var last = _bandPeaks.Length == 0 ? -1 : _viewTo >= _bandEdges[^1] ? _bandPeaks.Length - 1 : BandSliceAt(_viewTo);
            var max = 0;
            for (var i = first; i <= last; i++) max = Math.Max(max, _bandPeaks[i]);

            _text.Color = p.Dim;
            canvas.DrawText("Running at once", 8 * Dpi, top + 15 * Dpi, _smallFont, _text);
            if (_bandPeaks.Length > 0)
            {
                _text.Color = p.Fore;
                canvas.DrawText($"peak {max:N0}", 8 * Dpi, top + 29 * Dpi, _smallFont, _text);
            }

            canvas.Save();
            canvas.ClipRect(new SKRect(ChartLeft, top, ChartRight, bottom));
            _stroke.Color = p.Grid;
            foreach (var t in ticks)
            {
                var x = TimeToX(t);
                canvas.DrawLine(x, top, x, bottom, _stroke);
            }
            if (max > 0)
            {
                var height = bottom - top - 6 * Dpi;
                _fill.Color = p.Accent.WithAlpha(170);
                for (var i = first; i <= last; i++)
                {
                    if (_bandPeaks[i] == 0) continue;
                    var x1 = TimeToX(_bandEdges[i]);
                    var x2 = Math.Max(x1 + 1, TimeToX(_bandEdges[i + 1]));
                    var h = Math.Max(1.5f * Dpi, height * _bandPeaks[i] / max);
                    canvas.DrawRect(new SKRect(x1, bottom - h, x2, bottom), _fill);
                }
            }
            canvas.Restore();
        }

        /// <summary>A note centred along the bottom of the rows - over the bars, but where the rows usually run out first.</summary>
        private void DrawBanner(SKCanvas canvas, Palette p, string text)
        {
            var pad = 8 * Dpi;
            var label = Ellipsize(text, _smallFont, ChartWidth - pad * 4);
            var w = _smallFont.MeasureText(label, (SKPaint)null) + pad * 2;
            var h = 20 * Dpi;
            var left = ChartLeft + (ChartWidth - w) / 2;
            var rect = new SKRect(left, RowsBottom - 6 * Dpi - h, left + w, RowsBottom - 6 * Dpi);
            _fill.Color = p.TipBack.WithAlpha(230);
            canvas.DrawRoundRect(rect, h / 2, h / 2, _fill);
            _text.Color = p.TipFore;
            canvas.DrawText(label, rect.MidX, CenterBaseline(rect, _smallFont), SKTextAlign.Center, _smallFont, _text);
        }

        private void DrawPill(SKCanvas canvas, string text, float cx, float cy, SKColor back, SKColor fore)
        {
            var w = _smallFont.MeasureText(text, (SKPaint)null) + 12 * Dpi;
            var h = 17 * Dpi;
            var left = Math.Clamp(cx - w / 2, ChartLeft, Math.Max(ChartLeft, ChartRight - w));
            var rect = new SKRect(left, cy - h / 2, left + w, cy + h / 2);
            _fill.Color = back;
            canvas.DrawRoundRect(rect, h / 2, h / 2, _fill);
            _text.Color = fore;
            canvas.DrawText(text, rect.MidX, CenterBaseline(rect, _smallFont), SKTextAlign.Center, _smallFont, _text);
        }

        private SKImage _overviewImage;
        private (int Version, int Width, int Height, SKColor Back) _overviewKey;

        /// <summary>
        /// Every bar of every row, drawn small - kept as an image and drawn again only when the rows, the strip's size or
        /// the theme change.  Drawn every frame it cost time in proportion to all the bars, on every mouse move.
        /// </summary>
        private SKImage OverviewImage(SKRect strip, Palette p)
        {
            var key = (_layoutVersion, (int)strip.Width, (int)strip.Height, p.Back);
            if (_overviewImage != null && key == _overviewKey) return _overviewImage;
            _overviewImage?.Dispose();
            _overviewImage = null;
            _overviewKey = key;
            if (key.Item2 <= 0 || key.Item3 <= 0) return null;

            using var surface = SKSurface.Create(new SKImageInfo(key.Item2, key.Item3));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var local = new SKRect(0, 0, key.Item2, key.Item3);
            if (_rows.Count > 0)
            {
                var rowHeight = Math.Max(1f, local.Height / _rows.Count);
                for (var i = 0; i < _rows.Count; i++)
                {
                    var y = local.Height * i / _rows.Count;
                    foreach (var bar in _rows[i].Bars)
                    {
                        if (!IsVisible(bar)) continue;
                        var x1 = PeriodTimeToX(bar.Start < _periodFrom ? _periodFrom : bar.Start, local);
                        var x2 = Math.Max(x1 + 1, PeriodTimeToX(bar.End > _periodTo ? _periodTo : bar.End, local));
                        _fill.Color = TimelineBar.StatusColor(ShownStatus(bar)).ToSKColor().WithAlpha(200);
                        canvas.DrawRect(new SKRect(x1, y, x2, y + rowHeight), _fill);
                    }
                }
            }
            _overviewImage = surface.Snapshot();
            return _overviewImage;
        }

        private void DrawOverview(SKCanvas canvas, Palette p)
        {
            _fill.Color = p.Header;
            canvas.DrawRect(new SKRect(0, RowsBottom, _canvas.Width, CanvasHeight), _fill);
            _stroke.Color = p.GridStrong;
            _stroke.StrokeWidth = 1;
            canvas.DrawLine(0, RowsBottom, _canvas.Width, RowsBottom, _stroke);

            var strip = OverviewRect;
            _fill.Color = p.Back;
            canvas.DrawRect(strip, _fill);

            _text.Color = p.Dim;
            canvas.DrawText(_periodFrom.ToString("g"), 8 * Dpi, CenterBaseline(strip, _smallFont), _smallFont, _text);

            var image = OverviewImage(strip, p);
            if (image != null) canvas.DrawImage(image, strip.Left, strip.Top);

            // Dim what's out of view, and outline what's in it
            var vx1 = PeriodTimeToX(_viewFrom, strip);
            var vx2 = Math.Max(vx1 + 2 * Dpi, PeriodTimeToX(_viewTo, strip));
            var vy1 = strip.Top;
            var vy2 = strip.Bottom;
            if (_contentHeight > RowsHeight && _contentHeight > 0)
            {
                vy1 = strip.Top + strip.Height * (_scrollY / _contentHeight);
                vy2 = strip.Top + strip.Height * Math.Min(1, (_scrollY + RowsHeight) / _contentHeight);
            }
            var view = new SKRect(vx1, vy1, vx2, vy2);
            _fill.Color = p.Back.WithAlpha(150);
            using (var outside = new SKPath())
            {
                outside.FillType = SKPathFillType.EvenOdd;
                outside.AddRect(strip);
                outside.AddRect(view);
                canvas.DrawPath(outside, _fill);
            }
            _stroke.Color = p.Accent;
            _stroke.StrokeWidth = 1.5f * Dpi;
            canvas.DrawRect(view, _stroke);
        }

        private void DrawScrollBar(SKCanvas canvas, Palette p)
        {
            var thumb = ScrollThumbRect;
            if (thumb.IsEmpty) return;
            var active = _drag == DragMode.ScrollThumb || (_mouseInside && _mouse.X >= ChartRight && _mouse.Y >= RowsTop && _mouse.Y < RowsBottom);
            _fill.Color = p.Fore.WithAlpha((byte)(active ? 140 : 70));
            canvas.DrawRoundRect(thumb, thumb.Width / 2, thumb.Width / 2, _fill);
        }

        private void DrawTooltip(SKCanvas canvas, Palette p, TimelineBar bar, int width, int height)
        {
            var lines = BarDetails(bar).ToList();
            var pad = 8 * Dpi;
            var lineHeight = 17 * Dpi;
            var titleHeight = 24 * Dpi;
            var labelWidth = lines.Max(l => _smallFont.MeasureText(l.Item1, (SKPaint)null)) + 10 * Dpi;
            var valueWidth = lines.Max(l => _font.MeasureText(l.Item2, (SKPaint)null));
            var titleWidth = Math.Min(_boldFont.MeasureText(bar.JobName, (SKPaint)null), 420 * Dpi);
            var hint = bar.IsSummary ? (bar.Row.Collapsed ? "Double-click to show the instance's jobs" : null)
                : ActivateHint && BarActivated != null && bar.IsSingleExecution ? $"Double-click for {BarActivatedText.ToLowerInvariant()}" : null;
            var hintWidth = hint == null ? 0 : _smallFont.MeasureText(hint, (SKPaint)null);
            var w = Math.Max(Math.Max(titleWidth, hintWidth), labelWidth + valueWidth) + pad * 2;
            var h = titleHeight + (lines.Count + (hint == null ? 0 : 1)) * lineHeight + pad;

            var x = _mouse.X + 16 * Dpi;
            var y = _mouse.Y + 18 * Dpi;
            if (x + w > width - 4) x = _mouse.X - w - 12 * Dpi;
            if (y + h > height - 4) y = _mouse.Y - h - 10 * Dpi;
            x = Math.Max(4, x);
            y = Math.Max(4, y);
            var rect = new SKRect(x, y, x + w, y + h);
            var radius = 5 * Dpi;

            using (var shadow = new SKPaint { IsAntialias = true, Color = p.TipBack, ImageFilter = SKImageFilter.CreateDropShadow(0, 2 * Dpi, 4 * Dpi, 4 * Dpi, SKColors.Black.WithAlpha(90)) })
            {
                canvas.DrawRoundRect(rect, radius, radius, shadow);
            }

            var title = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + titleHeight);
            canvas.Save();
            canvas.ClipRoundRect(new SKRoundRect(rect, radius), antialias: true);
            _fill.Color = TimelineBar.StatusColor(ShownStatus(bar)).ToSKColor();
            canvas.DrawRect(title, _fill);
            canvas.Restore();
            _text.Color = SKColors.White;
            canvas.DrawText(Ellipsize(bar.JobName, _boldFont, w - pad * 2), rect.Left + pad, CenterBaseline(title, _boldFont), _boldFont, _text);

            var lineY = title.Bottom + pad / 2;
            foreach (var (label, value) in lines)
            {
                var lineRect = new SKRect(rect.Left, lineY, rect.Right, lineY + lineHeight);
                _text.Color = p.TipFore.WithAlpha(170);
                canvas.DrawText(label, rect.Left + pad, CenterBaseline(lineRect, _smallFont), _smallFont, _text);
                _text.Color = p.TipFore;
                canvas.DrawText(value, rect.Left + pad + labelWidth, CenterBaseline(lineRect, _font), _font, _text);
                lineY += lineHeight;
            }
            if (hint != null)
            {
                var hintRect = new SKRect(rect.Left, lineY, rect.Right, lineY + lineHeight);
                _text.Color = p.TipFore.WithAlpha(150);
                canvas.DrawText(hint, rect.Left + pad, CenterBaseline(hintRect, _smallFont), _smallFont, _text);
            }
        }

        #endregion Overlays
    }
}
