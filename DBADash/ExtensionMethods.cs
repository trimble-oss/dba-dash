using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public static string ToHexString(this byte[] bytes, bool prefixZeroX = false)
        {
            if (bytes == null) return "";
            var hex = BitConverter.ToString(bytes);
            return (prefixZeroX ? "0x" : string.Empty) + hex.Replace("-", "");
        }

        public static byte[] ToByteArray(this string hex)
        {
            if (hex.StartsWith("0x"))
            {
                hex = hex.Remove(0, 2);
            }
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        /// <summary>
        /// Replace single ' quote with two single quotes '' and encloses in single quotes.  Only to be used where input can't be parameterized
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlSingleQuoteWithEncapsulation(this string value) => $"'{value.SqlSingleQuote()}'";

        /// <summary>
        /// Replace single ' quote with two single quotes ''.  Only to be used where input can't be parameterized
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlSingleQuote(this string value) => value.Replace("'", "''");

        /// <summary>
        /// Replicates SQL Server QUOTENAME function - wrapping text in square brackets and doubling up on right square bracket.  Use SqlSingleQuote/SqlSingleQuoteWithEncapsulation for single quotes.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlQuoteName(this string value) => $"[{value.Truncate(128).Replace("]", "]]")}]";

        /// <summary>
        /// Attach a continuation that runs only if the task faults. Success or cancellation adds virtually no overhead.
        /// </summary>
        /// <param name="task">The task to observe.</param>
        /// <param name="onError">Optional error handler (log, trace, etc).</param>
        public static void ObserveFault(this Task task, Action<Exception> onError = null)
        {
            if (task == null) return;
            _ = task.ContinueWith(t =>
            {
                onError?.Invoke(t.Exception!.Flatten());
                _ = t.Exception; // mark observed
            },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Convert DateTime to DateTimeOffset treating it as UTC time (offset zero).
        /// </summary>
        /// <param name="dateTime">DateTime to convert (treated as UTC regardless of Kind)</param>
        /// <returns>DateTimeOffset with UTC offset</returns>
        public static DateTimeOffset ToUtcDateTimeOffset(this DateTime dateTime) => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));

        /// <summary>
        /// Formats a DateTimeOffset to a string in ISO 8601 format. If the offset is zero (UTC), it appends 'Z' to indicate UTC time. Otherwise, it includes the offset in the format ±hh:mm.
        /// </summary>
        /// <param name="date">The DateTimeOffset to format</param>
        /// <returns>ISO 8601 formatted string: yyyy-MM-dd'T'HH:mm:ss'Z' for UTC (zero offset), or yyyy-MM-dd'T'HH:mm:sszzz for non-UTC offsets</returns>
        public static string ToStandardString(this DateTimeOffset date)
        {
            return date.Offset == TimeSpan.Zero
                ? date.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
                : date.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        }
    }
}