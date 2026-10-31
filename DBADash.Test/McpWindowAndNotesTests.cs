using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DBADashAI.Mcp;
using DBADashAI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The summary tools that can look at a past window, and the notes that tell the model a list stopped at the row
    /// limit.  A "to" offered on a tool whose procedure doesn't take @ToDate would fail on every call, so which tools
    /// offer it is checked against the procedures themselves.
    /// </summary>
    [TestClass]
    public class McpWindowAndNotesTests
    {
        private static Dictionary<string, JsonElement> Args(string json) =>
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

        [TestMethod]
        public void To_IsReadAsUtc_WhereSupported()
        {
            var (request, error) = AiToolMcpAdapter.ParseArguments("waits-summary",
                Args("""{"to":"2026-10-23T14:00:00","hoursBack":2}"""), supportsEndTime: true);

            Assert.IsNull(error);
            Assert.AreEqual(new DateTime(2026, 10, 23, 14, 0, 0, DateTimeKind.Utc), request!.ToUtc);
            Assert.AreEqual(DateTimeKind.Utc, request.ToUtc!.Value.Kind);
        }

        [TestMethod]
        public void To_WithAnOffset_IsConvertedToUtc()
        {
            var (request, _) = AiToolMcpAdapter.ParseArguments("waits-summary",
                Args("""{"to":"2026-10-23T15:00:00+01:00"}"""), supportsEndTime: true);

            Assert.AreEqual(new DateTime(2026, 10, 23, 14, 0, 0, DateTimeKind.Utc), request!.ToUtc);
        }

        [TestMethod]
        public void To_IsRefused_WhereNotSupported()
        {
            var (request, error) = AiToolMcpAdapter.ParseArguments("config-current-summary",
                Args("""{"to":"2026-10-23T14:00:00"}"""));

            Assert.IsNull(request);
            StringAssert.Contains(error, "Unknown argument 'to'");
        }

        [TestMethod]
        public void To_MustBeADate()
        {
            var (request, error) = AiToolMcpAdapter.ParseArguments("waits-summary", Args("""{"to":"yesterday"}"""), supportsEndTime: true);

            Assert.IsNull(request);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }

        [TestMethod]
        public void Schema_OffersTo_OnlyWhenSupported()
        {
            Assert.IsTrue(AiToolMcpAdapter.InputSchema(true).GetProperty("properties").TryGetProperty("to", out _));
            Assert.IsFalse(AiToolMcpAdapter.InputSchema(false).GetProperty("properties").TryGetProperty("to", out _));
        }

        /// <summary>
        /// Every tool that says it supports an end time must call a procedure that takes @ToDate, and pass it -
        /// and every summary procedure that takes @ToDate should be offered with it.
        /// </summary>
        [TestMethod]
        public void EndTimeTools_MatchTheProceduresThatTakeToDate()
        {
            var root = MessageAuthorizationTests.FindRepositoryRoot();
            var sources = Directory.GetFiles(Path.Combine(root, "DBADashAI.Tools", "Services", "Tools"), "*.cs")
                .Concat(Directory.GetFiles(Path.Combine(root, "DBADashAI.Tools", "Mcp"), "*.cs"))
                .ToDictionary(f => f, File.ReadAllText);

            var supported = AiToolRegistration.ToolTypes.Concat(McpToolRegistration.McpOnlyToolTypes)
                .Where(t => ((IAiTool)Activator.CreateInstance(t, [null])!).SupportsEndTime)
                .ToList();
            Assert.IsNotEmpty(supported);

            var procsWithToDate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(Path.Combine(root, "DBADashDB", "AI", "Stored Procedures"), "*.sql"))
            {
                if (File.ReadAllText(file).Contains("@ToDate DATETIME2", StringComparison.OrdinalIgnoreCase))
                {
                    procsWithToDate.Add("AI." + Path.GetFileNameWithoutExtension(file));
                }
            }

            var offered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in supported)
            {
                var source = sources.Single(s => Regex.IsMatch(s.Value, $@"\bclass {type.Name}\b")).Value;
                var calls = Regex.Matches(source, @"Query(?:Multi)?Async\(""(AI\.\w+)""[^;]*request\.ToUtc").Select(m => m.Groups[1].Value).ToList();

                Assert.IsNotEmpty(calls, $"{type.Name} supports an end time but passes no request.ToUtc to its procedure.");
                foreach (var proc in calls)
                {
                    Assert.Contains(proc, procsWithToDate, $"{type.Name} passes @ToDate to {proc}, which doesn't take it.");
                    offered.Add(proc);
                }
            }

            // Only the procedures the summary tools call: others take @ToDate for their own callers (the AI service's
            // performance investigation, get-deadlock's AI.DeadlockGraph_Get).
            var calledByTools = sources.Values
                .SelectMany(s => Regex.Matches(s, @"""(AI\.\w+)""").Select(m => m.Groups[1].Value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            procsWithToDate.IntersectWith(calledByTools);
            procsWithToDate.Remove("AI.DeadlockGraph_Get");
            CollectionAssert.AreEquivalent(procsWithToDate.Order().ToList(), offered.Order().ToList(),
                "A summary procedure takes @ToDate but its tool doesn't offer to.");
        }

        [TestMethod]
        public void RowLimitNotes_FlagOnlyListsAtTheLimit()
        {
            var data = JsonSerializer.SerializeToElement(new
            {
                generatedUtc = DateTime.UtcNow,
                rows = Enumerable.Range(1, 5).ToArray(),
                detail = Enumerable.Range(1, 3).ToArray()
            });

            var notes = McpResultNotes.RowLimitNotes(data, maxRows: 5).ToList();

            Assert.HasCount(1, notes);
            StringAssert.StartsWith(notes[0], "rows has 5 rows");
            Assert.IsEmpty(McpResultNotes.RowLimitNotes(data, maxRows: 50));
        }
    }
}
