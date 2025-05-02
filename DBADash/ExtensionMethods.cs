﻿using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

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

        public static string ToHexString(this byte[] bytes)
        {
            if (bytes == null) return "";
            var hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
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
    }
}