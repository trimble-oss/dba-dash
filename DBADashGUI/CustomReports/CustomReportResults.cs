using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI.CustomReports
{
    public class CustomReportResult
    {
        /// <summary>
        /// Mapping of column names to associated metadata
        /// Replaces separate dictionaries in the Backward Compatibility section, making it easier to associate new metadata with columns
        /// </summary>
        public Dictionary<string, ColumnMetadata> Columns { get; set; } = new();

        /// <summary>
        /// Name of the result set
        /// </summary>
        public string ResultName { get; set; }

        #region "Backward compatibility"

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// Maps column names to aliases
        /// </summary>
        //[Obsolete]
        [JsonProperty("ColumnAlias", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ColumnAliasJson
        {
            set
            {
                if (value == null) return;
                foreach (var kvp in value)
                {
                    if (!Columns.ContainsKey(kvp.Key))
                        Columns[kvp.Key] = new ColumnMetadata();
                    Columns[kvp.Key].Alias = kvp.Value;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// Maps column names to aliases
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> ColumnAlias
        {
            get => Columns
                .Where(kvp => kvp.Value.Alias != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Alias);
            set => ColumnAliasJson = value;
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// Maps columns to cell format strings
        /// </summary>
        [JsonProperty("CellFormatString", NullValueHandling = NullValueHandling.Ignore)]
        private Dictionary<string, string> CellFormatStringJson
        {
            set
            {
                if (value == null) return;
                foreach (var kvp in value)
                {
                    if (!Columns.ContainsKey(kvp.Key))
                        Columns[kvp.Key] = new ColumnMetadata();
                    Columns[kvp.Key].FormatString = kvp.Value;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// Maps columns to cell format strings
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> CellFormatString
        {
            get => Columns
                .Where(kvp => kvp.Value.FormatString != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FormatString);
            set => CellFormatStringJson = value;
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// Maps columns to null value replacements
        /// </summary>
        [JsonProperty("CellNullValue", NullValueHandling = NullValueHandling.Ignore)]
        private Dictionary<string, string> CellNullValueJson
        {
            set
            {
                if (value == null) return;
                foreach (var kvp in value)
                {
                    if (!Columns.ContainsKey(kvp.Key))
                        Columns[kvp.Key] = new ColumnMetadata();
                    Columns[kvp.Key].NullValue = kvp.Value;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// Maps columns to null value replacements
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> CellNullValue
        {
            get => Columns
                .Where(kvp => kvp.Value.NullValue != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.NullValue);
            set => CellNullValueJson = value;
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// List of columns that we don't want to provide automatic conversion to local timezone
        /// </summary>
        [JsonProperty("DoNotConvertToLocalTimeZoneJson", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> DoNotConvertToLocalTimeZoneJson
        {
            set
            {
                if (value == null) return;
                foreach (var col in value)
                {
                    if (!Columns.ContainsKey(col))
                        Columns[col] = new ColumnMetadata();
                    Columns[col].ConvertToLocalTimeZone = false;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// List of columns that we don't want to provide automatic conversion to local timezone
        /// </summary>
        [JsonIgnore]
        public List<string> DoNotConvertToLocalTimeZone
        {
            get => Columns.Where(col => col.Value.ConvertToLocalTimeZone == false).Select(col => col.Key).ToList();
            set => DoNotConvertToLocalTimeZoneJson = value;
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// Maps columns to column layout information (size, position, visibility)
        /// </summary>
        [JsonProperty("ColumnLayout", NullValueHandling = NullValueHandling.Ignore)]
        public List<KeyValuePair<string, PersistedColumnLayout>> ColumnLayoutJson
        {
            set
            {
                if (value == null) return;
                foreach (var kvp in value)
                {
                    if (!Columns.ContainsKey(kvp.Key))
                        Columns[kvp.Key] = new ColumnMetadata();
                    Columns[kvp.Key].Visible = kvp.Value.Visible;
                    Columns[kvp.Key].DisplayIndex = kvp.Value.DisplayIndex;
                    Columns[kvp.Key].Width = kvp.Value.Width;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// Maps columns to column layout information (size, position, visibility)
        /// </summary>
        [JsonIgnore]
        public List<KeyValuePair<string, PersistedColumnLayout>> ColumnLayout
        {
            get => Columns.Select(col =>
                new KeyValuePair<string, PersistedColumnLayout>(col.Key, (PersistedColumnLayout)col.Value)).ToList();
            set => ColumnLayoutJson = value;
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// Maps columns to link info for the column
        /// </summary>
        [JsonProperty("LinkColumns", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, LinkColumnInfo> LinkColumnsJson
        {
            set
            {
                if (value == null) return;
                foreach (var kvp in value)
                {
                    if (!Columns.ContainsKey(kvp.Key))
                        Columns[kvp.Key] = new ColumnMetadata();
                    Columns[kvp.Key].Link = kvp.Value;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// Maps columns to link info for the column
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, LinkColumnInfo> LinkColumns
        {
            get => Columns
                .Where(kvp => kvp.Value.Link != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Link);
            set => LinkColumnsJson = value;
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility to ensure deserialization works for older reports
        /// Maps columns to highlighting rules
        /// </summary>
        [JsonProperty("CellHighlightingRules", NullValueHandling = NullValueHandling.Ignore)]
        public CellHighlightingRuleSetCollection CellHighlightingRulesJson
        {
            set
            {
                if (value == null) return;
                foreach (var kvp in value)
                {
                    if (!Columns.ContainsKey(kvp.Key))
                        Columns[kvp.Key] = new ColumnMetadata();
                    Columns[kvp.Key].Highlighting = kvp.Value;
                }
            }
        }

        /// <summary>
        /// [Obsolete - use ColumnMetadata associated with Columns instead. ]
        /// For backward compatibility
        /// Maps columns to highlighting rules
        /// </summary>
        [JsonIgnore]
        public CellHighlightingRuleSetCollection CellHighlightingRules
        {
            get => new CellHighlightingRuleSetCollection(Columns
                .Where(kvp => kvp.Value.Highlighting != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Highlighting));
            set => CellHighlightingRulesJson = value;
        }

        #endregion "Backward compatibility"
    }
}