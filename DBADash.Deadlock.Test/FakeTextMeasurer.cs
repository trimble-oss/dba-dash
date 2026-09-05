using DBADash.Deadlock.Layout;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// Deterministic stand-in for the renderer's font metrics: character count times a fixed width.
    /// Layout is verified for geometry, not typography, so exact glyph widths are irrelevant - what
    /// matters is that node sizing responds to text length and that nothing depends on a real font.
    /// </summary>
    internal sealed class FakeTextMeasurer : IDeadlockTextMeasurer
    {
        public const double TitleCharWidth = 8;
        public const double TitleHeight = 16;
        public const double DetailCharWidth = 6;
        public const double DetailHeight = 12;

        public LayoutSize Measure(string text, DeadlockTextRole role) =>
            role == DeadlockTextRole.Title
                ? new LayoutSize(text.Length * TitleCharWidth, TitleHeight)
                : new LayoutSize(text.Length * DetailCharWidth, DetailHeight);
    }
}
