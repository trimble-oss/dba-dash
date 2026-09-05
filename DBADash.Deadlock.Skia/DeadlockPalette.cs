using SkiaSharp;

namespace DBADash.Deadlock.Skia
{
    /// <summary>
    /// Every colour the renderer uses, as <see cref="SKColor"/>.
    ///
    /// Deliberately not wired to any application theme: the host maps its own colours onto this,
    /// which keeps System.Drawing.Color (Windows only) on the host's side of the boundary and lets
    /// the renderer be exercised with a known palette in tests.
    /// </summary>
    public sealed class DeadlockPalette
    {
        public SKColor Background { get; set; }

        public SKColor ProcessFill { get; set; }

        public SKColor ProcessBorder { get; set; }

        /// <summary>The victim is the process SQL Server rolled back - worth its own colour.</summary>
        public SKColor VictimFill { get; set; }

        public SKColor VictimBorder { get; set; }

        public SKColor ResourceFill { get; set; }

        public SKColor ResourceBorder { get; set; }

        public SKColor TitleText { get; set; }

        public SKColor DetailText { get; set; }

        /// <summary>The statement preview shown on a process node, drawn as a clickable link.</summary>
        public SKColor LinkText { get; set; }

        public SKColor Edge { get; set; }

        /// <summary>Edges on the deadlock cycle, which is the part worth following.</summary>
        public SKColor CycleEdge { get; set; }

        public SKColor EdgeLabelText { get; set; }

        public SKColor EdgeLabelBackground { get; set; }

        public SKColor Selection { get; set; }

        public SKColor Hover { get; set; }

        /// <summary>
        /// Nodes and edges on the ownership side of the selected node - what it holds, or who holds
        /// it.  Kept clear of <see cref="CycleEdge"/> red and <see cref="Selection"/> blue, and
        /// distinguishable from <see cref="WaiterHighlight"/> by hue rather than only by lightness.
        /// </summary>
        public SKColor OwnerHighlight { get; set; }

        /// <summary>Nodes and edges on the wait side of the selected node.</summary>
        public SKColor WaiterHighlight { get; set; }

        public SKColor TooltipBackground { get; set; }

        public SKColor TooltipBorder { get; set; }

        public SKColor TooltipTitleText { get; set; }

        public SKColor TooltipLabelText { get; set; }

        public SKColor TooltipValueText { get; set; }

        public static DeadlockPalette Light() => new()
        {
            Background = new SKColor(0xFA, 0xFA, 0xFA),
            ProcessFill = new SKColor(0xE3, 0xEF, 0xFB),
            ProcessBorder = new SKColor(0x3E, 0x76, 0xB5),
            VictimFill = new SKColor(0xFB, 0xE1, 0xE1),
            VictimBorder = new SKColor(0xC0, 0x39, 0x2B),
            ResourceFill = new SKColor(0xFF, 0xFF, 0xFF),
            ResourceBorder = new SKColor(0x8A, 0x8A, 0x8A),
            TitleText = new SKColor(0x1B, 0x1B, 0x1B),
            DetailText = new SKColor(0x5A, 0x5A, 0x5A),
            LinkText = new SKColor(0x1E, 0x6F, 0xC0),
            Edge = new SKColor(0xA0, 0xA0, 0xA0),
            CycleEdge = new SKColor(0xC0, 0x39, 0x2B),
            EdgeLabelText = new SKColor(0x3A, 0x3A, 0x3A),
            EdgeLabelBackground = new SKColor(0xFA, 0xFA, 0xFA, 0xE0),
            Selection = new SKColor(0x1E, 0x88, 0xE5),
            Hover = new SKColor(0x64, 0xB5, 0xF6),
            OwnerHighlight = new SKColor(0x6A, 0x1B, 0x9A),
            WaiterHighlight = new SKColor(0xC8, 0x7A, 0x00),
            TooltipBackground = new SKColor(0xFF, 0xFF, 0xFF, 0xF5),
            TooltipBorder = new SKColor(0xB0, 0xB0, 0xB0),
            TooltipTitleText = new SKColor(0x1B, 0x1B, 0x1B),
            TooltipLabelText = new SKColor(0x70, 0x70, 0x70),
            TooltipValueText = new SKColor(0x1B, 0x1B, 0x1B)
        };

        public static DeadlockPalette Dark() => new()
        {
            Background = new SKColor(0x1E, 0x1E, 0x1E),
            ProcessFill = new SKColor(0x24, 0x3A, 0x50),
            ProcessBorder = new SKColor(0x6F, 0xA8, 0xDC),
            VictimFill = new SKColor(0x4A, 0x24, 0x24),
            VictimBorder = new SKColor(0xE5, 0x73, 0x73),
            ResourceFill = new SKColor(0x2D, 0x2D, 0x2D),
            ResourceBorder = new SKColor(0x9E, 0x9E, 0x9E),
            TitleText = new SKColor(0xF0, 0xF0, 0xF0),
            DetailText = new SKColor(0xB0, 0xB0, 0xB0),
            LinkText = new SKColor(0x6F, 0xB3, 0xF0),
            Edge = new SKColor(0x80, 0x80, 0x80),
            CycleEdge = new SKColor(0xE5, 0x73, 0x73),
            EdgeLabelText = new SKColor(0xE0, 0xE0, 0xE0),
            EdgeLabelBackground = new SKColor(0x1E, 0x1E, 0x1E, 0xE0),
            Selection = new SKColor(0x64, 0xB5, 0xF6),
            Hover = new SKColor(0x90, 0xCA, 0xF9),
            OwnerHighlight = new SKColor(0xCE, 0x93, 0xD8),
            WaiterHighlight = new SKColor(0xFF, 0xC1, 0x07),
            TooltipBackground = new SKColor(0x2B, 0x2B, 0x2B, 0xF5),
            TooltipBorder = new SKColor(0x6A, 0x6A, 0x6A),
            TooltipTitleText = new SKColor(0xF0, 0xF0, 0xF0),
            TooltipLabelText = new SKColor(0xA0, 0xA0, 0xA0),
            TooltipValueText = new SKColor(0xF0, 0xF0, 0xF0)
        };
    }
}
