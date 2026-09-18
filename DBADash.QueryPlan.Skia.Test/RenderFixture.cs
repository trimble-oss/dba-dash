using System;
using System.IO;
using System.Reflection;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using DBADash.QueryPlan.Skia;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia.Test
{
    /// <summary>
    /// A plan laid out, a controller over it, and a surface to draw on - everything a rendering test
    /// needs, built the same way the real host builds it.
    ///
    /// Real fonts on purpose: this is the layer where the font metrics are the thing under test, and
    /// a fake measurer here would mean the layout was never checked against the text that is
    /// actually drawn.
    /// </summary>
    internal sealed class RenderFixture : IDisposable
    {
        public const string KeyLookupSeek = "KeyLookupSeek";

        public const string ParallelSpill = "ParallelSpill";

        public const string Batch = "Batch";

        public RenderFixture(string plan = ParallelSpill, int width = 1200, int height = 800, PlanLayoutOptions? options = null)
        {
            Fonts = new PlanFonts();
            Renderer = new PlanRenderer(Fonts);

            Statement = PlanParser.Parse(Xml(plan)).PrimaryStatement
                        ?? throw new InvalidOperationException("No statement in " + plan);

            Layout = new PlanLayoutEngine(new SkiaPlanTextMeasurer(Fonts), options).Layout(Statement);
            Controller = new PlanViewController(Layout);
            Controller.SetViewport(new LayoutSize(width, height));

            Surface = SKSurface.Create(new SKImageInfo(width, height));
        }

        public PlanFonts Fonts { get; }

        public PlanRenderer Renderer { get; }

        public PlanStatement Statement { get; }

        public PlanLayout Layout { get; }

        public PlanViewController Controller { get; }

        public SKSurface Surface { get; }

        public SKCanvas Canvas => Surface.Canvas;

        public void Render() => Renderer.Render(Canvas, Controller);

        /// <summary>
        /// The rendered pixels, for the assertions that can only be made about what came out - that
        /// something was drawn, that the background is the palette's, that a colour appears.
        /// </summary>
        public SKBitmap Snapshot()
        {
            using var image = Surface.Snapshot();
            return SKBitmap.FromImage(image);
        }

        /// <summary>How many pixels are not the background colour, as a rough "was anything drawn".</summary>
        public int DrawnPixelCount()
        {
            using var bitmap = Snapshot();
            var background = Renderer.Palette.Background;
            var count = 0;

            // Every fourth pixel in each direction: a sixteenth of the work for an answer that is
            // just as good for "is this blank".
            for (var x = 0; x < bitmap.Width; x += 4)
            {
                for (var y = 0; y < bitmap.Height; y += 4)
                {
                    if (bitmap.GetPixel(x, y) != background) count++;
                }
            }

            return count;
        }

        /// <summary>
        /// How many pixels are within <paramref name="tolerance"/> of a colour on every channel,
        /// counting every pixel - thin dashed lines are too sparse to sample.  Near rather than
        /// equal, because antialiasing blends the edges of everything drawn.
        /// </summary>
        public int PixelsNear(SKColor colour, int tolerance = 24)
        {
            using var bitmap = Snapshot();
            var count = 0;

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (Math.Abs(pixel.Red - colour.Red) <= tolerance &&
                        Math.Abs(pixel.Green - colour.Green) <= tolerance &&
                        Math.Abs(pixel.Blue - colour.Blue) <= tolerance)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static string Xml(string name)
        {
            var resourceName = $"Plans.{name}.sqlplan";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                               ?? throw new InvalidOperationException(
                                   $"The sample plan '{resourceName}' is not embedded.");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            Surface.Dispose();
            Renderer.Dispose();
            Fonts.Dispose();
        }
    }
}
