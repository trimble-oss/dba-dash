using DBADashSharedGUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Drawing;

namespace DBADashGUI.CustomReports
{
    /// <summary>How a data bar sits in its cell.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataBarStyle
    {
        /// <summary>A thin bar along the bottom of the cell, under the figure - the plan viewer's bar.</summary>
        Underline,

        /// <summary>A pale bar the height of the cell, behind the figure - a spreadsheet's data bar.</summary>
        Fill
    }

    /// <summary>How a data bar is coloured.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataBarColorMode
    {
        /// <summary>One colour, whatever the length.  The bar's length says it all.</summary>
        Solid,

        /// <summary>
        /// Green, then amber past <see cref="DataBarSettings.WarningThreshold"/>, then red past
        /// <see cref="DataBarSettings.CriticalThreshold"/> - for figures where more is worse (or, with
        /// <see cref="DataBarSettings.HigherIsBetter"/>, where less is).
        /// </summary>
        TrafficLight,

        /// <summary>A blend from <see cref="DataBarSettings.Color"/> at the bottom of the scale to <see cref="DataBarSettings.GradientEndColor"/> at the top.</summary>
        Gradient,

        /// <summary>
        /// Red for the bad side of zero and green for the good - a change, a difference, a gain or loss.  Positive is
        /// bad unless <see cref="DataBarSettings.HigherIsBetter"/>.
        /// </summary>
        PositiveNegative
    }

    /// <summary>
    /// A bar drawn in a numeric column's cells, its length the cell's value as a share of the column's
    /// range, so the big number stands out without reading every row.  Set on any grid from its context
    /// menu, and saved with a column in a custom report.
    /// </summary>
    public class DataBarSettings
    {
        public DataBarStyle Style { get; set; } = DataBarStyle.Underline;

        public DataBarColorMode ColorMode { get; set; } = DataBarColorMode.Solid;

        /// <summary>The bar colour for <see cref="DataBarColorMode.Solid"/>, and the start of a <see cref="DataBarColorMode.Gradient"/>.</summary>
        public Color Color { get; set; } = DashColors.BlueLight;

        /// <summary>The colour at the top of the scale for <see cref="DataBarColorMode.Gradient"/>.</summary>
        public Color GradientEndColor { get; set; } = DashColors.Fail;

        /// <summary>
        /// For <see cref="DataBarColorMode.TrafficLight"/>: the short bars are the bad ones, so the
        /// thresholds are measured down from the top of the scale rather than up from the bottom.
        /// For <see cref="DataBarColorMode.PositiveNegative"/>: positive values are the good ones.
        /// </summary>
        public bool HigherIsBetter { get; set; }

        /// <summary>
        /// For <see cref="DataBarColorMode.TrafficLight"/>: blue rather than green below the warning threshold, where
        /// a low figure is unremarkable rather than good - the plan viewer's cost bars.  Not saved.
        /// </summary>
        [JsonIgnore]
        internal bool NeutralBelowWarning { get; set; }

        /// <summary>The value an empty bar stands for.  Null takes the smaller of zero and the lowest value in the column.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Minimum { get; set; }

        /// <summary>The value a full bar stands for.  Null takes the highest value in the column.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Maximum { get; set; }

        /// <summary>
        /// For <see cref="DataBarColorMode.TrafficLight"/>: where a bar turns amber - a percentage of the scale, or with
        /// <see cref="ThresholdsAreValues"/>, a value.
        /// </summary>
        public decimal WarningThreshold { get; set; } = 20;

        /// <summary>
        /// For <see cref="DataBarColorMode.TrafficLight"/>: where a bar turns red - a percentage of the scale, or with
        /// <see cref="ThresholdsAreValues"/>, a value.
        /// </summary>
        public decimal CriticalThreshold { get; set; } = 50;

        /// <summary>
        /// For <see cref="DataBarColorMode.TrafficLight"/>: the thresholds are values in the column's own units - e.g. amber
        /// from 5ms of latency and red from 50ms - so the colour doesn't depend on the rest of the column, while the bar's
        /// length still does.  Where <see cref="HigherIsBetter"/>, a bar turns amber at or below the warning value and red at
        /// or below the critical one.
        /// </summary>
        public bool ThresholdsAreValues { get; set; }

        /// <summary>
        /// The colour of a bar for a value <paramref name="share"/> (0 to 1) of the way up the scale, for a value
        /// below zero where <paramref name="negative"/>.  <paramref name="value"/> is the value itself, which a
        /// traffic light with <see cref="ThresholdsAreValues"/> is coloured by.
        /// </summary>
        public Color ColorFor(double share, bool negative = false, double? value = null)
        {
            share = Math.Clamp(share, 0, 1);
            switch (ColorMode)
            {
                case DataBarColorMode.PositiveNegative:
                    return negative == HigherIsBetter ? DashColors.Fail : DashColors.Success;

                case DataBarColorMode.TrafficLight when ThresholdsAreValues && value.HasValue:
                    var v = value.Value;
                    var critical = (double)CriticalThreshold;
                    var warning = (double)WarningThreshold;
                    return (HigherIsBetter ? v <= critical : v >= critical) ? DashColors.Fail
                        : (HigherIsBetter ? v <= warning : v >= warning) ? DashColors.Warning
                        : NeutralBelowWarning ? DashColors.BlueLight : DashColors.Success;

                case DataBarColorMode.TrafficLight:
                    var measured = (HigherIsBetter ? 1 - share : share) * 100;
                    return measured >= (double)CriticalThreshold ? DashColors.Fail
                        : measured >= (double)WarningThreshold ? DashColors.Warning
                        : NeutralBelowWarning ? DashColors.BlueLight : DashColors.Success;

                case DataBarColorMode.Gradient:
                    return Blend(Color, GradientEndColor, share);

                default:
                    return Color;
            }
        }

        /// <summary>
        /// Make the bar a traffic light running the given way.  Value thresholds run the opposite way where higher is
        /// better - red at the lower value - so they swap round where they don't suit the new direction, keeping an
        /// amber band between them.
        /// </summary>
        public void SetTrafficLight(bool higherIsBetter)
        {
            ColorMode = DataBarColorMode.TrafficLight;
            HigherIsBetter = higherIsBetter;
            NeutralBelowWarning = false;
            if (ThresholdsAreValues && (higherIsBetter ? CriticalThreshold > WarningThreshold : WarningThreshold > CriticalThreshold))
            {
                (WarningThreshold, CriticalThreshold) = (CriticalThreshold, WarningThreshold);
            }
        }

        private static Color Blend(Color from, Color to, double amount) => Color.FromArgb(
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));

        public DataBarSettings Clone() => (DataBarSettings)MemberwiseClone();

        /// <summary>
        /// Blue, amber from a fifth of the scale, red from half - for figures where more is worse, like cost and time.
        /// The plan viewer's list bars use these colours.
        /// </summary>
        public static DataBarSettings MoreIsWorse() => new() { ColorMode = DataBarColorMode.TrafficLight, NeutralBelowWarning = true };
    }
}
