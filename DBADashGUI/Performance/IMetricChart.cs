using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Interface for chart controls to implement to allow them to be added to Metrics tab (PerformanceCounterSummary)
    /// </summary>
    interface IMetricChart 
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

        /// <summary>
        /// Refresh the chart
        /// </summary>
        public void RefreshData(int InstanceID);
 

    }
}
