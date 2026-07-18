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
        /// Display value for a boolean column when the value is true, e.g. "True", "Yes", "Y".
        /// If this or <see cref="BooleanFalseValue"/> is set, the column is rendered as text instead of the default checkbox.
        /// <see cref="NullValue"/> is used for null values once the column is rendered as text.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BooleanTrueValue { get; set; }

        /// <summary>
        /// Display value for a boolean column when the value is false, e.g. "False", "No", "N".
        /// If this or <see cref="BooleanTrueValue"/> is set, the column is rendered as text instead of the default checkbox.
        /// <see cref="NullValue"/> is used for null values once the column is rendered as text.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string BooleanFalseValue { get; set; }

        /// <summary>
        /// Description or tooltip for the column
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        public bool? ConvertToLocalTimeZone { get; set; }

        public LinkColumnInfo Link { get; set; }

        /// <summary>
        /// When true, this link column is only rendered when a matching column is present in the result set.
        /// Use for data-backed link columns that are only conditionally present (e.g. columns that appear
        /// for certain group by options) so they aren't rendered as empty link buttons showing the header
        /// text when absent. Synthetic "action" link columns that are never backed by data (e.g. Configure /
        /// History / Files) should leave this false so they are always added.
        /// </summary>
        public bool RequiresDataColumn { get; set; }

        public CellHighlightingRuleSet Highlighting { get; set; }
    }
}