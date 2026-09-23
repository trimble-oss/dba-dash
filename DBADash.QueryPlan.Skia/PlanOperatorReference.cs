using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// A page showing every operator: what it does, its icon, and the path data or SVG the icon is
    /// drawn from - for readers learning the operators, for reviewing the set, and for redrawing one.
    ///
    /// Built from the same data the renderer draws with - the symbols, the map from operator to
    /// symbol, the chip colours and the chip geometry - so it cannot drift from what a plan shows.
    /// It is written by the DBADash.QueryPlan.Reference project when that builds, into the git
    /// ignored build folder, so it is always current and never checked in.
    ///
    /// Plain HTML with inline SVG: the symbols are SVG path data already, so a browser draws them
    /// exactly, at any size, with no renderer involved.
    /// </summary>
    public static class PlanOperatorReference
    {
        /// <summary>The page, as HTML.</summary>
        public static string BuildHtml(DateTime? generated = null)
        {
            var style = new PlanRenderStyle();
            var chipSize = new PlanLayoutOptions().IconSize;
            var light = PlanPalette.Light();
            var dark = PlanPalette.Dark();

            // Every kind with a picture, including the fallback, in category order so the chip colours
            // come in blocks.
            var kinds = Enum.GetValues<PlanOperatorKind>()
                .OrderBy(PlanOperatorClassifier.CategoryOf)
                .ThenBy(kind => kind)
                .ToList();

            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            html.AppendLine("<title>Query plan operators</title>");
            html.AppendLine("<style>");
            html.AppendLine(":root {");
            AppendThemeVariables(html, light);
            html.AppendLine("  --grid: #E4E7EB; --grid-major: #C3C9D1; --accent-ink: #C77C00;");
            html.AppendLine("}");
            html.AppendLine("body.dark {");
            AppendThemeVariables(html, dark);
            html.AppendLine("  --grid: #33373D; --grid-major: #4C525A; --accent-ink: #FFD36A;");
            html.AppendLine("}");
            html.AppendLine(Css);
            html.AppendLine("</style></head><body>");

            // Recolours a tinted SVG icon the way the renderer does - black to the symbol colour, red
            // to the accent - so the page shows it as a plan draws it, not as the file draws itself.
            // One filter per theme: a filter's matrix is numbers, and cannot follow a CSS variable.
            html.Append("<svg width=\"0\" height=\"0\" style=\"position:absolute\">");
            AppendTwoToneFilter(html, "twotone-light", light);
            AppendTwoToneFilter(html, "twotone-dark", dark);
            html.AppendLine("</svg>");

            html.AppendLine("<header>");
            html.AppendLine("<h1>Query plan operators</h1>");
            html.AppendLine("<p>Every operator in the DBA Dash query plan viewer: what it does, and the icon it is drawn with. The descriptions are the ones the viewer shows on each operator's tooltip and in the properties panel.</p>");
            html.Append("<p class=\"muted\">The icons are SVG files in <code>DBADash.QueryPlan.Skia/Icons</code> where there is one, and otherwise ");
            html.Append("SVG path data on a 24 unit grid in <code>DBADash.QueryPlan.Skia/PlanOperatorGlyphs.cs</code>, ");
            html.Append("drawn as ").Append(Number(style.IconStrokeWidth)).Append(" unit lines on a chip in the operator's category colour. ");
            html.AppendLine("Edit either and rebuild <code>DBADash.QueryPlan.Reference</code> to see the change here.</p>");
            html.Append("<p class=\"muted\">To draw an operator from an SVG, add <code>Icons/&lt;Kind&gt;.svg</code> to the renderer project - ");
            html.Append("recoloured onto the chip like a glyph, its black in the symbol colour and its red in the accent - ");
            html.Append("or <code>Icons/&lt;Kind&gt;.color.svg</code>, drawn in its own colours with no chip. ");
            html.AppendLine("<code>&lt;Kind&gt;</code> is the operator's <code>PlanOperatorKind</code>, shown on each card below.</p>");

            if (PlanOperatorIcons.Problems.Count > 0)
            {
                html.AppendLine("<div class=\"problems\"><strong>Icon files not in use</strong><ul>");
                foreach (var problem in PlanOperatorIcons.Problems)
                {
                    html.Append("<li>").Append(Encode(problem)).AppendLine("</li>");
                }

                html.AppendLine("</ul></div>");
            }
            html.Append("<p class=\"muted\">Generated ")
                .Append(WebUtility.HtmlEncode((generated ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)))
                .AppendLine(". The application maps its own theme colours onto the chips; these are the viewer's default palettes.</p>");
            html.AppendLine("<button type=\"button\" onclick=\"document.body.classList.toggle('dark')\">Light / dark</button>");
            html.AppendLine("</header>");

            // The whole set at a glance, at the size a plan draws it.
            html.AppendLine("<h2>At a glance</h2>");
            html.AppendLine("<div class=\"overview\">");
            foreach (var kind in kinds)
            {
                html.Append("<div class=\"tile\">");
                html.Append(Chip(kind, chipSize, chipSize, style));
                html.Append("<span>").Append(Encode(PlanOperatorNames.For(kind, null))).Append("</span>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");

            // Then each one in detail, by category.
            foreach (var category in kinds.Select(PlanOperatorClassifier.CategoryOf).Distinct())
            {
                html.Append("<h2><span class=\"swatch\" style=\"background:var(--cat-").Append(category).Append(")\"></span>")
                    .Append(Encode(CategoryName(category))).AppendLine("</h2>");
                html.AppendLine("<div class=\"cards\">");

                foreach (var kind in kinds.Where(k => PlanOperatorClassifier.CategoryOf(k) == category))
                {
                    AppendCard(html, kind, kinds, chipSize, style);
                }

                html.AppendLine("</div>");
            }

            html.AppendLine("</body></html>");
            return html.ToString();
        }

        private static void AppendCard(
            StringBuilder html,
            PlanOperatorKind kind,
            IReadOnlyList<PlanOperatorKind> kinds,
            double chipSize,
            PlanRenderStyle style)
        {
            var glyph = PlanOperatorGlyphs.For(kind);

            // Symbols are shared on purpose; saying which others use this one is what stops a
            // redraw for one operator silently changing another.
            var sharedWith = kinds
                .Where(other => other != kind &&
                                PlanOperatorGlyphs.HasGlyph(other) &&
                                PlanOperatorIcons.SvgFor(other) is null &&
                                ReferenceEquals(PlanOperatorGlyphs.For(other), glyph))
                .Select(other => PlanOperatorNames.For(other, null))
                .ToList();

            var svg = PlanOperatorIcons.SvgFor(kind);

            html.AppendLine("<div class=\"card\">");

            // An SVG has no grid of ours to read coordinates off, so it is shown as its file draws it.
            html.Append("<div class=\"drawing\">")
                .Append(svg is null
                    ? GridView(glyph, style)
                    : "<img class=\"svgfile\" width=\"208\" height=\"208\" alt=\"\" src=\"" + DataUri(svg) + "\">")
                .AppendLine("</div>");

            html.Append("<div class=\"sizes\">");
            html.Append(Chip(kind, chipSize, chipSize, style));
            html.Append(Chip(kind, chipSize * 2, chipSize, style));
            html.Append(Chip(kind, chipSize * 4, chipSize, style));
            html.AppendLine("</div>");

            html.AppendLine("<div class=\"text\">");
            html.Append("<h3>").Append(Encode(PlanOperatorNames.For(kind, null))).AppendLine("</h3>");
            html.Append("<p class=\"muted\"><code>PlanOperatorKind.").Append(kind).Append("</code> &middot; ")
                .Append(Encode(CategoryName(PlanOperatorClassifier.CategoryOf(kind)))).AppendLine("</p>");

            // What the operator does comes first: it is what most readers open the page for.  Where
            // the kind covers physical operators that each do something different, each is listed.
            html.Append("<p class=\"description\">").Append(Encode(PlanOperatorDescriptions.For(kind))).AppendLine("</p>");

            var variants = PlanOperatorDescriptions.VariantsOf(kind);
            if (variants.Count > 0)
            {
                html.AppendLine("<dl class=\"variants\">");
                foreach (var (physicalOp, description) in variants)
                {
                    html.Append("<dt>").Append(Encode(physicalOp)).Append("</dt><dd>").Append(Encode(description)).AppendLine("</dd>");
                }

                html.AppendLine("</dl>");
            }

            if (svg is not null)
            {
                html.Append("<p class=\"note\">Icon: <code>Icons/").Append(Encode(svg.FileName)).Append("</code>, ")
                    .Append(svg.Style == PlanIconStyle.FullColourSvg
                        ? "drawn in its own colours, with no chip."
                        : "recoloured onto the chip: black in the symbol colour, red in the accent.")
                    .Append(" Replaces the <strong>").Append(Encode(glyph.Name))
                    .AppendLine("</strong> glyph, which is drawn again if the file is removed or stops loading.</p>");
                html.Append("<details><summary>SVG</summary><pre>").Append(Encode(svg.Source)).AppendLine("</pre></details>");
            }
            else
            {
                html.Append("<p class=\"note\">Icon: the <strong>").Append(Encode(glyph.Name)).Append("</strong> glyph");

                if (!PlanOperatorGlyphs.HasGlyph(kind))
                {
                    html.Append(", the fallback for an operator with no symbol of its own");
                }
                else if (sharedWith.Count > 0)
                {
                    html.Append(", shared with ").Append(Encode(string.Join(", ", sharedWith)));
                }

                html.AppendLine(".</p>");

                html.Append("<details><summary>Path data</summary><p class=\"note\">").Append(Encode(glyph.Meaning)).Append("</p><pre>stroke: ").Append(Encode(glyph.Stroke));
                if (glyph.Fill is not null) html.Append("\nfill:   ").Append(Encode(glyph.Fill));
                if (glyph.Accent is not null) html.Append("\naccent: ").Append(Encode(glyph.Accent));
                html.AppendLine("</pre></details>");
            }

            html.AppendLine("</div></div>");
        }

        /// <summary>
        /// An icon as a plan draws it: the category chip, the symbol inset on it and scaled to fit,
        /// drawn at <paramref name="size"/> pixels from a chip of <paramref name="chipSize"/> layout
        /// units - the same geometry as <see cref="PlanRenderer"/>.
        /// </summary>
        private static string Chip(PlanOperatorKind kind, double size, double chipSize, PlanRenderStyle style)
        {
            var glyph = PlanOperatorGlyphs.For(kind);
            var icon = PlanOperatorIcons.SvgFor(kind);
            var inset = style.IconSymbolInset;
            var scale = (chipSize - (inset * 2)) / PlanOperatorGlyphs.GlyphExtent;
            var category = PlanOperatorClassifier.CategoryOf(kind);

            var svg = new StringBuilder();
            svg.Append("<svg class=\"chip\" width=\"").Append(Number(size)).Append("\" height=\"").Append(Number(size))
                .Append("\" viewBox=\"0 0 ").Append(Number(chipSize)).Append(' ').Append(Number(chipSize)).Append("\">");

            // A full colour SVG is the whole icon - no chip, filling the square, as the renderer draws it.
            if (icon is { Style: PlanIconStyle.FullColourSvg })
            {
                svg.Append("<image href=\"").Append(DataUri(icon)).Append("\" width=\"").Append(Number(chipSize))
                    .Append("\" height=\"").Append(Number(chipSize)).Append("\"/></svg>");
                return svg.ToString();
            }

            svg.Append("<rect width=\"").Append(Number(chipSize)).Append("\" height=\"").Append(Number(chipSize))
                .Append("\" rx=\"").Append(Number(style.IconCornerRadius)).Append("\" style=\"fill:var(--cat-").Append(category).Append(")\"/>");

            if (icon is not null)
            {
                svg.Append("<image href=\"").Append(DataUri(icon)).Append("\" x=\"").Append(Number(inset)).Append("\" y=\"").Append(Number(inset))
                    .Append("\" width=\"").Append(Number(chipSize - (inset * 2))).Append("\" height=\"").Append(Number(chipSize - (inset * 2)))
                    .Append("\" class=\"twotone\"/></svg>");
                return svg.ToString();
            }

            svg.Append("<g transform=\"translate(").Append(Number(inset)).Append(' ').Append(Number(inset))
                .Append(") scale(").Append(Number(scale)).Append(")\">");
            AppendSymbol(svg, glyph, style.IconStrokeWidth, "var(--symbol)", "var(--accent)");
            svg.Append("</g></svg>");
            return svg.ToString();
        }

        /// <summary>
        /// The symbol large, on its 24 unit grid with a heavier line every four units, for reading
        /// coordinates off while redrawing it.  Drawn at the same line weight it has on a chip.
        /// </summary>
        private static string GridView(PlanGlyph glyph, PlanRenderStyle style)
        {
            const int extent = (int)PlanOperatorGlyphs.GlyphExtent;

            var svg = new StringBuilder();
            svg.Append("<svg width=\"208\" height=\"208\" viewBox=\"-2.5 -2.5 27.5 27.5\">");

            for (var i = 0; i <= extent; i++)
            {
                var major = i % 4 == 0;
                var colour = major ? "var(--grid-major)" : "var(--grid)";
                var width = major ? "0.08" : "0.04";

                svg.Append("<line x1=\"").Append(i).Append("\" y1=\"0\" x2=\"").Append(i).Append("\" y2=\"24\" style=\"stroke:")
                    .Append(colour).Append(";stroke-width:").Append(width).Append("\"/>");
                svg.Append("<line x1=\"0\" y1=\"").Append(i).Append("\" x2=\"24\" y2=\"").Append(i).Append("\" style=\"stroke:")
                    .Append(colour).Append(";stroke-width:").Append(width).Append("\"/>");

                // Coordinates along the top and left, so a point can be read off rather than counted.
                if (!major) continue;

                svg.Append("<text x=\"").Append(i).Append("\" y=\"-0.7\" text-anchor=\"middle\" class=\"axis\">").Append(i).Append("</text>");
                svg.Append("<text x=\"-0.6\" y=\"").Append(Number(i + 0.4)).Append("\" text-anchor=\"end\" class=\"axis\">").Append(i).Append("</text>");
            }

            AppendSymbol(svg, glyph, style.IconStrokeWidth, "var(--ink)", "var(--accent-ink)");
            svg.Append("</svg>");
            return svg.ToString();
        }

        private static void AppendSymbol(StringBuilder svg, PlanGlyph glyph, float strokeWidth, string colour, string accent)
        {
            if (glyph.Fill is not null)
            {
                svg.Append("<path d=\"").Append(Encode(glyph.Fill)).Append("\" style=\"fill:").Append(colour).Append("\"/>");
            }

            AppendStroke(svg, glyph.Stroke, strokeWidth, colour);

            // Over the rest, as the renderer draws it.
            if (glyph.Accent is not null) AppendStroke(svg, glyph.Accent, strokeWidth, accent);
        }

        private static void AppendStroke(StringBuilder svg, string data, float strokeWidth, string colour)
        {
            svg.Append("<path d=\"").Append(Encode(data)).Append("\" style=\"fill:none;stroke:").Append(colour)
                .Append(";stroke-width:").Append(Number(strokeWidth))
                .Append(";stroke-linecap:round;stroke-linejoin:round\"/>");
        }

        private static void AppendThemeVariables(StringBuilder html, PlanPalette palette)
        {
            html.Append("  --bg: ").Append(Hex(palette.Background)).Append("; --card: ").Append(Hex(palette.NodeFill))
                .Append("; --border: ").Append(Hex(palette.NodeBorder)).Append("; --text: ").Append(Hex(palette.TitleText))
                .Append("; --muted: ").Append(Hex(palette.DetailText)).Append("; --ink: ").Append(Hex(palette.TitleText))
                .Append("; --symbol: ").Append(Hex(palette.IconSymbol)).Append("; --accent: ").Append(Hex(palette.IconAccent)).AppendLine(";");

            foreach (var category in Enum.GetValues<PlanOperatorCategory>())
            {
                html.Append("  --cat-").Append(category).Append(": ").Append(Hex(palette.IconFor(category))).AppendLine(";");
            }
        }

        private static string CategoryName(PlanOperatorCategory category) => PlanOperatorClassifier.CategoryName(category);

        /// <summary>
        /// The renderer's two tone recolouring as an SVG filter: the same matrix, taking how red a
        /// mark is from the symbol colour to the accent.  In sRGB, as Skia does it, rather than the
        /// linear light SVG filters default to.
        /// </summary>
        private static void AppendTwoToneFilter(StringBuilder html, string id, PlanPalette palette)
        {
            static string Row(byte from, byte to)
            {
                var start = from / 255.0;
                var span = (to - from) / 255.0;
                return Number(span) + " " + Number(-span) + " 0 0 " + Number(start);
            }

            var symbol = palette.IconSymbol;
            var accent = palette.IconAccent;

            html.Append("<filter id=\"").Append(id).Append("\" color-interpolation-filters=\"sRGB\"><feColorMatrix type=\"matrix\" values=\"")
                .Append(Row(symbol.Red, accent.Red)).Append(' ')
                .Append(Row(symbol.Green, accent.Green)).Append(' ')
                .Append(Row(symbol.Blue, accent.Blue)).Append(" 0 0 0 1 0")
                .Append("\"/></filter>");
        }

        /// <summary>An SVG icon as a data URI, so the page stays one self-contained file.</summary>
        private static string DataUri(PlanSvgIcon icon) =>
            "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(icon.Source));

        private static string Hex(SKColor colour) =>
            "#" + colour.Red.ToString("X2", CultureInfo.InvariantCulture) +
            colour.Green.ToString("X2", CultureInfo.InvariantCulture) +
            colour.Blue.ToString("X2", CultureInfo.InvariantCulture);

        private static string Number(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private const string Css = """
            body { font-family: "Segoe UI", system-ui, sans-serif; background: var(--bg); color: var(--text); margin: 24px 32px; line-height: 1.5; }
            h1 { font-size: 24px; font-weight: 600; margin: 0 0 8px; }
            h2 { font-size: 18px; font-weight: 600; margin: 32px 0 12px; display: flex; align-items: center; gap: 10px; }
            h3 { font-size: 16px; font-weight: 600; margin: 0; }
            p { margin: 4px 0; max-width: 900px; }
            .muted { color: var(--muted); font-size: 13px; }
            .note { font-size: 13px; color: var(--muted); font-style: italic; }
            .description { margin: 8px 0; }
            .variants { margin: 8px 0; font-size: 14px; }
            .variants dt { font-weight: 600; margin-top: 6px; }
            .variants dd { margin: 0 0 0 16px; }
            code, pre { font-family: Consolas, "Cascadia Mono", monospace; font-size: 12px; }
            pre { white-space: pre-wrap; word-break: break-all; margin: 6px 0 0; }
            button { margin-top: 8px; padding: 4px 12px; background: var(--card); color: var(--text); border: 1px solid var(--border); border-radius: 4px; cursor: pointer; }
            .swatch { width: 14px; height: 14px; border-radius: 3px; display: inline-block; }
            .overview { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 6px 16px; }
            .tile { display: flex; align-items: center; gap: 10px; font-size: 13px; }
            .cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(560px, 1fr)); gap: 12px; }
            .card { display: flex; gap: 16px; align-items: flex-start; background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 12px; }
            .sizes { display: flex; flex-direction: column; align-items: center; gap: 8px; }
            .text { flex: 1; min-width: 0; }
            details summary { cursor: pointer; font-size: 13px; color: var(--muted); margin-top: 6px; }
            svg { display: block; flex: none; }
            .axis { font-size: 1.1px; fill: var(--muted); font-family: Consolas, monospace; }
            .svgfile { border: 1px solid var(--border); border-radius: 4px; padding: 8px; box-sizing: border-box; background: var(--card); }
            .twotone { filter: url(#twotone-light); }
            body.dark .twotone { filter: url(#twotone-dark); }
            .problems { margin: 12px 0; padding: 8px 12px; border: 1px solid #D9942E; border-left-width: 4px; border-radius: 4px; max-width: 900px; font-size: 13px; }
            .problems ul { margin: 4px 0 0; padding-left: 20px; }
            """;
    }
}
