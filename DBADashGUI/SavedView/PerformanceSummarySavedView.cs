using DBADashGUI.Performance;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DBADashGUI.SavedView;

namespace DBADashGUI
{

    /// <summary>
    /// Used to save the state of the Performance Summary page.  The performance counters selected and the column layout.
    /// </summary>
    internal class PerformanceSummarySavedView : SavedView
    {

        public override SavedView.ViewTypes Type => SavedView.ViewTypes.PerformanceSummary;

        public Dictionary<int, Counter> SelectedPerformanceCounters = new();
        public List<KeyValuePair<string, PersistedColumnLayout>> ColumnLayout;

        public override string Serialize()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            return json;
        }

        public static PerformanceSummarySavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<PerformanceSummarySavedView>(json);
        }

        public static Dictionary<string, string> GetSavedViews(int UserID)
        {
            return SavedView.GetSavedViews(ViewTypes.PerformanceSummary, UserID);
        }


    }
}
