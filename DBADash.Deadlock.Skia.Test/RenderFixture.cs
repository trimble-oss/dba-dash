using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using SkiaSharp;

namespace DBADash.Deadlock.Skia.Test
{
    /// <summary>
    /// Builds the parse -> layout -> view -> render pipeline for a test, and renders it to a bitmap
    /// so assertions can be made about what actually reached the canvas.
    /// </summary>
    internal sealed class RenderFixture : IDisposable
    {
        public RenderFixture(string xml, DeadlockRenderStyle? style = null, DeadlockPalette? palette = null)
        {
            Style = style ?? new DeadlockRenderStyle();
            Fonts = new DeadlockFonts(Style);

            var graph = DeadlockParser.Parse(xml).First();
            var layout = new DeadlockLayoutEngine(new SkiaTextMeasurer(Fonts)).Layout(graph);

            Controller = new DeadlockViewController(layout);
            Renderer = new DeadlockRenderer(Fonts, palette ?? TestPalette.Distinct(), Style);
        }

        public DeadlockRenderStyle Style { get; }

        public DeadlockFonts Fonts { get; }

        public DeadlockViewController Controller { get; }

        public DeadlockRenderer Renderer { get; }

        public DeadlockLayout Layout => Controller.Layout;

        /// <summary>Render at the given surface size, having set the viewport to match.</summary>
        public SKBitmap Render(int width = 900, int height = 700)
        {
            Controller.SetViewport(new LayoutSize(width, height));

            var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);
            Renderer.Render(canvas, Controller);
            return bitmap;
        }

        /// <summary>Render with the whole graph fitted and centred, which is the usual starting view.</summary>
        public SKBitmap RenderFitted(int width = 900, int height = 700)
        {
            Controller.SetViewport(new LayoutSize(width, height));
            Controller.ZoomToFit();
            return Render(width, height);
        }

        public static string LoadGraph(string name)
        {
            var resourceName = $"Graphs.{name}.xdl";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded test graph '{resourceName}' was not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            Renderer.Dispose();
            Fonts.Dispose();
        }
    }

    /// <summary>
    /// A palette of fully opaque, widely separated colours.
    ///
    /// Opacity matters because the shipped palettes use translucency, which blends against whatever
    /// is underneath and makes "is this colour present" meaningless.  Separation matters because
    /// everything is antialiased: a glyph stem or a thin line rarely produces a pixel of exactly the
    /// requested colour, so assertions match within a tolerance.  Every channel is drawn from
    /// {0, 85, 170, 255}, keeping entries at least ~120 apart.
    ///
    /// Separation does NOT mean a colour can only appear where it was asked for.  The antialiased
    /// boundary between two entries sweeps the straight line between them, and that line can pass
    /// through a third entry - the ProcessBorder/ProcessFill edge, for instance, passes exactly
    /// through DetailText a third of the way along.  So a "this colour is present" assertion is
    /// sound, but "this colour is absent" is not: for those, compare pixel counts between two
    /// renders that differ only in the behaviour under test.
    /// </summary>
    internal static class TestPalette
    {
        /// <summary>
        /// Euclidean RGB distance within which a pixel counts as the requested colour.  Chosen well
        /// below the minimum separation between entries, and comfortably above the residual error on
        /// a mostly covered antialiased pixel.
        /// </summary>
        public const double Tolerance = 50;

        public static readonly SKColor Background = new(0x00, 0x00, 0x00);
        public static readonly SKColor ProcessFill = new(0xFF, 0x00, 0x00);
        public static readonly SKColor ProcessBorder = new(0x00, 0xFF, 0x00);
        public static readonly SKColor VictimFill = new(0x00, 0x00, 0xFF);
        public static readonly SKColor VictimBorder = new(0xFF, 0xFF, 0x00);
        public static readonly SKColor ResourceFill = new(0xFF, 0x00, 0xFF);
        public static readonly SKColor ResourceBorder = new(0x00, 0xFF, 0xFF);
        public static readonly SKColor TitleText = new(0xFF, 0xFF, 0xFF);
        public static readonly SKColor DetailText = new(0xAA, 0x55, 0x00);
        public static readonly SKColor Edge = new(0x00, 0xAA, 0x55);
        public static readonly SKColor CycleEdge = new(0x55, 0x00, 0xAA);
        public static readonly SKColor EdgeLabelText = new(0xAA, 0x00, 0x55);
        public static readonly SKColor EdgeLabelBackground = new(0x55, 0xAA, 0x00);
        public static readonly SKColor Selection = new(0x00, 0x55, 0xAA);
        public static readonly SKColor Hover = new(0xFF, 0xAA, 0x55);
        public static readonly SKColor OwnerHighlight = new(0x55, 0x55, 0xAA);
        public static readonly SKColor WaiterHighlight = new(0xAA, 0x55, 0x55);
        public static readonly SKColor TooltipBackground = new(0x55, 0xFF, 0xAA);
        public static readonly SKColor TooltipBorder = new(0xAA, 0x55, 0xFF);
        public static readonly SKColor TooltipTitleText = new(0xFF, 0x55, 0xAA);
        public static readonly SKColor TooltipLabelText = new(0xAA, 0xFF, 0x55);
        public static readonly SKColor TooltipValueText = new(0x55, 0xAA, 0xFF);

        public static DeadlockPalette Distinct() => new()
        {
            Background = Background,
            ProcessFill = ProcessFill,
            ProcessBorder = ProcessBorder,
            VictimFill = VictimFill,
            VictimBorder = VictimBorder,
            ResourceFill = ResourceFill,
            ResourceBorder = ResourceBorder,
            TitleText = TitleText,
            DetailText = DetailText,
            Edge = Edge,
            CycleEdge = CycleEdge,
            EdgeLabelText = EdgeLabelText,
            EdgeLabelBackground = EdgeLabelBackground,
            Selection = Selection,
            Hover = Hover,
            OwnerHighlight = OwnerHighlight,
            WaiterHighlight = WaiterHighlight,
            TooltipBackground = TooltipBackground,
            TooltipBorder = TooltipBorder,
            TooltipTitleText = TooltipTitleText,
            TooltipLabelText = TooltipLabelText,
            TooltipValueText = TooltipValueText
        };
    }

    internal static class BitmapAssertions
    {
        private static bool Matches(SKColor pixel, SKColor target)
        {
            double dr = pixel.Red - target.Red;
            double dg = pixel.Green - target.Green;
            double db = pixel.Blue - target.Blue;
            return Math.Sqrt((dr * dr) + (dg * dg) + (db * db)) <= TestPalette.Tolerance;
        }

        /// <summary>True when any pixel is the given colour, within the antialiasing tolerance.</summary>
        public static bool Contains(SKBitmap bitmap, SKColor colour) => Count(bitmap, colour) > 0;

        public static int Count(SKBitmap bitmap, SKColor colour)
        {
            var count = 0;
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (Matches(bitmap.GetPixel(x, y), colour)) count++;
                }
            }
            return count;
        }

        public static bool ContainsWithin(SKBitmap bitmap, LayoutRect region, SKColor colour)
        {
            var left = Math.Max(0, (int)Math.Floor(region.Left));
            var top = Math.Max(0, (int)Math.Floor(region.Top));
            var right = Math.Min(bitmap.Width - 1, (int)Math.Ceiling(region.Right));
            var bottom = Math.Min(bitmap.Height - 1, (int)Math.Ceiling(region.Bottom));

            for (var x = left; x <= right; x++)
            {
                for (var y = top; y <= bottom; y++)
                {
                    if (Matches(bitmap.GetPixel(x, y), colour)) return true;
                }
            }
            return false;
        }

        /// <summary>Vertical extent of the given colour, or null when it is not present.</summary>
        public static (int Top, int Bottom)? VerticalExtent(SKBitmap bitmap, SKColor colour)
        {
            var top = int.MaxValue;
            var bottom = int.MinValue;

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (!Matches(bitmap.GetPixel(x, y), colour)) continue;
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }

            return bottom < top ? null : (top, bottom);
        }
    }
}
