using DBADashGUI.Performance;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace DBADashGUI
{
    /// <summary>
    /// A saved view that combines multiple IMetric charts to display.  This class gets serialized to save the state of all the chart controls
    /// </summary>
    internal class MetricsSavedView : SavedView
    {
        public List<IMetric> Metrics { get; set; }

        public bool ShowGrid { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public override ViewTypes Type => ViewTypes.Metric;

        public override string Serialize()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                MaxDepth = 1
            });
            return json;
        }

        public static MetricsSavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<MetricsSavedView>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });
        }

        public static Dictionary<string, string> GetSavedViews(int UserID)
        {
            return SavedView.GetSavedViews(ViewTypes.Metric, UserID);
        }
    }
}