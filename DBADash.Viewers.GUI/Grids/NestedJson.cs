using DBADashGUI.CustomReports;
using System;
using System.Data;
using System.IO;
using System.Text.Json;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// JSON inside a grid cell: a nested object or array of a JSON file's records (which <see cref="JsonRecords"/> keeps
    /// as its JSON), or JSON held as text - a log property that is itself serialized JSON, an nvarchar column of JSON
    /// from a query.  A column of it is shown as links, as SSMS shows a json column, and a cell of it can be opened as a
    /// grid of its own.
    /// </summary>
    public static class NestedJson
    {
        /// <summary>
        /// How many of a column's values are parsed to be sure it's JSON.  Every value has to look like it - the cheap
        /// check - but parsing them all could take a while for a large log.  One that turns out not to be JSON opens as
        /// it is.
        /// </summary>
        private const int ParsedSampleSize = 20;

        /// <summary>
        /// Cheaply, whether <paramref name="text"/> could be a JSON object or array: it starts with { or [ and ends with
        /// the matching bracket, whitespace aside.  Not parsed - see <see cref="IsJson"/>.
        /// </summary>
        public static bool LooksLikeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var start = 0;
            while (start < text.Length && char.IsWhiteSpace(text[start])) start++;
            var end = text.Length - 1;
            while (end > start && char.IsWhiteSpace(text[end])) end--;
            if (end <= start) return false;

            return (text[start], text[end]) is ('{', '}') or ('[', ']');
        }

        /// <summary>Whether <paramref name="text"/> is a JSON object or array.</summary>
        public static bool IsJson(string text)
        {
            if (!LooksLikeJson(text)) return false;
            try
            {
                using var _ = JsonDocument.Parse(text);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether every value of a text column is a JSON object or array - with at least one value, so a column that's
        /// all NULL isn't.
        /// </summary>
        public static bool IsJsonColumn(DataTable table, DataColumn column)
        {
            if (column.DataType != typeof(string)) return false;

            var parsed = 0;
            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted || row[column] is not string text) continue;
                if (!LooksLikeJson(text)) return false;
                if (parsed < ParsedSampleSize)
                {
                    if (!IsJson(text)) return false;
                    parsed++;
                }
            }

            return parsed > 0;
        }

        /// <summary>
        /// Reads a cell's JSON as grid tables, the way a JSON file is read (<see cref="JsonRecords"/>): an array of
        /// objects a row each, an object one row - or a table per property, where every property is an array of objects.
        /// </summary>
        public static DataSet ToDataSet(string json) => JsonRecords.Load(() => new StringReader(json));
    }
}
