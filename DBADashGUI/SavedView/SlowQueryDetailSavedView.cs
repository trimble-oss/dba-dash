using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI
{
    /// <summary>
    /// Used to save the state of the Slow Query detail grid.
    /// </summary>
    internal class SlowQueryDetailSavedView : SavedView
    {
        public override ViewTypes Type => ViewTypes.SlowQueryDetail;

        public List<KeyValuePair<string, PersistedColumnLayout>> ColumnLayout;

        public bool AutoSizeColumns { get; set; }

        public static SlowQueryDetailSavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<SlowQueryDetailSavedView>(json);
        }

        public static Dictionary<string, string> GetSavedViews(int UserID)
        {
            return SavedView.GetSavedViews(ViewTypes.SlowQueryDetail, UserID);
        }

        public static SlowQueryDetailSavedView GetDefaultSavedView()
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