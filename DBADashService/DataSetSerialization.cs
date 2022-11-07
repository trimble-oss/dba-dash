using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

        [ObsoleteAttribute("To be removed. Json serialization replaced with XML serialization", false)]
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

        public static DataSet DeserializeFromXmlFile(string filePath)
        {
            using FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.Read);
            var ds = new DataSet();
            ds.ReadXml(fs);
            return ds;
        }

        public static DataSet DeserializeFromFile(string filePath)
        {
            if (filePath.EndsWith(".xml"))
            {
                return DeserializeFromXmlFile(filePath);
            }
            else
            {
                throw new Exception($"Invalid file extention { filePath } expected: .xml");
            }
        }
    }
}
