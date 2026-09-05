using System.Linq;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// Tooltip content.  A node has room for a few lines; the tooltip is where the rest of what the
    /// graph captured surfaces, so what it includes - and what it leaves out when absent - matters.
    /// </summary>
    [TestClass]
    public class DeadlockTooltipBuilderTests
    {
        private static DeadlockLayout LayoutOf(string xmlOrSampleName, bool isSample = true)
        {
            var xml = isSample ? TestGraphs.Load(xmlOrSampleName) : xmlOrSampleName;
            var graph = DeadlockParser.Parse(xml).First();
            return new DeadlockLayoutEngine(new FakeTextMeasurer()).Layout(graph);
        }

        private static DeadlockTooltip TooltipForProcess(string sampleName, int spid)
        {
            var node = LayoutOf(sampleName).Nodes.OfType<DeadlockProcessNode>()
                .Single(n => n.Process.Spid == spid);
            return DeadlockTooltipBuilder.Build(node);
        }

        private static string? Value(DeadlockTooltip tooltip, string label) =>
            tooltip.Rows.SingleOrDefault(r => r.Label == label)?.Value;

        // ---------------------------------------------------------------- processes

        [TestMethod]
        public void Process_TitleIsTheDisplayNameAndVictimIsCalledOut()
        {
            var tooltip = TooltipForProcess(TestGraphs.KeyLock, 61);

            Assert.AreEqual("SPID 61", tooltip.Title);
            Assert.AreEqual("Deadlock victim", tooltip.Subtitle);
        }

        [TestMethod]
        public void Process_NonVictimHasNoSubtitle()
        {
            var tooltip = TooltipForProcess(TestGraphs.KeyLock, 74);

            Assert.IsNull(tooltip.Subtitle);
        }

        [TestMethod]
        public void Process_CarriesTheSessionDetail()
        {
            var tooltip = TooltipForProcess(TestGraphs.KeyLock, 61);

            Assert.AreEqual("suspended", Value(tooltip, "Status"));
            Assert.AreEqual("CONTOSO\\svc_app", Value(tooltip, "Login"));
            Assert.AreEqual("APPSRV01 (6120)", Value(tooltip, "Host"));
            Assert.AreEqual("DBA Dash", Value(tooltip, "Application"));
            Assert.AreEqual("Sales", Value(tooltip, "Database"));
            Assert.AreEqual("read committed (2)", Value(tooltip, "Isolation level"));
            Assert.AreEqual("U", Value(tooltip, "Lock mode"));
        }

        [TestMethod]
        public void Process_CarriesTheWaitAndTransactionDetail()
        {
            var tooltip = TooltipForProcess(TestGraphs.KeyLock, 61);

            Assert.AreEqual("KEY: 7:72057594043170816 (8194443284a0)", Value(tooltip, "Wait resource"));
            Assert.AreEqual("1842 ms", Value(tooltip, "Wait time"));
            Assert.AreEqual("user_transaction", Value(tooltip, "Transaction"));
            Assert.AreEqual("2", Value(tooltip, "Transaction count"));
            Assert.AreEqual("288 bytes", Value(tooltip, "Log used"));
            Assert.AreEqual("2024-03-14 09:21:44.523", Value(tooltip, "Transaction started"));
        }

        [TestMethod]
        public void Process_ShowsTheInnermostStatement()
        {
            var tooltip = TooltipForProcess(TestGraphs.KeyLock, 61);

            Assert.AreEqual(
                "UPDATE dbo.Orders SET Status = @Status WHERE OrderID = @OrderID;",
                Value(tooltip, "Statement"));
        }

        [TestMethod]
        public void Process_CollapsesAMultiLineStatementOntoOneLine()
        {
            const string xml =
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="5">
                      <executionStack>
                        <frame procname="adhoc" line="1">
                UPDATE dbo.T
                    SET C = 1
                  WHERE ID = 2;
                        </frame>
                      </executionStack>
                    </process>
                  </process-list>
                </deadlock>
                """;

            var node = LayoutOf(xml, isSample: false).Nodes.Single();
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("UPDATE dbo.T SET C = 1 WHERE ID = 2;", Value(tooltip, "Statement"));
        }

        [TestMethod]
        public void Process_TruncatesALongStatement()
        {
            var longStatement = new string('x', 500);
            var xml =
                $"""
                 <deadlock>
                   <process-list>
                     <process id="p1" spid="5"><inputbuf>{longStatement}</inputbuf></process>
                   </process-list>
                 </deadlock>
                 """;

            var node = LayoutOf(xml, isSample: false).Nodes.Single();
            var tooltip = DeadlockTooltipBuilder.Build(node, maxStatementLength: 50);

            var statement = Value(tooltip, "Statement")!;
            Assert.AreEqual(53, statement.Length);
            Assert.IsTrue(statement.EndsWith("...", System.StringComparison.Ordinal));
        }

        [TestMethod]
        public void Process_OmitsRowsTheGraphDidNotSupply()
        {
            // A truncated graph carries almost nothing; blank rows would be worse than no rows.
            var node = LayoutOf(TestGraphs.Truncated).Nodes.OfType<DeadlockProcessNode>().Single();
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.IsNull(Value(tooltip, "Login"));
            Assert.IsNull(Value(tooltip, "Host"));
            Assert.IsNull(Value(tooltip, "Wait time"));
            Assert.IsNull(Value(tooltip, "Database"));
            Assert.AreEqual("suspended", Value(tooltip, "Status"));
        }

        [TestMethod]
        public void Process_HostOmitsThePidWhenAbsent()
        {
            const string xml =
                """
                <deadlock><process-list><process id="p1" spid="5" hostname="SRV01" /></process-list></deadlock>
                """;

            var tooltip = DeadlockTooltipBuilder.Build(LayoutOf(xml, isSample: false).Nodes.Single());

            Assert.AreEqual("SRV01", Value(tooltip, "Host"));
        }

        // ---------------------------------------------------------------- resources

        [TestMethod]
        public void Resource_TitleIsTheObjectAndSubtitleIsTheLockType()
        {
            var node = LayoutOf(TestGraphs.KeyLock).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("dbo.Orders (PK_Orders)", tooltip.Title);
            Assert.AreEqual("keylock", tooltip.Subtitle);
        }

        [TestMethod]
        public void Resource_CarriesTheIdentifyingDetail()
        {
            var node = LayoutOf(TestGraphs.KeyLock).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            // The database is split off the object rather than dropped: the tooltip has the room, and
            // it is the one place the full name is still worth reading.
            Assert.AreEqual("Sales", Value(tooltip, "Database"));
            Assert.AreEqual("dbo.Orders", Value(tooltip, "Object"));
            Assert.AreEqual("PK_Orders", Value(tooltip, "Index"));
            Assert.AreEqual("X", Value(tooltip, "Mode"));
            Assert.AreEqual("7", Value(tooltip, "Database ID"));
            Assert.AreEqual("72057594043170816", Value(tooltip, "HoBt ID"));
        }

        /// <summary>
        /// A page lock is named by its page, and carries the page and the lock id as rows: with a
        /// dozen page locks of one table on screen, those are the only two values that differ.
        /// </summary>
        [TestMethod]
        public void Resource_PageLockIsNamedByItsPageAndCarriesTheLockId()
        {
            var node = LayoutOf(TestGraphs.ParallelPageLocks).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.PageId == 900);
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("Page 6:1:900", tooltip.Title);
            Assert.AreEqual("6:1:900", Value(tooltip, "Page"));
            Assert.AreEqual("lock2a71b400", Value(tooltip, "Lock ID"));
            Assert.AreEqual("Ops", Value(tooltip, "Database"));
            Assert.AreEqual("dbo.Attachment", Value(tooltip, "Object"));

            // The page rows are typed now, so they must not also come through as raw attributes.
            Assert.IsNull(Value(tooltip, "pageid"));
            Assert.IsNull(Value(tooltip, "fileid"));
        }

        /// <summary>
        /// Identifiers are written plainly.  Grouping separators are for quantities, where they help
        /// the eye judge magnitude; an id has none to judge, and the commas get in the way of what ids
        /// are actually for - matching one against another, and pasting one into a query.
        /// </summary>
        [TestMethod]
        public void Resource_WritesIdentifiersWithoutGroupingSeparators()
        {
            var node = LayoutOf(TestGraphs.KeyLock).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            foreach (var label in new[] { "Database ID", "HoBt ID", "Associated object ID" })
            {
                var value = Value(tooltip, label);
                if (value is null) continue;

                Assert.IsFalse(value.Contains(','), $"{label} was formatted as a quantity: {value}");
            }
        }

        [TestMethod]
        public void Resource_ListsOwnersAndWaitersWithTheirModes()
        {
            var node = LayoutOf(TestGraphs.KeyLock).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("SPID 74 (X)", Value(tooltip, "Owners"));
            Assert.AreEqual("SPID 61 (U)", Value(tooltip, "Waiters"));
        }

        [TestMethod]
        public void Resource_FallsBackToTheRawIdForAnUnresolvedParticipant()
        {
            // The owner is missing from the process-list, so there is no display name to show.
            var node = LayoutOf(TestGraphs.Truncated).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.TypeName == "ridlock");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("processMissing (X)", Value(tooltip, "Owners"));
        }

        [TestMethod]
        public void Resource_SurfacesAttributesThisLibraryDoesNotModel()
        {
            var node = LayoutOf(TestGraphs.Truncated).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.TypeName == "somethingNewInSql2030");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("42", Value(tooltip, "customAttribute"));
        }

        [TestMethod]
        public void Resource_DoesNotRepeatAttributesAlreadyShownAsTypedRows()
        {
            var node = LayoutOf(TestGraphs.KeyLock).Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.IsNull(Value(tooltip, "objectname"));
            Assert.IsNull(Value(tooltip, "indexname"));
            Assert.IsNull(Value(tooltip, "hobtid"));
            Assert.IsNull(Value(tooltip, "mode"));
        }

        [TestMethod]
        public void Resource_ParallelismResourceShowsItsWaitTypeAndNode()
        {
            var node = LayoutOf(TestGraphs.Parallel).Nodes.OfType<DeadlockResourceNode>().First();
            var tooltip = DeadlockTooltipBuilder.Build(node);

            Assert.AreEqual("exchangeEvent", tooltip.Title);
            Assert.IsNull(tooltip.Subtitle, "The title is already the element name.");
            Assert.AreEqual("e_waitPipeNewRow", Value(tooltip, "WaitType"));
            Assert.AreEqual("7", Value(tooltip, "nodeId"));
        }

        // ---------------------------------------------------------------- general

        [TestMethod]
        public void ToString_RendersTitleSubtitleAndRows()
        {
            var tooltip = TooltipForProcess(TestGraphs.KeyLock, 61);
            var text = tooltip.ToString();

            StringAssert.StartsWith(text, "SPID 61");
            StringAssert.Contains(text, "Deadlock victim");
            StringAssert.Contains(text, "Login: CONTOSO\\svc_app");
        }

        [TestMethod]
        public void Build_NullNode_Throws()
        {
            Assert.ThrowsExactly<System.ArgumentNullException>(() => DeadlockTooltipBuilder.Build(null!));
        }
    }
}
