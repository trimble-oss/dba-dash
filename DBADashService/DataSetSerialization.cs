using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashService
{
    class DataSetSerialization
    {
        public static void SetDateTimeKind(DataSet ds) // Required for binary serialization to prevent dates captured in UTC from being converted to local timezone on deserialization
        {
            foreach (DataTable dt in ds.Tables)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(DateTime))
                    {
                        col.DateTimeMode = DataSetDateTime.Unspecified;
                    }
                }
            }
        }


        private static readonly string binaryPrefix = "###BINARY###";
        public static string SerializeDS(DataSet ds)
        {
            foreach(DataTable dt in ds.Tables)
            {
                foreach(DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(byte[]))
                    {
                        col.ColumnName = binaryPrefix + col.ColumnName;
                    }
                }
            }
            string json = JsonConvert.SerializeObject(ds, Formatting.None);
            foreach (DataTable dt in ds.Tables)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(byte[]))
                    {
                        col.ColumnName = col.ColumnName.Replace(binaryPrefix, "");
                    }
                }
            }
            return json;
        }

        public static DataSet  DeserializeDS(string json)
        {
            var ds = JsonConvert.DeserializeObject<DataSet>(json);
            foreach (DataTable dt in ds.Tables)
            {
                for(Int32 i = 0;i<  dt.Columns.Count-1;  i++)
                {
                    var col = dt.Columns[i];
                    if (col.ColumnName.StartsWith(binaryPrefix))
                    {
                       var newCol = dt.Columns.Add(col.ColumnName.Remove(0, binaryPrefix.Length), typeof(byte[]));
                        foreach(DataRow r in dt.Rows)
                        {
                            if (r[col] != DBNull.Value)
                            {
                                r[newCol] = Convert.FromBase64String((string)r[col]);
                            }
                        }
                        newCol.SetOrdinal(col.Ordinal);
                        dt.Columns.Remove(col);
                    }
                    
                }
            }
            return ds;
        }
    }
}
