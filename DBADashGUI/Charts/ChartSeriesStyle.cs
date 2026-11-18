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
        /// Width of a line series' line and marker outlines.  Null for the width the theme gives lines.  Only
        /// applies with <see cref="Color"/>, as the line is repainted in that colour - and only to lines.
        /// </summary>
        public double? StrokeThickness { get; init; }

        /// <summary>
        /// Draw a line series' line dashed - for a reference line (the usual, a target) that shouldn't read as data.  Only applies with
        /// <see cref="Color"/>, as with <see cref="StrokeThickness"/>.
        /// </summary>
        public bool Dashed { get; init; }

        /// <summary>
        /// Fill a line series' area see-through at LiveCharts' default level, so a line over it still reads.  Only applies with <see cref="Color"/>.
        /// Stacked areas and columns keep the full colour, as LiveCharts draws them by default.
        /// </summary>
        public bool TranslucentFill { get; init; }

        /// <summary>
        /// Leave a line series' marker centre to the theme, as LiveCharts draws it by default (near white, with the coloured outline), rather than
        /// filling it with <see cref="Color"/>.  Only applies with <see cref="Color"/>.
        /// </summary>
        public bool ThemeMarkerFill { get; init; }

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
