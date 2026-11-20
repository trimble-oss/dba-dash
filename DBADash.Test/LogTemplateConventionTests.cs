#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The service's log is JSON by default, and each property a message template names is a field in it - a column when
    /// the log is opened as a grid.  So the templates follow a few rules:
    /// <list type="bullet">
    ///   <item>A message is a template with its values as properties - not built with $"..." or +, which turns the
    ///   values into text that can't be filtered on and gives every event a template of its own.  Holes are named, not
    ///   numbered.</item>
    ///   <item>Properties are PascalCase, Serilog's convention and that of the enrichers (ThreadId, MachineName).  Serilog
    ///   property names are case-sensitive, so {instance} and {Instance} are two fields.</item>
    ///   <item>The same thing has the same name wherever it's logged:
    ///     ConnectionID - DBA Dash's ID for a source connection.  Also added to everything logged while work for one
    ///     connection runs, as are MessageId and Handle for a message from the GUI - see LogScope.
    ///     Source - a source connection as configured (ConnectionForPrint: server|database, a folder or S3 path).
    ///     Destination - a destination as configured.
    ///     Instance - a SQL instance name, as the server reports it or a connection string names it, where there's no
    ///     source connection to name.
    ///     Database, Path, Procedure, Tool, SessionName (an XE session), SessionID (a session_id), MessageId, MessageType.
    ///     Error - an exception's text, where it's expected and logging the exception itself would only add a stack
    ///     trace.  Reason - why something was skipped.</item>
    ///   <item>A number of seconds or milliseconds says so: DurationMs, TimeoutSeconds.  A TimeSpan needn't.  The one
    ///   exception is how long something took: Elapsed, in milliseconds, as SerilogTimings logs it for every timed
    ///   operation - so it's one column, whoever logged it.</item>
    ///   <item>The names the JSON log's own fields are shown under - Timestamp, Level, Message, Exception, EventType - are
    ///   left to them.</item>
    /// </list>
    /// </summary>
    [TestClass]
    public class LogTemplateConventionTests
    {
        /// <summary>
        /// A logging call: Serilog's Log.X / logger.X, Microsoft's logger.LogX, or a SerilogTimings operation.  Not a
        /// LogError method of DBA Dash's own, such as DBCollector's, which records an error rather than logging a template.
        /// </summary>
        private static readonly Regex LogCall = new(
            @"\b(?:Log|_log|log|logger|_logger|Logger|op|Operation|LogTiming)\.(?:Verbose|Debug|Information|Warning|Error|Fatal|Write|Begin|Time)\s*\(" +
            @"|\b_?[Ll]ogger\.Log(?:Information|Warning|Error|Debug|Trace|Critical)\s*\(" +
            @"|\)\s*\.\s*(?:Begin|Time)\s*\(");

        /// <summary>A regular or verbatim string literal.</summary>
        private static readonly Regex Literal = new(@"^@?""((?:[^""\\\n]|\\.)*)""$");

        /// <summary>A template hole: {Name}, {@Name}, {$Name}, with an optional alignment or format.</summary>
        private static readonly Regex Hole = new(@"(?<!\{)\{[@$]?([A-Za-z_][A-Za-z0-9_]*)(?:[,:][^}]*)?\}(?!\})");

        /// <summary>A numbered hole, string.Format style: {0}.</summary>
        private static readonly Regex NumberedHole = new(@"(?<!\{)\{[0-9]+(?:[,:][^}]*)?\}(?!\})");

        /// <summary>Names used for one of the things above - the name to use instead.</summary>
        private static readonly Dictionary<string, string> Retired = new(StringComparer.Ordinal)
        {
            ["ConnectionString"] = "Source, or ConnectionID",
            ["DataSource"] = "Instance, or Source",
            ["InstanceName"] = "Instance, or ConnectionID",
            ["DbName"] = "Database",
            ["DB"] = "Database",
            ["DatabaseName"] = "Database",
            ["FilePath"] = "Path",
            ["ToolName"] = "Tool",
            ["Proc"] = "Procedure",
            ["Session"] = "SessionName, or SessionID",
            ["ErrorMessage"] = "Error",
            ["Duration"] = "a name with its unit - DurationMs, DurationSeconds",
            ["ElapsedMs"] = "Elapsed - in milliseconds, as SerilogTimings logs it",
            ["ElapsedSeconds"] = "Elapsed - in milliseconds, as SerilogTimings logs it",
            ["Ms"] = "Elapsed, or another name with its unit",
            ["Timeout"] = "a name with its unit - TimeoutMs, TimeoutSeconds",
            ["Latency"] = "LatencyMs",
            ["Limit"] = "a name with its unit - LimitSeconds",
            ["WaitTime"] = "a name with its unit - WaitSeconds"
        };

        /// <summary>The JSON log's own fields, as the log grid shows them - a property of the same name would be a second column.</summary>
        private static readonly string[] Reserved = ["Timestamp", "Level", "Message", "Exception", "EventType"];

        private static string RepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "DBADash.sln"))) return dir.FullName;
            }

            throw new DirectoryNotFoundException("DBADash.sln not found above " + AppContext.BaseDirectory);
        }

        /// <summary>The index just past the string or char literal starting at <paramref name="i"/>.</summary>
        private static int SkipLiteral(string text, int i)
        {
            if (text[i] == '\'')
            {
                var close = text.IndexOf('\'', text[i + 1] == '\\' ? i + 3 : i + 2);
                return close < 0 ? text.Length : close + 1;
            }

            var verbatim = i > 0 && (text[i - 1] == '@' || (text[i - 1] == '$' && i > 1 && text[i - 2] == '@'));
            for (i++; i < text.Length; i++)
            {
                if (!verbatim && text[i] == '\\') i++;
                else if (text[i] == '"')
                {
                    if (verbatim && i + 1 < text.Length && text[i + 1] == '"') i++;
                    else return i + 1;
                }
            }

            return text.Length;
        }

        /// <summary>
        /// Splits <paramref name="text"/> at <paramref name="separator"/> where it's at the top level - not in brackets, a
        /// string, or a comment.  From <paramref name="start"/> to the first unmatched closing bracket.
        /// </summary>
        private static List<string> SplitTopLevel(string text, int start, char separator)
        {
            var parts = new List<string>();
            var depth = 0;
            var partStart = start;
            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];
                if (c is '"' or '\'')
                {
                    i = SkipLiteral(text, i) - 1;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    i = text.IndexOf('\n', i);
                    if (i < 0) break;
                }
                else if (c is '(' or '[' or '{') depth++;
                else if (c is ')' or ']' or '}')
                {
                    if (depth == 0)
                    {
                        parts.Add(text[partStart..i]);
                        return parts;
                    }

                    depth--;
                }
                else if (c == separator && depth == 0)
                {
                    parts.Add(text[partStart..i]);
                    partStart = i + 1;
                }
            }

            parts.Add(text[partStart..]);
            return parts;
        }

        /// <summary>
        /// Each logging call's message template argument - the first that starts with a string - with where it is.  A call
        /// whose message is a variable or an expression isn't checked: what it holds can't be seen from here.
        /// </summary>
        private static IEnumerable<(string Template, int Values, string Where)> Templates()
        {
            var root = RepoRoot();
            var skip = new[] { "bin", "obj", ".claude", ".git", "DBADashBuild", "TestResults" };
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file);
                if (relative.Split(Path.DirectorySeparatorChar).Any(part => skip.Contains(part, StringComparer.OrdinalIgnoreCase))) continue;

                var text = File.ReadAllText(file);
                foreach (Match call in LogCall.Matches(text))
                {
                    var arguments = SplitTopLevel(text, call.Index + call.Length, ',').Select(a => a.Trim()).ToList();
                    var index = arguments.FindIndex(a => a.StartsWith('"') || a.StartsWith("@\"") || a.StartsWith('$'));
                    if (index < 0) continue;
                    var line = 1 + text.AsSpan(0, call.Index).Count('\n');
                    // The values are the arguments after the template
                    yield return (arguments[index], arguments.Count - index - 1, $"{relative}:{line}");
                }
            }
        }

        /// <summary>Each property named in a log template, with where it's used.</summary>
        private static IEnumerable<(string Name, string Where)> TemplateProperties() =>
            from t in Templates()
            from part in SplitTopLevel(t.Template, 0, '+')
            let literal = Literal.Match(part.Trim())
            where literal.Success
            from Match hole in Hole.Matches(literal.Groups[1].Value)
            select (hole.Groups[1].Value, t.Where);

        private static string List(IEnumerable<string> lines) => "\n" + string.Join("\n", lines);

        [TestMethod]
        public void LogMessages_AreTemplates_NotBuiltFromValues()
        {
            var built = Templates()
                .Where(t => t.Template.StartsWith('$') ||
                            SplitTopLevel(t.Template, 0, '+').Any(part => !Literal.IsMatch(part.Trim())))
                .Select(t => $"{t.Where}: {t.Template.Split('\n')[0]}")
                .ToList();

            Assert.AreEqual(0, built.Count,
                "Pass values as template properties, not with $\"...\" or +:" + List(built));
        }

        [TestMethod]
        public void LogTemplateHoles_AreNamed()
        {
            var numbered = Templates().Where(t => NumberedHole.IsMatch(t.Template)).Select(t => t.Where).ToList();

            Assert.AreEqual(0, numbered.Count, "Name template holes - {Instance}, not {0}:" + List(numbered));
        }

        /// <summary>
        /// Values fill holes in order, so one too many shifts the rest along - each hole shows the value meant for the
        /// one before - and the last is logged under a made-up name, __4.  One too few leaves a hole empty.
        /// </summary>
        [TestMethod]
        public void LogTemplates_HaveAValuePerHole()
        {
            var mismatched = Templates()
                .Select(t => (t.Where, t.Values, Pieces: SplitTopLevel(t.Template, 0, '+').Select(p => Literal.Match(p.Trim())).ToList()))
                .Where(t => t.Pieces.All(p => p.Success))
                .Select(t => (t.Where, t.Values, Holes: t.Pieces
                    .SelectMany(p => Hole.Matches(p.Groups[1].Value).Select(h => h.Groups[1].Value))
                    .Distinct(StringComparer.Ordinal).Count()))
                .Where(t => t.Holes != t.Values)
                .Select(t => $"{t.Where}: {t.Holes} hole(s), {t.Values} value(s)")
                .ToList();

            Assert.AreEqual(0, mismatched.Count, "A value for each hole in the template:" + List(mismatched));
        }

        [TestMethod]
        public void LogTemplateProperties_ArePascalCase()
        {
            var camel = TemplateProperties().Where(p => !char.IsUpper(p.Name[0])).ToList();

            Assert.AreEqual(0, camel.Count,
                "Name template properties in PascalCase:" + List(camel.Select(p => $"{{{p.Name}}} at {p.Where}")));
        }

        [TestMethod]
        public void LogTemplateProperties_HaveOneSpellingEach()
        {
            var clashes = TemplateProperties()
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() > 1)
                .Select(g => string.Join(", ", g.GroupBy(p => p.Name, StringComparer.Ordinal).Select(s => $"{{{s.Key}}} ({s.First().Where})")))
                .ToList();

            Assert.AreEqual(0, clashes.Count, "The same property spelled differently:" + List(clashes));
        }

        [TestMethod]
        public void LogTemplateProperties_UseTheSharedNames()
        {
            var retired = TemplateProperties().Where(p => Retired.ContainsKey(p.Name)).ToList();

            Assert.AreEqual(0, retired.Count,
                "Use the name the rest of the logging uses:" +
                List(retired.Select(p => $"{{{p.Name}}} at {p.Where} - use {Retired[p.Name]}")));
        }

        [TestMethod]
        public void LogTemplateProperties_LeaveTheLogsOwnFieldNames()
        {
            var reserved = TemplateProperties().Where(p => Reserved.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            Assert.AreEqual(0, reserved.Count,
                "The JSON log shows its own fields under these names - use another, e.g. {Error}, {Reason}, {Detail}:" +
                List(reserved.Select(p => $"{{{p.Name}}} at {p.Where}")));
        }

        /// <summary>
        /// Operation.At(level) shares one Operation between callers when its level is off, which mixes up their log
        /// context - see <see cref="LogTiming"/>, which is used instead.
        /// </summary>
        [TestMethod]
        public void TimedOperationsAtALevel_UseLogTiming()
        {
            var root = RepoRoot();
            var skip = new[] { "bin", "obj", ".claude", ".git", "DBADashBuild", "TestResults" };
            var direct = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !Path.GetRelativePath(root, f).Split(Path.DirectorySeparatorChar)
                    .Any(part => skip.Contains(part, StringComparer.OrdinalIgnoreCase)))
                .Where(f => Path.GetFileName(f) is not ("LogTiming.cs" or "LogTemplateConventionTests.cs"))
                .SelectMany(f => File.ReadLines(f).Select((line, i) => (line, where: $"{Path.GetRelativePath(root, f)}:{i + 1}")))
                .Where(l => l.line.Contains("Operation.At(", StringComparison.Ordinal) && !l.line.TrimStart().StartsWith("//"))
                .Select(l => l.where)
                .ToList();

            Assert.AreEqual(0, direct.Count, "Use LogTiming.Begin(level, ...) rather than Operation.At(level):" + List(direct));
        }

        [TestMethod]
        public void Templates_AreReadToTheEndOfTheCall()
        {
            // A ; in a message once ended the statement early, and the properties after it went unchecked.
            Assert.IsTrue(TemplateProperties().Any(p => p.Name == "Grace"),
                "XETraceMessage's \"...of cancellation; forcing teardown\" warning should be read to its end");
            // And a message split over lines with + is one template, not a value joined in.
            Assert.IsTrue(TemplateProperties().Any(p => p.Name == "AnalysesRead"));
        }
    }
}
