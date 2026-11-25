using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// JSON records from a file still being written to: a fresh read of the file is taken as the same records grown, and
    /// only what's new is added to the rows already shown - so a grid keeps its scroll position, sort, filter and column
    /// layout.  Shared by the grid viewer and the service configuration tool's log, which both watch a log.
    /// </summary>
    /// <remarks>
    /// A last record still being written reads as a row with <see cref="JsonRecords.UnreadableColumnName"/> set.  That
    /// column comes and goes with them, so it isn't compared - it's added when it first appears, and blank when none is
    /// left.  The unreadable rows at the end are replaced by what the file now holds there.
    /// </remarks>
    public static class JsonRecordsGrowth
    {
        /// <summary>
        /// Brings <paramref name="table"/> up to date with a fresh read of the same file.  Returns false, changing nothing,
        /// when the read isn't the file grown: different columns, or rows changed in the middle - the caller then shows the
        /// file afresh.  On success, <paramref name="added"/> is the rows now at the end that weren't there before.
        /// </summary>
        /// <param name="onUnreadableColumnAdded">Called when the unreadable column is added to <paramref name="table"/>, so a
        /// grid bound to it - or to a copy of it - can show the column too.</param>
        /// <param name="removed">The rows taken out of <paramref name="table"/>: the records at its end that the read replaced.</param>
        public static bool TryAppend(DataTable table, DataTable fresh, Action onUnreadableColumnAdded,
            out List<DataRow> added, out List<DataRow> removed)
        {
            added = null;
            removed = null;
            if (!SameColumns(table, fresh)) return false;

            var names = DataColumnNames(fresh);
            var same = 0;
            while (same < table.Rows.Count && same < fresh.Rows.Count && SameRow(table.Rows[same], fresh.Rows[same], names)) same++;
            for (var i = same; i < table.Rows.Count; i++)
            {
                if (!IsUnreadable(table.Rows[i])) return false;
            }

            if (fresh.Columns.Contains(JsonRecords.UnreadableColumnName) && !table.Columns.Contains(JsonRecords.UnreadableColumnName))
            {
                table.Columns.Add(JsonRecords.UnreadableColumnName, typeof(string));
                onUnreadableColumnAdded?.Invoke();
            }

            removed = new List<DataRow>();
            for (var i = table.Rows.Count - 1; i >= same; i--)
            {
                removed.Add(table.Rows[i]);
                table.Rows.Remove(table.Rows[i]);
            }

            added = new List<DataRow>();
            for (var i = same; i < fresh.Rows.Count; i++)
            {
                var row = table.NewRow();
                foreach (var name in names) row[name] = fresh.Rows[i][name];
                if (table.Columns.Contains(JsonRecords.UnreadableColumnName))
                {
                    row[JsonRecords.UnreadableColumnName] = fresh.Columns.Contains(JsonRecords.UnreadableColumnName)
                        ? fresh.Rows[i][JsonRecords.UnreadableColumnName]
                        : DBNull.Value;
                }
                // The parsing error, and where in the file it was, as on a first load.
                row.RowError = fresh.Rows[i].RowError;
                table.Rows.Add(row);
                added.Add(row);
            }

            return true;
        }

        /// <summary>
        /// Adds the unreadable column to <paramref name="grid"/>, bound to the column of that name in its table - at the end,
        /// where the JSON reader puts it.
        /// </summary>
        public static void AddUnreadableGridColumn(DataGridView grid)
        {
            if (grid.Columns[JsonRecords.UnreadableColumnName] is not null) return;
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = JsonRecords.UnreadableColumnName,
                HeaderText = JsonRecords.UnreadableColumnName,
                DataPropertyName = JsonRecords.UnreadableColumnName,
                ValueType = typeof(string),
                SortMode = DataGridViewColumnSortMode.Automatic,
                ReadOnly = true
            });
        }

        /// <summary>
        /// The unreadable rows, and how many of them are the ones at the end with nothing readable after them - a record
        /// still being written, rather than a problem in the file.
        /// </summary>
        public static (int Pending, int Unreadable) UnreadableCounts(DataTable table)
        {
            if (table is null || !table.Columns.Contains(JsonRecords.UnreadableColumnName)) return (0, 0);

            var unreadable = table.Rows.Cast<DataRow>().Select(IsUnreadable).ToList();
            var pending = 0;
            for (var i = unreadable.Count - 1; i >= 0 && unreadable[i]; i--) pending++;
            return (pending, unreadable.Count(u => u));
        }

        /// <summary>Whether a row is one the reader couldn't read - it holds the text of the part it couldn't.</summary>
        public static bool IsUnreadable(DataRow row) =>
            row.Table.Columns.Contains(JsonRecords.UnreadableColumnName) && row[JsonRecords.UnreadableColumnName] != DBNull.Value;

        /// <summary>What's wrong with the file, where it's more than records still being written - or null.</summary>
        public static string ProblemText(int problems) => problems <= 0
            ? null
            : $"{problems:N0} {(problems == 1 ? "part" : "parts")} of the file couldn't be read as JSON - see the {JsonRecords.UnreadableColumnName} column.";

        /// <summary>What's still being written at the end of the file - or null.</summary>
        public static string PendingText(int pending) => pending <= 0
            ? null
            : $"{pending:N0} incomplete {(pending == 1 ? "record" : "records")} at the end - probably still being written";

        /// <summary>The columns a read of the file has, leaving out the unreadable column - it may come and go.</summary>
        private static string[] DataColumnNames(DataTable table) => table.Columns.Cast<DataColumn>()
            .Select(c => c.ColumnName).Where(n => n != JsonRecords.UnreadableColumnName).ToArray();

        private static bool SameColumns(DataTable a, DataTable b)
        {
            var ac = a.Columns.Cast<DataColumn>().Where(c => c.ColumnName != JsonRecords.UnreadableColumnName).ToArray();
            var bc = b.Columns.Cast<DataColumn>().Where(c => c.ColumnName != JsonRecords.UnreadableColumnName).ToArray();
            if (ac.Length != bc.Length) return false;
            for (var i = 0; i < ac.Length; i++)
            {
                if (ac[i].ColumnName != bc[i].ColumnName || ac[i].DataType != bc[i].DataType) return false;
            }
            return true;
        }

        /// <summary>
        /// Whether two reads have the same row.  The unreadable text counts too: a record still being written, and the
        /// same record once complete, differ there even though their data cells are both blank.
        /// </summary>
        private static bool SameRow(DataRow a, DataRow b, string[] names) =>
            names.All(n => SameValue(a[n], b[n])) && SameValue(Unreadable(a), Unreadable(b));

        private static object Unreadable(DataRow row) =>
            row.Table.Columns.Contains(JsonRecords.UnreadableColumnName) ? row[JsonRecords.UnreadableColumnName] : DBNull.Value;

        /// <summary>Equal by content - a binary cell (a plan handle, say) is a new array on each read.</summary>
        private static bool SameValue(object a, object b) => a is byte[] x && b is byte[] y
            ? x.AsSpan().SequenceEqual(y)
            : Equals(a, b);
    }
}
