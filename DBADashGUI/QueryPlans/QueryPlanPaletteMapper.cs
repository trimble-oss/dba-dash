using DBADash.QueryPlan.Skia;
using DBADashGUI.Theme;
using SkiaSharp;
using System.Drawing;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// Maps a DBA Dash theme onto the plan renderer's palette.
    ///
    /// This is the only place the two colour worlds meet.  Keeping the mapping here is what lets
    /// DBADash.QueryPlan.Skia stay free of System.Drawing (Windows only on modern .NET) while still
    /// following the application's theme.
    /// </summary>
    internal static class QueryPlanPaletteMapper
    {
        public static PlanPalette FromTheme(BaseTheme theme)
        {
            var dark = theme.ThemeIdentifier == ThemeType.Dark;

            return new PlanPalette
            {
                Background = theme.BackgroundColor.ToSKColor(),

                // Nodes are cards standing off the background rather than tinted by category: with
                // forty operators on screen, colouring the whole card turns the plan into a
                // patchwork and the arrows - which carry the volume, the thing worth seeing - lose
                // the contest for attention.  The category colour goes on the icon chip instead.
                NodeFill = (dark ? DashColors.Gray9 : DashColors.White).ToSKColor(),
                NodeBorder = (dark ? DashColors.Gray7 : DashColors.Gray1).ToSKColor(),

                // The operator families.  Data access is Trimble blue because it is the family
                // people look for first and the one the product's own colour should land on; the
                // rest are spaced around the wheel far enough apart to be told apart at icon size,
                // and each has a light and a dark variant rather than one colour used on both.
                RootIcon = (dark ? DashColors.Gray4 : DashColors.Gray7).ToSKColor(),
                DataAccessIcon = (dark ? DashColors.BlueLight : DashColors.TrimbleBlue).ToSKColor(),
                JoinIcon = (dark ? Color.FromArgb(164, 123, 216) : Color.FromArgb(106, 61, 168)).ToSKColor(),
                TransformIcon = (dark ? Color.FromArgb(44, 168, 141) : DashColors.GreenDark).ToSKColor(),
                SpoolIcon = (dark ? DashColors.Yellow : DashColors.YellowDark).ToSKColor(),
                ParallelismIcon = (dark ? Color.FromArgb(44, 163, 181) : Color.FromArgb(0, 122, 140)).ToSKColor(),
                DataModificationIcon = (dark ? DashColors.RedLight : DashColors.Fail).ToSKColor(),
                ComputeIcon = (dark ? DashColors.Gray5 : DashColors.Gray6).ToSKColor(),
                OtherIcon = DashColors.Gray4.ToSKColor(),

                // The symbol sits on a saturated chip in both themes, so it takes the opposite end
                // of the scale from the chip rather than following the theme's text colour.
                IconSymbol = (dark ? DashColors.Gray10 : DashColors.White).ToSKColor(),
                IconAccent = (dark ? DashColors.YellowLight : DashColors.Yellow).ToSKColor(),

                TitleText = theme.GridCellForeColor.ToSKColor(),
                DetailText = (dark ? DashColors.Gray2 : DashColors.Gray6).ToSKColor(),
                MetricText = (dark ? DashColors.Gray3 : DashColors.Gray5).ToSKColor(),

                MetricBarTrack = (dark ? DashColors.Gray8 : DashColors.Gray0).ToSKColor(),

                // The bar runs blue to amber to red, so the expensive operator is a different colour
                // and not merely a longer bar - which is what makes it findable without reading
                // every node.
                MetricBarLow = (dark ? Color.FromArgb(62, 111, 158) : DashColors.BlueLight).ToSKColor(),
                MetricBarMedium = (dark ? DashColors.Yellow : DashColors.YellowDark).ToSKColor(),
                MetricBarHigh = (dark ? DashColors.RedLight : DashColors.Fail).ToSKColor(),

                // Arrows are grey rather than coloured: their weight already carries the row count,
                // and colouring them as well would be the same fact said twice, in the way of the
                // facts that are only said once.
                Edge = (dark ? DashColors.Gray6 : DashColors.Gray2).ToSKColor(),
                EdgeActual = (dark ? DashColors.Gray5 : DashColors.Gray3).ToSKColor(),
                EdgeHighlight = DashColors.Information.ToSKColor(),
                EdgeLabelText = (dark ? DashColors.Gray2 : DashColors.Gray6).ToSKColor(),

                // Translucent so an arrow passing behind a label still reads as continuous.
                EdgeLabelBackground = WithAlpha(theme.BackgroundColor, 224),

                Selection = DashColors.Information.ToSKColor(),
                Hover = (dark ? DashColors.BlueLight : DashColors.TrimbleBlue).ToSKColor(),
                SearchMatch = (dark ? DashColors.Yellow : DashColors.YellowDark).ToSKColor(),

                Warning = (dark ? DashColors.Yellow : DashColors.YellowDark).ToSKColor(),
                Critical = (dark ? DashColors.RedLight : DashColors.Fail).ToSKColor(),
                Success = (dark ? DashColors.GreenLight : DashColors.Green).ToSKColor(),
                Info = (dark ? DashColors.BlueLight : DashColors.TrimbleBlue).ToSKColor(),

                TooltipBackground = WithAlpha(dark ? DashColors.Gray10 : DashColors.White, 247),
                TooltipBorder = DashColors.Gray4.ToSKColor(),
                TooltipTitleText = theme.GridCellForeColor.ToSKColor(),
                TooltipLabelText = (dark ? DashColors.Gray3 : DashColors.Gray6).ToSKColor(),
                TooltipValueText = theme.GridCellForeColor.ToSKColor()
            };
        }

        private static SKColor WithAlpha(Color colour, byte alpha) => new(colour.R, colour.G, colour.B, alpha);
    }
}
