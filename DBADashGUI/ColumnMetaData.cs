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
        public Int32 axis = 0;
    }
}
