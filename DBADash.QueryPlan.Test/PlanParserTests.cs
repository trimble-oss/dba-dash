using System;
using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanParserTests
    {
        [TestMethod]
        public void Parse_ReadsDocumentLevelAttributes()
        {
            var plan = TestPlans.Load(TestPlans.KeyLookupSeek);

            Assert.AreEqual("1.539", plan.Version);
            Assert.AreEqual("16.0.4165.4", plan.Build);
            Assert.AreEqual(1, plan.Statements.Count);
        }

        [TestMethod]
        public void Parse_ReadsStatementAttributes()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);

            Assert.AreEqual(1, statement.StatementId);
            Assert.AreEqual("SELECT", statement.StatementType);
            Assert.AreEqual(0.5, statement.StatementSubTreeCost);
            Assert.AreEqual("FULL", statement.OptimisationLevel);
            Assert.AreEqual("0x1122334455667788", statement.QueryHash);
            Assert.AreEqual(true, statement.RetrievedFromCache);
            Assert.AreEqual(1, statement.DegreeOfParallelism);
            Assert.AreEqual("EstimatedDOPIsOne", statement.NonParallelPlanReason);
            Assert.IsTrue(statement.HasPlan);
            Assert.IsFalse(statement.IsActualPlan, "An estimated plan carries no runtime counters.");
        }

        [TestMethod]
        public void Parse_BuildsTheOperatorTreeWithParentsAndDepths()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);
            var root = statement.RootOperator!;

            Assert.AreEqual(PlanOperatorKind.NestedLoops, root.Kind);
            Assert.IsNull(root.Parent);
            Assert.AreEqual(0, root.Depth);
            Assert.AreEqual(2, root.Children.Count);

            // Showplan order is meaningful for a join: the first input is the outer one.
            Assert.AreEqual(1, root.Children[0].NodeId);
            Assert.AreEqual(2, root.Children[1].NodeId);

            foreach (var child in root.Children)
            {
                Assert.AreSame(root, child.Parent);
                Assert.AreEqual(1, child.Depth);
            }

            Assert.AreEqual(3, statement.Operators.Count());
        }

        [TestMethod]
        public void Parse_ClassifiesALookupSeekAsAKeyLookup()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);

            // The physical operator says Clustered Index Seek; the Lookup attribute is the only
            // thing that makes it the key lookup everyone actually goes hunting for.
            var lookup = TestPlans.Operator(statement, 2);

            Assert.AreEqual("Clustered Index Seek", lookup.PhysicalOp);
            Assert.IsTrue(lookup.IsLookup);
            Assert.AreEqual(PlanOperatorKind.KeyLookup, lookup.Kind);
            Assert.AreEqual("Key Lookup", lookup.DisplayName);

            // ...and the one without the attribute stays an ordinary seek.
            Assert.AreEqual(PlanOperatorKind.NonClusteredIndexSeek, TestPlans.Operator(statement, 1).Kind);
        }

        [TestMethod]
        public void Parse_DerivesOperatorCostFromTheSubtreeCosts()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);

            // Showplan only gives subtree costs, so the root appears to cost everything until its
            // children are subtracted: 0.5 - 0.0032886 - 0.4967114 is what is left for the join.
            var root = statement.RootOperator!;
            Assert.AreEqual(0, root.OperatorCost, 0.000001);

            var lookup = TestPlans.Operator(statement, 2);
            Assert.AreEqual(0.4967114, lookup.OperatorCost, 0.000001);
            Assert.AreEqual(0.9934228, lookup.CostPercent, 0.000001);
            Assert.AreEqual(0.9934228, lookup.SubtreeCostPercent, 0.000001);
        }

        [TestMethod]
        public void Parse_ReadsObjectsWithBracketsStripped()
        {
            var seek = TestPlans.Operator(TestPlans.Statement(TestPlans.KeyLookupSeek), 1);
            var target = seek.PrimaryObject!;

            Assert.AreEqual("Sales", target.Database);
            Assert.AreEqual("dbo", target.Schema);
            Assert.AreEqual("Orders", target.Table);
            Assert.AreEqual("IX_Orders_CustomerID", target.Index);
            Assert.AreEqual("NonClustered", target.IndexKind);
            Assert.AreEqual("o", target.Alias);

            Assert.AreEqual("Orders.IX_Orders_CustomerID AS o", target.ShortName);
            Assert.AreEqual("Sales.dbo.Orders", target.QualifiedTableName);
            Assert.AreEqual("[Sales].[dbo].[Orders].[IX_Orders_CustomerID] AS [o]", target.ToString());
        }

        [TestMethod]
        public void Parse_DescribesSeekAndResidualPredicatesSeparately()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);

            // The difference between the two is the difference between an index doing its job and an
            // index being read end to end, so they must not be merged.
            var seek = TestPlans.Operator(statement, 1);
            Assert.AreEqual("CustomerID = (42)", seek.SeekPredicate);
            Assert.IsNull(seek.Predicate);

            var lookup = TestPlans.Operator(statement, 2);
            Assert.AreEqual("OrderID = [Sales].[dbo].[Orders].[OrderID] as [o].[OrderID]", lookup.SeekPredicate);
            Assert.AreEqual("[Sales].[dbo].[Orders].[Total] as [o].[Total]>(100.)", lookup.Predicate);
        }

        [TestMethod]
        public void Parse_ReadsMissingIndexesAndWritesARunnableCreateStatement()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);

            Assert.AreEqual(1, statement.MissingIndexes.Count);
            var missing = statement.MissingIndexes[0];

            Assert.AreEqual(92.4, missing.Impact, 0.001);
            Assert.AreEqual("Sales.dbo.Orders", missing.QualifiedTableName);
            CollectionAssert.AreEqual(new[] { "CustomerID" }, missing.EqualityColumns.ToArray());
            CollectionAssert.AreEqual(new[] { "OrderDate" }, missing.InequalityColumns.ToArray());
            CollectionAssert.AreEqual(new[] { "Total" }, missing.IncludedColumns.ToArray());

            var create = missing.CreateStatement;

            // Equality columns lead the key, inequality columns follow, includes go in INCLUDE.
            StringAssert.Contains(create, "CREATE NONCLUSTERED INDEX [IX_Orders_CustomerID_OrderDate]");
            StringAssert.Contains(create, "ON [Sales].[dbo].[Orders] ([CustomerID], [OrderDate])");
            StringAssert.Contains(create, "INCLUDE ([Total])");
            StringAssert.EndsWith(create, ";");
        }

        [TestMethod]
        public void Parse_AggregatesRuntimeCountersAcrossThreads()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);
            var sort = TestPlans.Operator(statement, 1);
            var runtime = sort.Runtime!;

            // Rows, CPU and reads are summed; elapsed is the slowest thread, because the threads ran
            // at the same time and summing would report four times the wall clock.
            Assert.AreEqual(1000, runtime.ActualRows);
            Assert.AreEqual(1280, runtime.ActualCpuMs);
            Assert.AreEqual(900, runtime.ActualElapsedMs);
            Assert.AreEqual(500, runtime.ActualLogicalReads);

            // Thread 0 is the coordinator and did no work, so it is not one of the four workers.
            Assert.AreEqual(4, runtime.WorkerThreadCount);
        }

        [TestMethod]
        public void Parse_ReportsThreadSkew()
        {
            var sort = TestPlans.Operator(TestPlans.Statement(TestPlans.ParallelSpill), 1);

            // One thread took 700 of 1000 rows where an even share of four is 250: 700 / 250.
            Assert.AreEqual(2.8, sort.Runtime!.ThreadSkew!.Value, 0.0001);

            // A serial operator has nothing to compare, so there is no skew rather than a skew of 1.
            var serial = TestPlans.Operator(TestPlans.Statement(TestPlans.ParallelSpill), 0);
            Assert.IsNull(serial.Runtime!.ThreadSkew);
        }

        [TestMethod]
        public void Parse_ScalesTheEstimateByLogicalExecutionsNotThreadCount()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);
            var scan = TestPlans.Operator(statement, 3);

            // Four threads, one execution each, so the operator ran once as far as the estimate is
            // concerned.  Multiplying by four instead would report every parallel operator in every
            // plan as having over-estimated by the degree of parallelism.
            Assert.AreEqual(4, scan.Runtime!.ActualExecutions);
            Assert.AreEqual(1, scan.Runtime.LogicalExecutions);
            Assert.AreEqual(100, scan.EstimateRowsAllExecutions!.Value, 0.0001);

            // 10,000 actual against 100 estimated is a hundredfold under-estimate.
            Assert.AreEqual(100, scan.RowEstimateRatio!.Value, 0.0001);
        }

        [TestMethod]
        public void Parse_ReportsRowsReadButDiscarded()
        {
            var scan = TestPlans.Operator(TestPlans.Statement(TestPlans.ParallelSpill), 3);

            // 40,000 read, 10,000 returned: the residual predicate threw away three quarters of the
            // work, which is invisible in the row count the plan draws on the arrow.
            Assert.AreEqual(40000, scan.Runtime!.ActualRowsRead);
            Assert.AreEqual(10000, scan.Runtime.ActualRows);
            Assert.AreEqual(30000, scan.RowsDiscarded);
        }

        [TestMethod]
        public void Parse_ReadsSpillWarningsWithTheirDetail()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);

            var sortWarning = TestPlans.Operator(statement, 1).Warnings.Single();
            Assert.AreEqual(PlanWarningKind.SortSpill, sortWarning.Kind);
            Assert.AreEqual(PlanWarningSeverity.Critical, sortWarning.Severity);
            Assert.IsTrue(sortWarning.IsSpill);
            StringAssert.Contains(sortWarning.Detail, "8 MB");

            var hashWarning = TestPlans.Operator(statement, 2).Warnings.Single();
            Assert.AreEqual(PlanWarningKind.HashSpill, hashWarning.Kind);
            Assert.IsTrue(hashWarning.IsSpill);
        }

        [TestMethod]
        public void Parse_ReadsPlanLevelWarnings()
        {
            var plan = TestPlans.Load(TestPlans.Batch);
            var statement = plan.Statements.Single(s => s.StatementId == 2);

            var warning = statement.Warnings.Single();
            Assert.AreEqual(PlanWarningKind.NoJoinPredicate, warning.Kind);
            Assert.AreEqual(PlanWarningSeverity.Critical, warning.Severity);
        }

        [TestMethod]
        public void Parse_ReadsBatchAndMemoryGrantFigures()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);

            Assert.AreEqual(4, statement.DegreeOfParallelism);
            Assert.AreEqual(9, statement.UsedThreads);
            Assert.AreEqual(8, statement.ReservedThreads);
            Assert.AreEqual(1500, statement.QueryTimeStats!.ElapsedMs);
            Assert.AreEqual(3200, statement.QueryTimeStats.CpuMs);

            var grant = statement.MemoryGrant!;
            Assert.AreEqual(8192, grant.RequestedMemoryKb);
            Assert.AreEqual(1024, grant.MaxUsedMemoryKb);
            Assert.AreEqual(25, grant.GrantWaitTimeMs);

            // An eighth of a large grant used is the number the grant warning is really about.
            Assert.AreEqual(0.125, grant.GrantUsedFraction!.Value, 0.0001);
        }

        [TestMethod]
        public void Parse_PairsCompiledAndRuntimeParameterValues()
        {
            var parameter = TestPlans.Statement(TestPlans.ParallelSpill).Parameters.Single();

            Assert.AreEqual("@CustomerID", parameter.Name);
            Assert.AreEqual("int", parameter.DataType);
            Assert.AreEqual("(1)", parameter.CompiledValue);
            Assert.AreEqual("(9999)", parameter.RuntimeValue);
            Assert.IsTrue(parameter.CompiledValueDiffers, "Compiled for one value and run with another.");
        }

        [TestMethod]
        public void Parse_OrdersWaitStatsByTimeSpent()
        {
            var waits = TestPlans.Statement(TestPlans.ParallelSpill).WaitStats;

            Assert.AreEqual(2, waits.Count);
            Assert.AreEqual("PAGEIOLATCH_SH", waits[0].WaitType);
            Assert.AreEqual(1200, waits[0].WaitTimeMs);
            Assert.AreEqual(4, waits[0].AverageWaitMs, "1,200 ms over 300 waits.");
        }

        [TestMethod]
        public void Parse_PutsTheParametersInTheStatementProperties()
        {
            var properties = TestPlans.Statement(TestPlans.ParallelSpill).Properties;

            // Where SSMS shows them: a list on the statement, each parameter with its values under it.
            var list = properties.Single(p => p.Name == "Parameter List");
            Assert.AreEqual("1", list.Value);

            var parameter = list.Children.Single();
            Assert.AreEqual("@CustomerID", parameter.Name);
            Assert.AreEqual("int", parameter.Children.Single(p => p.Name == "Data Type").Value);
            Assert.AreEqual("(1)", parameter.Children.Single(p => p.Name == "Compiled Value").Value);
            Assert.AreEqual("(9999)", parameter.Children.Single(p => p.Name == "Runtime Value").Value);
        }

        [TestMethod]
        public void Parse_PutsEveryWaitInTheStatementProperties()
        {
            var properties = TestPlans.Statement(TestPlans.ParallelSpill).Properties;

            var waits = properties.Single(p => p.Name == "Wait Stats");
            Assert.AreEqual("1,680 ms", waits.Value, "The total time spent waiting.");
            CollectionAssert.AreEqual(
                new[] { "PAGEIOLATCH_SH", "CXPACKET" },
                waits.Children.Select(w => w.Name).ToArray(),
                "Longest first, like the list they came from.");
            Assert.AreEqual("1,200 ms  (300 waits)", waits.Children[0].Value);
        }

        [TestMethod]
        public void Parse_LeavesOutParametersAndWaitsAPlanDoesNotHave()
        {
            var properties = TestPlans.Statement(TestPlans.KeyLookupSeek).Properties;

            Assert.IsFalse(properties.Any(p => p.Name is "Parameter List" or "Wait Stats"));
        }

        [TestMethod]
        public void Parse_FlattensNestedStatementsAndKeepsTheirNesting()
        {
            var plan = TestPlans.Load(TestPlans.Batch);

            // Three statements at the top level, plus the one inside the conditional's THEN branch.
            Assert.AreEqual(4, plan.Statements.Count);

            var nested = plan.Statements.Single(s => s.StatementId == 4);
            Assert.AreEqual(1, nested.NestingLevel, "A statement inside a branch is one level in.");
            Assert.AreEqual(0, plan.Statements[0].NestingLevel);
        }

        [TestMethod]
        public void PrimaryStatement_IsTheMostExpensiveRatherThanTheFirst()
        {
            var plan = TestPlans.Load(TestPlans.Batch);

            // A batch that starts with a few cheap statements and ends with the slow one is the
            // normal case; opening on the first would send everybody straight to the dropdown.
            Assert.AreEqual(2, plan.PrimaryStatement!.StatementId);
        }

        [TestMethod]
        public void Parse_KeepsTheOptimiserEarlyAbortReason()
        {
            var statement = TestPlans.Load(TestPlans.Batch).Statements.Single(s => s.StatementId == 2);

            Assert.AreEqual("TimeOut", statement.OptimisationEarlyAbortReason);
        }

        [TestMethod]
        public void Parse_BuildsThePropertyTree()
        {
            var lookup = TestPlans.Operator(TestPlans.Statement(TestPlans.KeyLookupSeek), 2);

            Assert.AreEqual("Physical Operation", lookup.Properties[0].Name);
            Assert.AreEqual("Clustered Index Seek", lookup.Properties[0].Value);

            // Rebinds only appear when showplan reported them, rather than as a zero on every node.
            Assert.IsTrue(lookup.Properties.Any(p => p.Name == "Estimated Rebinds"));
            Assert.IsFalse(TestPlans.Operator(TestPlans.Statement(TestPlans.KeyLookupSeek), 1)
                .Properties.Any(p => p.Name == "Estimated Rebinds"));
        }

        [TestMethod]
        public void Parse_ShowsTheDerivedOperatorCostInTheProperties()
        {
            // Operator cost is derived by subtracting the children's subtree costs, which can only
            // happen once the whole tree is parsed - so the property list has to be built after
            // that, or every operator reports a cost of zero.
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);
            var lookup = TestPlans.Operator(statement, 2);

            var cost = lookup.Properties.Single(p => p.Name == "Estimated Operator Cost");
            Assert.AreEqual(PlanFormat.Cost(lookup.OperatorCost), cost.Value);
            Assert.AreNotEqual("0", cost.Value);
        }

        [TestMethod]
        public void EstimateRatio_SaysSoWhenNoRowsCameBack()
        {
            // "0x" reads as a typo; an operator that returned nothing should say so.
            Assert.AreEqual("no rows returned", PlanFormat.EstimateRatio(0));
            Assert.AreEqual("as estimated", PlanFormat.EstimateRatio(1.0000012));
            Assert.AreEqual("as estimated", PlanFormat.EstimateRatio(0.95));
            Assert.AreEqual("1.5x more", PlanFormat.EstimateRatio(1.5));
            Assert.AreEqual("12x more", PlanFormat.EstimateRatio(12));
            Assert.AreEqual("50x fewer", PlanFormat.EstimateRatio(0.02));
        }

        [TestMethod]
        public void ShortDuration_KeepsAboutThreeSignificantFigures()
        {
            Assert.AreEqual("0 ms", PlanFormat.ShortDuration(0));
            Assert.AreEqual("807 ms", PlanFormat.ShortDuration(807));
            Assert.AreEqual("1.00 s", PlanFormat.ShortDuration(1000));
            Assert.AreEqual("1.15 s", PlanFormat.ShortDuration(1154));
            Assert.AreEqual("9.99 s", PlanFormat.ShortDuration(9994));
            Assert.AreEqual("10.0 s", PlanFormat.ShortDuration(9995));
            Assert.AreEqual("59.9 s", PlanFormat.ShortDuration(59_949));
            Assert.AreEqual("1m 00s", PlanFormat.ShortDuration(60_000));
            Assert.AreEqual("3m 46s", PlanFormat.ShortDuration(225_870));
            Assert.AreEqual("1h 00m", PlanFormat.ShortDuration(3_599_600));
            Assert.AreEqual("1h 02m", PlanFormat.ShortDuration(3_720_000));
        }

        [TestMethod]
        public void Bytes_UsesTheLargestUnitThatKeepsTheNumberShort()
        {
            Assert.AreEqual("0 B", PlanFormat.Bytes(0));
            Assert.AreEqual("512 B", PlanFormat.Bytes(512));
            Assert.AreEqual("293 KB", PlanFormat.Bytes(300_000));
            Assert.AreEqual("100 MB", PlanFormat.Bytes(100d * 1024 * 1024));
            Assert.AreEqual("1.5 GB", PlanFormat.Bytes(1.5 * 1024 * 1024 * 1024));
        }

        [TestMethod]
        public void DataSize_IsRowsTimesRowSize()
        {
            var scan = TestPlans.Operator(TestPlans.Statement(TestPlans.ParallelSpill), 3);

            // Measured rows times the estimated row size: showplan never reports an actual size.
            Assert.AreEqual(10_000 * 30, scan.DataSizeForDisplay, 0.0001);
        }

        [TestMethod]
        public void Parse_NestsActualCountersUnderOneHeading()
        {
            var sort = TestPlans.Operator(TestPlans.Statement(TestPlans.ParallelSpill), 1);

            var actual = sort.Properties.Single(p => p.Name == "Actual Execution");
            Assert.IsTrue(actual.HasChildren);
            Assert.IsTrue(actual.Children.Any(c => c.Name == "Thread Skew"));

            // Per thread rows, so an uneven parallel operator can be seen rather than inferred.
            var perThread = actual.Children.Single(c => c.Name == "Rows Per Thread");
            Assert.AreEqual(5, perThread.Children.Count);
        }

        [TestMethod]
        public void Parse_AcceptsAPlanNestedInsideAnotherDocument()
        {
            // Plans reach DBA Dash wrapped in whatever the tool that produced them uses - an
            // extended events envelope, a results element, a query_plan column.
            var wrapped = "<event name=\"query_post_execution_showplan\"><data name=\"showplan_xml\"><value>" +
                          TestPlans.Xml(TestPlans.KeyLookupSeek).Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", string.Empty) +
                          "</value></data></event>";

            var plan = PlanParser.Parse(wrapped);
            Assert.AreEqual(1, plan.Statements.Count);
        }

        [TestMethod]
        public void Parse_AcceptsAPlanWithAByteOrderMark()
        {
            var plan = PlanParser.Parse("﻿" + TestPlans.Xml(TestPlans.KeyLookupSeek));
            Assert.AreEqual(1, plan.Statements.Count);
        }

        [TestMethod]
        public void Parse_RejectsWhatIsNotAPlanWithAMessageWorthShowing()
        {
            var empty = Assert.ThrowsExactly<PlanParseException>(() => PlanParser.Parse("   "));
            StringAssert.Contains(empty.Message, "empty");

            var malformed = Assert.ThrowsExactly<PlanParseException>(() => PlanParser.Parse("<ShowPlanXML"));
            StringAssert.Contains(malformed.Message, "not well formed");

            var wrongDocument = Assert.ThrowsExactly<PlanParseException>(() => PlanParser.Parse("<deadlock />"));
            StringAssert.Contains(wrongDocument.Message, "deadlock");
        }

        [TestMethod]
        public void TryParse_ReportsFailureWithoutThrowing()
        {
            Assert.IsFalse(PlanParser.TryParse("<deadlock />", out var none));
            Assert.IsNull(none);

            Assert.IsTrue(PlanParser.TryParse(TestPlans.Xml(TestPlans.Batch), out var plan));
            Assert.IsNotNull(plan);
        }

        [TestMethod]
        public void TryExtractShowPlanXml_LiftsAPlanOutOfItsEnvelope()
        {
            var bare = TestPlans.Xml(TestPlans.KeyLookupSeek);
            var wrapped = "<event name=\"query_post_execution_showplan\"><data name=\"showplan_xml\"><value>" +
                          bare.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", string.Empty) +
                          "</value></data></event>";

            Assert.IsTrue(PlanParser.TryExtractShowPlanXml(wrapped, out var extracted));
            Assert.IsNotNull(extracted);

            // What comes out is a .sqlplan another application will open: the plan and nothing above
            // it, still carrying the showplan namespace it was declared with.
            StringAssert.StartsWith(extracted, "<ShowPlanXML");
            StringAssert.Contains(extracted, "schemas.microsoft.com/sqlserver/2004/07/showplan");
            Assert.AreEqual(1, PlanParser.Parse(extracted).Statements.Count);
        }

        [TestMethod]
        public void TryExtractShowPlanXml_HandsBackABarePlanUntouched()
        {
            var bare = TestPlans.Xml(TestPlans.Batch);

            Assert.IsTrue(PlanParser.TryExtractShowPlanXml(bare, out var extracted));
            Assert.AreEqual(bare, extracted);
        }

        [TestMethod]
        public void TryExtractShowPlanXml_RefusesWhatIsNotAPlan()
        {
            Assert.IsFalse(PlanParser.TryExtractShowPlanXml("<deadlock />", out var none));
            Assert.IsNull(none);

            Assert.IsFalse(PlanParser.TryExtractShowPlanXml("<ShowPlanXML", out _));
            Assert.IsFalse(PlanParser.TryExtractShowPlanXml("   ", out _));
            Assert.IsFalse(PlanParser.TryExtractShowPlanXml(null, out _));
        }

        [TestMethod]
        public void LooksLikeExecutionPlan_AnswersWithoutParsing()
        {
            Assert.IsTrue(PlanParser.LooksLikeExecutionPlan(TestPlans.Xml(TestPlans.Batch)));
            Assert.IsFalse(PlanParser.LooksLikeExecutionPlan("SELECT 1"));
            Assert.IsFalse(PlanParser.LooksLikeExecutionPlan(null));
        }

        /// <summary>
        /// Every operator in Hugo Kornelis's Execution Plan Reference operator list
        /// (https://sqlserverfast.com/epr/operator-list/) that turns up as a RelOp, so none of them is
        /// drawn as unknown.  Hash Match and Parallelism are classified by their logical operator and
        /// covered elsewhere; the language elements, COND and Result are statement types rather than
        /// operators, and name the statement's root.
        /// </summary>
        [TestMethod]
        public void Classify_KnowsEveryOperatorInTheExecutionPlanReference()
        {
            string[] operators =
            [
                "Adaptive Join", "Assert", "Batch Hash Table Build", "Bitmap",
                "Clustered Index Delete", "Clustered Index Insert", "Clustered Index Merge",
                "Clustered Index Scan", "Clustered Index Seek", "Clustered Index Update", "Collapse",
                "Columnstore Index Delete", "Columnstore Index Insert", "Columnstore Index Merge",
                "Columnstore Index Scan", "Columnstore Index Update", "Compute Scalar", "Concatenation",
                "Constant Scan", "Deleted Scan", "Filter", "Foreign Key References Check",
                "Index Delete", "Index Insert", "Index Scan", "Index Seek", "Index Spool", "Index Update",
                "Inserted Scan", "JSON Index Delete", "JSON Index Insert", "JSON Index Seek",
                "JSON Index Update", "Key Lookup", "Merge Interval", "Merge Join", "Nested Loops",
                "Online Index Insert", "Parameter Table Scan", "Put", "Remote Delete", "Remote Insert",
                "Remote Query", "Remote Scan", "Remote Update", "RID Lookup", "Row Count Spool",
                "Segment", "Sequence", "Sequence Project", "Sort", "Split", "Stream Aggregate", "Switch",
                "Table Delete", "Table Insert", "Table Merge", "Table Scan", "Table Spool", "Table Update",
                "Table Valued Function", "Table-valued function", "Top", "UDX", "Window Aggregate",
                "Window Spool",

                // Cursor types, in the plan as operators under a cursor's statement.
                "Dynamic", "Fast Forward", "Fetch Query", "Keyset", "Population Query", "Snapshot"
            ];

            var unknown = operators
                .Where(op => PlanOperatorClassifier.Classify(op, op) == PlanOperatorKind.Unknown)
                .ToList();

            Assert.AreEqual(0, unknown.Count, "Drawn as unknown: " + string.Join(", ", unknown));
        }

        [TestMethod]
        public void OperatorNames_KeepThePlansNameWhereAKindCoversSeveral()
        {
            // The kind picks the icon; the caption stays what the plan said.
            Assert.AreEqual("JSON Index Seek", PlanOperatorNames.For(PlanOperatorClassifier.Classify("JSON Index Seek", "Index Seek"), "JSON Index Seek"));
            Assert.AreEqual("Batch Hash Table Build", PlanOperatorNames.For(PlanOperatorClassifier.Classify("Batch Hash Table Build", null), "Batch Hash Table Build"));
            Assert.AreEqual("Foreign Key References Check", PlanOperatorNames.For(PlanOperatorClassifier.Classify("Foreign Key References Check", null), "Foreign Key References Check"));
            Assert.AreEqual("Index Seek", PlanOperatorNames.For(PlanOperatorClassifier.Classify("Index Seek", "Index Seek"), "Index Seek"));
        }

        [TestMethod]
        public void Classify_FallsBackRatherThanThrowingOnAnUnknownOperator()
        {
            // New physical operators arrive with every release, and a plan containing one is still
            // worth drawing.
            Assert.AreEqual(
                PlanOperatorKind.Unknown,
                PlanOperatorClassifier.Classify("Quantum Entanglement Join", "Inner Join"));

            Assert.AreEqual(
                PlanOperatorCategory.Other,
                PlanOperatorClassifier.CategoryOf(PlanOperatorKind.Unknown));
        }

        [TestMethod]
        public void Classify_SplitsHashMatchByWhatItIsDoing()
        {
            Assert.AreEqual(
                PlanOperatorKind.HashMatchJoin,
                PlanOperatorClassifier.Classify("Hash Match", "Inner Join"));

            Assert.AreEqual(
                PlanOperatorKind.HashMatchAggregate,
                PlanOperatorClassifier.Classify("Hash Match", "Partial Aggregate"));

            Assert.AreEqual(
                PlanOperatorCategory.Join,
                PlanOperatorClassifier.CategoryOf(PlanOperatorKind.HashMatchJoin));

            Assert.AreEqual(
                PlanOperatorCategory.Transform,
                PlanOperatorClassifier.CategoryOf(PlanOperatorKind.HashMatchAggregate));
        }

        [TestMethod]
        public void Classify_NamesWhichExchangeAParallelismOperatorIs()
        {
            Assert.AreEqual(
                PlanOperatorKind.GatherStreams,
                PlanOperatorClassifier.Classify("Parallelism", "Gather Streams"));

            Assert.AreEqual(
                PlanOperatorKind.RepartitionStreams,
                PlanOperatorClassifier.Classify("Parallelism", "Repartition Streams"));

            Assert.AreEqual(
                PlanOperatorKind.DistributeStreams,
                PlanOperatorClassifier.Classify("Parallelism", "Distribute Streams"));
        }

        [TestMethod]
        public void AllWarnings_PutsTheWorstFirst()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);
            var warnings = statement.AllWarnings.ToList();

            Assert.AreEqual(2, warnings.Count);
            Assert.IsTrue(warnings.All(w => w.Severity == PlanWarningSeverity.Critical));
        }
    }
}
