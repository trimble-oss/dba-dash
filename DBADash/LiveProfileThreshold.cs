using System;

namespace DBADash
{
    ///<summary>
    /// When the running queries collection keeps the operator counters of a long running query, so its plan can be shown
    /// with the rows each operator had got through - see <see cref="LiveProfileCollector"/>.<br/>
    /// Applies on top of <see cref="PlanCollectionThreshold"/>: the counters are only kept for a statement whose plan
    /// meets that threshold too, as they are no use without the plan to show them on.<br/>
    /// Thresholds set to max value = not used.  Only 1 threshold must be exceeded.<br/>
    ///</summary>
    public class LiveProfileThreshold
    {
        /// <summary>CPU time of the request, in ms.</summary>
        public int CPUThreshold = Int32.MaxValue;

        /// <summary>
        /// How long the request has been running, in ms.  A blocked request doesn't count towards it: its operators aren't
        /// moving, so its counters say nothing the plan doesn't.
        /// </summary>
        public int DurationThreshold = Int32.MaxValue;

        /// <summary>
        /// The most requests captured in one collection, highest CPU first.  Bounds the cost on an instance with a lot of
        /// long running queries.
        /// </summary>
        public int MaxPerCollection = DefaultMaxPerCollection;

        public const int DefaultMaxPerCollection = 10;

        public bool Enabled => (CPUThreshold != Int32.MaxValue || DurationThreshold != Int32.MaxValue) && MaxPerCollection > 0;

        public static LiveProfileThreshold DefaultThreshold => new() { CPUThreshold = 60000, DurationThreshold = 300000 };
    }
}
