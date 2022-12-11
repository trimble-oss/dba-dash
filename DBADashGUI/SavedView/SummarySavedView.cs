using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI
{
    internal class SummarySavedView : SavedView
    {
        public override ViewTypes Type => ViewTypes.Summary;

        public bool FocusedView { get; set; }
        public bool ShowTestSummary { get; set; }
        public bool ShowHidden { get; set; }

        public static SummarySavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<SummarySavedView>(json);
        }

        public static Dictionary<string, string> GetSavedViews(int UserID)
        {
            return SavedView.GetSavedViews(ViewTypes.Summary, UserID);
        }

        public static SummarySavedView GetDefaultSavedView()
        {
            var saved = GetSavedViews(DBADashUser.UserID).Where(x => x.Key == "Default").FirstOrDefault(new KeyValuePair<string, string>("Default", "")).Value;
            if (string.IsNullOrEmpty(saved))
            {
                return null;
            }
            else
            {
                return Deserialize(saved);
            }
        }
    }
}