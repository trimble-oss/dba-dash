using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Drawing;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// The marker drawn at each data point.  Default leaves the marker LiveCharts would draw for the
    /// series type - a circle - so a chart that says nothing about markers looks as it always did.
    /// </summary>
    public enum ChartMarkers
    {
        Default,
        Circle,
        Square,
        Diamond,
        Cross,
        Star
    }

    /// <summary>
    /// Overrides the colour and marker LiveCharts would pick for one series.
    ///
    /// The theme's palette is right for a chart of several comparable series, where the colours only have
    /// to be told apart.  It is wrong where the series means something on its own - deadlocks are a fault,
    /// and the theme's first colour for them is the same friendly blue everything else opens with - so a
    /// chart can name the colour and shape it wants and leave the rest to the palette.
    /// </summary>
    public record ChartSeriesStyle
    {
        /// <summary>
        /// Series colour as an HTML colour - "#AB1F26" or a known name.  Stored as text rather than a
        /// <see cref="Color"/> so that a report's chart configuration round-trips through JSON.  An
        /// unparseable value leaves the series on the theme colour rather than failing the chart.
        /// </summary>
        public string Color { get; init; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChartMarkers Marker { get; init; } = ChartMarkers.Default;

        /// <summary>
        /// Convenience for report definitions, which have the palette as <see cref="Color"/> values:
        /// <c>ChartSeriesStyle.FromColor(DashColors.Fail, ChartMarkers.Square)</c>.
        /// </summary>
        public static ChartSeriesStyle FromColor(System.Drawing.Color color, ChartMarkers marker = ChartMarkers.Default) =>
            new() { Color = ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(color.R, color.G, color.B)), Marker = marker };

        [JsonIgnore]
        internal SKColor? SkiaColor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Color)) return null;
                try
                {
                    var color = ColorTranslator.FromHtml(Color);
                    return new SKColor(color.R, color.G, color.B, color.A);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ChartSeriesStyle: '{Color}' is not a colour - {ex.Message}");
                    return null;
                }
            }
        }
    }
}
