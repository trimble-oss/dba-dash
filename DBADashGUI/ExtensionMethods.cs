using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


    }
}
