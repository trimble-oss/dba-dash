using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

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

        public static DataTable AsDataTable(this List<int> list)
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

        public static Color GetColor(this DBADashStatusEnum value)
        {
            return DBADashStatus.GetStatusColour(value);
        }

        public static Color ContrastColor(this Color value)
        {
            return ((value.R * 0.299) + (value.G * 0.587) + (value.B * 0.114)) > 186 ? Color.Black : Color.White;
        }

        public static void SetStatusColor(this DataGridViewCell cell, Color StatusColor)
        {
            cell.Style.BackColor = StatusColor;
            cell.Style.ForeColor = StatusColor.ContrastColor();
            if (cell.GetType() == typeof(DataGridViewLinkCell))
            {
                ((DataGridViewLinkCell)cell).LinkColor = StatusColor.ContrastColor();
                ((DataGridViewLinkCell)cell).VisitedLinkColor = StatusColor.ContrastColor();
            }
            cell.Style.SelectionBackColor = ControlPaint.Light(StatusColor);
        }

        public static void SetStatusColor(this DataGridViewCell cell, DBADashStatusEnum Status)
        {
            cell.SetStatusColor(Status.GetColor());
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
    }
}