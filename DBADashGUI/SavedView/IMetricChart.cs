using System;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Interface for chart controls to implement to allow them to be added to Metrics tab (PerformanceCounterSummary)
    /// IMetricChart now inherits ISetContext and IRefreshData so callers can set the context and request a refresh
    /// without relying on a RefreshData overload that accepts an instance id.
    /// </summary>
    public interface IMetricChart : ISetContext, IRefreshData
    {
        /// <summary>
        /// Event fired when Close button is clicked to remove the chart
        /// </summary>
        public event EventHandler<EventArgs> Close;

        /// <summary>
        /// Event fired when Move Up button is clicked to change the order the chart is displayed
        /// </summary>
        public event EventHandler<EventArgs> MoveUp;

        /// <summary>
        /// Hide/Show Close button to remove the chart
        /// </summary>
        public bool CloseVisible { get; set; }

        /// <summary>
        /// Hide/Show Move Up button to change the order the chart is displayed
        /// </summary>
        public bool MoveUpVisible { get; set; }

        public IMetric Metric { get; }
    }
}