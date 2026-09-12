using System;
using System.Globalization;
using System.Text;

namespace DBADash.QueryStats
{
    /// <summary>
    /// The text an ad hoc query shape is shown with: one variant's statement with its literal values replaced and
    /// its comments removed.
    ///
    /// <para>A shape is every statement sharing a query_hash in a database, and the hash is computed from the
    /// logical query tree - so the variants behind it read the same tables with the same joins, predicates and
    /// hints, and differ only in literal values, comments and layout.  Any one variant's text would present its
    /// values as though every execution had used them, when the values are what decide how many rows an
    /// execution touches.  So those are what is taken out: strings become '?', Unicode strings N'?', numbers ?,
    /// money $? and binary 0x?.  Comments go too - they are not part of the hash either, and an application that
    /// tags each request with a user or trace id makes them as variable as any literal.  Layout is kept.</para>
    ///
    /// <para>The masking is lexical, so the only mistake it can make is to hide something every variant shared,
    /// such as a varchar(10) length or an ORDER BY ordinal, rather than to show a value that varied.  It follows
    /// the T-SQL tokenizer: doubled quotes inside strings and delimited identifiers, nested block comments,
    /// digits inside identifiers, and double quotes, which delimit an identifier or a string depending on
    /// QUOTED_IDENTIFIER.  SQL Server's sp_get_query_template does much the same on the server, but only for
    /// statements forced parameterization would accept, and at the cost of a call per statement on the monitored
    /// instance.</para>
    /// </summary>
    public static class QueryTemplate
    {
        /// <summary>
        /// The statement a pair of offsets points at, cut from its batch the way dbo.QueryStatementText cuts it in
        /// the repository: offsets are byte positions in an NVARCHAR batch, an end of -1 (or 0) means the end of
        /// the batch, and a start of -1 means the whole batch.
        /// </summary>
        public static string GetStatementText(string batchText, int startOffset, int endOffset)
        {
            if (string.IsNullOrEmpty(batchText) || startOffset < 0) return batchText;
            var start = startOffset / 2;
            if (start >= batchText.Length) return string.Empty;
            var length = endOffset is -1 or 0 ? batchText.Length - start : ((endOffset - startOffset) / 2) + 1;
            return batchText.Substring(start, Math.Clamp(length, 0, batchText.Length - start));
        }

        /// <summary>
        /// <paramref name="statementText"/> with its literal values replaced and its comments removed - see the
        /// class summary.  <paramref name="quotedIdentifier"/> is the QUOTED_IDENTIFIER setting the statement was
        /// compiled under: on, a double quoted token is an identifier and is kept; off, it is a string.
        /// </summary>
        public static string Create(string statementText, bool quotedIdentifier = true)
        {
            if (string.IsNullOrEmpty(statementText)) return statementText;

            var sql = statementText;
            var n = sql.Length;
            var sb = new StringBuilder(n);
            var i = 0;
            while (i < n)
            {
                var c = sql[i];
                var next = i + 1 < n ? sql[i + 1] : '\0';

                if (c == '-' && next == '-')
                {
                    // To the end of the line.  The line break itself is kept, so the lines either side stay apart.
                    while (i < n && sql[i] != '\r' && sql[i] != '\n') i++;
                }
                else if (c == '/' && next == '*')
                {
                    i = SkipBlockComment(sql, i);
                    // A comment can be all that separates two tokens: SELECT/*x*/a must not become SELECTa
                    if (sb.Length > 0 && !char.IsWhiteSpace(sb[^1]) && i < n && !char.IsWhiteSpace(sql[i])) sb.Append(' ');
                }
                else if (c == '\'')
                {
                    i = SkipDelimited(sql, i, '\'');
                    sb.Append("'?'");
                }
                else if (c is 'N' or 'n' && next == '\'')
                {
                    i = SkipDelimited(sql, i + 1, '\'');
                    sb.Append(c).Append("'?'");
                }
                else if (c == '[')
                {
                    var end = SkipDelimited(sql, i, ']');
                    sb.Append(sql, i, end - i);
                    i = end;
                }
                else if (c == '"')
                {
                    var end = SkipDelimited(sql, i, '"');
                    if (quotedIdentifier)
                    {
                        sb.Append(sql, i, end - i);
                    }
                    else
                    {
                        sb.Append("\"?\"");
                    }
                    i = end;
                }
                else if (c == '0' && next is 'x' or 'X')
                {
                    sb.Append(sql, i, 2).Append('?');
                    i += 2;
                    while (i < n && char.IsAsciiHexDigit(sql[i])) i++;
                }
                else if (char.IsAsciiDigit(c) || (c == '.' && char.IsAsciiDigit(next)))
                {
                    i = SkipNumber(sql, i);
                    sb.Append('?');
                }
                else if (IsCurrencySymbol(c) && (char.IsAsciiDigit(next) || (next == '.' && i + 2 < n && char.IsAsciiDigit(sql[i + 2]))))
                {
                    // Money: $12.34, and any other currency symbol T-SQL accepts in front of a number.  The symbol is
                    // kept, because it says the value was money rather than a number.
                    sb.Append(c).Append('?');
                    i = SkipNumber(sql, i + 1);
                }
                else if (IsIdentifierStart(c))
                {
                    // Copied whole, so the digits in Table1, @p1 or #t2 are not taken for numbers.  Variables, temp
                    // tables and pseudo columns such as $action are identifiers here too.
                    var start = i++;
                    while (i < n && IsIdentifierPart(sql[i])) i++;
                    sb.Append(sql, start, i - start);
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>Past the closing delimiter, treating a doubled delimiter as an escaped one.  An unclosed one runs to the end.</summary>
        private static int SkipDelimited(string sql, int open, char close)
        {
            var i = open + 1;
            while (i < sql.Length)
            {
                if (sql[i] == close)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == close)
                    {
                        i += 2;
                        continue;
                    }
                    return i + 1;
                }
                i++;
            }
            return sql.Length;
        }

        /// <summary>Past the end of a block comment.  T-SQL nests them, so /* a /* b */ c */ is one comment.</summary>
        private static int SkipBlockComment(string sql, int open)
        {
            var depth = 0;
            var i = open;
            while (i < sql.Length - 1)
            {
                if (sql[i] == '/' && sql[i + 1] == '*')
                {
                    depth++;
                    i += 2;
                }
                else if (sql[i] == '*' && sql[i + 1] == '/')
                {
                    i += 2;
                    if (--depth == 0) return i;
                }
                else
                {
                    i++;
                }
            }
            return sql.Length;
        }

        /// <summary>Past an integer, decimal or float: 1, 1.5, .5, 5., 1e10, 1.5E-3 - and 1e, which T-SQL also reads as a float.</summary>
        private static int SkipNumber(string sql, int i)
        {
            var n = sql.Length;
            while (i < n && char.IsAsciiDigit(sql[i])) i++;
            if (i < n && sql[i] == '.')
            {
                i++;
                while (i < n && char.IsAsciiDigit(sql[i])) i++;
            }
            if (i < n && sql[i] is 'e' or 'E')
            {
                i++;
                if (i + 1 < n && sql[i] is '+' or '-' && char.IsAsciiDigit(sql[i + 1])) i++;
                while (i < n && char.IsAsciiDigit(sql[i])) i++;
            }
            return i;
        }

        private static bool IsCurrencySymbol(char c) =>
            c == '$' || (c > 127 && CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.CurrencySymbol);

        private static bool IsIdentifierStart(char c) =>
            char.IsLetter(c) || c is '_' or '@' or '#' or '$' || char.IsSurrogate(c);

        private static bool IsIdentifierPart(char c) =>
            char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$' || char.IsSurrogate(c);
    }
}
