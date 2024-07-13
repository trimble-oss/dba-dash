using System;
using System.Data;
using System.IO;

namespace DBADash
{
    public class DataSetSerialization
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
                throw new Exception($"Invalid file extension {filePath} expected: .xml");
            }
        }
    }
}