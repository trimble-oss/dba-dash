using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Reads a results grid SSMS saved as XML (Save Results As... XML): a &lt;Data&gt; root with a &lt;Row&gt; per row
    /// and an element per column, named with XmlConvert's encoding ("dd hh:mm:ss.mss" is dd_x0020_hh_x003A_...), and
    /// xsi:nil="true" for a NULL.
    ///
    /// Unlike the DataTable XML DBA Dash writes, there's no schema - every value is text.  So each column's type is
    /// worked out from its values: whole numbers, decimals and dates where every value in the column is one, so they
    /// sort and filter as such.  sp_WhoIsActive's padded "   9,688" counts are the reason thousands separators are
    /// accepted.  Anything else stays text.
    /// </summary>
    public static partial class SsmsResultsXml
    {
        private const string RootName = "Data";
        private const string RowName = "Row";
        private static readonly XNamespace Xsi = XmlSchema.InstanceNamespace;

        private static readonly XmlReaderSettings ReaderSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            CloseInput = false
        };

        /// <summary>
        /// True when the XML is SSMS's results format: a &lt;Data&gt; root, empty or with a &lt;Row&gt; first.  Only
        /// reads as far as that first row, so the start of a file is enough.
        /// </summary>
        public static bool IsResultsXml(TextReader text)
        {
            try
            {
                using var reader = XmlReader.Create(text, ReaderSettings);
                if (reader.MoveToContent() != XmlNodeType.Element || !IsNamed(reader, RootName)) return false;
                if (reader.IsEmptyElement) return true;

                reader.Read();
                return reader.MoveToContent() switch
                {
                    XmlNodeType.EndElement => true,
                    XmlNodeType.Element => IsNamed(reader, RowName),
                    _ => false
                };
            }
            catch (XmlException)
            {
                return false;
            }
        }

        /// <summary>True when the XML is SSMS's results format - see <see cref="IsResultsXml(TextReader)"/>.</summary>
        public static bool IsResultsXml(Stream stream)
        {
            using var text = new StreamReader(stream, leaveOpen: true);
            return IsResultsXml(text);
        }

        private static bool IsNamed(XmlReader reader, string name) =>
            reader.LocalName == name && reader.NamespaceURI.Length == 0;

        /// <summary>Reads the results into a table, its column types inferred from the values.</summary>
        public static DataTable Load(Stream stream)
        {
            var names = new List<string>();
            var rows = new List<string[]>();

            using (var reader = XmlReader.Create(stream, ReaderSettings))
            {
                reader.MoveToContent();
                if (!IsNamed(reader, RootName)) throw new InvalidDataException("Not an SSMS results XML file.");

                if (!reader.IsEmptyElement)
                {
                    reader.Read();
                    while (reader.MoveToContent() == XmlNodeType.Element)
                    {
                        rows.Add(ReadRow((XElement)XNode.ReadFrom(reader), names));
                    }
                }
            }

            var table = new DataTable("Results");
            for (var i = 0; i < names.Count; i++)
            {
                var column = i;
                table.Columns.Add(names[i], InferType(rows.Select(r => column < r.Length ? r[column] : null)));
            }

            table.BeginLoadData();
            foreach (var row in rows)
            {
                var values = new object[names.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = Convert(i < row.Length ? row[i] : null, table.Columns[i].DataType);
                }

                table.Rows.Add(values);
            }

            table.EndLoadData();
            return table;
        }

        /// <summary>
        /// A row's values in column order, null for a NULL.  Columns are added as they're first seen, so a column a row
        /// leaves out is NULL in it.  A name seen twice in a row is a second column of the same name - a result set can
        /// have those - and is told apart with a number, as the grid needs unique column names.
        /// </summary>
        private static string[] ReadRow(XElement row, List<string> names)
        {
            var values = new string[names.Count];
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in row.Elements())
            {
                var name = XmlConvert.DecodeName(cell.Name.LocalName);
                if (string.IsNullOrWhiteSpace(name)) name = "(No column name)";

                var occurrence = seen[name] = seen.GetValueOrDefault(name) + 1;
                var columnName = occurrence == 1 ? name : $"{name} ({occurrence})";

                var index = names.FindIndex(n => n.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    index = names.Count;
                    names.Add(columnName);
                }

                if (index >= values.Length) Array.Resize(ref values, names.Count);
                values[index] = CellValue(cell);
            }

            return values;
        }

        /// <summary>
        /// A cell's value: null for xsi:nil, its text otherwise - or, if it holds elements rather than text, the XML
        /// itself, so a plan or deadlock graph written unescaped still opens in its viewer from the grid.
        /// </summary>
        private static string CellValue(XElement cell)
        {
            if ((bool?)cell.Attribute(Xsi + "nil") == true) return null;
            return cell.HasElements ? string.Concat(cell.Nodes()) : cell.Value;
        }

        /// <summary>
        /// The narrowest of long, decimal and DateTime every non-NULL value in the column is, or string.  A column of
        /// NULLs is string, as is one with a number that has a leading zero - a code, not a quantity.
        /// </summary>
        private static Type InferType(IEnumerable<string> values)
        {
            bool isLong = true, isDecimal = true, isDate = true, any = false;
            foreach (var value in values)
            {
                if (value == null) continue;
                any = true;
                if (isLong) isLong = TryParseLong(value, out _);
                if (isDecimal) isDecimal = TryParseDecimal(value, out _);
                if (isDate) isDate = TryParseDate(value, out _);
                if (!isLong && !isDecimal && !isDate) return typeof(string);
            }

            if (!any) return typeof(string);
            if (isLong) return typeof(long);
            if (isDecimal) return typeof(decimal);
            return isDate ? typeof(DateTime) : typeof(string);
        }

        private static object Convert(string value, Type type)
        {
            if (value == null) return DBNull.Value;
            if (type == typeof(long) && TryParseLong(value, out var l)) return l;
            if (type == typeof(decimal) && TryParseDecimal(value, out var d)) return d;
            if (type == typeof(DateTime) && TryParseDate(value, out var dt)) return dt;
            return value;
        }

        // Thousands separators only in their proper places, so a list like "1,2" isn't read as 12.
        [GeneratedRegex(@"^-?(0|[1-9]\d{0,2}(,\d{3})+|[1-9]\d*)(\.\d+)?$")]
        private static partial Regex NumberPattern();

        private static bool TryParseLong(string value, out long result)
        {
            result = 0;
            var trimmed = value.Trim();
            return !trimmed.Contains('.') && NumberPattern().IsMatch(trimmed) &&
                   long.TryParse(trimmed.Replace(",", ""), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseDecimal(string value, out decimal result)
        {
            result = 0;
            var trimmed = value.Trim();
            return NumberPattern().IsMatch(trimmed) &&
                   decimal.TryParse(trimmed.Replace(",", ""), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                       CultureInfo.InvariantCulture, out result);
        }

        /// <summary>The way SSMS writes datetime, datetime2, smalldatetime and date.</summary>
        private static readonly string[] DateFormats =
        [
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-dd"
        ];

        private static bool TryParseDate(string value, out DateTime result) =>
            DateTime.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}
