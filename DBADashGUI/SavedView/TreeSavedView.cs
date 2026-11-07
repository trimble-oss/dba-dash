using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI
{
    internal class TreeSavedView : SavedView
    {
        /// <summary>
        /// Single level of grouping, from layouts saved before multi-level grouping.  Still saved (as the first level) so
        /// an older GUI reading a newer layout groups by that.
        /// </summary>
        public string GroupByTag { get; set; }

        /// <summary>Tag names to group by, outermost first.  e.g. Role, then Region</summary>
        public List<string> GroupByTags { get; set; }

        public bool ShowCounts { get; set; }

        public override ViewTypes Type => ViewTypes.Tree;

        /// <summary>Grouping levels, falling back to the single GroupByTag on layouts saved before GroupByTags existed</summary>
        public IEnumerable<string> GetGroupByTags() =>
            GroupByTags is { Count: > 0 }
                ? GroupByTags.Where(t => !string.IsNullOrEmpty(t))
                : string.IsNullOrEmpty(GroupByTag) ? Enumerable.Empty<string>() : new[] { GroupByTag };

        public static TreeSavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<TreeSavedView>(json);
        }
    }
}
