namespace DBADash.QueryPlan.Layout
{
    /// <summary>Which text role is being measured, so the caller can apply the right font.</summary>
    public enum PlanTextRole
    {
        /// <summary>The operator name on a node.</summary>
        Title,

        /// <summary>The object name under the title.</summary>
        Detail,

        /// <summary>The metrics line at the foot of a node.</summary>
        Metric,

        /// <summary>A row count on an arrow.</summary>
        EdgeLabel
    }

    /// <summary>
    /// Measures a string so nodes can be sized to their content.
    ///
    /// Text measurement needs a font, which is a rendering concern, so it is injected rather than
    /// done here.  That keeps the layout engine free of any drawing dependency: the Skia renderer
    /// implements this with SKFont, and tests supply a deterministic fake.
    /// </summary>
    public interface IPlanTextMeasurer
    {
        LayoutSize Measure(string text, PlanTextRole role);
    }
}
