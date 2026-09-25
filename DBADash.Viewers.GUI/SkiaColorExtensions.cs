using SkiaSharp;

namespace DBADashGUI
{
    public static class SkiaColorExtensions
    {
        public static SKColor ToSKColor(this Color color) => new SKColor(color.R, color.G, color.B, color.A);
    }
}
