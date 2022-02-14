using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI
{
    public static class ExtensionMethods
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
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
            foreach(int i in list)
            {
               var r =  dt.NewRow();
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
            if(cell.GetType()== typeof(DataGridViewLinkCell))
            {
                ((DataGridViewLinkCell)cell).LinkColor = StatusColor.ContrastColor();
            }
        }
        public static void SetStatusColor(this DataGridViewCell cell, DBADashStatusEnum Status)
        {
            cell.SetStatusColor(Status.GetColor());
        }
    }
}
