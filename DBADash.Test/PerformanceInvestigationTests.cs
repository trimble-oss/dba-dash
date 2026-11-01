using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The lookups a performance analysis can make.  The model writes the request, so everything about
    /// it is untrusted: what it may reach has to be decided here, not by what it asked for.
    /// </summary>
    [TestClass]
    public class PerformanceInvestigationTests
    {
        private static readonly DateTime From = new(2026, 10, 18, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime To = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

        private static AiPerformanceAnalysisRequest Request(bool allow = true) => new()
        {
            Instance = "SQL01",
            InstanceId = 1,
            Window = "w",
            AllowInvestigation = allow,
            FromUtc = From,
            ToUtc = To,
            UtcOffsetMinutes = 60
        };

        /// <summary>No repository connection, which is all a request the parser rejects ever needs.</summary>
        private static PerformanceInvestigation Investigation() =>
            new(new SqlToolExecutor(new ConfigurationBuilder().Build()));

        [TestMethod]
        public void FindRequest_ReadsTheBlockAnywhereInTheReply()
        {
            var reply = "Let me check the blockers.\n```investigate\n{\"tool\": \"blocking\"}\n```\n";

            Assert.AreEqual("{\"tool\": \"blocking\"}", PerformanceInvestigation.FindRequest(reply));
            Assert.IsNull(PerformanceInvestigation.FindRequest("## Summary\nNo pressure.\n```sql\nSELECT 1\n```"));
            Assert.AreEqual("Let me check the blockers.", PerformanceInvestigation.RemoveRequests(reply));
        }

        [TestMethod]
        public void Times_AreTheReadersAndComeBackAsUtc()
        {
            Assert.AreEqual(new DateTime(2026, 10, 18, 10, 30, 0), PerformanceInvestigation.Utc("2026-10-18 11:30", Request()));
            Assert.IsNull(PerformanceInvestigation.Utc("half past eleven", Request()));
            Assert.AreEqual("2026-10-18 11:00", PerformanceInvestigation.Local(From, Request()));
        }

        [TestMethod]
        public void Instructions_OfferTheWindowInTheReadersTime()
        {
            var text = PerformanceInvestigation.Instructions(Request(), 3);

            StringAssert.Contains(text, "up to 3 times");
            StringAssert.Contains(text, "2026-10-18 11:00 to 2026-10-18 13:00");
        }

        [TestMethod]
        public void CanInvestigate_NeedsTheReadersSayAndARepository()
        {
            Assert.IsFalse(Investigation().CanInvestigate(Request(allow: false)));
            // Asked for, but local mode has no repository to read.
            Assert.IsFalse(Investigation().CanInvestigate(Request()));
        }

        [TestMethod]
        public async Task BadRequests_AreToldToTheModelRatherThanRun()
        {
            var investigation = Investigation();

            foreach (var json in new[]
                     {
                         "not json",
                         "{\"tool\": \"drop_table\"}",
                         "{\"tool\": \"waiting_queries\"}",
                         "{\"tool\": \"waiting_queries\", \"wait_type\": \"LCK'; DROP TABLE x--\"}",
                         "{\"tool\": \"deadlock\", \"signature\": \"1234\"}"
                     })
            {
                var (_, result) = await investigation.RunAsync(json, Request(), CancellationToken.None);
                StringAssert.StartsWith(result, "That lookup could not be run", json);
            }
        }

        [TestMethod]
        public void Format_ShowsTimesInTheReadersZoneAndKeepsWithinTheLimit()
        {
            var sets = new List<List<Dictionary<string, object>>>
            {
                new() { new() { ["SnapshotsWithBlocking"] = 12, ["SnapshotsAnalysed"] = 12 } },
                new() { new() { ["ObjectName"] = "dbo.usp_Orders", ["StatementText"] = "UPDATE  Orders |\r\n SET x = 1", ["FirstSeenUTC"] = From } },
                new()
            };

            var text = PerformanceInvestigation.Format("Blocking", sets!, PerformanceInvestigation.BlockingTool, Request());

            StringAssert.Contains(text, "Root blockers, worst first:");
            StringAssert.Contains(text, "ObjectName | StatementText | FirstSeen");
            StringAssert.Contains(text, "dbo.usp_Orders | UPDATE Orders / SET x = ? | 2026-10-18 11:00");
            StringAssert.Contains(text, "(none)");

            var big = new List<Dictionary<string, object>>();
            for (var i = 0; i < 500; i++) big.Add(new() { ["StatementText"] = new string('x', 100) });
            var cut = PerformanceInvestigation.Format("Long", new() { big! }, PerformanceInvestigation.LongRunningTool, Request());
            Assert.IsTrue(cut.Length < PerformanceInvestigation.MaxResultLength + 200);
            StringAssert.Contains(cut, "cut short");
        }

        [TestMethod]
        public void Parse_NarrowsTheWindowButNeverWidensIt()
        {
            // 11:30-12:00 reader time is 10:30-11:00 UTC, inside the window.
            var narrowed = PerformanceInvestigation.Parse("{\"tool\": \"blocking\", \"from\": \"2026-10-18 11:30\", \"to\": \"2026-10-18 12:00\"}", Request());
            Assert.AreEqual(From.AddMinutes(30), narrowed.FromUtc);
            Assert.AreEqual(From.AddHours(1), narrowed.ToUtc);

            // A day either side is the window.
            var widened = PerformanceInvestigation.Parse("{\"tool\": \"blocking\", \"from\": \"2026-10-17 11:00\", \"to\": \"2026-10-19 11:00\"}", Request());
            Assert.AreEqual(From, widened.FromUtc);
            Assert.AreEqual(To, widened.ToUtc);

            var waits = PerformanceInvestigation.Parse("{\"tool\": \"WAITING_QUERIES\", \"wait_type\": \"pagelatch%\", \"tempdb_only\": true}", Request());
            Assert.AreEqual("PAGELATCH%", waits.WaitType);
            Assert.IsTrue(waits.TempDBOnly);
        }

        [TestMethod]
        public async Task LooseJson_IsReadAndAnUnreadableRequestSaysWhy()
        {
            var loose = PerformanceInvestigation.Parse("{\"tool\": \"long_running\", // the peak\n \"reason\": \"x\",}", Request());
            Assert.AreEqual(PerformanceInvestigation.LongRunningTool, loose.Tool);

            var (summary, _) = await Investigation().RunAsync("{\"tool\": \"drop_table\"}", Request(), CancellationToken.None);
            StringAssert.Contains(summary, "must be one of");
        }

        [TestMethod]
        public async Task MalformedBlock_ReachesTheParserSoTheModelCanTryAgain()
        {
            var block = PerformanceInvestigation.FindRequest("```investigate\n{\"tool\": \"blocking\"\n```");
            Assert.AreEqual("{\"tool\": \"blocking\"", block);

            var (_, result) = await Investigation().RunAsync(block!, Request(), CancellationToken.None);
            StringAssert.StartsWith(result, "That lookup could not be run");
        }

        [TestMethod]
        public void ClockChange_EachTimeUsesTheOffsetThatAppliedThen()
        {
            // UK clocks go back at 01:00 UTC on 2026-10-25: BST (+60) before, GMT (0) after.
            var change = new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc);
            var request = Request();
            request.FromUtc = change.AddHours(-3);
            request.ToUtc = change.AddHours(3);
            request.UtcOffsetMinutes = 0;
            request.UtcOffsetChanges.Add(new AiUtcOffsetChange { FromUtc = change, PreviousOffsetMinutes = 60, OffsetMinutes = 0 });

            Assert.AreEqual("2026-10-24 23:00", PerformanceInvestigation.Local(change.AddHours(-3), request));
            Assert.AreEqual("2026-10-25 04:00", PerformanceInvestigation.Local(change.AddHours(3), request));
            // 23:30 local was BST, so 22:30 UTC - not 23:30 as the offset at the end would make it.
            Assert.AreEqual(change.AddHours(-2.5), PerformanceInvestigation.Utc("2026-10-24 23:30", request));
            Assert.AreEqual(change.AddHours(2), PerformanceInvestigation.Utc("2026-10-25 03:00", request));
        }

        [TestMethod]
        public void ResultTurn_SaysHowManyLookupsAreLeft()
        {
            StringAssert.Contains(PerformanceInvestigation.ResultTurn("r", 2), "2 more lookups");
            StringAssert.Contains(PerformanceInvestigation.ResultTurn("r", 0), "Answer now");
        }

        [TestMethod]
        public void Validate_RejectsAWindowThatEndsBeforeItStarts()
        {
            var request = Request();
            request.Sections.Add(new AiPerformanceAnalysisSection { Title = "CPU", Content = "x" });
            Assert.IsNull(request.Validate());

            request.FromUtc = To;
            request.ToUtc = From;
            Assert.IsNotNull(request.Validate());
        }
    }
}
