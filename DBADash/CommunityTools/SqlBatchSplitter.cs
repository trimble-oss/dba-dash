using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DBADash.CommunityTools
{
    /// <summary>
    /// Splits a script into batches on GO separators, the way SSMS and sqlcmd do.
    ///
    /// <para>A GO only counts at the start of a line and outside comments and string literals - community scripts
    /// carry long block comments and dynamic SQL, and either can contain a line that reads "GO".  "GO n" repeats the
    /// batch n times, as sqlcmd does.</para>
    /// </summary>
    public static class SqlBatchSplitter
    {
        private static readonly Regex GoLine = new(@"^\s*GO(?:\s+(?<count>\d+))?\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex UseOnly = new(@"^\s*(?:USE\s+(?:\[[^\]]+\]|""[^""]+""|\w+)\s*;?\s*)+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static List<string> Split(string script)
        {
            var batches = new List<string>();
            var current = new StringBuilder();
            var blockCommentDepth = 0;
            var inString = false;

            foreach (var rawLine in script.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (blockCommentDepth == 0 && !inString)
                {
                    var go = GoLine.Match(line);
                    if (go.Success)
                    {
                        var count = go.Groups["count"].Success ? int.Parse(go.Groups["count"].Value) : 1;
                        AddBatch(batches, current, count);
                        continue;
                    }
                }
                ScanLine(line, ref blockCommentDepth, ref inString);
                current.Append(line).Append("\r\n");
            }
            AddBatch(batches, current, 1);
            return batches;
        }

        /// <summary>
        /// True for a batch that does nothing but switch database.  Deployment picks the database itself, so a
        /// script's own "USE master" is skipped rather than allowed to send the procedure somewhere else.
        /// </summary>
        public static bool IsUseOnlyBatch(string batch) => UseOnly.IsMatch(StripComments(batch));

        private static void AddBatch(List<string> batches, StringBuilder current, int count)
        {
            var batch = current.ToString();
            current.Clear();
            if (string.IsNullOrWhiteSpace(StripComments(batch)))
            {
                return;
            }
            for (var i = 0; i < count; i++)
            {
                batches.Add(batch);
            }
        }

        private static void ScanLine(string line, ref int blockCommentDepth, ref bool inString)
        {
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                var next = i + 1 < line.Length ? line[i + 1] : '\0';
                if (inString)
                {
                    if (c == '\'')
                    {
                        if (next == '\'')
                        {
                            i++; // Escaped quote
                        }
                        else
                        {
                            inString = false;
                        }
                    }
                }
                else if (blockCommentDepth > 0)
                {
                    if (c == '/' && next == '*')
                    {
                        blockCommentDepth++;
                        i++;
                    }
                    else if (c == '*' && next == '/')
                    {
                        blockCommentDepth--;
                        i++;
                    }
                }
                else if (c == '-' && next == '-')
                {
                    return; // Rest of the line is a comment
                }
                else if (c == '/' && next == '*')
                {
                    blockCommentDepth++;
                    i++;
                }
                else if (c == '\'')
                {
                    inString = true;
                }
            }
        }

        /// <summary>Removes comments, leaving string literals intact.  Used to check what a batch actually does.</summary>
        internal static string StripComments(string sql)
        {
            var sb = new StringBuilder(sql.Length);
            var blockCommentDepth = 0;
            var inString = false;
            var inLineComment = false;
            for (var i = 0; i < sql.Length; i++)
            {
                var c = sql[i];
                var next = i + 1 < sql.Length ? sql[i + 1] : '\0';
                if (inLineComment)
                {
                    if (c == '\n')
                    {
                        inLineComment = false;
                        sb.Append(c);
                    }
                }
                else if (inString)
                {
                    sb.Append(c);
                    if (c == '\'')
                    {
                        if (next == '\'')
                        {
                            sb.Append(next);
                            i++;
                        }
                        else
                        {
                            inString = false;
                        }
                    }
                }
                else if (blockCommentDepth > 0)
                {
                    if (c == '/' && next == '*')
                    {
                        blockCommentDepth++;
                        i++;
                    }
                    else if (c == '*' && next == '/')
                    {
                        blockCommentDepth--;
                        i++;
                        sb.Append(' ');
                    }
                }
                else if (c == '-' && next == '-')
                {
                    inLineComment = true;
                    i++;
                }
                else if (c == '/' && next == '*')
                {
                    blockCommentDepth++;
                    i++;
                }
                else
                {
                    if (c == '\'')
                    {
                        inString = true;
                    }
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
