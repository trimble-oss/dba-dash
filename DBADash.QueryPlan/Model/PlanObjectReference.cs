using System;
using System.Collections.Generic;
using System.Text;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// A table, view, index or function an operator touches.
    ///
    /// Showplan writes every part bracketed - [StackOverflow2013], [dbo], [Posts] - and the brackets
    /// are stripped on the way in.  They are quoting, not part of the name, and carrying them around
    /// means every consumer that wants to match, display or paste a name has to strip them again.
    /// <see cref="ToString"/> puts them back for the places that want the fully qualified form.
    /// </summary>
    public sealed class PlanObjectReference
    {
        public string? Database { get; internal set; }

        public string? Schema { get; internal set; }

        /// <summary>The table, view or function.  Named Table by showplan whatever it actually is.</summary>
        public string? Table { get; internal set; }

        public string? Index { get; internal set; }

        /// <summary>Clustered, NonClustered, Heap - whatever showplan reported, or null.</summary>
        public string? IndexKind { get; internal set; }

        /// <summary>The alias the query gave it, which is how the reader refers to it.</summary>
        public string? Alias { get; internal set; }

        /// <summary>RowStore or ColumnStore, where showplan says.</summary>
        public string? Storage { get; internal set; }

        /// <summary>
        /// What the node caption shows: the table, then the index when there is one, then the alias.
        /// Deliberately short - the database and schema are the same for nearly every operator in a
        /// plan, so spending node width on them buys nothing.  The full name is a tooltip away.
        /// </summary>
        public string ShortName
        {
            get
            {
                if (string.IsNullOrEmpty(Table)) return Index ?? string.Empty;

                var text = new StringBuilder(Table);
                if (!string.IsNullOrEmpty(Index)) text.Append('.').Append(Index);
                if (!string.IsNullOrEmpty(Alias)) text.Append(" AS ").Append(Alias);
                return text.ToString();
            }
        }

        /// <summary>
        /// The object without its index, for grouping operators by what they read.  Fully qualified,
        /// because two tables of the same name in different schemas are different tables.
        /// </summary>
        public string QualifiedTableName => Join(Database, Schema, Table);

        /// <summary>The fully qualified, bracketed form, as showplan wrote it.</summary>
        public override string ToString()
        {
            var text = new StringBuilder();
            foreach (var part in new[] { Database, Schema, Table, Index })
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (text.Length > 0) text.Append('.');
                text.Append('[').Append(part).Append(']');
            }

            if (!string.IsNullOrEmpty(Alias)) text.Append(" AS [").Append(Alias).Append(']');
            return text.ToString();
        }

        private static string Join(params string?[] parts)
        {
            var text = new StringBuilder();
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (text.Length > 0) text.Append('.');
                text.Append(part);
            }

            return text.ToString();
        }

        /// <summary>
        /// Strip the brackets showplan quotes a name part with.  Only a matched pair is removed, so a
        /// name that genuinely contains a bracket is left alone.
        /// </summary>
        internal static string? Unquote(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value.Length >= 2 && value[0] == '[' && value[^1] == ']'
                ? value[1..^1]
                : value;
        }

        /// <summary>
        /// True when <paramref name="stored"/> is the name tempdb holds the temp table
        /// <paramref name="shortName"/> under: the name the query used, padded with underscores to
        /// 116 characters, then a 12 digit hex suffix that keeps one session's table apart from
        /// another's.
        ///
        /// Showplan writes the query's name on an operator's Object but the stored name on a
        /// MissingIndex, so the two have to be compared this way for a missing index on a temp table
        /// to reach the operator that reads it.  Asked rather than trimming the stored name back,
        /// because the padding and the last character of a name that ends in an underscore are the
        /// same character: only a name something in the plan actually uses says where one ends.
        /// </summary>
        internal static bool IsStoredTempTableName(string? stored, string? shortName)
        {
            const int storedLength = 128;
            const int suffixLength = 12;
            const int paddedLength = storedLength - suffixLength;

            if (stored is null || stored.Length != storedLength) return false;
            if (string.IsNullOrEmpty(shortName) || shortName!.Length > paddedLength || shortName[0] != '#') return false;
            if (!stored.StartsWith(shortName, StringComparison.OrdinalIgnoreCase)) return false;

            for (var i = shortName.Length; i < paddedLength; i++)
            {
                if (stored[i] != '_') return false;
            }

            for (var i = paddedLength; i < storedLength; i++)
            {
                if (!Uri.IsHexDigit(stored[i])) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// A column an operator reads, outputs or defines.  Also carries the computed-column and
    /// internal-name cases, where showplan gives no table at all.
    /// </summary>
    public sealed class PlanColumnReference
    {
        public string? Database { get; internal set; }

        public string? Schema { get; internal set; }

        public string? Table { get; internal set; }

        public string? Alias { get; internal set; }

        public string? Column { get; internal set; }

        /// <summary>
        /// Showplan's name for a value the plan invented - Expr1002, and the like.  Present instead
        /// of <see cref="Column"/> on computed values.
        /// </summary>
        public string? ComputedColumn { get; internal set; }

        /// <summary>
        /// A parameter reference, for the columns that are actually parameters.
        /// </summary>
        public string? ParameterCompiledValue { get; internal set; }

        /// <summary>
        /// How the column reads in a property list: the alias when the query gave one, otherwise the
        /// table, then the column.  Nothing longer, because these appear by the dozen.
        /// </summary>
        public override string ToString()
        {
            var qualifier = !string.IsNullOrEmpty(Alias) ? Alias : Table;
            var name = !string.IsNullOrEmpty(Column) ? Column : ComputedColumn;

            if (string.IsNullOrEmpty(name)) return qualifier ?? string.Empty;
            return string.IsNullOrEmpty(qualifier) ? name : qualifier + "." + name;
        }

        internal static string Describe(IReadOnlyList<PlanColumnReference> columns, int maxColumns = int.MaxValue)
        {
            var text = new StringBuilder();
            var shown = Math.Min(columns.Count, maxColumns);

            for (var i = 0; i < shown; i++)
            {
                if (i > 0) text.Append(", ");
                text.Append(columns[i]);
            }

            if (columns.Count > shown) text.Append(", ... (").Append(columns.Count - shown).Append(" more)");
            return text.ToString();
        }
    }
}
