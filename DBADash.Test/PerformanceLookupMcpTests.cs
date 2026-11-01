using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DBADashAI.Mcp;
using DBADashAI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Server;

namespace DBADash.Test
{
    /// <summary>
    /// The performance lookups the AI service's analysis can ask for, offered over MCP too.  One formatter serves both:
    /// the AI service shows times in the reader's zone, MCP in UTC, and the result must say which.
    /// </summary>
    [TestClass]
    public class PerformanceLookupMcpTests
    {
        private static List<List<Dictionary<string, object?>>> Sets() =>
        [
            [new Dictionary<string, object?> { ["SnapshotDateUTC"] = new DateTime(2026, 10, 24, 14, 0, 0, DateTimeKind.Utc), ["Blocked"] = 3 }],
            [new Dictionary<string, object?> { ["session_id"] = 55, ["Statement"] = "UPDATE dbo.T\r\n SET x = 1 | 2", ["Sleeping"] = true }],
            []
        ];

        [TestMethod]
        public void Format_InUtc_KeepsUtcColumnNamesAndSaysSo()
        {
            var text = PerformanceLookups.Format("Root blockers on SQL01", Sets(), PerformanceLookups.Blocking,
                t => t.ToString("yyyy-MM-dd HH:mm:ss"), "Times are UTC.", timesAreLocal: false, 10_000);

            StringAssert.Contains(text, "Times are UTC.");
            StringAssert.Contains(text, "SnapshotDateUTC | Blocked");
            StringAssert.Contains(text, "2026-10-24 14:00:00 | 3");
            StringAssert.Contains(text, "Root blockers, worst first:");
            StringAssert.Contains(text, "55 | UPDATE dbo.T SET x = ? / ? | yes", "Whitespace collapsed, pipes kept out of the table and literals masked.");
            StringAssert.Contains(text, "What the blocked sessions waited on:\n(none)".Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void Format_InTheReadersZone_DropsUtcFromColumnNames()
        {
            var text = PerformanceLookups.Format("x", Sets(), PerformanceLookups.Blocking,
                t => t.AddHours(1).ToString("yyyy-MM-dd HH:mm"), "Times are in the same zone as the figures above.", timesAreLocal: true, 10_000);

            StringAssert.Contains(text, "SnapshotDate | Blocked");
            StringAssert.Contains(text, "2026-10-24 15:00 | 3");
        }

        [TestMethod]
        public void Format_IsCutAtTheLimit_AndSaysSo()
        {
            var text = PerformanceLookups.Format("x", Sets(), PerformanceLookups.Blocking, t => t.ToString("O"), "Times are UTC.", false, 60);

            StringAssert.EndsWith(text.TrimEnd(), "(cut short - the rest of the result was left out)");
        }

        [TestMethod]
        public void EveryLookup_HasAProcedure()
        {
            foreach (var lookup in PerformanceLookups.All)
            {
                StringAssert.StartsWith(PerformanceLookups.Procedure(lookup), "AI.PerformanceInvestigation");
            }
        }

        [TestMethod]
        [DataRow("PAGELATCH%", true)]
        [DataRow("LCK_M_X", true)]
        [DataRow("PAGELATCH%'; DROP", false)]
        [DataRow("", false)]
        public void WaitTypes_AreChecked(string value, bool valid) => Assert.AreEqual(valid, PerformanceLookups.IsWaitType(value));

        [TestMethod]
        public void Tools_RequireWhatTheirProcedureNeeds()
        {
            McpServerTool[] tools = [new GetRootBlockersTool(null), new GetWaitingQueriesTool(null), new GetLongRunningQueriesTool(null), new GetDeadlockSignatureTool(null)];
            var required = tools.ToDictionary(t => t.ProtocolTool.Name,
                t => t.ProtocolTool.InputSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList());

            CollectionAssert.AreEqual(new[] { "instance" }, required["get-root-blockers"]);
            CollectionAssert.AreEqual(new[] { "instance", "waitType" }, required["get-waiting-queries"]);
            CollectionAssert.AreEqual(new[] { "instance" }, required["get-long-running-queries"]);
            CollectionAssert.AreEqual(new[] { "instance", "signature" }, required["get-deadlock-signature"]);

            foreach (var tool in tools)
            {
                var properties = tool.ProtocolTool.InputSchema.GetProperty("properties");
                Assert.IsTrue(properties.TryGetProperty("to", out _), tool.ProtocolTool.Name);
                Assert.IsTrue(tool.ProtocolTool.Annotations?.ReadOnlyHint, tool.ProtocolTool.Name);
            }
        }

        [TestMethod]
        public void PerformancePrompt_PointsAtTheLookups()
        {
            var prompt = AnalysisMcpPrompt.All().Single(p => p.ProtocolPrompt.Name == "analyse-performance")
                .Render(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"instance":"SQL01"}"""));

            foreach (var tool in new[] { "get-root-blockers", "get-waiting-queries", "get-long-running-queries", "get-deadlock-signature" })
            {
                StringAssert.Contains(prompt, tool);
            }
        }
    }
}
