using System;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// Every colour the renderer uses, as <see cref="SKColor"/>.
    ///
    /// Deliberately not wired to any application theme: the host maps its own colours onto this,
    /// which keeps System.Drawing.Color (Windows only) on the host's side of the boundary and lets
    /// the renderer be exercised with a known palette in tests.
    /// </summary>
    public sealed class PlanPalette
    {
        public SKColor Background { get; set; }

        public SKColor NodeFill { get; set; }

        public SKColor NodeBorder { get; set; }

        /// <summary>The fill behind the operator glyph, per category - see <see cref="IconFor"/>.</summary>
        public SKColor RootIcon { get; set; }

        public SKColor DataAccessIcon { get; set; }

        public SKColor JoinIcon { get; set; }

        public SKColor TransformIcon { get; set; }

        public SKColor SpoolIcon { get; set; }

        public SKColor ParallelismIcon { get; set; }

        public SKColor DataModificationIcon { get; set; }

        public SKColor ComputeIcon { get; set; }

        public SKColor OtherIcon { get; set; }

        /// <summary>The symbol drawn on top of the glyph chip, which has to read against every one.</summary>
        public SKColor IconSymbol { get; set; }

        /// <summary>
        /// The second colour on a glyph, for the part that says what the operator did - the path a
        /// seek takes down an index.  Has to stand apart from both <see cref="IconSymbol"/> and the
        /// chip.  See <see cref="PlanGlyph.Accent"/>.
        /// </summary>
        public SKColor IconAccent { get; set; }

        public SKColor TitleText { get; set; }

        public SKColor DetailText { get; set; }

        public SKColor MetricText { get; set; }

        /// <summary>The unfilled part of the bar at the foot of a node.</summary>
        public SKColor MetricBarTrack { get; set; }

        /// <summary>
        /// The bar at its lowest values.  The bar runs from this to <see cref="MetricBarHigh"/>
        /// through <see cref="MetricBarMedium"/>, so the expensive operator in a plan is a different
        /// colour and not merely a longer bar.
        /// </summary>
        public SKColor MetricBarLow { get; set; }

        public SKColor MetricBarMedium { get; set; }

        public SKColor MetricBarHigh { get; set; }

        public SKColor Edge { get; set; }

        /// <summary>Arrows carrying measured rows, as opposed to estimated ones.</summary>
        public SKColor EdgeActual { get; set; }

        /// <summary>Arrows on the path from the selected node to the root.</summary>
        public SKColor EdgeHighlight { get; set; }

        public SKColor EdgeLabelText { get; set; }

        public SKColor EdgeLabelBackground { get; set; }

        public SKColor Selection { get; set; }

        public SKColor Hover { get; set; }

        /// <summary>The outline around a node matching the current search.</summary>
        public SKColor SearchMatch { get; set; }

        public SKColor Warning { get; set; }

        public SKColor Critical { get; set; }

        /// <summary>
        /// The good end of success, warning and critical - an arrow whose estimate came within ten
        /// times of what actually flowed.
        /// </summary>
        public SKColor Success { get; set; }

        /// <summary>The colour for how close an arrow's estimate came.</summary>
        public SKColor ColourFor(PlanEstimateAccuracy accuracy) => accuracy switch
        {
            PlanEstimateAccuracy.Critical => Critical,
            PlanEstimateAccuracy.Warning => Warning,
            _ => Success
        };

        /// <summary>The badge for something worth knowing rather than worth fixing.</summary>
        public SKColor Info { get; set; }

        public SKColor TooltipBackground { get; set; }

        public SKColor TooltipBorder { get; set; }

        public SKColor TooltipTitleText { get; set; }

        public SKColor TooltipLabelText { get; set; }

        public SKColor TooltipValueText { get; set; }

        /// <summary>The chip colour for an operator category.</summary>
        public SKColor IconFor(PlanOperatorCategory category) => category switch
        {
            PlanOperatorCategory.Root => RootIcon,
            PlanOperatorCategory.DataAccess => DataAccessIcon,
            PlanOperatorCategory.Join => JoinIcon,
            PlanOperatorCategory.Transform => TransformIcon,
            PlanOperatorCategory.Spool => SpoolIcon,
            PlanOperatorCategory.Parallelism => ParallelismIcon,
            PlanOperatorCategory.DataModification => DataModificationIcon,
            PlanOperatorCategory.Compute => ComputeIcon,
            _ => OtherIcon
        };

        /// <summary>
        /// The bar colour for a share of the worst value, blended through the three stops.
        ///
        /// A gradient rather than three bands: banding invites the reader to treat the boundary as
        /// meaningful, and it is not - the metric is continuous, and the only claim being made is
        /// "more of this than that".
        /// </summary>
        public SKColor MetricBarColour(double fraction)
        {
            var share = Math.Clamp(fraction, 0, 1);

            return share <= 0.5
                ? Blend(MetricBarLow, MetricBarMedium, share * 2)
                : Blend(MetricBarMedium, MetricBarHigh, (share - 0.5) * 2);
        }

        private static SKColor Blend(SKColor from, SKColor to, double amount)
        {
            var t = Math.Clamp(amount, 0, 1);

            return new SKColor(
                (byte)(from.Red + ((to.Red - from.Red) * t)),
                (byte)(from.Green + ((to.Green - from.Green) * t)),
                (byte)(from.Blue + ((to.Blue - from.Blue) * t)),
                (byte)(from.Alpha + ((to.Alpha - from.Alpha) * t)));
        }

        /// <summary>
        /// A neutral light palette, used by the tests and by any host that has no theme of its own.
        /// The application maps its own colours on instead.
        /// </summary>
        public static PlanPalette Light() => new()
        {
            Background = new SKColor(0xF7, 0xF8, 0xFA),
            NodeFill = new SKColor(0xFF, 0xFF, 0xFF),
            NodeBorder = new SKColor(0xD2, 0xD6, 0xDC),

            RootIcon = new SKColor(0x3A, 0x3A, 0x3A),
            DataAccessIcon = new SKColor(0x1E, 0x6F, 0xC0),
            JoinIcon = new SKColor(0x6A, 0x3D, 0xA8),
            TransformIcon = new SKColor(0x0E, 0x7C, 0x66),
            SpoolIcon = new SKColor(0xB0, 0x6A, 0x00),
            ParallelismIcon = new SKColor(0x00, 0x7A, 0x8C),
            DataModificationIcon = new SKColor(0xC0, 0x39, 0x2B),
            ComputeIcon = new SKColor(0x5A, 0x63, 0x72),
            OtherIcon = new SKColor(0x78, 0x7C, 0x84),
            IconSymbol = new SKColor(0xFF, 0xFF, 0xFF),
            IconAccent = new SKColor(0xFF, 0xC4, 0x3D),

            TitleText = new SKColor(0x1B, 0x1B, 0x1B),
            DetailText = new SKColor(0x5A, 0x5A, 0x5A),
            MetricText = new SKColor(0x70, 0x74, 0x7C),

            MetricBarTrack = new SKColor(0xE8, 0xEA, 0xED),
            MetricBarLow = new SKColor(0x9E, 0xC5, 0xE8),
            MetricBarMedium = new SKColor(0xE8, 0xA3, 0x3D),
            MetricBarHigh = new SKColor(0xC0, 0x39, 0x2B),

            Edge = new SKColor(0xB4, 0xB9, 0xC0),
            EdgeActual = new SKColor(0x8F, 0xA6, 0xBC),
            EdgeHighlight = new SKColor(0x1E, 0x88, 0xE5),
            EdgeLabelText = new SKColor(0x3A, 0x3A, 0x3A),
            EdgeLabelBackground = new SKColor(0xF7, 0xF8, 0xFA, 0xE0),

            Selection = new SKColor(0x1E, 0x88, 0xE5),
            Hover = new SKColor(0x64, 0xB5, 0xF6),
            SearchMatch = new SKColor(0xE8, 0xA3, 0x3D),

            Warning = new SKColor(0xE8, 0xA3, 0x3D),
            Critical = new SKColor(0xC0, 0x39, 0x2B),
            Success = new SKColor(0x1E, 0x8A, 0x44),
            Info = new SKColor(0x1E, 0x6F, 0xC0),

            TooltipBackground = new SKColor(0xFF, 0xFF, 0xFF, 0xF7),
            TooltipBorder = new SKColor(0xB0, 0xB4, 0xBA),
            TooltipTitleText = new SKColor(0x1B, 0x1B, 0x1B),
            TooltipLabelText = new SKColor(0x70, 0x74, 0x7C),
            TooltipValueText = new SKColor(0x1B, 0x1B, 0x1B)
        };

        /// <summary>A neutral dark palette, for the same purpose as <see cref="Light"/>.</summary>
        public static PlanPalette Dark() => new()
        {
            Background = new SKColor(0x1E, 0x1E, 0x1E),
            NodeFill = new SKColor(0x2B, 0x2D, 0x30),
            NodeBorder = new SKColor(0x45, 0x48, 0x4D),

            RootIcon = new SKColor(0x9E, 0xA3, 0xAA),
            DataAccessIcon = new SKColor(0x4F, 0x9A, 0xE0),
            JoinIcon = new SKColor(0xA4, 0x7B, 0xD8),
            TransformIcon = new SKColor(0x2C, 0xA8, 0x8D),
            SpoolIcon = new SKColor(0xD9, 0x94, 0x2E),
            ParallelismIcon = new SKColor(0x2C, 0xA3, 0xB5),
            DataModificationIcon = new SKColor(0xE5, 0x73, 0x73),
            ComputeIcon = new SKColor(0x88, 0x91, 0x9E),
            OtherIcon = new SKColor(0x8A, 0x8F, 0x97),
            IconSymbol = new SKColor(0x1A, 0x1A, 0x1A),
            IconAccent = new SKColor(0xFF, 0xE0, 0x8A),

            TitleText = new SKColor(0xF0, 0xF0, 0xF0),
            DetailText = new SKColor(0xB4, 0xB4, 0xB4),
            MetricText = new SKColor(0x96, 0x9A, 0xA0),

            MetricBarTrack = new SKColor(0x3A, 0x3D, 0x42),
            MetricBarLow = new SKColor(0x3E, 0x6F, 0x9E),
            MetricBarMedium = new SKColor(0xD9, 0x94, 0x2E),
            MetricBarHigh = new SKColor(0xE5, 0x73, 0x73),

            Edge = new SKColor(0x63, 0x67, 0x6D),
            EdgeActual = new SKColor(0x7C, 0x8A, 0x99),
            EdgeHighlight = new SKColor(0x64, 0xB5, 0xF6),
            EdgeLabelText = new SKColor(0xE0, 0xE0, 0xE0),
            EdgeLabelBackground = new SKColor(0x1E, 0x1E, 0x1E, 0xE0),

            Selection = new SKColor(0x64, 0xB5, 0xF6),
            Hover = new SKColor(0x90, 0xCA, 0xF9),
            SearchMatch = new SKColor(0xD9, 0x94, 0x2E),

            Warning = new SKColor(0xD9, 0x94, 0x2E),
            Critical = new SKColor(0xE5, 0x73, 0x73),
            Success = new SKColor(0x4E, 0xA6, 0x46),
            Info = new SKColor(0x4F, 0x9A, 0xE0),

            TooltipBackground = new SKColor(0x2B, 0x2B, 0x2B, 0xF7),
            TooltipBorder = new SKColor(0x6A, 0x6A, 0x6A),
            TooltipTitleText = new SKColor(0xF0, 0xF0, 0xF0),
            TooltipLabelText = new SKColor(0xA0, 0xA4, 0xAA),
            TooltipValueText = new SKColor(0xF0, 0xF0, 0xF0)
        };
    }
}
