using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DBADash.QueryPlan.Model;
using SkiaSharp;
using Svg.Skia;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>How an operator's icon is drawn.  For an SVG, the file name decides.</summary>
    public enum PlanIconStyle
    {
        /// <summary>The operator's glyph, from <see cref="PlanOperatorGlyphs"/>.</summary>
        Glyph,

        /// <summary>
        /// An SVG recoloured onto the category chip, exactly like a glyph: every mark it makes is
        /// drawn in the chip's symbol colour, whatever colours the file uses.  So it follows the
        /// theme and fits the set.  Named for the operator: <c>ComputeScalar.svg</c>.
        /// </summary>
        TintedSvg,

        /// <summary>
        /// An SVG drawn in its own colours, filling the icon's square with no chip - an SSMS style
        /// icon.  It does not follow the theme.  Named <c>ComputeScalar.color.svg</c>, or
        /// <c>.colour.svg</c>.
        /// </summary>
        FullColourSvg
    }

    /// <summary>An SVG icon standing in for an operator's glyph.</summary>
    public sealed class PlanSvgIcon
    {
        internal PlanSvgIcon(PlanOperatorKind kind, PlanIconStyle style, string fileName, string source, SKSvg svg)
        {
            Kind = kind;
            Style = style;
            FileName = fileName;
            Source = source;
            Svg = svg;
        }

        public PlanOperatorKind Kind { get; }

        public PlanIconStyle Style { get; }

        /// <summary>The file it came from, e.g. <c>ComputeScalar.color.svg</c>.</summary>
        public string FileName { get; }

        /// <summary>The SVG itself, for the operator reference page.</summary>
        public string Source { get; }

        /// <summary>Kept for the life of the icon: the picture belongs to it.</summary>
        private SKSvg Svg { get; }

        /// <summary>The drawing, in the SVG's own coordinates - see <see cref="SKPicture.CullRect"/>.</summary>
        internal SKPicture Picture => Svg.Picture!;
    }

    /// <summary>
    /// SVG icons that replace an operator's glyph, so an icon that is easier to draw in a design tool
    /// than to write as path data - or one that wants colour - can be dropped in without touching
    /// the rest of the set.
    ///
    /// The SVGs are the files in the renderer project's Icons folder, embedded in the assembly.  Each
    /// is named after a <see cref="PlanOperatorKind"/>, and the name also says how it is drawn - see
    /// <see cref="PlanIconStyle"/> and Icons\README.md.  An operator with no SVG keeps its glyph, and
    /// so does one whose SVG does not load: a bad file costs its own icon, never the plan.  What went
    /// wrong is kept in <see cref="Problems"/>, which the tests and the operator reference both report.
    ///
    /// Loaded once, the first time an icon is asked for.
    /// </summary>
    public static class PlanOperatorIcons
    {
        /// <summary>The prefix the project file embeds the icons under.</summary>
        internal const string ResourcePrefix = "PlanIcons.";

        private static readonly object LoadLock = new();

        private static IconSet? _icons;

        /// <summary>The SVG drawn for a kind, or null when it is drawn with its glyph.</summary>
        public static PlanSvgIcon? SvgFor(PlanOperatorKind kind) =>
            Icons.ByKind.TryGetValue(kind, out var icon) ? icon : null;

        public static PlanIconStyle StyleFor(PlanOperatorKind kind) => SvgFor(kind)?.Style ?? PlanIconStyle.Glyph;

        /// <summary>
        /// Icon files that are not being used, and why: a name that is not an operator kind, an SVG
        /// that did not load, two files for one operator.  Empty when every file is in use.
        /// </summary>
        public static IReadOnlyList<string> Problems => Icons.Problems;

        /// <summary>
        /// The kind and style a file name stands for, or null when it names no operator kind.  Case
        /// insensitive, since nobody should have to remember the capitals of an enum to add an icon.
        /// </summary>
        internal static (PlanOperatorKind Kind, PlanIconStyle Style)? ParseFileName(string fileName)
        {
            if (!fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return null;

            var name = fileName[..^4];
            var style = PlanIconStyle.TintedSvg;

            foreach (var suffix in new[] { ".color", ".colour" })
            {
                if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;

                name = name[..^suffix.Length];
                style = PlanIconStyle.FullColourSvg;
                break;
            }

            // Enum.TryParse would also take "7" as a kind; an icon file is only ever named.
            if (name.Length == 0 || char.IsDigit(name[0])) return null;

            return Enum.TryParse<PlanOperatorKind>(name, ignoreCase: true, out var kind) &&
                   Enum.IsDefined(kind)
                ? (kind, style)
                : null;
        }

        /// <summary>
        /// Load one icon from its file name and contents.  Null, with the reason added to
        /// <paramref name="problems"/>, when the name or the SVG is not usable.
        /// </summary>
        internal static PlanSvgIcon? Load(string fileName, string source, ICollection<string> problems)
        {
            if (ParseFileName(fileName) is not { } parsed)
            {
                problems.Add($"{fileName}: not named after an operator kind, so no operator uses it.");
                return null;
            }

            var (kind, style) = parsed;

            SKSvg? svg = null;
            try
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(source));
                svg = SKSvg.CreateFromStream(stream);

                var bounds = svg.Picture?.CullRect ?? SKRect.Empty;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    problems.Add($"{fileName}: loaded, but has no size to draw - give it a viewBox or a width and height.");
                    svg.Dispose();
                    return null;
                }

                return new PlanSvgIcon(kind, style, fileName, source, svg);
            }
            catch (Exception ex)
            {
                svg?.Dispose();
                problems.Add($"{fileName}: did not load as SVG ({ex.GetType().Name}: {ex.Message}).");
                return null;
            }
        }

        /// <summary>
        /// Replace the loaded icons with <paramref name="icons"/> until the result is disposed - for
        /// tests of how an SVG is drawn, without shipping a test icon in the product.
        /// </summary>
        internal static IDisposable OverrideForTesting(params PlanSvgIcon[] icons)
        {
            lock (LoadLock)
            {
                var previous = _icons;
                _icons = new IconSet(icons.ToDictionary(i => i.Kind), []);
                return new Restore(previous);
            }
        }

        private static IconSet Icons
        {
            get
            {
                lock (LoadLock)
                {
                    return _icons ??= LoadEmbedded();
                }
            }
        }

        private static IconSet LoadEmbedded()
        {
            var assembly = typeof(PlanOperatorIcons).Assembly;
            var byKind = new Dictionary<PlanOperatorKind, PlanSvgIcon>();
            var problems = new List<string>();

            // Sorted so a clash between two files for one operator is settled the same way every time.
            foreach (var resource in assembly.GetManifestResourceNames()
                         .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                         .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = resource[ResourcePrefix.Length..];

                if (Load(fileName, Read(assembly, resource), problems) is not { } icon) continue;

                if (byKind.TryGetValue(icon.Kind, out var existing))
                {
                    problems.Add($"{fileName}: {existing.FileName} is already the icon for {icon.Kind}; only one is used.");
                    continue;
                }

                byKind[icon.Kind] = icon;
            }

            return new IconSet(byKind, problems);
        }

        private static string Read(Assembly assembly, string resource)
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private sealed record IconSet(IReadOnlyDictionary<PlanOperatorKind, PlanSvgIcon> ByKind, IReadOnlyList<string> Problems);

        private sealed class Restore(IconSet? previous) : IDisposable
        {
            public void Dispose()
            {
                lock (LoadLock)
                {
                    _icons = previous;
                }
            }
        }
    }
}
