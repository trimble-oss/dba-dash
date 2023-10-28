using DBADashGUI.Pickers;
using LiveCharts.Defaults;
using System;

namespace DBADashGUI
{
    public class ColumnMetaData : ISelectable
    {
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public DateTimePoint[] Points;
        public int axis = 0;
    }
}
