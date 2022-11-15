using Newtonsoft.Json;

namespace DBADashGUI
{
    internal class TreeSavedView : SavedView
    {
        public string GroupByTag { get; set; }
        public bool ShowCounts { get; set; }

        public override ViewTypes Type => ViewTypes.Tree;

        public override string Serialize()
        {
            string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            return json;
        }

        public static TreeSavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<TreeSavedView>(json);
        }
    }
}
