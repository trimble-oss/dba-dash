using DBADashGUI.Grids;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    [TestClass]
    public class StatisticsMessagesTests
    {
        // As SSMS shows it, from SET STATISTICS IO, TIME ON then a query - the first execution time is SET's own.
        private const string Example = """
             SQL Server Execution Times:
               CPU time = 0 ms,  elapsed time = 0 ms.
            SQL Server parse and compile time:
               CPU time = 0 ms, elapsed time = 4 ms.

            (1 row affected)
            Table 'Worktable'. Scan count 5, logical reads 78656, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

             SQL Server Execution Times:
               CPU time = 4406 ms,  elapsed time = 6010 ms.

            Completion time: 2026-11-10T22:04:09.5048787+00:00

            """;

        [TestMethod]
        public void Parse_Example_SplitsIntoStatements()
        {
            var report = StatisticsMessages.Parse(Example);

            Assert.AreEqual(2, report.Statements.Count);
            var set = report.Statements[0];
            Assert.AreEqual(0L, set.CpuMs);
            Assert.IsNull(set.CompileCpuMs);
            Assert.AreEqual(0, set.Tables.Count);

            var query = report.Statements[1];
            Assert.AreEqual(2, query.Number);
            Assert.AreEqual(0L, query.CompileCpuMs);
            Assert.AreEqual(4L, query.CompileElapsedMs);
            Assert.AreEqual(1L, query.RowsAffected);
            Assert.AreEqual(4406L, query.CpuMs);
            Assert.AreEqual(6010L, query.ElapsedMs);
            Assert.AreEqual("Worktable", query.Tables.Single().Table);
            Assert.AreEqual(5L, query.Tables[0].Counter(StatisticsMessages.ScanCount));
            Assert.AreEqual(78656L, query.Tables[0].Counter(StatisticsMessages.LogicalReads));

            Assert.AreEqual(4406L, report.CpuMs);
            Assert.AreEqual(6010L, report.ElapsedMs);
            Assert.AreEqual(4L, report.CompileElapsedMs);
            Assert.AreEqual("2026-11-10T22:04:09.5048787+00:00", report.CompletionTime);
            Assert.IsTrue(report.HasIO);
            Assert.IsTrue(report.HasTime);
        }

        [TestMethod]
        public void TryParseTableLine_ReadsCountersByName()
        {
            Assert.IsTrue(StatisticsMessages.TryParseTableLine(
                "Table 'Orders'. Scan count 1, logical reads 3, physical reads 2, read-ahead reads 7, lob logical reads 4, lob physical reads 0, lob read-ahead reads 0.",
                out var io));
            Assert.AreEqual("Orders", io.Table);
            Assert.AreEqual(3L, io.Counter("Logical reads"));
            Assert.AreEqual(7L, io.Counter("Read-ahead reads"));
            Assert.AreEqual(4L, io.Counter("LOB logical reads"));
            Assert.AreEqual(0L, io.Counter("Page server reads"), "a counter this version didn't print is zero");
        }

        [TestMethod]
        public void TryParseTableLine_NameWithQuotesAndDots()
        {
            Assert.IsTrue(StatisticsMessages.TryParseTableLine("Table 'O'Brien'. x'. Scan count 2, logical reads 9.", out var io));
            Assert.AreEqual("O'Brien'. x", io.Table);
            Assert.AreEqual(9L, io.Counter(StatisticsMessages.LogicalReads));
        }

        [TestMethod]
        [DataRow("Table 'X'. was dropped.")]
        [DataRow("Table 'X'. Scan count lots.")]
        [DataRow("Tables 'X'. Scan count 1.")]
        [DataRow("PRINT Table 'X'. Scan count 1.")]
        public void TryParseTableLine_NotATableLine(string line)
        {
            Assert.IsFalse(StatisticsMessages.TryParseTableLine(line, out _));
        }

        [TestMethod]
        [DataRow("logical reads", "Logical reads")]
        [DataRow("lob page server read-ahead reads", "LOB page server read-ahead reads")]
        [DataRow("Scan  count", "Scan count")]
        [DataRow("segment skipped", "Segment skipped")]
        public void CounterName_Normalises(string raw, string expected)
        {
            Assert.AreEqual(expected, StatisticsMessages.CounterName(raw));
        }

        /// <summary>With STATISTICS TIME off there's no execution time to end each statement - the next row count does.</summary>
        [TestMethod]
        public void Parse_IOOnly_RowCountStartsNextStatement()
        {
            var report = StatisticsMessages.Parse("""
                (10 rows affected)
                Table 'A'. Scan count 1, logical reads 5, physical reads 0, read-ahead reads 0.
                Table 'B'. Scan count 1, logical reads 6, physical reads 0, read-ahead reads 0.

                (3 rows affected)
                Table 'A'. Scan count 2, logical reads 7, physical reads 1, read-ahead reads 0.
                """);

            Assert.AreEqual(2, report.Statements.Count);
            Assert.AreEqual(10L, report.Statements[0].RowsAffected);
            Assert.AreEqual(2, report.Statements[0].Tables.Count);
            Assert.AreEqual(3L, report.Statements[1].RowsAffected);
            Assert.AreEqual(18L, report.Counter(StatisticsMessages.LogicalReads));
            Assert.IsFalse(report.HasTime);
        }

        /// <summary>SET NOCOUNT ON and STATISTICS TIME off: a table read again is the next statement.</summary>
        [TestMethod]
        public void Parse_SameTableAgain_StartsNextStatement()
        {
            var report = StatisticsMessages.Parse("""
                Table 'A'. Scan count 1, logical reads 5.
                Table 'A'. Scan count 1, logical reads 8.
                """);

            Assert.AreEqual(2, report.Statements.Count);
            Assert.AreEqual(8L, report.Statements[1].Counter(StatisticsMessages.LogicalReads));
        }

        /// <summary>Columnstore's segment line follows the table's own line, and belongs with it.</summary>
        [TestMethod]
        public void Parse_SegmentLine_MergesIntoTable()
        {
            var report = StatisticsMessages.Parse("""
                Table 'FactSales'. Scan count 4, logical reads 0, physical reads 0, read-ahead reads 0, lob logical reads 1200.
                Table 'FactSales'. Segment reads 10, segment skipped 2.
                """);

            var io = report.Statements.Single().Tables.Single();
            Assert.AreEqual(1200L, io.Counter("LOB logical reads"));
            Assert.AreEqual(10L, io.Counter("Segment reads"));
            Assert.AreEqual(2L, io.Counter("Segment skipped"));
        }

        [TestMethod]
        public void Parse_OldRowCountFormat_AndErrors()
        {
            var report = StatisticsMessages.Parse("""
                (1 row(s) affected)
                Msg 208, Level 16, State 1, Line 3
                Invalid object name 'nope'.
                """);

            Assert.AreEqual(1L, report.Statements.Single().RowsAffected);
            Assert.AreEqual(1, report.ErrorCount);
            Assert.IsFalse(report.HasStatistics, "row counts alone aren't statistics");
        }

        [TestMethod]
        public void Parse_NothingRecognised_IsEmpty()
        {
            var report = StatisticsMessages.Parse("hello\nCommands completed successfully.\n");
            Assert.AreEqual(0, report.Statements.Count);
            Assert.IsFalse(report.HasStatistics);
            Assert.AreEqual(0, StatisticsMessages.Parse((string)null).Statements.Count);
        }

        /// <summary>A procedure: each statement's time, then the procedure's own time as a statement of its own.</summary>
        [TestMethod]
        public void Parse_Procedure_OuterTimeIsItsOwnStatement()
        {
            var report = StatisticsMessages.Parse("""
                SQL Server parse and compile time:
                   CPU time = 2 ms, elapsed time = 3 ms.
                Table 'A'. Scan count 1, logical reads 10.

                 SQL Server Execution Times:
                   CPU time = 15 ms,  elapsed time = 20 ms.
                Table 'B'. Scan count 1, logical reads 30.

                 SQL Server Execution Times:
                   CPU time = 16 ms,  elapsed time = 25 ms.

                 SQL Server Execution Times:
                   CPU time = 31 ms,  elapsed time = 45 ms.
                """);

            Assert.AreEqual(3, report.Statements.Count);
            Assert.AreEqual(2L, report.Statements[0].CompileCpuMs);
            Assert.AreEqual(10L, report.Statements[0].Counter(StatisticsMessages.LogicalReads));
            Assert.AreEqual(30L, report.Statements[1].Counter(StatisticsMessages.LogicalReads));
            Assert.AreEqual(31L, report.Statements[2].CpuMs);
            Assert.AreEqual(0, report.Statements[2].Tables.Count);
        }

        [TestMethod]
        public void IOByTable_TotalsAcrossStatements_MostReadsFirst()
        {
            var report = StatisticsMessages.Parse("""
                (1 row affected)
                Table 'Small'. Scan count 1, logical reads 10, physical reads 0, page server reads 0, read-ahead reads 0, lob logical reads 0.
                Table 'Big'. Scan count 1, logical reads 60, physical reads 2, page server reads 0, read-ahead reads 4, lob logical reads 0.
                (1 row affected)
                Table 'Small'. Scan count 3, logical reads 30, physical reads 0, page server reads 0, read-ahead reads 0, lob logical reads 0.
                """);

            var table = StatisticsMessages.IOByTable(report);
            var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "Table", "Statements", "Scan count", "Logical reads", "Physical reads", "Read-ahead reads", "% Logical reads"
            }, columns, "all-zero counters beyond the core four are left out");

            Assert.AreEqual(2, table.Rows.Count);
            Assert.AreEqual("Big", table.Rows[0]["Table"]);
            Assert.AreEqual(60L, table.Rows[0]["Logical reads"]);
            Assert.AreEqual(60.0m, table.Rows[0]["% Logical reads"]);
            Assert.AreEqual("Small", table.Rows[1]["Table"]);
            Assert.AreEqual(2L, table.Rows[1]["Statements"]);
            Assert.AreEqual(4L, table.Rows[1]["Scan count"]);
            Assert.AreEqual(40L, table.Rows[1]["Logical reads"]);
            Assert.AreEqual("N0", table.Columns["Logical reads"]!.ExtendedProperties["Format"]);
        }

        [TestMethod]
        public void IOByStatement_And_Statements()
        {
            var report = StatisticsMessages.Parse(Example);

            var detail = StatisticsMessages.IOByStatement(report);
            Assert.AreEqual(1, detail.Rows.Count);
            Assert.AreEqual(2L, detail.Rows[0]["Statement"]);
            Assert.AreEqual("Worktable", detail.Rows[0]["Table"]);

            var statements = StatisticsMessages.Statements(report);
            Assert.AreEqual(2, statements.Rows.Count);
            Assert.AreEqual(DBNull.Value, statements.Rows[0]["Logical reads"], "no I/O for SET STATISTICS itself");
            Assert.AreEqual(4406L, statements.Rows[1]["CPU ms"]);
            Assert.AreEqual(78656L, statements.Rows[1]["Logical reads"]);
            Assert.AreEqual("Worktable", statements.Rows[1]["Tables"]);
        }

        [TestMethod]
        public void Summary_ShowsTotals()
        {
            var (headline, details, note) = StatisticsViewerControl.Summary(StatisticsMessages.Parse(Example));
            Assert.AreEqual("CPU 4,406 ms   ·   Elapsed 6,010 ms   ·   Logical reads 78,656   ·   Physical reads 0", headline);
            StringAssert.Contains(note, "add up everything printed");
            StringAssert.Contains(details, "Compile CPU 0 ms, elapsed 4 ms");
            StringAssert.Contains(details, "1 table");
            StringAssert.Contains(details, "2 statements");
            StringAssert.Contains(details, "Completed 2026-11-10 22:04:09 +00:00", "to the second, and kept on one line");
        }

        [TestMethod]
        public void Summary_IOOnly_LeavesOutTime_AndCountsErrors()
        {
            var (headline, details, note) = StatisticsViewerControl.Summary(StatisticsMessages.Parse("""
                Table 'A'. Scan count 1, logical reads 5.
                Msg 208, Level 16, State 1, Line 3
                """));
            Assert.AreEqual("Logical reads 5   ·   Physical reads 0", headline);
            StringAssert.Contains(details, "1 error");
            Assert.IsFalse(details.Contains("Compile"));
            StringAssert.Contains(note, "none of it is counted twice");
        }

        [STATestMethod]
        public void StatisticsViewer_ShowsSummaryCard()
        {
            using var viewer = new StatisticsViewerControl(StatisticsMessages.Parse(Example), "t") { Size = new System.Drawing.Size(900, 600) };
            var card = Descendants(viewer).OfType<DBADashGUI.Controls.InsightCard>().Single();
            Assert.AreEqual(DBADashGUI.Controls.InsightCard.CardIcon.Information, card.Icon);
            StringAssert.Contains(card.CardText, "Logical reads 78,656");
            StringAssert.Contains(card.CardText, "worked out from the order of the messages");
            StringAssert.Contains(card.CardText, "ticks of about 15 ms");
            Assert.IsTrue(card.Parent!.Height > card.Padding.Vertical + 20, "tall enough for its text");
        }

        private static IEnumerable<Control> Descendants(Control control) =>
            control.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));

        [TestMethod]
        public void TakeMessages_RemovesTheMessagesTable()
        {
            using var dataSet = new DataSet();
            var results = dataSet.Tables.Add("Result 1");
            results.Columns.Add("a", typeof(int));
            var messages = dataSet.Tables.Add("Messages");
            messages.ExtendedProperties[QueryResultsViewerControl.KindProperty] = QueryResultsViewerControl.MessagesKind;
            messages.Columns.Add("Message", typeof(string));
            messages.Rows.Add("(1 row affected)");
            messages.Rows.Add("Table 'T'. Scan count 1, logical reads 2.");

            messages.ExtendedProperties[QueryResultsViewerControl.QueryTextProperty] = "SET STATISTICS IO ON;\r\nEXEC dbo.p;";

            var taken = QueryResultsViewerControl.TakeMessages(dataSet);

            Assert.AreEqual("(1 row affected)\nTable 'T'. Scan count 1, logical reads 2.\n", taken.Text);
            Assert.IsNull(taken.Error);
            Assert.AreEqual("SET STATISTICS IO ON;\r\nEXEC dbo.p;", taken.QueryText);
            Assert.AreEqual(1, dataSet.Tables.Count);
            Assert.AreSame(results, dataSet.Tables[0]);
            Assert.IsNull(QueryResultsViewerControl.TakeMessages(dataSet), "only a table marked as messages is taken");
            Assert.AreEqual(1, dataSet.Tables.Count);
        }

        /// <summary>Where the extension couldn't read the Messages tab, it says why, and the app passes that on.</summary>
        [STATestMethod]
        public void TakeMessages_Error_IsPassedOn()
        {
            using var dataSet = new DataSet();
            dataSet.Tables.Add("Result 1").Columns.Add("a", typeof(int));
            var messages = dataSet.Tables.Add("Messages");
            messages.ExtendedProperties[QueryResultsViewerControl.KindProperty] = QueryResultsViewerControl.MessagesKind;
            messages.ExtendedProperties[QueryResultsViewerControl.ErrorProperty] = "The Messages tab wasn't found.";
            messages.Columns.Add("Message", typeof(string));

            var taken = QueryResultsViewerControl.TakeMessages(dataSet);
            Assert.AreEqual(string.Empty, taken.Text);
            Assert.AreEqual("The Messages tab wasn't found.", taken.Error);
            Assert.AreEqual(1, dataSet.Tables.Count);

            using var viewer = new QueryResultsViewerControl(new Label(), "t", "t", taken);
            CollectionAssert.AreEqual(new[] { "Results", "Messages" }, viewer.TabNames);
            Assert.AreEqual(taken.Error, viewer.MessagesError);
        }

        // --- A single statement: its own time is the total, not added to what it ran ---

        /// <summary>
        /// EXEC one procedure, PRINT 'DBADash-Compare', EXEC another (captured on LocalDB): two runs, each a single call
        /// whose own time is its total - and the PRINT's own execution time, straight after the marker, in neither.
        /// </summary>
        [TestMethod]
        public void SplitRuns_TwoCalls_EachHasItsOwnTotal()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_calls.txt"), Fixture("compare_calls.sql"));

            Assert.IsNotNull(runs);
            Assert.AreEqual(2, runs.Count);
            Assert.AreEqual("EXEC dbo.StatsInner;", runs[0].Label);
            Assert.AreEqual("EXEC dbo.StatsOuter;", runs[1].Label);
            Assert.AreEqual(62L, runs[0].Report.CpuMs);
            Assert.AreEqual(77L, runs[0].Report.ElapsedMs);
            Assert.AreEqual(78L, runs[1].Report.CpuMs);
            Assert.AreEqual(102L, runs[1].Report.ElapsedMs);
            Assert.IsTrue(runs.All(r => r.Report.SingleIsCall));
            Assert.AreEqual(16L, runs[1].Report.CompileCpuMsInside, "the second call's compile is in its time");
            Assert.IsFalse(runs[1].Report.Statements.Any(s => s.Number == 1 && s.CompileCpuMs == null && s.CpuMs == 0),
                "the marker's own PRINT time isn't the second run's first statement");
        }

        [TestMethod]
        public void SplitRuns_LabelsFromTheMarker_AndNothingBeforeTheFirst()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_labels.txt"), Fixture("compare_labels.sql"));

            Assert.IsNotNull(runs);
            CollectionAssert.AreEqual(new[] { "Before", "After" }, runs.Select(r => r.Label).ToArray());
            Assert.AreEqual(43L, runs[0].Report.Counter(StatisticsMessages.LogicalReads));
            Assert.AreEqual(60L, runs[1].Report.Counter(StatisticsMessages.LogicalReads));
        }

        [TestMethod]
        public void SplitRuns_WithoutQueryText_StillSplits()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_calls.txt"), (string)null);

            Assert.IsNotNull(runs);
            CollectionAssert.AreEqual(new[] { "Run 1", "Run 2" }, runs.Select(r => r.Label).ToArray());
            Assert.IsNull(runs[0].Report.Total, "without the text, nothing says it's a single call");
        }

        [TestMethod]
        public void SplitRuns_NoMarker_IsNull()
        {
            Assert.IsNull(StatisticsMessages.SplitRuns(Fixture("two_calls.txt"), Fixture("two_calls.sql")));
            Assert.IsNull(StatisticsMessages.SplitQuery(Fixture("two_calls.sql")));
        }

        /// <summary>A marker that's not a statement of its own - inside an IF - doesn't split the text, so it isn't trusted.</summary>
        [TestMethod]
        public void SplitRuns_MarkerTheTextDoesNotSplitAt_IgnoresTheText()
        {
            const string query = "EXEC dbo.StatsInner;\nIF 1 = 1 PRINT 'DBADash-Compare';\nEXEC dbo.StatsOuter;";
            Assert.IsNull(StatisticsMessages.SplitQuery(query));

            var runs = StatisticsMessages.SplitRuns(Fixture("compare_calls.txt"), query);
            CollectionAssert.AreEqual(new[] { "Run 1", "Run 2" }, runs.Select(r => r.Label).ToArray());
        }

        [TestMethod]
        public void SplitQuery_KeepsEachPartsText()
        {
            var parts = StatisticsMessages.SplitQuery("SET STATISTICS IO ON;\nPRINT 'DBADash-Compare: A'\nEXEC dbo.A;\nprint 'dbadash-compare'\nEXEC dbo.B");

            CollectionAssert.AreEqual(new[] { "SET STATISTICS IO ON;", "EXEC dbo.A;", "EXEC dbo.B" }, parts);
        }

        [TestMethod]
        public void UniqueLabels_NumbersRepeats()
        {
            var report = new StatisticsReport();
            var runs = StatisticsMessages.UniqueLabels([new("EXEC dbo.P", report), new("EXEC dbo.P", report), new("exec dbo.P", report)]);

            CollectionAssert.AreEqual(new[] { "EXEC dbo.P", "EXEC dbo.P (2)", "exec dbo.P (3)" }, runs.Select(r => r.Label).ToArray());
        }

        [TestMethod]
        public void Comparison_Summary_ComparesFirstWithLast()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_calls.txt"), Fixture("compare_calls.sql"));
            var (headline, details, note, warning) = StatisticsComparison.Summary(runs);

            StringAssert.Contains(headline, "CPU 62 → 78 ms (+25.8%)");
            StringAssert.Contains(headline, "Elapsed 77 → 102 ms (+32.5%)");
            StringAssert.Contains(headline, "Logical reads 599 → 1,240 (+107%)");
            Assert.AreEqual("(1) EXEC dbo.StatsInner; → (2) EXEC dbo.StatsOuter;", details);
            StringAssert.Contains(note, "its own time as its total");
            Assert.IsFalse(warning);
        }

        [TestMethod]
        public void Comparison_Tables_ShowEachRunAndTheChange()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_labels.txt"), Fixture("compare_labels.sql"));

            var totals = StatisticsComparison.Totals(runs);
            var logical = totals.Rows.Cast<DataRow>().Single(r => (string)r["Measure"] == StatisticsMessages.LogicalReads);
            Assert.AreEqual(43L, logical["(1) Before"]);
            Assert.AreEqual(60L, logical["(2) After"]);
            Assert.AreEqual(17L, logical["Change"]);
            Assert.AreEqual(17m * 100 / 43, logical["Change %"]);

            var io = StatisticsComparison.TableIO(runs);
            var colpars = io.Rows.Cast<DataRow>().Single(r => (string)r["Table"] == "syscolpars");
            Assert.AreEqual(0L, colpars["Logical reads (1)"]);
            Assert.AreEqual(14L, colpars["Logical reads (2)"]);
            Assert.AreEqual(14L, colpars["Logical reads change"]);
            Assert.AreEqual(DBNull.Value, colpars["Logical reads change %"], "no change % from nothing");
            Assert.AreEqual("sysschobjs", io.Rows[0]["Table"], "busiest first");
        }

        /// <summary>Three runs: a change column for each later run against the first, numbered.</summary>
        [TestMethod]
        public void Comparison_ThreeRuns_NumbersTheChanges()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_labels.txt"), Fixture("compare_labels.sql"));
            var three = StatisticsMessages.UniqueLabels([.. runs, runs[1]]);

            var totals = StatisticsComparison.Totals(three);
            CollectionAssert.IsSubsetOf(new[] { "Change (2)", "Change % (2)", "Change (3)", "Change % (3)" },
                totals.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
            StringAssert.Contains(StatisticsComparison.Summary(three).Headline, "Logical reads 43 → 60 → 60");
        }

        private static DBADashGUI.CustomReports.DataBarSettings Bar(DataTable table, string column) =>
            table.Columns[column].ExtendedProperties[GridViewerControl.DataBarProperty] as DBADashGUI.CustomReports.DataBarSettings;

        /// <summary>Time and I/O start with bars; numbers that only identify something - a statement's number - don't.</summary>
        [TestMethod]
        public void StatisticsTables_HaveDataBars()
        {
            var report = StatisticsMessages.Parse(Fixture("sp_WhoIsActive.txt"), Fixture("sp_WhoIsActive.sql"));

            var byTable = StatisticsMessages.IOByTable(report);
            Assert.IsNotNull(Bar(byTable, StatisticsMessages.LogicalReads));
            Assert.AreEqual(100m, Bar(byTable, "% Logical reads").Maximum, "a share of the whole");
            Assert.IsNull(Bar(byTable, "Statements"));

            var statements = StatisticsMessages.Statements(report);
            Assert.AreEqual(DBADashGUI.CustomReports.DataBarColorMode.TrafficLight, Bar(statements, "CPU ms").ColorMode);
            Assert.AreEqual(DBADashGUI.CustomReports.DataBarColorMode.Solid, Bar(statements, "Rows affected").ColorMode);
            Assert.IsNull(Bar(statements, "Statement"));

            Assert.IsNotNull(Bar(StatisticsMessages.IOByStatement(report), StatisticsMessages.LogicalReads));
        }

        /// <summary>
        /// A comparison's runs share a counter's scale, so the same figure is the same bar in each; changes are red or
        /// green; and the totals, a different unit a row, only bar the change %.  None of it is remembered.
        /// </summary>
        [TestMethod]
        public void ComparisonTables_HaveDataBars()
        {
            var runs = StatisticsMessages.SplitRuns(Fixture("compare_labels.txt"), Fixture("compare_labels.sql"));

            var io = StatisticsComparison.TableIO(runs);
            Assert.AreEqual(43m, Bar(io, "Logical reads (1)").Maximum);
            Assert.AreEqual(43m, Bar(io, "Logical reads (2)").Maximum, "the same scale as run 1");
            Assert.AreEqual(DBADashGUI.CustomReports.DataBarColorMode.PositiveNegative, Bar(io, "Logical reads change").ColorMode);
            Assert.AreEqual(DBADashGUI.CustomReports.DataBarColorMode.PositiveNegative, Bar(io, "Logical reads change %").ColorMode);
            Assert.AreEqual(false, io.ExtendedProperties[GridViewerControl.RememberDataBarsProperty]);

            var totals = StatisticsComparison.Totals(runs);
            Assert.IsNull(Bar(totals, "(1) Before"));
            Assert.IsNull(Bar(totals, "Change"));
            Assert.AreEqual(DBADashGUI.CustomReports.DataBarColorMode.PositiveNegative, Bar(totals, "Change %").ColorMode);
        }

        /// <summary>
        /// A plain SELECT prints no times but its own, so messages with other statements doing something aren't its - the
        /// text was changed, or another part selected, since it ran - and are added up, with a warning, not taken as its.
        /// </summary>
        [TestMethod]
        public void Parse_SelectTextWithOtherStatementsMessages_DoesNotFit()
        {
            var report = StatisticsMessages.Parse(Fixture("two_calls.txt"), "SELECT COUNT(*) FROM sys.objects;");

            Assert.IsNull(report.Total);
            Assert.IsTrue(report.QueryDoesNotFit);
            Assert.AreEqual(report.SumOfStatementsCpuMs, report.CpuMs, "added up");
            StringAssert.Contains(StatisticsViewerControl.Summary(report).Note, "the messages don't fit it");
        }

        [TestMethod]
        public void Parse_SelectThatFits_IsItsOwnTotal()
        {
            var report = StatisticsMessages.Parse(Example, "SET STATISTICS IO, TIME ON;\nSELECT COUNT(*) FROM dbo.T;");
            Assert.IsFalse(report.QueryDoesNotFit);
        }

        /// <summary>Text read from the editor when the messages were opened, not as the query ran, is said to be so.</summary>
        [TestMethod]
        public void Parse_TextReadWhenOpened_SaysItMayHaveChanged()
        {
            var query = StatisticsMessages.ParseQuery(Fixture("sp_WhoIsActive.sql"), mayHaveChanged: true);
            var report = StatisticsMessages.Parse(Fixture("sp_WhoIsActive.txt"), query);

            Assert.IsTrue(report.QueryMayHaveChanged);
            Assert.AreEqual(188L, report.CpuMs, "still used");
            StringAssert.Contains(StatisticsViewerControl.Summary(report).Note, "read from the editor when this was opened");

            var executed = StatisticsMessages.Parse(Fixture("sp_WhoIsActive.txt"), StatisticsMessages.ParseQuery(Fixture("sp_WhoIsActive.sql")));
            Assert.IsFalse(executed.QueryMayHaveChanged);
            Assert.IsFalse(StatisticsViewerControl.Summary(executed).Note.Contains("read from the editor"));
        }

        [TestMethod]
        public void TakeMessages_ReadsWhereTheQueryTextCameFrom()
        {
            foreach (var (source, executed) in new[] { ("Executed", true), ("Editor", false), (null, false) })
            {
                var dataSet = new DataSet();
                var table = dataSet.Tables.Add("Messages");
                table.ExtendedProperties[QueryResultsViewerControl.KindProperty] = QueryResultsViewerControl.MessagesKind;
                table.ExtendedProperties[QueryResultsViewerControl.QueryTextProperty] = "EXEC dbo.P";
                if (source != null) table.ExtendedProperties[QueryResultsViewerControl.QueryTextSourceProperty] = source;
                table.Columns.Add("Message", typeof(string));

                Assert.AreEqual(executed, QueryResultsViewerControl.TakeMessages(dataSet).QueryTextExecuted, source ?? "an older extension");
            }
        }

        /// <summary>
        /// A run with STATISTICS TIME off has no times to compare: blank in Totals with no change, left off the card's
        /// headline, and said so - not 0 ms and -100%.
        /// </summary>
        [TestMethod]
        public void Comparison_RunWithoutTime_IsBlankNotZero()
        {
            const string messages = """
                Table 'T'. Scan count 1, logical reads 100, physical reads 0, read-ahead reads 0.

                 SQL Server Execution Times:
                   CPU time = 47 ms,  elapsed time = 50 ms.
                DBADash-Compare: No time
                Table 'T'. Scan count 1, logical reads 80, physical reads 0, read-ahead reads 0.
                """;
            var runs = StatisticsMessages.SplitRuns(messages, (string)null);
            Assert.AreEqual(2, runs.Count);

            var cpu = StatisticsComparison.Totals(runs).Rows.Cast<DataRow>().Single(r => (string)r["Measure"] == "CPU ms");
            Assert.AreEqual(47L, cpu[1]);
            Assert.AreEqual(DBNull.Value, cpu[2], "blank, not 0");
            Assert.AreEqual(DBNull.Value, cpu["Change"]);
            Assert.AreEqual(DBNull.Value, cpu["Change %"]);

            var (headline, _, note, _) = StatisticsComparison.Summary(runs);
            Assert.IsFalse(headline.Contains("CPU"), headline);
            StringAssert.Contains(headline, "Logical reads 100 → 80 (-20%)");
            StringAssert.Contains(note, "No time had STATISTICS TIME off");
        }

        /// <summary>A run with STATISTICS IO off has blank I/O in the table grid, not 0 reads of every table.</summary>
        [TestMethod]
        public void Comparison_RunWithoutIO_IsBlankNotZero()
        {
            const string messages = """
                Table 'T'. Scan count 1, logical reads 100, physical reads 0, read-ahead reads 0.

                 SQL Server Execution Times:
                   CPU time = 47 ms,  elapsed time = 50 ms.
                DBADash-Compare

                 SQL Server Execution Times:
                   CPU time = 15 ms,  elapsed time = 20 ms.

                 SQL Server Execution Times:
                   CPU time = 31 ms,  elapsed time = 30 ms.
                """;
            var runs = StatisticsMessages.SplitRuns(messages, (string)null);

            var t = StatisticsComparison.TableIO(runs).Rows.Cast<DataRow>().Single();
            Assert.AreEqual(100L, t["Logical reads (1)"]);
            Assert.AreEqual(DBNull.Value, t["Logical reads (2)"]);
            Assert.AreEqual(DBNull.Value, t["Logical reads change"]);
            Assert.IsFalse(StatisticsComparison.Summary(runs).Headline.Contains("Logical reads"));
        }

        [TestMethod]
        public void CompareControl_Caption_IsShort()
        {
            var report = new StatisticsReport();
            Assert.AreEqual("A vs B", new StatisticsCompareControl([new("A", report), new("B", report)], "t").Caption);
            Assert.AreEqual("A vs C (3 runs)", new StatisticsCompareControl([new("A", report), new("B", report), new("C", report)], "t").Caption);
        }

        /// <summary>
        /// Plain text - the Messages tab - follows the dark theme as SQL does; only highlighting made for a light
        /// background keeps the light colours.
        /// </summary>
        [TestMethod]
        public void CodeEditor_PlainTextFollowsTheDarkTheme()
        {
            Assert.IsFalse(DBADashGUI.SchemaCompare.CodeEditor.UsesLightHighlighting(DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.None));
            Assert.IsFalse(DBADashGUI.SchemaCompare.CodeEditor.UsesLightHighlighting(DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.SQL));
            Assert.IsTrue(DBADashGUI.SchemaCompare.CodeEditor.UsesLightHighlighting(DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.XML));
        }

        /// <summary>
        /// With STATISTICS TIME on, a statement reading the same table twice (its plan reading it in two places) is still
        /// one statement - its execution time ends it - so a plain SELECT doing so still fits its text.
        /// </summary>
        [TestMethod]
        public void Parse_TimedStatementReadingATableTwice_IsOneStatement()
        {
            const string messages = """
                (1 rows affected)
                Table 'syscolpars'. Scan count 1, logical reads 562, physical reads 0, read-ahead reads 0.
                Table 'sysobjvalues'. Scan count 2, logical reads 6, physical reads 0, read-ahead reads 0.
                Table 'syscolpars'. Scan count 2, logical reads 30, physical reads 0, read-ahead reads 0.

                 SQL Server Execution Times:
                   CPU time = 31 ms,  elapsed time = 23 ms.
                """;
            var report = StatisticsMessages.Parse(messages, "SELECT COUNT_BIG(*) FROM sys.all_columns a CROSS JOIN sys.all_columns b;");

            Assert.AreEqual(1, report.Statements.Count);
            Assert.AreEqual(3, report.Statements[0].Tables.Count, "each line kept, for I/O by statement");
            Assert.AreEqual(1L, report.Statements[0].RowsAffected);
            Assert.AreEqual(598L, report.Counter(StatisticsMessages.LogicalReads));
            Assert.IsFalse(report.QueryDoesNotFit);
            Assert.AreEqual(31L, report.CpuMs);

            var byTable = StatisticsMessages.IOByTable(report).Rows.Cast<DataRow>().Single(r => (string)r["Table"] == "syscolpars");
            Assert.AreEqual(592L, byTable[StatisticsMessages.LogicalReads]);
        }

        /// <summary>The fixtures that read a table twice in one statement: no statement is split off without a time.</summary>
        [TestMethod]
        [DataRow("sp_executesql.txt")]
        [DataRow("two_calls.txt")]
        [DataRow("insert_with_trigger.txt")]
        public void Parse_Timed_EveryStatementWithIOHasATime(string fixture)
        {
            var report = StatisticsMessages.Parse(Fixture(fixture));
            Assert.IsFalse(report.Statements.Any(s => s.Tables.Count > 0 && s.CpuMs == null && s.CompileCpuMs == null),
                string.Join(", ", report.Statements.Select(s => $"{s.Number}: {s.Tables.Count} tables, cpu {s.CpuMs}")));
        }

        /// <summary>
        /// An INSERT prints its row count after its execution time: it's the INSERT's, not a statement of its own.  A
        /// SELECT's count comes before its I/O and time, and stays the SELECT's.
        /// </summary>
        [TestMethod]
        public void Parse_DataChangeRowCountAfterItsTime_IsItsOwn()
        {
            var report = StatisticsMessages.Parse(Fixture("insert_with_trigger.txt"), Fixture("insert_with_trigger.sql"));

            var insert = report.Statements[^1];
            Assert.AreEqual(31L, insert.CpuMs);
            Assert.AreEqual(1L, insert.RowsAffected);
            Assert.IsTrue(insert.IsTotal);

            var labels = StatisticsMessages.Parse(Fixture("compare_labels.txt"));
            var select = labels.Statements.Single(s => s.Tables.Any(t => t.Table == "sysschobjs") && s.Tables.Count == 1);
            Assert.AreEqual(1L, select.RowsAffected, "the SELECT's own, printed before its I/O");
        }

        private static string Fixture(string name) =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Statistics", name));

        [TestMethod]
        [DataRow("EXEC sp_WhoIsActive", "EXEC sp_WhoIsActive", true)]
        [DataRow("sp_WhoIsActive", "sp_WhoIsActive", true)]
        [DataRow("SET STATISTICS IO, TIME ON;\r\nEXEC dbo.sp_WhoIsActive @get_plans = 1;\r\nSET STATISTICS IO, TIME OFF;", "EXEC dbo.sp_WhoIsActive @get_plans = 1;", true)]
        [DataRow("SET STATISTICS IO ON\r\nGO\r\nSET STATISTICS TIME ON\r\nGO\r\n-- a comment\r\nexec   dbo.p\r\n  @a = 'x; y'\r\nGO", "exec dbo.p @a = 'x; y'", true)]
        [DataRow("USE tempdb; DECLARE @rc int; EXEC @rc = dbo.p; PRINT 'done'", "EXEC @rc = dbo.p;", true)]
        [DataRow("EXEC('SELECT 1; SELECT 2')", "EXEC('SELECT 1; SELECT 2')", true)]
        [DataRow("EXEC sys.sp_executesql N'SELECT @a', N'@a int', @a = 1;", "EXEC sys.sp_executesql N'SELECT @a', N'@a int', @a = 1;", true)]
        [DataRow("INSERT #t EXEC dbo.p", "INSERT #t EXEC dbo.p", true)]
        [DataRow("SELECT COUNT(*) FROM sys.objects", "SELECT COUNT(*) FROM sys.objects", false)]
        [DataRow("SET NOCOUNT ON;\r\nINSERT dbo.T VALUES (1);", "INSERT dbo.T VALUES (1);", false)]
        public void ReadQuery_SingleStatement(string query, string expected, bool isCall)
        {
            var shape = StatisticsMessages.ReadQuery(query);
            Assert.AreEqual(expected, shape.SingleStatement);
            Assert.AreEqual(1, shape.Statements);
            Assert.AreEqual(isCall, shape.SingleIsCall);
        }

        [TestMethod]
        [DataRow("EXEC dbo.p; EXEC dbo.q", 2, 2, 0)]
        [DataRow("SELECT 1; EXEC dbo.p", 2, 1, 0)]
        [DataRow("EXEC dbo.p\r\nGO\r\nSELECT * FROM sys.objects", 2, 1, 0)]
        [DataRow("DECLARE @n int = (SELECT COUNT(*) FROM sys.objects); EXEC dbo.p @n", 2, 1, 0)]
        [DataRow("SET @x = (SELECT 1); EXEC dbo.p", 2, 1, 0)]
        [DataRow("IF 1 = 1 EXEC dbo.p", 1, 1, 0)]
        [DataRow("BEGIN TRY EXEC dbo.p; END TRY BEGIN CATCH THROW; END CATCH", 1, 1, 0)]
        [DataRow("UPDATE dbo.T SET a = 1; DELETE dbo.T WHERE a = 2", 2, 0, 2)]
        [DataRow("SELECT 1; SELECT 2", 2, 0, 0)]
        public void ReadQuery_NotASingleStatement(string query, int statements, int calls, int modifications)
        {
            Assert.AreEqual(new QueryShape(statements, calls, modifications, null), StatisticsMessages.ReadQuery(query));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("EXEC dbo.p (")]
        public void ReadQuery_NothingToRead(string query)
        {
            Assert.IsNull(StatisticsMessages.ReadQuery(query));
        }

        /// <summary>
        /// Real output for each kind of query, from SSMS (sp_WhoIsActive) or sqlcmd against SQL Server 2025 with a cold plan
        /// cache, and the query that printed it.  A call's own time is the total, and includes its compiles - checked
        /// against the output, where the call's CPU is its compiles' and statements' added up (StatsDeferred: 187 = 15 +
        /// 31 + 94 + 31 + 16).  A plain statement's own compile is on top, and a trigger it fires is in its time.
        /// </summary>
        [TestMethod]
        [DataRow("sp_WhoIsActive", true, 188L, 220L, 0L, 142L, 282L)]
        [DataRow("nested_procedures", true, 78L, 114L, 0L, 32L, 171L)]
        [DataRow("bare_call", true, 109L, 106L, 0L, 37L, 234L)]
        [DataRow("dynamic_sql", true, 31L, 47L, 0L, 16L, 46L)]
        [DataRow("sp_executesql", true, 31L, 51L, 0L, 0L, 62L)]
        [DataRow("deferred_compile", true, 187L, 204L, 0L, 109L, 265L)]
        [DataRow("insert_with_trigger", false, 31L, 32L, 0L, 18L, 31L)]
        public void Parse_SingleStatement_OwnTimeIsTheTotal(string fixture, bool isCall, long cpu, long elapsed,
            long compileOnTop, long compileInside, long addedUp)
        {
            var report = StatisticsMessages.Parse(Fixture(fixture + ".txt"), Fixture(fixture + ".sql"));

            Assert.IsNotNull(report.Total, "its own time was found");
            Assert.AreEqual(isCall, report.SingleIsCall);
            Assert.AreEqual(cpu, report.CpuMs);
            Assert.AreEqual(elapsed, report.ElapsedMs);
            Assert.AreEqual(compileOnTop, report.CompileCpuMs);
            Assert.AreEqual(compileInside, report.CompileCpuMsInside);
            Assert.AreEqual(addedUp, report.SumOfStatementsCpuMs);
            Assert.IsFalse(StatisticsViewerControl.LikelyCountsTwice(report));
            Assert.AreEqual(1, StatisticsMessages.Statements(report).Rows.Cast<DataRow>().Count(r => r["Note"] is string),
                "the total's row is marked");
        }

        [TestMethod]
        public void Summary_SingleCall_ExplainsTheTotal()
        {
            var report = StatisticsMessages.Parse(Fixture("sp_WhoIsActive.txt"), Fixture("sp_WhoIsActive.sql"));

            var (headline, details, note) = StatisticsViewerControl.Summary(report);
            StringAssert.StartsWith(headline, "CPU 188 ms   ·   Elapsed 220 ms");
            StringAssert.Contains(details, "Compile CPU 142 ms, elapsed 192 ms (in its time)");
            StringAssert.Contains(note, "single call, EXEC sp_WhoIsActive;");
            StringAssert.Contains(note, "(adding everything up would give 282 ms CPU)");
            Assert.AreEqual(1_458L, report.Counter(StatisticsMessages.LogicalReads), "I/O is never printed twice");
            Assert.AreEqual("Total for the call - includes everything it ran", StatisticsMessages.Statements(report).Rows[^1]["Note"]);
        }

        [TestMethod]
        public void Summary_InsertWithTrigger_OwnCompileOnTop()
        {
            var report = StatisticsMessages.Parse(Fixture("insert_with_trigger.txt"), Fixture("insert_with_trigger.sql"));

            var (_, details, note) = StatisticsViewerControl.Summary(report);
            StringAssert.Contains(details, "Compile CPU 0 ms, elapsed 0 ms (+18 ms CPU as it ran, in its time)");
            StringAssert.Contains(note, "includes the triggers it fired");
        }

        /// <summary>A single plain statement, nothing nested: its figures as they are.</summary>
        [TestMethod]
        public void Summary_SingleSelect()
        {
            var report = StatisticsMessages.Parse(Example, "SET STATISTICS IO, TIME ON;\r\nSELECT * FROM big;");

            Assert.AreEqual(4406L, report.CpuMs, "the SET statement's own 0 ms aside");
            Assert.AreEqual(4L, report.CompileElapsedMs);
            Assert.IsFalse(StatisticsMessages.Statements(report).Columns.Contains("Note"), "only one statement did anything");
            StringAssert.Contains(StatisticsViewerControl.Summary(report).Note, "single statement, SELECT * FROM big;");
        }

        /// <summary>
        /// Two calls: each prints its own total, so adding up counts their statements twice - the card says so, as a warning,
        /// rather than guessing which time is whose.
        /// </summary>
        [TestMethod]
        public void Parse_TwoCalls_ShowsARange_AndWarns()
        {
            var report = StatisticsMessages.Parse(Fixture("two_calls.txt"), Fixture("two_calls.sql"));

            Assert.IsNull(report.Total);
            Assert.IsNull(report.SingleStatement);
            Assert.AreEqual(298L, report.CpuMs, "47 + 94 really");
            Assert.AreEqual(94L, report.LargestCpuMs, "the second call's own time");
            Assert.AreEqual(91L, report.LargestElapsedMs);
            Assert.AreEqual(31L, report.CompileCpuMs);
            Assert.IsTrue(StatisticsViewerControl.LikelyCountsTwice(report));
            var (headline, _, note) = StatisticsViewerControl.Summary(report);
            StringAssert.Contains(headline, "CPU 94 to 298 ms");
            StringAssert.Contains(headline, $"Elapsed 91 to {report.ElapsedMs:N0} ms");
            StringAssert.Contains(note, "counts some time twice");
            StringAssert.Contains(note, "the times are a range");
            StringAssert.Contains(note, "The query makes 2 calls");
        }

        [TestMethod]
        public void Summary_NoCalls_ShowsSingleTimes()
        {
            var (headline, _, _) = StatisticsViewerControl.Summary(
                StatisticsMessages.Parse(Fixture("insert_with_trigger.txt"), Fixture("insert_with_trigger.sql")));
            StringAssert.Contains(headline, "CPU 31 ms");
            Assert.IsFalse(headline.Contains(" to "), headline);
        }

        [TestMethod]
        [DataRow("UPDATE dbo.T SET a = 1; DELETE dbo.T WHERE a = 2", "if triggers fired, they're counted twice")]
        [DataRow("SELECT 1; SELECT 2", "calls nothing that prints times of its own")]
        [DataRow(null, "a query with any of them counts that time twice")]
        public void Summary_SeveralStatements_SaysHowFarToTrustTheTotals(string query, string expected)
        {
            var report = StatisticsMessages.Parse(Example, query);
            Assert.IsNull(report.Total);
            Assert.IsFalse(StatisticsViewerControl.LikelyCountsTwice(report));
            StringAssert.Contains(StatisticsViewerControl.Summary(report).Note, expected);
        }

        /// <summary>Without the query text, every statement is added up, as before.</summary>
        [TestMethod]
        public void Parse_WhoIsActive_NoQueryText_AddsUp()
        {
            var report = StatisticsMessages.Parse(Fixture("sp_WhoIsActive.txt"), (string)null);
            Assert.IsNull(report.Total);
            Assert.AreEqual(282L, report.CpuMs);
            Assert.AreEqual(142L, report.CompileCpuMs);
            Assert.IsFalse(StatisticsMessages.Statements(report).Columns.Contains("Note"));
        }

        /// <summary>
        /// Messages that don't fit a single call - something measurable after the longest time, as when the query's been
        /// edited since it ran - are added up rather than trusted to it.
        /// </summary>
        [TestMethod]
        public void Parse_SingleCall_DoesNotFitMessages_AddsUp()
        {
            var report = StatisticsMessages.Parse("""
                 SQL Server Execution Times:
                   CPU time = 50 ms,  elapsed time = 60 ms.
                Table 'A'. Scan count 1, logical reads 10.

                 SQL Server Execution Times:
                   CPU time = 10 ms,  elapsed time = 12 ms.
                """, "EXEC dbo.p");

            Assert.IsNull(report.Total);
            Assert.AreEqual(60L, report.CpuMs);
        }
    }
}
