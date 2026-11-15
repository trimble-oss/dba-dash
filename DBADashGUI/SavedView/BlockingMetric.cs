using DBADashGUI.Performance;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI
{
    /// <summary>
    /// Used to store the state of the Blocking chart
    /// </summary>
    public class BlockingMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.Blocking;

        public bool BlockingSnapshots { get; set; } = true;

        public bool Deadlocks { get; set; } = true;

        /// <summary>
        /// Shade lock waits (LCK_* wait time, ms/sec) behind the blocking and deadlock markers.  Wait stats are
        /// instance wide, so at database level this is still the instance's lock waits.
        /// </summary>
        public bool LockWaits { get; set; } = true;

        /// <summary>
        /// Count deadlocks from the performance counter even where the Deadlocks collection is enabled.  The counter
        /// has the longer history - the collection starts when it was switched on - but is engine wide, so on Azure
        /// SQL DB it can include deadlocks outside the database.  See dbo.Deadlocks_Get.
        /// </summary>
        public bool DeadlockCountsFromCounter { get; set; }

        public IMetricChart GetChart()
        {
            return new Blocking() { Metric = this };
        }
    }
}