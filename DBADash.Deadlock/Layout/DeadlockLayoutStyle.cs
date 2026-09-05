namespace DBADash.Deadlock.Layout
{
    /// <summary>How <see cref="DeadlockLayoutEngine"/> arranges the nodes.</summary>
    public enum DeadlockLayoutStyle
    {
        /// <summary>
        /// The deadlock cycle on a ring, in cycle order, with anything outside the cycle placed beside
        /// the node it hangs off.  The cycle becomes the shape of the picture - a two process deadlock
        /// is a diamond, a three way a hexagon - which makes what kind of deadlock it is readable at a
        /// glance.
        /// </summary>
        Ring,

        /// <summary>
        /// Columns left to right, by distance from the victim: who is waiting, what they are waiting
        /// for, who is holding it, and so on.  This is how SSMS draws a deadlock, and it reads as a
        /// flow rather than a shape, which suits a graph with more going on around the cycle.
        /// </summary>
        Layered
    }
}
