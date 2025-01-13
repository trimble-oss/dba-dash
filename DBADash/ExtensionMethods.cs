using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.IO;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System;

namespace DBADash
{
    public static class ExtensionMethods
    {
        public static void AppendCollection(this StringCollection sc1, StringCollection sc2)
        {
            var array = new string[sc2.Count];
            sc2.CopyTo(array, 0);
            sc1.AddRange(array);
        }

        //https://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-like
        public static bool Like(this string toSearch, string toFind)
        {
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }

        public static T DeepCopy<T>(this T self)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto // Preserve type information
            };

            var serialized = JsonConvert.SerializeObject(self, settings);
            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }

        public static string Truncate(this string value, int maxLength, bool ellipsis = false)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (ellipsis)
            {
                return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
            }
            else
            {
                return value.Length <= maxLength ? value : value[..maxLength];
            }
        }

        /// <summary>
        /// Add items from dict2 to dict1 if they don't already exist in dict1.  If they do exist, use the value from dict1.
        /// </summary>
        /// <param name="dict1"></param>
        /// <param name="dict2"></param>
        /// <returns></returns>
        public static Dictionary<string, CustomCollection> CombineCollections(
            this Dictionary<string, CustomCollection> dict1, Dictionary<string, CustomCollection> dict2)
        {
            if (dict1 == null && dict2 == null) return new Dictionary<string, CustomCollection>();
            if (dict2 == null) return dict1;
            if (dict1 == null) return dict2;

            var result = new Dictionary<string, CustomCollection>(dict1);
            foreach (var item in dict2.Where(item => !result.ContainsKey(item.Key)))
            {
                result.Add(item.Key, item.Value);
            }
            return result;
        }

        public static byte[] Compress(this byte[] data)
        {
            using var sourceStream = new MemoryStream(data);
            using var destinationStream = new MemoryStream();
            sourceStream.CompressTo(destinationStream);
            return destinationStream.ToArray();
        }

        public static byte[] Decompress(this byte[] data)
        {
            using var sourceStream = new MemoryStream(data);
            using var destinationStream = new MemoryStream();
            sourceStream.DecompressTo(destinationStream);
            return destinationStream.ToArray();
        }

        public static void CompressTo(this Stream stream, Stream outputStream)
        {
            using var gZipStream = new GZipStream(outputStream, CompressionMode.Compress);
            stream.CopyTo(gZipStream);
            gZipStream.Flush();
        }

        public static void DecompressTo(this Stream stream, Stream outputStream)
        {
            using var gZipStream = new GZipStream(stream, CompressionMode.Decompress);
            gZipStream.CopyTo(outputStream);
        }

        public static string AppendToUrl(this string url, string appendString)
        {
            return (url.EndsWith('/') ? url : url + '/') + appendString;
        }

        public static SqlParameter[] GetParameters(this List<CustomSqlParameter> parameters) => parameters.Where(p => !p.UseDefaultValue).Select(p => p.Param).ToArray();

        public static void AddDecompressedColumns(this DataTable table, Dictionary<string, string> columnMappings)
        {
            // Validate all compressed columns exist before processing
            var missingColumns = columnMappings.Keys.Where(col => !table.Columns.Contains(col)).ToList();
            if (missingColumns.Count != 0)
                throw new ArgumentException($"Columns not found in DataTable: {string.Join(", ", missingColumns)}");

            // Add new columns first
            foreach (var mapping in columnMappings)
            {
                if (!table.Columns.Contains(mapping.Value))
                    table.Columns.Add(mapping.Value, typeof(string));
            }

            // Process each row once for all columns
            foreach (DataRow row in table.Rows)
            {
                foreach (var mapping in columnMappings)
                {
                    string compressedColumnName = mapping.Key;
                    string decompressedColumnName = mapping.Value;

                    // Skip if the compressed data is null
                    if (row[compressedColumnName] == DBNull.Value)
                    {
                        continue;
                    }

                    try
                    {
                        byte[] compressedData = (byte[])row[compressedColumnName];
                        string decompressedString = Encoding.Unicode.GetString(compressedData.Decompress().ToArray());
                        row[decompressedColumnName] = decompressedString;
                    }
                    catch (Exception ex)
                    {
                        row[decompressedColumnName] = $"Decompression error: {ex.Message}";
                    }
                }
            }
        }
    }
}