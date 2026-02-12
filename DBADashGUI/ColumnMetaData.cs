using DBADashGUI.Pickers;
using LiveCharts.Defaults;

namespace DBADashGUI
{
    public class ColumnMetaData : ISelectable
    {
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public DateTimePoint[] Points;

        /// <summary>
        /// Axis name for name-based axis mapping (preferred). E.g., "MBsec", "IOPs", "Latency"
        /// </summary>
        public string AxisName { get; set; } = "Primary";

        /// <summary>
        /// Numeric axis index for backward compatibility with existing code. Use AxisName instead for new code.
        /// </summary>
        public int axis { get; set; } = 0;
    }
}
