using System;
using System.Data;
using System.IO;
using System.IO.Compression;

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

        /// <summary>
        /// Reads a DataSet from a stream of DataSet XML, gunzipping it first when <paramref name="compressed"/> is set.
        /// Works on a non-seekable stream, such as an S3 response.
        /// </summary>
        public static DataSet ReadFromStream(Stream stream, bool compressed)
        {
            var ds = new DataSet();
            if (compressed)
            {
                using var gz = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
                ds.ReadXml(gz);
            }
            else
            {
                ds.ReadXml(stream);
            }
            return ds;
        }

        public static DataSet DeserializeFromXmlFile(string filePath)
        {
            // FileMode.Open (not OpenOrCreate).  A file that was deleted between being listed and read here
            // must throw FileNotFoundException so the caller can skip it.  Creating the file instead leaves a
            // zero byte file behind that can never be imported ("Root element is missing").
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            return ReadFromStream(fs, compressed: false);
        }

        public static DataSet DeserializeFromCompressedXmlFile(string filePath)
        {
            // As DeserializeFromXmlFile - FileMode.Open so a file deleted since it was listed throws rather than being created.
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            return ReadFromStream(fs, compressed: true);
        }

        public static DataSet DeserializeFromFile(string filePath)
        {
            if (DestinationHandling.IsCompressedFileName(filePath))
            {
                return DeserializeFromCompressedXmlFile(filePath);
            }
            if (filePath.EndsWith(DestinationHandling.FileExtension))
            {
                return DeserializeFromXmlFile(filePath);
            }
            else
            {
                throw new Exception($"Invalid file extension {filePath} expected: {DestinationHandling.FileExtension} or {DestinationHandling.CompressedFileExtension}");
            }
        }
    }
}
