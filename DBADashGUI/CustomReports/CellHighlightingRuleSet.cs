using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;
using Newtonsoft.Json;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// A set of highlighting rules for a single column
    /// </summary>
    public class CellHighlightingRuleSet
    {
        private List<CellHighlightingRule> _rules = new();
        public List<CellHighlightingRule> Rules { get => IsStatusColumn ? new() : _rules; set => _rules = value; }

        /// <summary>
        /// Name of the column that the rules will be evaluated against
        /// </summary>
        public string TargetColumn { get; set; }

        /// <summary>
        /// Indicates that the value is a DBADashStatusEnum and to apply associated formatting.  Rules are not evaluated if this is true
        /// </summary>
        public bool IsStatusColumn { get; set; }

        /// <summary>
        /// Option to indicate that the TargetColumn is a column in the data source and not a column in the DataGridView.  This is useful for columns that are in the data source but not included in the DataGridView
        /// </summary>
        public bool EvaluateConditionAgainstDataSource { get; set; }

        public CellHighlightingRuleSet(string targetColumn)
        {
            TargetColumn = targetColumn;
        }

        public CellHighlightingRuleSet(string targetColumn, bool evaluateConditionAgainstDataSource)
        {
            EvaluateConditionAgainstDataSource = evaluateConditionAgainstDataSource;
            TargetColumn = targetColumn;
        }

        public CellHighlightingRuleSet()
        {
        }

        /// <summary>
        /// True if there are any rules in this set or if this is a status column
        /// </summary>
        [JsonIgnore]
        public bool HasRules => Rules.Count > 0 || IsStatusColumn;

        /// <summary>
        /// Evaluate all rules against the given value and apply formatting associated with the first rule that matches
        /// </summary>
        /// <param name="formattedCell">Cell to format</param>
        /// <param name="value">Value to check against the rules</param>
        /// <param name="isDarkMode">Option for dark mode formatting if available</param>
        /// <param name="defaultStyle">Default cell style if no rules match</param>
        /// <returns></returns>
        public bool ApplyFormatting(DataGridViewCell formattedCell, object value, bool isDarkMode, DataGridViewCellStyle defaultStyle)
        {
            if (IsStatusColumn)
            {
                if (int.TryParse((value.DBNullToNull() ?? string.Empty).ToString(), out var intValue))
                {
                    var status = DBADashStatus.ConvertToDBADashStatusEnum(intValue);
                    formattedCell.SetStatusColor(status ?? DBADashStatus.DBADashStatusEnum.NA);
                }
                else
                {
                    formattedCell.SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                }

                return true;
            }
            foreach (var rule in Rules)
            {
                if (rule.ApplyFormatting(formattedCell, value, isDarkMode))
                {
                    return true;
                }
            }
            formattedCell.Style = defaultStyle;
            return false;
        }
    }
}