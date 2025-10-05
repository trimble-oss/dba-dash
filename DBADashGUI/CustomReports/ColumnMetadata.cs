using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.CustomReports
{
    public class ColumnMetadata : PersistedColumnLayout
    {
        /// <summary>
        /// Display alias for the column
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Alias { get; set; }

        /// <summary>
        /// Format string for cell values
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FormatString { get; set; }

        /// <summary>
        /// Display value when cell is null
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string NullValue { get; set; }

        /// <summary>
        /// Description or tooltip for the column
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        public bool? ConvertToLocalTimeZone { get; set; }

        public LinkColumnInfo Link { get; set; }

        public CellHighlightingRuleSet Highlighting { get; set; }
    }
}