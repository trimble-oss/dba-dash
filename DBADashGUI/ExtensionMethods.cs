using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DBADashGUI.Pickers;
using Microsoft.SqlServer.Management.Smo;
using static DBADashGUI.DBADashStatus;
using SpreadsheetLight;

namespace DBADashGUI
{
    public static class ExtensionMethods
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value[..Math.Min(value.Length, maxLength)];
        }

        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        public static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        public static DataTable AsDataTable(this IEnumerable<int> list)
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            foreach (int i in list)
            {
                var r = dt.NewRow();
                r["ID"] = i;
                dt.Rows.Add(r);
            }

            return dt;
        }

        public static Color GetBackColor(this DBADashStatusEnum value)
        {
            return DBADashStatus.GetStatusBackColor(value);
        }

        public static Color GetForeColor(this DBADashStatusEnum value)
        {
            return DBADashStatus.GetStatusForeColor(value);
        }

        public static void SetStatusColor(this DataGridViewCellStyle value, DBADashStatusEnum Status)
        {
            value.BackColor = Status.GetBackColor();
            value.ForeColor = Status.GetForeColor();
        }

        public static void SetColor(this DataGridViewCell cell, Color backColor, Color foreColor, Color selectionBackColor, Color selectionForeColor)
        {
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
            if (cell.GetType() == typeof(DataGridViewLinkCell))
            {
                ((DataGridViewLinkCell)cell).LinkColor = foreColor;
                ((DataGridViewLinkCell)cell).VisitedLinkColor = foreColor;
                ((DataGridViewLinkCell)cell).ActiveLinkColor = selectionForeColor;
            }

            cell.Style.SelectionForeColor = selectionForeColor;
            cell.Style.SelectionBackColor = selectionBackColor;
        }

        public static void SetColor(this DataGridViewCell cell, Color backColor, Color foreColor)
        {
            SetColor(cell, backColor, foreColor, backColor.AdjustBasedOnLuminance(), foreColor);
        }

        public static void SetColor(this DataGridViewCell cell, Color backColor)
        {
            SetColor(cell, backColor, backColor.ContrastColor());
        }

        public static void SetStatusColor(this DataGridViewCell cell, DBADashStatusEnum Status)
        {
            cell.SetColor(Status.GetBackColor(), Status.GetForeColor(), Status.GetBackColor().AdjustBasedOnLuminance(), Status.GetForeColor());
        }

        public static string ToHexString(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        /// <summary>
        /// Returns structure with column layout - size, position & visibility
        /// </summary>
        internal static List<KeyValuePair<string, PersistedColumnLayout>> GetColumnLayout(this DataGridView dgv)
        {
            return dgv.Columns.Cast<DataGridViewColumn>()
           .Select(c => new KeyValuePair<string, PersistedColumnLayout>(c.Name, new PersistedColumnLayout() { Visible = c.Visible, Width = c.Width, DisplayIndex = c.DisplayIndex }))
           .ToList();
        }

        /// <summary>
        /// Loads a saved column layout to the grid.  Size, position & visibility of columns
        /// </summary>
        internal static void LoadColumnLayout(this DataGridView dgv, List<KeyValuePair<string, PersistedColumnLayout>> savedCols)
        {
            if (savedCols == null)
            {
                return;
            }
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (savedCols.Where(savedCol => savedCol.Key == col.Name).Count() == 1)
                {
                    var savedCol = savedCols.Where(savedCol => savedCol.Key == col.Name).First();
                    col.Visible = savedCol.Value.Visible;
                    col.Width = savedCol.Value.Width;
                    if (savedCol.Value.DisplayIndex >= 0)
                    {
                        col.DisplayIndex = savedCol.Value.DisplayIndex;
                    }
                }
                else
                {
                    col.Visible = false;
                }
            }
        }

        internal static SQLTreeItem SelectedSQLTreeItem(this TreeView value)
        {
            return value.SelectedNode.AsSQLTreeItem();
        }

        internal static SQLTreeItem AsSQLTreeItem(this TreeNode value)
        {
            return (SQLTreeItem)value;
        }

        /// <summary>
        /// Add Guid SqlParameter to the collection only if parameter value is not empty
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddGuidIfNotEmpty(this SqlParameterCollection p, string parameterName, Guid value)
        {
            if (value != Guid.Empty)
            {
                return p.AddWithValue(parameterName, value);
            }
            return null;
        }

        /// <summary>
        /// Add Guid SqlParameter to the collection only if parameter value is not null or empty
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddStringIfNotNullOrEmpty(this SqlParameterCollection p, string parameterName, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return p.AddWithValue(parameterName, value);
            }
            return null;
        }

        /// <summary>
        /// Add parameter with value, passing DBNull.value in place of null
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddWithNullableValue(this SqlParameterCollection p, string parameterName, object value)
        {
            if (value == null)
                return p.AddWithValue(parameterName, DBNull.Value);
            else
                return p.AddWithValue(parameterName, value);
        }

        /// <summary>
        /// Add parameter with value if value is greater than zero
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddIfGreaterThanZero(this SqlParameterCollection p, string parameterName, int value)
        {
            if (value > 0)
                return p.AddWithValue(parameterName, value);
            else
                return null;
        }

        /// <summary>
        /// Add parameter with value if value is greater than zero
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddIfGreaterThanZero(this SqlParameterCollection p, string parameterName, long value)
        {
            if (value > 0)
                return p.AddWithValue(parameterName, value);
            else
                return null;
        }

        /// <summary>
        /// Add parameter with value if value is less than max value
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddIfLessThanMaxValue(this SqlParameterCollection p, string parameterName, long value)
        {
            if (value != long.MaxValue)
                return p.AddWithValue(parameterName, value);
            else
                return null;
        }

        /// <summary>
        /// Check a single ToolStripMenuItem.  Other menu items to be unchecked
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="checkedItem">Item to be checked.  Other drop down items will be unchecked</param>
        internal static void CheckSingleItem(this ToolStripDropDownButton dropdown, ToolStripMenuItem checkedItem)
        {
            foreach (ToolStripMenuItem mnu in dropdown.DropDownItems.OfType<ToolStripMenuItem>())
            {
                mnu.Checked = mnu == checkedItem;
            }
        }

        internal static void OpenAsTextFile(this string value)
        {
            string path = Common.GetTempFilePath(".txt");
            System.IO.File.WriteAllText(path, value);
            ProcessStartInfo psi = new() { FileName = path, UseShellExecute = true };
            Process.Start(psi);
        }

        public static List<ISelectable> ToSelectableList(this DataGridViewColumnCollection columns) => columns.Cast<DataGridViewColumn>()
                .Select(column => new SelectableColumn(column) as ISelectable)
                .ToList();

        public static void ApplyVisibility(this DataGridViewColumnCollection columns, List<ISelectable> selectables)
        {
            var columnDict = columns.Cast<DataGridViewColumn>()
                .ToDictionary(c => c.HeaderText, c => c);

            foreach (var selectable in selectables)
            {
                if (columnDict.TryGetValue(selectable.Name, out var column))
                {
                    column.Visible = selectable.IsVisible;
                }
            }
        }

        public static void ApplyVisibility(this Dictionary<string, ColumnMetaData> metrics, List<ISelectable> selectables) => selectables.Where(s => metrics.ContainsKey(s.Name))
                .ToList()
                .ForEach(s => metrics[s.Name].IsVisible = s.IsVisible);

        public static DialogResult PromptColumnSelection(this DataGridView dgv)
        {
            using var frm = new SelectColumns() { Items = dgv.Columns.ToSelectableList() };
            frm.ApplyTheme(DBADashUser.SelectedTheme);
            frm.ShowDialog(dgv);
            if (frm.DialogResult == DialogResult.OK)
            {
                dgv.Columns.ApplyVisibility(frm.Items);
            }

            return frm.DialogResult;
        }

        public static double RoundUpToSignificantFigures(this double num, int n = 1)
        {
            if (num == 0) return 0;

            double d = Math.Ceiling(Math.Log10(Math.Abs(num)));
            int power = n - (int)d;

            double magnitude = Math.Pow(10, power);
            long shifted = Convert.ToInt64(Math.Ceiling(num * magnitude));
            return shifted / magnitude;
        }

        public static List<ISelectable> ToSelectableList(this List<string> list)
        {
            return list.Select(s => new SelectableString(s) as ISelectable).ToList();
        }

        public static object DBNullToNull(this object obj)
        {
            return obj == DBNull.Value ? null : obj;
        }

        public static int GetValueAsInt(this Dictionary<string, object> dict, string key, int defaultValue)
        {
            // Check if the key exists
            if (dict.TryGetValue(key, out object value))
            {
                // Try to cast the value to an integer
                if (value is int intValue)
                {
                    return intValue;
                }
            }

            // If the key does not exist or the value is not an integer, return the default value
            return defaultValue;
        }

        public static double GetValueAsDouble(this Dictionary<string, object> dict, string key, double defaultValue)
        {
            // Check if the key exists
            if (dict.TryGetValue(key, out object value))
            {
                // Try to cast the value to a double
                if (double.TryParse(value.ToString(), out double dValue))
                {
                    return dValue;
                }
            }

            // If the key does not exist or the value is not a double, return the default value
            return defaultValue;
        }

        public static bool IsNumericType(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.Decimal => true,
                TypeCode.Double => true,
                TypeCode.Int16 => true,
                TypeCode.Int32 => true,
                TypeCode.Int64 => true,
                TypeCode.SByte => true,
                TypeCode.Single => true,
                TypeCode.UInt16 => true,
                TypeCode.UInt32 => true,
                TypeCode.UInt64 => true,
                _ => false
            };
        }

        public static SqlParameter Clone(this SqlParameter original)
        {
            return new SqlParameter
            {
                ParameterName = original.ParameterName,
                SqlDbType = original.SqlDbType,
                Direction = original.Direction,
                Size = original.Size,
                Value = original.Value,
                IsNullable = original.IsNullable,
                Precision = original.Precision,
                Scale = original.Scale,
                SourceColumn = original.SourceColumn,
                SourceVersion = original.SourceVersion
            };
        }

        /// <summary>
        /// Replace single ' quote with two single quotes ''.  Only to be used where input can't be parameterized
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlSingleQuote(this string value) => value.Replace("'", "''");
    }
}