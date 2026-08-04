using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DBADashGUI.Controls
{
    /// <summary>
    /// Modern "card" style container: rounded corners, a subtle border and a coloured left accent bar.
    /// Reusable wherever a card-style callout is needed (e.g. insights / advice boxes).
    /// </summary>
    public sealed class InsightCard : Panel
    {
        public const int AccentWidth = 5;
        private const int Radius = 8;

        // Icon geometry: a fixed-size icon painted at the top-left of the content area.
        public const int IconSize = 18;
        private const int IconLeftPadding = 10;
        private const int IconTopPadding = 12;

        /// <summary>The severity glyph drawn at the top-left of the card.</summary>
        public enum CardIcon
        {
            None,
            Information,
            Warning,
            Critical
        }

        /// <summary>Total horizontal space the painted icon (plus its padding) reserves on the left.</summary>
        public static int IconGutter => IconLeftPadding + IconSize + 10;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CardIcon Icon { get; set; } = CardIcon.None;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FillColor { get; set; } = DashColors.BluePale;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = DashColors.Information;

        public InsightCard()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Blend the rounded-corner gaps into the parent so the card floats cleanly.
            g.Clear(Parent?.BackColor ?? BackColor);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, Radius);

            using (var fill = new SolidBrush(FillColor))
            {
                g.FillPath(fill, path);
            }

            // Coloured accent bar down the left edge, clipped to the rounded shape.
            var oldClip = g.Clip;
            g.SetClip(path);
            using (var accent = new SolidBrush(AccentColor))
            {
                g.FillRectangle(accent, 0, 0, AccentWidth, Height);
            }
            g.Clip = oldClip;

            using (var border = new Pen(Color.FromArgb(60, AccentColor)))
            {
                g.DrawPath(border, path);
            }

            DrawIcon(g);

            base.OnPaint(e);
        }

        /// <summary>
        /// Draw a crisp, vector severity icon (circle-i / warning triangle / no-entry) so the appearance is
        /// consistent regardless of the system font's emoji presentation.
        /// </summary>
        private void DrawIcon(Graphics g)
        {
            if (Icon == CardIcon.None) return;

            var bounds = new Rectangle(AccentWidth + IconLeftPadding, IconTopPadding, IconSize, IconSize);
            var color = AccentColor;

            switch (Icon)
            {
                case CardIcon.Information:
                    using (var brush = new SolidBrush(color))
                    {
                        g.FillEllipse(brush, bounds);
                        // "i": a dot and a stem in the card's pale fill colour.
                        using var glyph = new SolidBrush(FillColor);
                        var cx = bounds.X + bounds.Width / 2f;
                        var dotSize = bounds.Width * 0.16f;
                        g.FillEllipse(glyph, cx - dotSize / 2f, bounds.Y + bounds.Height * 0.22f, dotSize, dotSize);
                        var stemW = bounds.Width * 0.16f;
                        g.FillRectangle(glyph, cx - stemW / 2f, bounds.Y + bounds.Height * 0.44f, stemW, bounds.Height * 0.34f);
                    }
                    break;

                case CardIcon.Warning:
                    using (var brush = new SolidBrush(color))
                    {
                        using var tri = new GraphicsPath();
                        var top = new PointF(bounds.X + bounds.Width / 2f, bounds.Y);
                        var bl = new PointF(bounds.X, bounds.Bottom);
                        var br = new PointF(bounds.Right, bounds.Bottom);
                        tri.AddLines(new[] { top, br, bl });
                        tri.CloseFigure();
                        g.FillPath(brush, tri);
                        // "!" in the card's pale fill colour.
                        using var glyph = new SolidBrush(FillColor);
                        var cx = bounds.X + bounds.Width / 2f;
                        var stemW = bounds.Width * 0.14f;
                        g.FillRectangle(glyph, cx - stemW / 2f, bounds.Y + bounds.Height * 0.34f, stemW, bounds.Height * 0.34f);
                        var dotSize = bounds.Width * 0.14f;
                        g.FillEllipse(glyph, cx - dotSize / 2f, bounds.Y + bounds.Height * 0.76f, dotSize, dotSize);
                    }
                    break;

                case CardIcon.Critical:
                    using (var brush = new SolidBrush(color))
                    {
                        g.FillEllipse(brush, bounds);
                        // Horizontal "no entry" bar in the pale fill colour.
                        using var glyph = new SolidBrush(FillColor);
                        var barH = bounds.Height * 0.18f;
                        g.FillRectangle(glyph, bounds.X + bounds.Width * 0.22f, bounds.Y + bounds.Height / 2f - barH / 2f,
                            bounds.Width * 0.56f, barH);
                    }
                    break;
            }
        }


        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Matches inline markdown links: [text](url)
        private static readonly Regex MarkdownLinkRegex =
            new(@"\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)\)", RegexOptions.Compiled);

        /// <summary>
        /// Prefix in the url position of a markdown link that denotes an in-app action rather than a web URL.
        /// e.g. <c>[session 55](action:open-session)</c> invokes the action registered under the key "open-session".
        /// </summary>
        public const string ActionScheme = "action:";

        /// <summary>
        /// Build a <see cref="LinkLabel"/> that renders text containing minimal markdown-style
        /// inline links (<c>[text](url)</c>) as clickable links. Text without links renders as a
        /// plain label.
        /// <para>
        /// A link whose url starts with <see cref="ActionScheme"/> (e.g. <c>action:my-key</c>) runs the
        /// matching delegate from <paramref name="actions"/> instead of opening a browser, allowing generic
        /// in-app navigation. Any other url is opened in the default browser.
        /// </para>
        /// Reusable wherever a callout needs inline hyperlinks or actions (e.g. insight cards).
        /// </summary>
        public static LinkLabel CreateContentLabel(string markdown, IReadOnlyDictionary<string, Action> actions = null, IReadOnlyDictionary<string, string> tooltips = null)
        {
            var link = new TooltipLinkLabel(tooltips)
            {
                AutoSize = true,
                Margin = new Padding(0),
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = DashColors.LinkColor
            };

            var builder = new StringBuilder();
            var linkSpans = new List<(int Start, int Length, string Url)>();
            var lastIndex = 0;
            foreach (Match match in MarkdownLinkRegex.Matches(markdown ?? string.Empty))
            {
                builder.Append(markdown, lastIndex, match.Index - lastIndex);
                var start = builder.Length;
                var text = match.Groups["text"].Value;
                builder.Append(text);
                linkSpans.Add((start, text.Length, match.Groups["url"].Value));
                lastIndex = match.Index + match.Length;
            }
            if (markdown != null && lastIndex < markdown.Length)
            {
                builder.Append(markdown, lastIndex, markdown.Length - lastIndex);
            }

            link.Text = builder.ToString();
            // LinkLabel auto-creates a default link spanning the entire text. Clear it so text without
            // markdown links renders as plain (non-clickable) text and only the parsed spans are links.
            link.Links.Clear();
            foreach (var span in linkSpans)
            {
                link.Links.Add(span.Start, span.Length, span.Url);
            }

            link.LinkClicked += (_, e) =>
            {
                if (e.Link?.LinkData is not string url) return;
                if (url.StartsWith(ActionScheme, StringComparison.OrdinalIgnoreCase))
                {
                    var key = url.Substring(ActionScheme.Length);
                    if (actions != null && actions.TryGetValue(key, out var action))
                    {
                        action?.Invoke();
                    }
                }
                else
                {
                    CommonShared.OpenURL(url);
                }
            };

            // Show a tooltip while hovering over a link whose url has associated tooltip text.
            // Handled inside TooltipLinkLabel, which has access to the protected PointInLink hit-testing.

            return link;
        }

        /// <summary>
        /// LinkLabel that shows a per-link tooltip on hover. Uses the protected <see cref="LinkLabel.PointInLink"/>
        /// hit-testing (only accessible from a derived type) to show the tooltip for the specific link under the mouse.
        /// </summary>
        private sealed class TooltipLinkLabel : LinkLabel
        {
            private readonly IReadOnlyDictionary<string, string> tooltips;
            private readonly ToolTip toolTip;
            private string shownForUrl;

            public TooltipLinkLabel(IReadOnlyDictionary<string, string> tooltips)
            {
                this.tooltips = tooltips;
                if (tooltips != null && tooltips.Count > 0)
                {
                    toolTip = new ToolTip { AutoPopDelay = 30000, InitialDelay = 300, ReshowDelay = 100 };
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (toolTip == null) return;

                var url = PointInLink(e.X, e.Y)?.LinkData as string;
                if (url != null && tooltips.TryGetValue(url, out var tip))
                {
                    if (shownForUrl != url)
                    {
                        shownForUrl = url;
                        toolTip.Show(tip, this, e.X + 12, e.Y + 16, toolTip.AutoPopDelay);
                    }
                }
                else if (shownForUrl != null)
                {
                    shownForUrl = null;
                    toolTip.Hide(this);
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                shownForUrl = null;
                toolTip?.Hide(this);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    toolTip?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
