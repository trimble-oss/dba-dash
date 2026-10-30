using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    /// <summary>
    /// Checks that protect an existing install from being broken by an update, for families with GuardCustomizations
    /// (tools people run their backups with): customized copies aren't replaced, and parameters removed by the new version
    /// are checked against Agent job steps.
    /// </summary>
    public static class CommunityToolSafety
    {
        /// <summary>
        /// A procedure definition reduced to what identifies a release: line endings and trailing whitespace ignored, and the
        /// leading CREATE/ALTER treated the same (SQL Server stores an ALTER PROCEDURE batch as CREATE PROCEDURE).
        /// </summary>
        public static string NormalizeDefinition(string definition)
        {
            var lines = definition.ReplaceLineEndings("\n").Split('\n').Select(l => l.TrimEnd());
            var text = string.Join("\n", lines).Trim();
            var keyword = FindLeadingKeyword(text);
            if (keyword.Index >= 0)
            {
                text = string.Concat(text.AsSpan(0, keyword.Index), "CREATE", text.AsSpan(keyword.Index + keyword.Length));
            }
            return text;
        }

        public static string HashDefinition(string definition) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeDefinition(definition))));

        private static readonly Regex CreateOrAlter = new(@"\G(CREATE\s+OR\s+ALTER|CREATE|ALTER)\b",RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>The CREATE or ALTER that starts the module, skipping leading whitespace and comments.  Index -1 if there isn't one.</summary>
        internal static (int Index, int Length) FindLeadingKeyword(string text)
        {
            var i = 0;
            while (i < text.Length)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    i++;
                }
                else if (string.CompareOrdinal(text, i, "--", 0, 2) == 0)
                {
                    var end = text.IndexOf('\n', i);
                    i = end < 0 ? text.Length : end + 1;
                }
                else if (string.CompareOrdinal(text, i, "/*", 0, 2) == 0)
                {
                    var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 2;
                }
                else
                {
                    var match = CreateOrAlter.Match(text, i);
                    return match.Success ? (i, match.Length) : (-1, 0);
                }
            }
            return (-1, 0);
        }

        /// <summary>The batch in a script that creates or alters dbo.<paramref name="procedure"/>, or null.</summary>
        public static string FindProcedureBatch(string script, string procedure)
        {
            var pattern = new Regex(@"^(CREATE|ALTER)\s+PROC(EDURE)?\s+(\[?dbo\]?\.)?\[?" + Regex.Escape(procedure) + @"\]?(\s|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return SqlBatchSplitter.Split(script).FirstOrDefault(batch =>
            {
                var keyword = FindLeadingKeyword(batch);
                return keyword.Index >= 0 && pattern.IsMatch(batch[keyword.Index..]);
            });
        }

        public sealed record Parameter(string Name, bool HasDefault);

        private static readonly Regex ProcedureName = new(@"\GPROC(EDURE)?\s+(\[?\w+\]?\s*\.\s*)?\[?[^\s\]]+\]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Parameters from a procedure's header, up to the AS (or WITH) that ends it.  Comments and string defaults are
        /// skipped.  Only used to compare an installed version with a new one, so a parameter that's missed is reported as
        /// removed - never silently accepted.
        /// </summary>
        public static List<Parameter> ParseParameters(string procedureBatch)
        {
            var result = new List<Parameter>();
            var keyword = FindLeadingKeyword(procedureBatch);
            if (keyword.Index < 0)
            {
                return result;
            }
            var i = keyword.Index + keyword.Length;
            while (i < procedureBatch.Length && char.IsWhiteSpace(procedureBatch[i])) i++;
            var name = ProcedureName.Match(procedureBatch, i);
            if (!name.Success)
            {
                return result;
            }
            i = name.Index + name.Length;
            while (i < procedureBatch.Length && char.IsWhiteSpace(procedureBatch[i])) i++;
            // The parameter list can be wrapped in brackets: CREATE PROC dbo.X (@a int, @b int) AS
            var wrapped = i < procedureBatch.Length && procedureBatch[i] == '(';
            if (wrapped) i++;

            var segment = new StringBuilder();
            var hasDefault = false;
            var depth = 0;
            void EndSegment()
            {
                var match = Regex.Match(segment.ToString(), @"^\s*\(?\s*(@\w+)");
                if (match.Success)
                {
                    result.Add(new Parameter(match.Groups[1].Value, hasDefault));
                }
                segment.Clear();
                hasDefault = false;
            }
            while (i < procedureBatch.Length)
            {
                var c = procedureBatch[i];
                if (string.CompareOrdinal(procedureBatch, i, "--", 0, 2) == 0)
                {
                    var end = procedureBatch.IndexOf('\n', i);
                    i = end < 0 ? procedureBatch.Length : end + 1;
                    segment.Append(' ');
                    continue;
                }
                if (string.CompareOrdinal(procedureBatch, i, "/*", 0, 2) == 0)
                {
                    var end = procedureBatch.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? procedureBatch.Length : end + 2;
                    segment.Append(' ');
                    continue;
                }
                if (c == '\'')
                {
                    // Skip the string, including doubled quotes
                    i++;
                    while (i < procedureBatch.Length && !(procedureBatch[i] == '\'' && (i + 1 >= procedureBatch.Length || procedureBatch[i + 1] != '\''))) i += procedureBatch[i] == '\'' ? 2 : 1;
                    i++;
                    segment.Append("''");
                    continue;
                }
                if (c == '(') depth++;
                else if (c == ')')
                {
                    if (depth == 0 && wrapped) break;
                    depth--;
                }
                else if (depth == 0 && c == ',')
                {
                    EndSegment();
                    i++;
                    continue;
                }
                else if (depth <= 0 && c == '=')
                {
                    hasDefault = true;
                }
                else if (depth <= 0 && (i == 0 || !IsWordChar(procedureBatch[i - 1])) && Regex.IsMatch(procedureBatch.AsSpan(i, Math.Min(5, procedureBatch.Length - i)).ToString(), @"^(AS|WITH)(?![\w@#$])", RegexOptions.IgnoreCase)
                         // "@x AS int" is a parameter, not the end of the header
                         && !Regex.IsMatch(segment.ToString(), @"^\s*\(?\s*@\w+\s*$"))
                {
                    break;
                }
                segment.Append(c);
                i++;
            }
            EndSegment();
            return result;
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

        public sealed record JobStep(string Job, int StepId, string StepName, string Command, string Subsystem = "TSQL", string Database = null);

        /// <summary>Agent job steps that mention the procedure.  A text search, so it also finds sqlcmd (CmdExec) steps.</summary>
        public static async Task<List<JobStep>> GetJobStepsAsync(SqlConnection cn, string procedure, CancellationToken cancellationToken)
        {
            await using var cmd = new SqlCommand(@"SELECT J.name, S.step_id, S.step_name, S.command, S.subsystem, S.database_name
FROM msdb.dbo.sysjobsteps S
JOIN msdb.dbo.sysjobs J ON J.job_id = S.job_id
WHERE S.command LIKE N'%' + @Procedure + N'%'", cn);
            cmd.Parameters.AddWithValue("@Procedure", procedure);
            var result = new List<JobStep>();
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                result.Add(new JobStep(rdr.GetString(0), rdr.GetInt32(1), rdr.GetString(2), rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                    rdr.IsDBNull(4) ? "" : rdr.GetString(4), rdr.IsDBNull(5) ? null : rdr.GetString(5)));
            }
            return result;
        }

        /// <summary>Whether the command passes the parameter by name.</summary>
        public static bool UsesParameter(string command, string parameter) =>
            Regex.IsMatch(command, Regex.Escape(parameter) + @"(?![\w@#$])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static Regex ProcedureCall(string procedure) =>
            new(@"(?:(?<db>\[[^\]]+\]|\w+)\s*\.\s*(?:\[?dbo\]?)?\s*\.\s*|(?:\[?dbo\]?\s*\.\s*)|(?<![\w.\]]))\[?" + Regex.Escape(procedure) + @"\]?(?![\w@#$])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Whether a step that mentions the procedure calls the copy in <paramref name="database"/>: a database-qualified call
        /// names it, otherwise a T-SQL step runs in it.  Other subsystems (e.g. sqlcmd in CmdExec), and steps that change
        /// database with USE, are assumed to, to be safe.
        /// </summary>
        public static bool CallsProcedureIn(JobStep step, string procedure, string database)
        {
            var matches = ProcedureCall(procedure).Matches(step.Command);
            if (matches.Count == 0)
            {
                return false;
            }
            var databaseUnknown = !string.Equals(step.Subsystem, "TSQL", StringComparison.OrdinalIgnoreCase)
                                  || Regex.IsMatch(step.Command, @"(?<![\w@#$.])USE\s", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return matches.Any(m => m.Groups["db"].Success
                ? string.Equals(m.Groups["db"].Value.Trim('[', ']'), database, StringComparison.OrdinalIgnoreCase)
                : databaseUnknown || string.Equals(step.Database, database, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly Regex StatementStart = new(@"\G[A-Za-z_]", RegexOptions.CultureInvariant);

        /// <summary>
        /// Whether the command calls the procedure with a value that isn't passed by name (@Name = value), so it can't be
        /// checked.  Only the first argument needs checking: once one is passed by name, T-SQL requires the rest to be too.
        /// </summary>
        public static bool PassesPositionalParameters(string command, string procedure)
        {
            foreach (Match call in ProcedureCall(procedure).Matches(command))
            {
                var i = call.Index + call.Length;
                var newLine = false;
                while (i < command.Length)
                {
                    if (char.IsWhiteSpace(command[i]))
                    {
                        newLine |= command[i] == '\n';
                        i++;
                    }
                    else if (string.CompareOrdinal(command, i, "--", 0, 2) == 0)
                    {
                        var end = command.IndexOf('\n', i);
                        i = end < 0 ? command.Length : end;
                    }
                    else if (string.CompareOrdinal(command, i, "/*", 0, 2) == 0)
                    {
                        var end = command.IndexOf("*/", i + 2, StringComparison.Ordinal);
                        i = end < 0 ? command.Length : end + 2;
                    }
                    else
                    {
                        break;
                    }
                }
                if (i >= command.Length)
                {
                    continue;
                }
                var rest = command[i..];
                if (rest[0] == ';' || rest[0] == '"' || Regex.IsMatch(rest, @"^GO\b", RegexOptions.IgnoreCase))
                {
                    continue; // No arguments (or the end of a sqlcmd -Q string)
                }
                if (Regex.IsMatch(rest, @"^@\w+\s*=", RegexOptions.CultureInvariant))
                {
                    continue; // Named
                }
                // A word starting a new line is the next statement - e.g. EXEC dbo.X <newline> SELECT ...  N'..' is a value.
                if (newLine && StatementStart.IsMatch(rest) && !Regex.IsMatch(rest, @"^N'", RegexOptions.CultureInvariant))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// ANSI_NULLS and QUOTED_IDENTIFIER as the script leaves them for the procedure's batch - the last SET before it, or ON
        /// (the SqlClient default).
        /// </summary>
        public static (bool AnsiNulls, bool QuotedIdentifier) GetSetOptions(string script, string procedureBatch)
        {
            bool ansiNulls = true, quotedIdentifier = true;
            foreach (var batch in SqlBatchSplitter.Split(script))
            {
                if (batch == procedureBatch)
                {
                    break;
                }
                foreach (Match m in Regex.Matches(batch, @"\bSET\s+(ANSI_NULLS|QUOTED_IDENTIFIER)\s+(ON|OFF)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    var on = m.Groups[2].Value.Equals("ON", StringComparison.OrdinalIgnoreCase);
                    if (m.Groups[1].Value.Equals("ANSI_NULLS", StringComparison.OrdinalIgnoreCase)) ansiNulls = on;
                    else quotedIdentifier = on;
                }
            }
            return (ansiNulls, quotedIdentifier);
        }
    }

    /// <summary>
    /// Hashes (<see cref="CommunityToolSafety.HashDefinition"/>) of every known upstream release of each guarded tool, so an
    /// installed copy can be confirmed as unmodified.  Built from the upstream git history and extended each time a pin moves.
    /// </summary>
    public sealed class CommunityToolKnownVersions
    {
        private const string ResourceName = "DBADash.CommunityTools.CommunityToolsKnownVersions.json";
        public const string FileName = "CommunityToolsKnownVersions.json";

        public sealed record KnownVersion(string Version, string Sha256);

        public SortedDictionary<string, List<KnownVersion>> Tools { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lazy<CommunityToolKnownVersions> _default = new(() => Parse(Utility.GetResourceString(ResourceName)));

        private static CommunityToolKnownVersions _override;

        /// <summary>The embedded catalog, unless <see cref="UseInstead"/> replaced it.</summary>
        public static CommunityToolKnownVersions Default => _override ?? _default.Value;

        /// <summary>
        /// Use this catalog for the rest of the process - for the update workflow, which adds the new pins' releases before
        /// testing them, so they aren't reported as customized by a build whose embedded catalog predates them.
        /// </summary>
        public static void UseInstead(CommunityToolKnownVersions known) => _override = known;

        public static CommunityToolKnownVersions Parse(string json)
        {
            var parsed = JsonConvert.DeserializeObject<CommunityToolKnownVersions>(json) ?? new CommunityToolKnownVersions();
            parsed.Tools = new SortedDictionary<string, List<KnownVersion>>(parsed.Tools, StringComparer.OrdinalIgnoreCase);
            return parsed;
        }

        /// <summary>The release the definition is, or null if it doesn't match any known release.</summary>
        public KnownVersion Find(string tool, string definition)
        {
            if (string.IsNullOrEmpty(definition) || !Tools.TryGetValue(tool, out var versions))
            {
                return null;
            }
            var hash = CommunityToolSafety.HashDefinition(definition);
            return versions.FirstOrDefault(v => string.Equals(v.Sha256, hash, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Label for releases from before the tool had a version in its header (Ola's scripts before mid 2018).</summary>
        public const string Unversioned = "unversioned";

        /// <summary>Adds the release in a script, if it's new.  True if it was added.</summary>
        public bool Add(CommunityTool tool, string script)
        {
            var batch = CommunityToolSafety.FindProcedureBatch(script, tool.Name);
            if (batch == null || !CommunityToolVersionChecker.IsOwned(tool, batch))
            {
                return false;
            }
            var version = CommunityToolVersionChecker.ParseVersion(tool.Family, batch).version ?? Unversioned;
            var hash = CommunityToolSafety.HashDefinition(batch);
            if (!Tools.TryGetValue(tool.Name, out var versions))
            {
                versions = new List<KnownVersion>();
                Tools[tool.Name] = versions;
            }
            if (versions.Any(v => string.Equals(v.Sha256, hash, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            versions.Add(new KnownVersion(version, hash));
            return true;
        }

        /// <summary>One release per line, newest first, so additions show up as small diffs.</summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"Tools\": {");
            var firstTool = true;
            foreach (var (tool, versions) in Tools)
            {
                sb.Append(firstTool ? "\n" : ",\n");
                firstTool = false;
                sb.Append("    ").Append(JsonConvert.ToString(tool)).Append(": [\n");
                sb.Append(string.Join(",\n", versions
                    .OrderBy(v => v.Version == Unversioned).ThenByDescending(v => v.Version, StringComparer.Ordinal).ThenBy(v => v.Sha256, StringComparer.Ordinal)
                    .Select(v => $"      {{ \"Version\": {JsonConvert.ToString(v.Version)}, \"Sha256\": \"{v.Sha256}\" }}")));
                sb.Append("\n    ]");
            }
            sb.Append("\n  }\n}\n");
            return sb.ToString();
        }
    }

    /// <summary>
    /// A script that puts back the procedures a deployment replaced, written before each one is replaced.  If it can't be
    /// written, the procedure isn't replaced.
    /// </summary>
    public sealed class CommunityToolRollbackScript
    {
        public string Path { get; }

        private bool started;

        public CommunityToolRollbackScript(string folder, string instance)
        {
            var safeInstance = string.Concat(instance.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            Path = System.IO.Path.Combine(folder, $"{safeInstance}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.sql");
        }

        /// <summary>
        /// The DBA Dash folder if it's writable, otherwise local app data.  The config tool and CLI run as an admin, so the
        /// application folder is normally fine.
        /// </summary>
        public static string DefaultFolder()
        {
            foreach (var folder in new[]
                     {
                         System.IO.Path.Combine(AppContext.BaseDirectory, "CommunityToolsRollback"),
                         System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash", "CommunityToolsRollback")
                     })
            {
                try
                {
                    Directory.CreateDirectory(folder);
                    var probe = System.IO.Path.Combine(folder, ".write-test");
                    File.WriteAllText(probe, "");
                    File.Delete(probe);
                    return folder;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Try the next one
                }
            }
            throw new InvalidOperationException("No writable folder for community tool rollback scripts");
        }

        public sealed record ExistingProcedure(string Definition, bool UsesAnsiNulls, bool UsesQuotedIdentifier);

        /// <param name="existing">Null if the procedure didn't exist.</param>
        public void Add(string instance, string database, CommunityTool tool, ExistingProcedure existing)
        {
            var sb = new StringBuilder();
            if (!started)
            {
                sb.AppendLine($"-- Rollback for community tools deployed to {instance} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("-- Puts back each procedure as it was before the deployment.  Tables the deployment created (e.g. CommandLog) aren't removed.");
                sb.AppendLine();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                started = true;
            }
            var name = $"{CommunityTool.Schema}.{QuoteName(tool.Name)}";
            sb.AppendLine($"-- {name} in {QuoteName(database)}");
            sb.AppendLine($"USE {QuoteName(database)}");
            sb.AppendLine("GO");
            if (existing == null)
            {
                sb.AppendLine("-- Didn't exist before the deployment.  Uncomment to remove it:");
                sb.AppendLine($"-- DROP PROCEDURE {name}");
            }
            else if (string.IsNullOrEmpty(existing.Definition))
            {
                sb.AppendLine("-- Existed, but its definition couldn't be read (encrypted, or no VIEW DEFINITION), so it couldn't be saved.");
            }
            else
            {
                var keyword = CommunityToolSafety.FindLeadingKeyword(existing.Definition);
                var definition = keyword.Index < 0
                    ? existing.Definition
                    : string.Concat(existing.Definition.AsSpan(0, keyword.Index), "ALTER", existing.Definition.AsSpan(keyword.Index + keyword.Length));
                sb.AppendLine($"SET ANSI_NULLS {(existing.UsesAnsiNulls ? "ON" : "OFF")}");
                sb.AppendLine("GO");
                sb.AppendLine($"SET QUOTED_IDENTIFIER {(existing.UsesQuotedIdentifier ? "ON" : "OFF")}");
                sb.AppendLine("GO");
                sb.AppendLine($"IF OBJECT_ID(N'{name.Replace("'", "''")}', N'P') IS NULL EXEC(N'CREATE PROCEDURE {name.Replace("'", "''")} AS RETURN')");
                sb.AppendLine("GO");
                if (Regex.IsMatch(definition, @"^[ \t]*GO\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant))
                {
                    // A line starting with GO (e.g. in a comment) would split the batch in SSMS/sqlcmd, so run it as a string.
                    sb.AppendLine($"EXEC(N'{definition.TrimEnd().Replace("'", "''")}')");
                }
                else
                {
                    sb.AppendLine(definition.TrimEnd());
                }
                sb.AppendLine("GO");
            }
            sb.AppendLine();
            File.AppendAllText(Path, sb.ToString());
        }

        private static string QuoteName(string name) => "[" + name.Replace("]", "]]") + "]";
    }
}
