using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class CustomReportResult
    {
        /// <summary>
        /// Mapping of column names returned from SQL to aliases used in grid
        /// </summary>
        public Dictionary<string, string> ColumnAlias { get; set; } = new();

        public Dictionary<string, string> CellFormatString { get; set; } = new();

        public Dictionary<string, string> CellNullValue { get; set; } = new();

        /// <summary>
        /// Option to exclude DateTime columns from automatic time zone conversion
        /// </summary>
        public List<string> DoNotConvertToLocalTimeZone { get; set; } = new();

        public List<KeyValuePair<string, PersistedColumnLayout>> ColumnLayout { get; set; } = new();

        public string ResultName { get; set; }

        public Dictionary<string, LinkColumnInfo> LinkColumns { get; set; } = new();

        public CellHighlightingRuleSetCollection CellHighlightingRules { get; set; } = new();
    }
}