using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// A single rule and associated formatting that should be applied if the rule is met.
    /// </summary>
    [Serializable]
    public class CellHighlightingRule
    {
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }

        public Color BackColorDark { get; set; }
        public Color ForeColorDark { get; set; }

        public Font Font { get; set; }

        public DBADashStatus.DBADashStatusEnum? Status { get; set; }

        private bool value1IsNumeric;
        private bool value2IsNumeric;
        private bool value1IsDateTime;
        private bool value2IsDateTime;

        /// <summary>
        /// Value associated with the condition.  Used for all conditions except IsNull, IsNotNull, and All.
        /// </summary>
        public string Value1
        {
            get => ShouldValue1BeNull ? null : value1String;
            set
            {
                value1IsNumeric = decimal.TryParse(value, out value1Decimal);
                value1IsDateTime = DateTime.TryParse(value, out value1DateTime);
                value1String = value;
            }
        }

        /// <summary>
        /// Used for Between condition only
        /// </summary>
        public string Value2
        {
            get => ShouldValue2BeNull ? null : value2String;
            set
            {
                value2IsNumeric = decimal.TryParse(value, out value2Decimal);
                value2IsDateTime = DateTime.TryParse(value, out value2DateTime);
                value2String = value;
            }
        }

        /// <summary>
        /// Option for case sensitive string comparision.  Default is false.
        /// </summary>
        public bool CaseSensitive { get; set; }

        private StringComparison StringComparison => CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
        private RegexOptions RegexOpt => CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

        private string value1String;
        private string value2String;
        private decimal value1Decimal;
        private decimal value2Decimal;
        private DateTime value1DateTime;
        private DateTime value2DateTime;

        private bool ShouldValue1BeNull => ConditionType is ConditionTypes.IsNull or ConditionTypes.IsNotNull or ConditionTypes.All;

        private bool ShouldValue2BeNull => ConditionType != ConditionTypes.Between;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum ConditionTypes
        {
            Equals,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
            Between,
            BeginsWith, /* string only */
            EndsWith, /* string only */
            Contains, /* string only */
            NotContains, /* string only */
            Like, /* string only */
            NotLike, /* string only */
            IsNull,
            IsNotNull,
            All
        }

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ConditionTypes ConditionType { get; set; }

        [JsonIgnore]
        public string RuleDescription => $"{ConditionType} {Value1} {Value2}";

        /// <summary>
        /// Call to apply formatting to a cell if the condition is met.
        /// </summary>
        /// <param name="formattedCell">The cell to apply formatting to</param>
        /// <param name="value">The value to check against the rule.  Usually the value of the formatted cell but it could also be from a different column</param>
        /// <param name="isDarkMode"></param>
        /// <returns></returns>
        public bool ApplyFormatting(DataGridViewCell formattedCell, object value, bool isDarkMode)
        {
            bool conditionMet;
            try
            {
                conditionMet = CheckCondition(value);
            }
            catch
            {
                return false;
            }

            if (!conditionMet) return false;
            ApplyFormatting(formattedCell, isDarkMode);
            return true;
        }

        /// <summary>
        /// Basic validation of the rule.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool RuleIsValid()
        {
            // Check that values are valid for the condition type
            switch (ConditionType)
            {
                case ConditionTypes.IsNull:
                case ConditionTypes.IsNotNull:
                case ConditionTypes.All:
                    return true;

                case ConditionTypes.BeginsWith:
                case ConditionTypes.EndsWith:
                case ConditionTypes.Contains:
                case ConditionTypes.NotContains:
                case ConditionTypes.Like:
                case ConditionTypes.NotLike:
                case ConditionTypes.Equals:
                case ConditionTypes.GreaterThan:
                case ConditionTypes.GreaterThanOrEqual:
                case ConditionTypes.LessThan:
                case ConditionTypes.LessThanOrEqual:
                    return Value1 != null;

                case ConditionTypes.Between:
                    return Value1 != null && Value2 != null && (string.Compare(Value2, Value1, StringComparison) > 0 || value1IsDateTime && value2IsDateTime && value2DateTime > value1DateTime || value2IsNumeric && value1IsNumeric && value2Decimal > value1Decimal);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Applies formatting to a cell when the condition is met.
        /// </summary>
        /// <param name="formattedCell">The cell to apply formatting to</param>
        /// <param name="isDarkMode">Option to use dark mode formatting if specified</param>
        private void ApplyFormatting(DataGridViewCell formattedCell, bool isDarkMode)
        {
            if (Status != null)
            {
                formattedCell.SetStatusColor((DBADashStatus.DBADashStatusEnum)(Status));
                formattedCell.Style.Font = Font;
                return;
            }
            var b = isDarkMode && BackColorDark != Color.Empty ? BackColorDark : BackColor;
            var f = isDarkMode && ForeColorDark != Color.Empty ? ForeColorDark : ForeColor;
            formattedCell.Style.BackColor = b;
            formattedCell.Style.ForeColor = f;
            formattedCell.Style.Font = Font;

            if (formattedCell is DataGridViewLinkCell linkCell)
            {
                linkCell.LinkColor = f;
                linkCell.ActiveLinkColor = f;
                linkCell.VisitedLinkColor = f;
            }
            formattedCell.Style.SelectionBackColor = b.AdjustBasedOnLuminance();
        }

        /// <summary>
        /// Evaluates if the condition is met for the given value.
        /// </summary>
        /// <param name="value">The value to check the condition for.</param>
        /// <returns>True if the condition is met, otherwise false.</returns>
        private bool CheckCondition(object value)
        {
            switch (ConditionType)
            {
                case ConditionTypes.IsNull:
                    return value.DBNullToNull() == null;

                case ConditionTypes.IsNotNull:
                    return value.DBNullToNull() != null;

                case ConditionTypes.All:
                    return true;

                case ConditionTypes.BeginsWith:
                case ConditionTypes.EndsWith:
                case ConditionTypes.Contains:
                case ConditionTypes.NotContains:
                case ConditionTypes.Like:
                case ConditionTypes.NotLike:
                    // string only conditions
                    return CheckConditionString(value.DBNullToNull()?.ToString());

                case ConditionTypes.Equals:
                case ConditionTypes.GreaterThan:
                case ConditionTypes.GreaterThanOrEqual:
                case ConditionTypes.LessThan:
                case ConditionTypes.LessThanOrEqual:
                case ConditionTypes.Between:
                default:
                    if (value.GetType().IsNumericType() &&
                        decimal.TryParse(value.DBNullToNull()?.ToString(), out var cellValueDecimal) && value1IsNumeric)
                    {
                        // Do a numeric comparision is cell is numeric and condition is numeric
                        return CheckConditionNumeric(cellValueDecimal);
                    }
                    else if (value is DateTime && value1IsDateTime)
                    {
                        // Do a date comparision is cell is date and condition is date
                        if (value is DateTime cellValueDateTime)
                        {
                            return CheckConditionDateTime(cellValueDateTime);
                        }

                        return false;
                    }
                    else
                    {
                        // Default to string comparision
                        return CheckConditionString(value.DBNullToNull()?.ToString());
                    }
            }
        }

        /// <summary>
        /// Evaluates if the condition is met for the given value that is DateTime.
        /// </summary>
        /// <param name="cellValue">The value to check the condition for.</param>
        /// <returns>True if the condition is met, otherwise false.</returns>
        private bool CheckConditionDateTime(DateTime cellValue)
        {
            return ConditionType switch
            {
                ConditionTypes.Equals => cellValue == value1DateTime,
                ConditionTypes.GreaterThan => cellValue > value1DateTime,
                ConditionTypes.GreaterThanOrEqual => cellValue >= value1DateTime,
                ConditionTypes.LessThan => cellValue < value1DateTime,
                ConditionTypes.LessThanOrEqual => cellValue <= value1DateTime,
                ConditionTypes.Between => cellValue >= value1DateTime && cellValue <= value2DateTime,
                _ => false
            };
        }

        /// <summary>
        /// Evaluates if the condition is met for the given value that is a number.
        /// </summary>
        /// <param name="cellValue">The value to check the condition for.</param>
        /// <returns>True if the condition is met, otherwise false.</returns>

        private bool CheckConditionNumeric(decimal cellValue)
        {
            return ConditionType switch
            {
                ConditionTypes.Equals => cellValue == value1Decimal,
                ConditionTypes.GreaterThan => cellValue > value1Decimal,
                ConditionTypes.GreaterThanOrEqual => cellValue >= value1Decimal,
                ConditionTypes.LessThan => cellValue < value1Decimal,
                ConditionTypes.LessThanOrEqual => cellValue <= value1Decimal,
                ConditionTypes.Between => cellValue >= value1Decimal && cellValue <= value2Decimal,
                _ => false
            };
        }

        /// <summary>
        /// Evaluates if the condition is met for the given value that is a string or other object without special handling.
        /// </summary>
        /// <param name="cellValue">The value to check the condition for.</param>
        /// <returns>True if the condition is met, otherwise false.</returns>
        private bool CheckConditionString(string cellValue)
        {
            if (Value1 == null || cellValue == null) return false;
            return ConditionType switch
            {
                ConditionTypes.Equals => cellValue.Equals(Value1, StringComparison),
                ConditionTypes.BeginsWith => cellValue.StartsWith(Value1, StringComparison),
                ConditionTypes.EndsWith => cellValue.EndsWith(Value1, StringComparison),
                ConditionTypes.Contains => CaseSensitive ? cellValue.Contains(Value1) : cellValue.ToLower().Contains(Value1.ToLower()),
                ConditionTypes.NotContains => !(CaseSensitive ? cellValue.Contains(Value1) : cellValue.ToLower().Contains(Value1.ToLower())),
                ConditionTypes.Like => Regex.IsMatch(cellValue, Value1, RegexOpt),
                ConditionTypes.NotLike => !Regex.IsMatch(cellValue, Value1, RegexOpt),
                ConditionTypes.GreaterThan => string.Compare(cellValue, Value1, StringComparison) > 0,
                ConditionTypes.GreaterThanOrEqual => string.Compare(cellValue, Value1, StringComparison) >= 0,
                ConditionTypes.LessThan => string.Compare(cellValue, Value1, StringComparison) < 0,
                ConditionTypes.LessThanOrEqual => string.Compare(cellValue, Value1, StringComparison) <= 0,
                ConditionTypes.Between =>
                    Value2 != null &&
                    string.Compare(cellValue, Value1, StringComparison) >= 0 &&
                    string.Compare(cellValue, Value2, StringComparison) <= 0,
                _ => false
            };
        }
    }
}