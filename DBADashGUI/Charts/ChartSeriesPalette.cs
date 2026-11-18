using System.Drawing;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// The colours of the series on a chart, in the order they're shown.  These are LiveCharts' own default colours (its Material Design 500
    /// palette, as <c>LiveCharts.Configure(s => s.UseDefaults())</c> gives them), so a chart looks as it does by default and a comparison
    /// line can be drawn in its series' colour as the chart is drawn - the theme only gives colours once the chart is painted.
    /// </summary>
    internal static class ChartSeriesPalette
    {
        private static readonly Color[] Colours =
        {
            Color.FromArgb(0x21, 0x96, 0xF3), // #2196F3
            Color.FromArgb(0xF4, 0x43, 0x36), // #F44336
            Color.FromArgb(0x8B, 0xC3, 0x4A), // #8BC34A
            Color.FromArgb(0x00, 0xBC, 0xD4), // #00BCD4
            Color.FromArgb(0x3F, 0x51, 0xB5), // #3F51B5
            Color.FromArgb(0xFF, 0xC1, 0x07), // #FFC107
            Color.FromArgb(0x00, 0x96, 0x88), // #009688
            Color.FromArgb(0xE9, 0x1E, 0x63), // #E91E63
            Color.FromArgb(0x60, 0x7D, 0x8B), // #607D8B
        };

        /// <summary>The colour of the series at <paramref name="index"/>, cycling through the palette as LiveCharts does</summary>
        public static Color ColourAt(int index) => Colours[index % Colours.Length];
    }
}
