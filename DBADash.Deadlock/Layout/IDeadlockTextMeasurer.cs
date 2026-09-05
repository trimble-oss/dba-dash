namespace DBADash.Deadlock.Layout
{
    /// <summary>Which text role is being measured, so the caller can apply the right font.</summary>
    public enum DeadlockTextRole
    {
        /// <summary>The node heading.</summary>
        Title,

        /// <summary>A supporting line under the heading.</summary>
        Detail,

        /// <summary>An edge label.</summary>
        EdgeLabel
    }

    /// <summary>
    /// Measures a string so nodes can be sized to their content.
    ///
    /// Text measurement needs a font, which is a rendering concern, so it is injected rather than
    /// done here.  That keeps the layout engine free of any drawing dependency: the Skia renderer
    /// implements this with SKFont, and tests supply a deterministic fake.
    /// </summary>
    public interface IDeadlockTextMeasurer
    {
        LayoutSize Measure(string text, DeadlockTextRole role);
    }
}
