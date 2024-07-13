using System.Collections.Generic;

namespace DBADashGUI.Performance
{
    public class Counter
    {
        public int CounterID { get; set; }
        public string ObjectName { get; set; }
        public string CounterName { get; set; }
        public string InstanceName { get; set; }

        public bool Max { get; set; }
        public bool Avg { get; set; }
        public bool Min { get; set; }
        public bool Total { get; set; }
        public bool SampleCount { get; set; }

        public bool Current { get; set; }

        public List<string> GetAggColumns()
        {
            var agg = new List<string>();
            if (Avg) { agg.Add("Avg"); }
            if (Max) { agg.Add("Max"); }
            if (Min) { agg.Add("Min"); }
            if (Total) { agg.Add("Total"); }
            if (Current) { agg.Add("Current"); }
            if (SampleCount) { agg.Add("SampleCount"); }
            return agg;
        }

        public override string ToString()
        {
            return ObjectName + "\\" + CounterName + "\\" + InstanceName;
        }
    }
}
