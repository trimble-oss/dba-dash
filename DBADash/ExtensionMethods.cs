using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.IO;

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
            var serialized = JsonConvert.SerializeObject(self);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value[..maxLength];
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
    }
}