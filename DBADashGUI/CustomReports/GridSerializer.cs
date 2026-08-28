using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Saves/loads a <see cref="DataTable"/> to/from a DBA Dash-native file, dispatching on the file extension.
    /// Supported formats:
    /// <list type="bullet">
    ///   <item><b>.xml</b> - <c>DataTable.WriteXml</c> with <see cref="XmlWriteMode.WriteSchema"/>.  Loss-less: the
    ///   schema (column names and types) travels with the data so it round-trips exactly.</item>
    ///   <item><b>.json</b> - Newtonsoft JSON (compact, human-readable).  Column types are inferred from the JSON
    ///   token types on load (integer/float/date map back to numeric/DateTime).</item>
    ///   <item><b>.json.gz</b> - the same JSON, gzip-compressed.  Best for large traces (event JSON compresses very
    ///   well) while staying re-openable.</item>
    /// </list>
    /// Used both for the general grid export (right-click a grid) and the XE trace file viewer's save/reload, so any
    /// grid's data can be saved and re-opened.
    /// </summary>
    public static class GridSerializer
    {
        public const string JsonExtension = ".json";
        public const string CompressedJsonExtension = ".json.gz";
        public const string XmlExtension = ".xml";
        public const string CompressedXmlExtension = ".xml.gz";

        /// <summary>Filter for a Save dialog offering the native formats (JSON first / default).</summary>
        public const string SaveFilter =
            "JSON (*.json)|*.json|Compressed JSON (*.json.gz)|*.json.gz|" +
            "DataTable XML (*.xml)|*.xml|Compressed XML (*.xml.gz)|*.xml.gz";

        /// <summary>Filter for an Open dialog offering all native formats.</summary>
        public const string OpenFilter =
            "DBA Dash grid files (*.json;*.json.gz;*.xml;*.xml.gz)|*.json;*.json.gz;*.xml;*.xml.gz|" +
            "JSON (*.json)|*.json|Compressed JSON (*.json.gz)|*.json.gz|" +
            "DataTable XML (*.xml)|*.xml|Compressed XML (*.xml.gz)|*.xml.gz";

        private enum Format
        { Json, Xml }

        /// <summary>Resolves a path's extension to (format, is-gzip-compressed), or throws for an unsupported type.</summary>
        private static (Format Fmt, bool Compressed) Resolve(string path)
        {
            if (EndsWith(path, CompressedJsonExtension)) return (Format.Json, true);
            if (EndsWith(path, CompressedXmlExtension)) return (Format.Xml, true);
            if (EndsWith(path, ".gz")) return (Format.Json, true); // bare .gz -> treat as compressed JSON
            if (EndsWith(path, XmlExtension)) return (Format.Xml, false);
            if (EndsWith(path, JsonExtension)) return (Format.Json, false);
            throw new NotSupportedException(
                $"Unsupported grid file type '{Path.GetExtension(path)}'.  Use {JsonExtension}, {CompressedJsonExtension} or {XmlExtension}.");
        }

        private static bool EndsWith(string path, string suffix) =>
            path != null && path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>True when the path has an extension this serializer can read/write.</summary>
        public static bool IsNativeExtension(string path) =>
            EndsWith(path, CompressedJsonExtension) || EndsWith(path, CompressedXmlExtension) || EndsWith(path, ".gz") ||
            EndsWith(path, XmlExtension) || EndsWith(path, JsonExtension);

        /// <summary>The 1-based Save-dialog filter index that matches the given save extension (see <see cref="SaveFilter"/>).</summary>
        public static int SaveFilterIndex(string extension)
        {
            if (EndsWith(extension, CompressedXmlExtension)) return 4;
            if (EndsWith(extension, XmlExtension)) return 3;
            if (EndsWith(extension, CompressedJsonExtension) || EndsWith(extension, ".gz")) return 2;
            return 1; // .json
        }

        /// <summary>Writes the table to <paramref name="path"/> in the format implied by its extension.</summary>
        public static void SaveDataTable(DataTable dt, string path)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            var (fmt, compressed) = Resolve(path);

            using var fileStream = File.Create(path);
            // Dispose order for the gzip case: writer/XML flush -> gzip footer written -> file closed.
            using var outStream = compressed
                ? (Stream)new GZipStream(fileStream, CompressionLevel.Optimal)
                : fileStream;

            if (fmt == Format.Xml)
            {
                // WriteXml throws when the DataTable is unnamed (history/DataView-derived tables often are), so give it a
                // temporary name and restore it afterwards to avoid a surprising side effect on the caller's table.
                var originalName = dt.TableName;
                if (string.IsNullOrEmpty(dt.TableName)) dt.TableName = "Grid";
                try
                {
                    // WriteSchema so column types survive the round-trip (a schema-less XML would reload as string).
                    dt.WriteXml(outStream, XmlWriteMode.WriteSchema);
                }
                finally
                {
                    dt.TableName = originalName;
                }
                return;
            }

            var json = JsonConvert.SerializeObject(dt, Formatting.Indented);
            using var writer = new StreamWriter(outStream, new UTF8Encoding(false));
            writer.Write(json);
        }

        /// <summary>Reads a table previously written by <see cref="SaveDataTable"/>.</summary>
        public static DataTable LoadDataTable(string path)
        {
            var (fmt, compressed) = Resolve(path);

            using var fileStream = File.OpenRead(path);
            using var inStream = compressed
                ? (Stream)new GZipStream(fileStream, CompressionMode.Decompress)
                : fileStream;

            if (fmt == Format.Xml)
            {
                var dt = new DataTable();
                dt.ReadXml(inStream);
                return dt;
            }

            using var reader = new StreamReader(inStream);
            return JsonConvert.DeserializeObject<DataTable>(reader.ReadToEnd()) ?? new DataTable();
        }
    }
}
