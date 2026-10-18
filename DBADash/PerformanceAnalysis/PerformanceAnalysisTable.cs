#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace DBADash.PerformanceAnalysis
{
    /// <summary>
    /// A table as the model is given it: a header and pipe-separated rows.
    ///
    /// Not JSON.  The same figures as JSON repeat every property name on every row, which is most of the
    /// payload for a time series; a header said once costs a fraction of that, and models read it at
    /// least as well.
    /// </summary>
    internal sealed class PerformanceAnalysisTable
    {
        private readonly string[] _columns;
        private readonly List<string[]> _rows = new();

        public PerformanceAnalysisTable(params string[] columns)
        {
            _columns = columns;
        }

        public int RowCount => _rows.Count;

        public void Add(params string[] values)
        {
            if (values.Length != _columns.Length)
            {
                throw new ArgumentException($"Expected {_columns.Length} values, got {values.Length}.", nameof(values));
            }

            _rows.Add(values);
        }

        public void AppendTo(StringBuilder text)
        {
            text.AppendLine(string.Join(" | ", _columns));
            foreach (var row in _rows)
            {
                text.AppendLine(string.Join(" | ", row));
            }
        }

        /// <summary>
        /// A number to as many places as it is worth: none for hundreds and up, more as it gets smaller.
        /// A rate of 1234.5678 waits/sec and one of 0.0123 are both read for their first couple of digits,
        /// and every digit past those costs a token and says nothing.
        /// </summary>
        public static string Num(double? value)
        {
            if (value is not { } v || double.IsNaN(v) || double.IsInfinity(v)) return string.Empty;
            if (v == 0) return "0";

            var abs = Math.Abs(v);
            var format = abs switch
            {
                >= 100 => "0",
                >= 10 => "0.#",
                >= 1 => "0.##",
                >= 0.001 => "0.###",
                _ => "0.######"
            };

            var text = v.ToString(format, CultureInfo.InvariantCulture);
            return text is "0" or "-0" ? v.ToString("G2", CultureInfo.InvariantCulture) : text;
        }

        /// <summary>
        /// How a figure compares with its baseline, as a ratio - "2.4x" - because that is the question
        /// the reader is asking: is this normal?  "new" where the baseline had none of it.
        /// </summary>
        public static string Change(double? now, double? baseline)
        {
            if (now is not { } n || baseline is not { } b) return string.Empty;
            if (b == 0) return n == 0 ? "same" : "new";

            return Num(n / b) + "x";
        }

        public static object? Get(DataRow row, string column) =>
            row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column] : null;

        public static double? Double(DataRow row, string column) =>
            Get(row, column) is { } value ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : null;

        public static string Text(DataRow row, string column) =>
            Get(row, column) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;

        public static DateTime? Time(DataRow row, string column) => Get(row, column) is DateTime value ? value : null;

        public static bool Flag(DataRow row, string column) => Get(row, column) switch
        {
            bool b => b,
            null => false,
            var other => Convert.ToInt32(other, CultureInfo.InvariantCulture) != 0
        };
    }
}
