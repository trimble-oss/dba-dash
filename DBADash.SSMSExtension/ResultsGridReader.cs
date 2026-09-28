using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlTypes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Reads the result set behind SSMS's focused results grid into a DataTable, so it can be opened in DBA Dash's
    /// own grid - with its filtering, grouping and export - rather than SSMS's.
    ///
    /// The grid (Microsoft.SqlServer.Management.UI.VSIntegration.Editors.GridResultsGrid, a subclass of
    /// Microsoft.SqlServer.Management.UI.Grid.GridControl) keeps its data in a QEResultSet, reached through
    /// GridControl's public GridStorage property. QEResultSet hands back typed values (GetCellData) and each
    /// column's CLR type (GetFieldType) - the same members SSMS 22's own Save Results As exporters read - so the
    /// types survive rather than everything arriving as display text. None of it is a supported API, and the
    /// types live in SSMS's own assemblies this project can't reference, so it's all reached by name through
    /// reflection and returns null if a future SSMS build changes those internals. All confirmed directly against
    /// the SSMS 22 install.
    /// </summary>
    internal static class ResultsGridReader
    {
        private const string GridControlTypeName = "Microsoft.SqlServer.Management.UI.Grid.GridControl";

        // The column types kept as themselves. Anything else - binary, sql_variant, spatial, hierarchyid - is
        // read as SSMS's own display text for the cell, which is what the user was looking at anyway.
        private static readonly Type[] SupportedTypes =
        {
            // All ones DataTable.ReadXml accepts on the viewer's .NET, which restricts the types a schema can
            // name. byte[] is left out on purpose: DataGridView renders a byte[] column as an image, so binary
            // is shown as SSMS's own 0x... text instead.
            typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double),
            typeof(decimal), typeof(string), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
        };

        private const int ProgressInterval = 5000;

        /// <summary>
        /// A handle on the focused grid's result set, found on the UI thread and then read with
        /// <see cref="ReadTable"/> - on a background thread if need be, as SSMS's own export does: QEResultSet
        /// takes its own lock around every cell read.
        /// </summary>
        internal sealed class ResultSet
        {
            private readonly Func<long, int, object> _getCellData;
            private readonly Func<long, int, string> _getCellDataAsString;
            private readonly Func<int, Type> _getFieldType;
            private readonly Func<int, string> _getServerDataTypeName;
            private readonly int _columnOffset;

            internal ResultSet(object storage, int columnOffset, int? index)
            {
                Index = index;
                _getCellData = Bind<Func<long, int, object>>(storage, "GetCellData");
                _getCellDataAsString = Bind<Func<long, int, string>>(storage, "GetCellDataAsString");
                _getFieldType = Bind<Func<int, Type>>(storage, "GetFieldType");
                _getServerDataTypeName = TryBind<Func<int, string>>(storage, "GetServerDataTypeName");
                _columnOffset = columnOffset;

                RowCount = (long)GetProperty(storage, "TotalNumberOfRows");
                ColumnNames = (StringCollection)GetProperty(storage, "ColumnNames");
                ColumnCount = (int)GetProperty(storage, "NumberOfDataColumns");
                IsComplete = GetProperty(storage, "StoredAllData") as bool? ?? true;
            }

            /// <summary>Which of the query's result sets this is, from 0, where the grid says.</summary>
            internal int? Index { get; }

            internal long RowCount { get; }
            internal int ColumnCount { get; }
            internal StringCollection ColumnNames { get; }

            /// <summary>False while the query is still running - the rows read are the ones retrieved so far.</summary>
            internal bool IsComplete { get; }

            /// <summary>
            /// Copies the result set into a DataTable. <paramref name="progress"/> is told the number of rows read so
            /// far, every few thousand.
            /// </summary>
            internal DataTable ReadTable(string title, Action<long> progress, CancellationToken cancellationToken)
            {
                var table = new DataTable("Results");
                table.ExtendedProperties["Title"] = title;

                // Each column's type, or null to read it as text: one with no supported type, or one a value has
                // turned out not to fit.
                var types = new Type[ColumnCount];
                var names = new UniqueNames();
                for (var col = 0; col < ColumnCount; col++)
                {
                    types[col] = SupportedType(_getFieldType(col));
                    AddColumn(table, names.Get(ColumnNames[col]), types[col] ?? typeof(string), col);
                }

                // Straight into the table a row at a time, rather than a column at a time into arrays first: the
                // DataTable keeps value types unboxed, so an intermediate copy of boxed values - inside SSMS's own
                // process - would be bigger than the table itself.
                table.BeginLoadData();
                var row = new object[ColumnCount];
                for (long r = 0; r < RowCount; r++)
                {
                    if (r % ProgressInterval == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Invoke(r);
                    }

                    for (var col = 0; col < ColumnCount; col++)
                    {
                        if (types[col] != null)
                        {
                            if (TryConvert(_getCellData(r, col + _columnOffset), types[col], out var converted))
                            {
                                row[col] = converted;
                                continue;
                            }

                            // e.g. a decimal(38) too big for decimal - the column falls back to text from here on.
                            ConvertColumnToText(table, col, r, cancellationToken);
                            types[col] = null;
                        }

                        row[col] = ReadText(r, col);
                    }

                    table.Rows.Add(row);
                }
                table.EndLoadData();

                return table;
            }

            private DataColumn AddColumn(DataTable table, string name, Type type, int col)
            {
                var column = table.Columns.Add(name, type);
                // Written without a UTC offset and read back as written - it's the server's value, not a moment in
                // this machine's time zone to convert.
                if (type == typeof(DateTime)) column.DateTimeMode = DataSetDateTime.Unspecified;

                var sqlType = TryGet(() => _getServerDataTypeName?.Invoke(col));
                if (!string.IsNullOrEmpty(sqlType)) column.ExtendedProperties["SqlType"] = sqlType;

                return column;
            }

            /// <summary>
            /// Swaps a typed column for a text one in the same place, re-reading the <paramref name="rowsLoaded"/> rows
            /// already in the table as text. A DataColumn's type can't be changed once it holds data.
            /// </summary>
            private void ConvertColumnToText(DataTable table, int col, long rowsLoaded, CancellationToken cancellationToken)
            {
                var name = table.Columns[col].ColumnName;
                table.Columns.RemoveAt(col);
                var column = AddColumn(table, name, typeof(string), col);
                column.SetOrdinal(col);

                for (var r = 0; r < rowsLoaded; r++)
                {
                    if (r % ProgressInterval == 0) cancellationToken.ThrowIfCancellationRequested();
                    table.Rows[r][column] = ReadText(r, col);
                }
            }

            private object ReadText(long row, int col) =>
                // Null checked on the typed value: the display text of a NULL is the string "NULL".
                IsNull(_getCellData(row, col + _columnOffset))
                    ? DBNull.Value
                    : ValidXmlText(_getCellDataAsString(row, col + _columnOffset));
        }

        /// <summary>
        /// The focused results grid's result set, or null if focus isn't in a results grid, or a future SSMS build
        /// has changed the grid's shape.
        /// </summary>
        public static ResultSet TryGetFocused()
        {
            var grid = FocusedGrid();
            if (grid == null) return null;

            try
            {
                var storage = GetProperty(grid, "GridStorage");
                if (storage == null) return null;

                // In grid mode, column 0 is the row number column, so a data column's cell is one along from its
                // index among the data columns - which is what GetFieldType and ColumnNames are indexed by.
                var gridMode = GetProperty(storage, "GridMode") as bool? ?? true;
                // SSMS tags each grid with its result set's index (GridResultsTabPage.GetGridResultSet reads it back).
                return new ResultSet(storage, gridMode ? 1 : 0, grid.Tag as int?);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>True if focus is in a results grid. Cheap enough for BeforeQueryStatus.</summary>
        public static bool IsFocused() => FocusedGrid() != null;

        /// <summary>
        /// The grid holding focus, walking up from the focused control rather than down from its window: a query
        /// window has a grid per result set, and it's the one the user is in (or right-clicked, which focuses it)
        /// that's wanted.
        /// </summary>
        private static Control FocusedGrid()
        {
            for (var control = Control.FromHandle(GetFocus()); control != null; control = control.Parent)
            {
                for (var type = control.GetType(); type != null; type = type.BaseType)
                {
                    if (string.Equals(type.FullName, GridControlTypeName, StringComparison.Ordinal)) return control;
                }
            }

            return null;
        }

        /// <summary>
        /// The column type to use for a result set column, or null to show it as text. The storage can describe a
        /// column by its SqlTypes type rather than the CLR one (its cells come back as SqlTypes either way - see
        /// <see cref="Unwrap"/>), so those map to the CLR type they hold.
        /// </summary>
        private static Type SupportedType(Type type)
        {
            if (type == null) return null;
            if (type == typeof(SqlInt16)) type = typeof(short);
            else if (type == typeof(SqlInt32)) type = typeof(int);
            else if (type == typeof(SqlInt64)) type = typeof(long);
            else if (type == typeof(SqlByte)) type = typeof(byte);
            else if (type == typeof(SqlBoolean)) type = typeof(bool);
            else if (type == typeof(SqlDecimal) || type == typeof(SqlMoney)) type = typeof(decimal);
            else if (type == typeof(SqlDouble)) type = typeof(double);
            else if (type == typeof(SqlSingle)) type = typeof(float);
            else if (type == typeof(SqlString) || type == typeof(SqlXml) || type == typeof(SqlChars)) type = typeof(string);
            else if (type == typeof(SqlDateTime)) type = typeof(DateTime);
            else if (type == typeof(SqlGuid)) type = typeof(Guid);

            return Array.IndexOf(SupportedTypes, type) >= 0 ? type : null;
        }

        private static bool IsNull(object value) =>
            value == null || value is DBNull || value is INullable { IsNull: true };

        private static bool TryConvert(object value, Type type, out object converted)
        {
            converted = DBNull.Value;
            if (IsNull(value)) return true;

            try
            {
                value = Unwrap(value);
                var invariant = System.Globalization.CultureInfo.InvariantCulture;

                converted = value switch
                {
                    _ when type.IsInstanceOfType(value) => value,
                    // Not IConvertible targets, so Convert.ChangeType can't parse text into them.
                    string text when type == typeof(TimeSpan) => TimeSpan.Parse(text, invariant),
                    string text when type == typeof(DateTimeOffset) => DateTimeOffset.Parse(text, invariant),
                    string text when type == typeof(Guid) => Guid.Parse(text),
                    "1" or "0" when type == typeof(bool) => (string)value == "1",
                    _ => Convert.ChangeType(value, type, invariant)
                };

                if (converted is string s) converted = ValidXmlText(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// The CLR value inside a SqlTypes value - what the storage hands back for most columns (SSMS's own JSON
        /// export checks for SqlInt32, SqlMoney, SqlDecimal and the like), and which isn't IConvertible.
        /// </summary>
        private static object Unwrap(object value) => value switch
        {
            SqlInt16 v => v.Value,
            SqlInt32 v => v.Value,
            SqlInt64 v => v.Value,
            SqlByte v => v.Value,
            SqlBoolean v => v.Value,
            SqlDecimal v => v.Value,
            SqlMoney v => v.Value,
            SqlDouble v => v.Value,
            SqlSingle v => v.Value,
            SqlString v => v.Value,
            SqlDateTime v => v.Value,
            SqlGuid v => v.Value,
            SqlXml v => v.Value,
            SqlChars v => new string(v.Value),
            _ => value
        };

        /// <summary>
        /// A string DataTable.WriteXml can write: data can hold characters XML 1.0 can't (e.g. a stray \0),
        /// which would otherwise fail the whole export for one cell.
        /// </summary>
        private static string ValidXmlText(string text)
        {
            if (text == null) return null;

            var firstInvalid = -1;
            for (var i = 0; i < text.Length; i++)
            {
                if (IsXmlChar(text, i)) continue;
                firstInvalid = i;
                break;
            }

            if (firstInvalid < 0) return text;

            var sb = new StringBuilder(text.Length);
            sb.Append(text, 0, firstInvalid);
            for (var i = firstInvalid; i < text.Length; i++)
            {
                if (IsXmlChar(text, i)) sb.Append(text[i]);
                else sb.Append('�');
            }

            return sb.ToString();
        }

        private static bool IsXmlChar(string text, int i)
        {
            var c = text[i];
            if (XmlConvert.IsXmlChar(c)) return true;

            // A surrogate pair is valid as a pair, not as either half on its own.
            if (char.IsHighSurrogate(c)) return i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);
            return char.IsLowSurrogate(c) && i > 0 && char.IsHighSurrogate(text[i - 1]);
        }

        private static T Bind<T>(object target, string methodName) where T : Delegate =>
            TryBind<T>(target, methodName) ?? throw new MissingMethodException(target.GetType().FullName, methodName);

        /// <summary>
        /// A delegate straight onto the storage's method, rather than MethodInfo.Invoke per cell - a large result
        /// set is millions of calls.
        /// </summary>
        private static T TryBind<T>(object target, string methodName) where T : Delegate
        {
            var parameters = Array.ConvertAll(typeof(T).GetMethod("Invoke").GetParameters(), p => p.ParameterType);
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);

            return method == null ? null : (T)Delegate.CreateDelegate(typeof(T), target, method, throwOnBindFailure: false);
        }

        private static object GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);

        private static string TryGet(Func<string> get)
        {
            try
            {
                return get();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// DataTable needs every column named, and uniquely - SSMS doesn't: an unnamed expression shows as
        /// "(No column name)", and a join can return two columns of the same name.
        /// </summary>
        private sealed class UniqueNames
        {
            private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

            internal string Get(string name)
            {
                if (string.IsNullOrEmpty(name)) name = "(No column name)";

                var unique = name;
                for (var i = 2; !_used.Add(unique); i++) unique = $"{name} ({i})";
                return unique;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
    }
}
