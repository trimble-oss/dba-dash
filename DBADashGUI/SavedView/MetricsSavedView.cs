using DBADashGUI.Performance;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    /// <summary>
    /// A saved view that combines multiple IMetric charts to display.  This class gets serialized to save the state of all the chart controls
    /// </summary>
    internal class MetricsSavedView : SavedView
    {

        public List<IMetric> Metrics { get; set; }

        public bool ShowGrid { get; set; }

        public override SavedView.ViewTypes Type => SavedView.ViewTypes.Metric;

        public override string Serialize()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                MaxDepth=1
            });
            return json;
        }

        public static MetricsSavedView Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<MetricsSavedView>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        public static Dictionary<string, string> GetSavedViews(int UserID)
        {
           return SavedView.GetSavedViews(ViewTypes.Metric,UserID);
        }

   
    }


}
