namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// How <see cref="PlanLayoutEngine"/> chooses each node's row - a plan's columns are set by its
    /// depth either way, so this is the whole difference between the two shapes a plan can take.
    /// </summary>
    public enum PlanVerticalLayout
    {
        /// <summary>
        /// Stack the leaves in order and centre every other node on its inputs.  A consumer sits
        /// level with the middle of what feeds it, which is the honest picture of a plan as a tree
        /// and the easier one to follow a fan-in on.  It is also the taller one: every branch a node
        /// has pushes the rest of the plan further down the page.
        /// </summary>
        Centred,

        /// <summary>
        /// Put each node level with its first input and drop the rest below, as SSMS draws plans.
        /// The whole first-input spine - the statement root, through to the leaf that feeds it -
        /// shares one row, so a plan is as tall as it has branches rather than as tall as it has
        /// leaves.  Much shorter on a wide plan, at the cost of a consumer no longer sitting
        /// between its inputs.
        /// </summary>
        FirstChildAligned
    }
}
