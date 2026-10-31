using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The pipe the GUI publishes what it is showing on, and the local MCP server reads.  The pipe name is per
    /// user and session, so these share it with a GUI the developer might have open - they run one at a time,
    /// and are inconclusive rather than failing when a GUI already holds the pipe.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class GuiContextPipeTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private static GuiContextSnapshot Sample() => new()
        {
            NodePath = @"Production\SQL01",
            NodeType = "Instance",
            Tab = "Performance",
            Instance = "SQL01",
            InstanceId = 3,
            InstanceIds = [3],
            Database = "Sales",
            FromUtc = new DateTime(2026, 10, 23, 6, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 10, 23, 7, 0, 0, DateTimeKind.Utc)
        };

        private static async Task SkipIfAGuiIsOpen()
        {
            if (await GuiContextPipe.TryReadAsync(TimeSpan.FromMilliseconds(300), CancellationToken.None) is not null)
            {
                Assert.Inconclusive("A DBA Dash GUI is open and serving the context pipe.");
            }
        }

        [TestMethod]
        public async Task ClientReadsWhatTheServerCaptures()
        {
            await SkipIfAGuiIsOpen();
            var captures = 0;

            using (GuiContextPipeServer.Start(() => { captures++; return Sample(); }, () => null))
            {
                for (var i = 0; i < 2; i++)
                {
                    var snapshot = await GuiContextPipe.TryReadAsync(Timeout, CancellationToken.None);

                    Assert.IsNotNull(snapshot);
                    Assert.AreEqual("SQL01", snapshot.Instance);
                    Assert.AreEqual(3, snapshot.InstanceId);
                    Assert.AreEqual(@"Production\SQL01", snapshot.NodePath);
                    Assert.AreEqual(new DateTime(2026, 10, 23, 7, 0, 0, DateTimeKind.Utc), snapshot.ToUtc);
                    Assert.IsNull(snapshot.RepositoryFingerprint, "No connection string, so no fingerprint.");
                    Assert.IsTrue((DateTime.UtcNow - snapshot.CapturedUtc).Duration() < TimeSpan.FromMinutes(1));
                }
            }

            // Captured afresh for each reader, not once.
            Assert.AreEqual(2, captures);
        }

        [TestMethod]
        public async Task OpenDeadlockAndPlan_CarryTheirXml()
        {
            await SkipIfAGuiIsOpen();
            var sample = Sample();
            sample.Deadlock = new GuiContextDeadlock { Instance = "SQL01", InstanceId = 3, Title = "victim", Xml = "<deadlock/>", DeadlockHash = "0xab" };
            sample.Plan = new GuiContextPlan { FileName = @"C:\plans\slow.sqlplan", Xml = new string('x', 300_000), QueryPlanHash = "0x01", StatementIndex = 2 };

            using (GuiContextPipeServer.Start(() => sample, () => null))
            {
                var snapshot = await GuiContextPipe.TryReadAsync(Timeout, CancellationToken.None);

                Assert.IsNotNull(snapshot?.Deadlock);
                Assert.AreEqual("<deadlock/>", snapshot.Deadlock.Xml);
                Assert.AreEqual(3, snapshot.Deadlock.InstanceId);
                Assert.AreEqual("0xab", snapshot.Deadlock.DeadlockHash);
                Assert.IsNotNull(snapshot.Plan);
                Assert.AreEqual(300_000, snapshot.Plan.Xml.Length, "A large plan arrives whole.");
                Assert.AreEqual(2, snapshot.Plan.StatementIndex);
                Assert.IsNull(snapshot.Plan.InstanceId);
            }
        }

        [TestMethod]
        public async Task NoServer_ReadsNothing()
        {
            await SkipIfAGuiIsOpen();

            Assert.IsNull(await GuiContextPipe.TryReadAsync(TimeSpan.FromMilliseconds(300), CancellationToken.None));
        }

        [TestMethod]
        public async Task ASecondCopy_LeavesTheFirstServing()
        {
            await SkipIfAGuiIsOpen();

            using var first = GuiContextPipeServer.Start(() => Sample(), () => null);
            await GuiContextPipe.TryReadAsync(Timeout, CancellationToken.None);

            var other = Sample();
            other.Instance = "SQL02";
            using var second = GuiContextPipeServer.Start(() => other, () => null);
            await Task.Delay(200);

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual("SQL01", (await GuiContextPipe.TryReadAsync(Timeout, CancellationToken.None))?.Instance);
            }
        }
    }
}
