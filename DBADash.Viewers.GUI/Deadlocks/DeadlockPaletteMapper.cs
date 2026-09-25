using DBADash.Deadlock.Skia;
using DBADashGUI.Theme;
using SkiaSharp;
using System.Drawing;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Maps a DBA Dash theme onto the renderer's palette.
    ///
    /// This is the only place the two colour worlds meet.  Keeping the mapping here is what lets
    /// DBADash.Deadlock.Skia stay free of System.Drawing (Windows only on modern .NET) while still
    /// following the application's theme.
    /// </summary>
    internal static class DeadlockPaletteMapper
    {
        public static DeadlockPalette FromTheme(BaseTheme theme)
        {
            var dark = theme.ThemeIdentifier == ThemeType.Dark;

            return new DeadlockPalette
            {
                Background = theme.BackgroundColor.ToSKColor(),

                ProcessFill = (dark ? DashColors.Gray9 : DashColors.BluePale).ToSKColor(),
                ProcessBorder = (dark ? DashColors.BlueLight : DashColors.TrimbleBlue).ToSKColor(),

                // The victim is the process that got rolled back - the first thing anyone looks for.
                VictimFill = (dark ? Color.FromArgb(74, 36, 36) : DashColors.RedPale).ToSKColor(),
                VictimBorder = (dark ? DashColors.RedLight : DashColors.Fail).ToSKColor(),

                ResourceFill = (dark ? DashColors.Gray10 : DashColors.White).ToSKColor(),
                ResourceBorder = DashColors.Gray4.ToSKColor(),

                TitleText = theme.GridCellForeColor.ToSKColor(),
                DetailText = (dark ? DashColors.Gray2 : DashColors.Gray6).ToSKColor(),

                // The statement preview is a link into the code viewer, so it follows the theme's link colour.
                LinkText = (dark ? DashColors.BlueLight : DashColors.TrimbleBlue).ToSKColor(),

                Edge = DashColors.Gray4.ToSKColor(),
                CycleEdge = (dark ? DashColors.RedLight : DashColors.Fail).ToSKColor(),
                EdgeLabelText = theme.GridCellForeColor.ToSKColor(),

                // Translucent so an edge passing behind a label still reads as continuous.
                EdgeLabelBackground = WithAlpha(theme.BackgroundColor, 224),

                Selection = DashColors.Information.ToSKColor(),
                Hover = (dark ? DashColors.Yellow : DashColors.YellowDark).ToSKColor(),

                // What the selected node holds, and what it is waiting for.  Neither is a theme colour:
                // they have to stay clear of the selection blue and the cycle red they are drawn
                // alongside, and be told apart by hue rather than by weight alone.
                OwnerHighlight = (dark ? Color.FromArgb(206, 147, 216) : Color.FromArgb(106, 27, 154)).ToSKColor(),
                WaiterHighlight = (dark ? Color.FromArgb(255, 193, 7) : Color.FromArgb(200, 122, 0)).ToSKColor(),

                TooltipBackground = WithAlpha(dark ? DashColors.Gray10 : DashColors.White, 245),
                TooltipBorder = DashColors.Gray4.ToSKColor(),
                TooltipTitleText = theme.GridCellForeColor.ToSKColor(),
                TooltipLabelText = (dark ? DashColors.Gray3 : DashColors.Gray6).ToSKColor(),
                TooltipValueText = theme.GridCellForeColor.ToSKColor()
            };
        }

        private static SKColor WithAlpha(Color colour, byte alpha) => new(colour.R, colour.G, colour.B, alpha);
    }
}