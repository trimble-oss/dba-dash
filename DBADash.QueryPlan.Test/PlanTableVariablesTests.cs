using System.Linq;
using System.Text.RegularExpressions;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// The cards for table variables and table-valued functions, against plans captured at the
    /// compatibility levels that change how their rows are estimated - see
    /// <see cref="TestPlans.TableVariableDeferred"/> for the schema.
    /// </summary>
    [TestClass]
    public class PlanTableVariablesTests
    {
        private static PlanInsight? Card(PlanStatement statement, string title) =>
            PlanInsights.ForStatement(statement).SingleOrDefault(i => i.Title == title);

        private static PlanInsight? TableVariableCard(PlanStatement statement) => Card(statement, "Table variable");

        private static PlanInsight? FunctionCard(PlanStatement statement) => Card(statement, "Table-valued function");

        private static PlanInsight? BuiltInCard(PlanStatement statement) => Card(statement, "Built-in table-valued functions");

        private static PlanStatement StatementStarting(string plan, string text) =>
            TestPlans.Load(plan).Statements.Single(s => s.StatementText!.TrimStart(';', ' ', '\r', '\n').StartsWith(text));

        private const string TempTableAdvice =
            "\nFor more than a few rows, consider a temp table (#table) instead: it has statistics, and filling one can use a parallel plan - " +
            "SELECT ... INTO can, and so can INSERT ... SELECT into a heap with a TABLOCK hint (compatibility level 130, SQL Server 2016, and above).";

        private const string DeferredCompilation =
            "Table variable deferred compilation (compatibility level 150, SQL Server 2019, and above) compiles a statement with the table's row count " +
            "the first time it runs, and later runs reuse that plan; OPTION (RECOMPILE) compiles it with the current count every time.";

        [TestMethod]
        public void Parser_ReadsTableCardinalityAndInterleavedExecution()
        {
            var deferred = TestPlans.Statement(TestPlans.TableVariableDeferred);
            var scan = TestPlans.Operator(deferred, 4);
            Assert.AreEqual(50000, scan.TableCardinality);
            Assert.IsTrue(scan.PrimaryObject!.IsTableVariable);
            Assert.AreEqual("50,000", scan.Properties.Single(p => p.Name == "Table Cardinality").Value);
            Assert.IsFalse(TestPlans.Operator(deferred, 5).PrimaryObject!.IsTableVariable);

            Assert.IsTrue(TestPlans.Operator(TestPlans.Statement(TestPlans.FunctionInterleaved), 1).Runtime!.IsInterleavedExecuted);
            Assert.IsTrue(TestPlans.Operator(TestPlans.Statement(TestPlans.FunctionInterleavedReused), 1).Runtime!.IsInterleavedExecuted);
            Assert.IsFalse(TestPlans.Operator(TestPlans.Statement(TestPlans.FunctionFixedGuess), 1).Runtime!.IsInterleavedExecuted);
        }

        // ---------------------------------------------------------------- table variables

        [TestMethod]
        public void DeferredCompilation_SaysHowManyRowsTheOptimizerKnewAndThatItGuessedTheMatches()
        {
            var statement = TestPlans.Statement(TestPlans.TableVariableDeferred);
            var card = TableVariableCard(statement);

            Assert.IsNotNull(card);
            Assert.AreEqual(PlanWarningSeverity.Information, card!.Severity);
            Assert.IsTrue(card.IsAnalysis);
            CollectionAssert.AreEqual(new[] { 4 }, card.Operators.Select(o => o.NodeId).ToList());
            StringAssert.StartsWith(card.Text, "Table variable @t has no statistics, so the optimizer has to guess how many rows match a predicate or join on it.");
            StringAssert.Contains(card.Text, "The optimizer knew @t held 50,000 rows when it compiled the plan, but not how many of them match: it estimated 224 rows for node 4, which returned 486.");
            StringAssert.Contains(card.Text, "Unless the statement recompiles, later runs reuse this plan however many rows the table holds then.");
            StringAssert.Contains(card.Text, "Reading a table variable can use a parallel plan, but the statement that fills it always runs on a single thread.");
            StringAssert.EndsWith(card.Text, TempTableAdvice);
            Assert.IsFalse(card.Text.Contains("costed for one row"), card.Text);
            Assert.IsFalse(card.Text.Contains("deferred compilation"), card.Text);
        }

        [TestMethod]
        public void CompiledWhileEmpty_SaysItWasCostedForOneRow_AndWhyThePlanDidntGetTheCount()
        {
            var card = TableVariableCard(TestPlans.Statement(TestPlans.TableVariableCompiledEmpty));

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "The plan was compiled while @t was empty, so it is costed for one row, but node 3 returned 486 rows.");
            StringAssert.Contains(card.Text, DeferredCompilation + " This plan was compiled with neither, or while the table was empty.");
            StringAssert.Contains(card.Text, "A plan costed for one row looks cheap, which favors nested loops joins and a serial plan.");
        }

        [TestMethod]
        public void EstimatedPlan_SaysAnEstimatedPlanAlwaysShowsOneRow_AndACachedOneWasCompiledWithout()
        {
            var card = TableVariableCard(StatementStarting(TestPlans.TableVariableEstimated, "SELECT"));

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "The plan was compiled while @t was empty, so it is costed for one row. " + DeferredCompilation);
            StringAssert.Contains(card.Text, "An estimated plan (SET SHOWPLAN_XML, or Display Estimated Execution Plan) always shows one row, as nothing runs to fill the table; " +
                                             "a plan from the plan cache or Query Store was compiled with neither, or while the table was empty.");
        }

        [TestMethod]
        public void Modifying_SaysTheStatementIsSerial_InPlaceOfTheSerialPlanCard()
        {
            var statement = TestPlans.Statement(TestPlans.TableVariableInsert);
            Assert.AreEqual(PlanTableVariables.NonParallelPlanReason, statement.NonParallelPlanReason);

            var card = TableVariableCard(statement);

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "This statement changes @t, so it runs on a single thread: SQL Server never uses a parallel plan for a statement that modifies a table variable.");
            StringAssert.EndsWith(card.Text, TempTableAdvice);
            Assert.IsFalse(card.Text.Contains("Reading a table variable"), card.Text);
            Assert.IsNull(Card(statement, "Serial plan"), "The reason is said by the table variable card.");
        }

        [TestMethod]
        public void OtherSerialReasons_StillHaveTheirOwnCard()
        {
            Assert.IsNotNull(Card(TestPlans.Statement(TestPlans.TableVariableDeferred), "Serial plan"));
        }

        [TestMethod]
        public void TableVariableOperatorCard_IsOnlyOnTheOperatorsThatTouchATableVariable()
        {
            var statement = TestPlans.Statement(TestPlans.TableVariableDeferred);

            Assert.AreEqual(1, PlanInsights.ForOperator(TestPlans.Operator(statement, 4), statement).Count(i => i.Title == "Table variable"));
            Assert.AreEqual(0, PlanInsights.ForOperator(TestPlans.Operator(statement, 5), statement).Count(i => i.Title == "Table variable"));
        }

        // ---------------------------------------------------------------- user-defined functions

        private const string MultiStatementOpening =
            "Multi-statement table-valued function dbo.MSTVF: SQL Server runs it to fill a table variable with its rows, then reads them from there. " +
            "The work inside it isn't shown in this plan, its rows have no statistics, and filling the table variable runs on a single thread.";

        private const string InlineAdvice =
            "\nIf it can be written as an inline table-valued function (RETURNS TABLE AS RETURN SELECT ...), SQL Server expands it into the calling query, " +
            "where the optimizer estimates it like the rest of the query.";

        [TestMethod]
        public void InterleavedFunction_SaysThePlanWasBuiltForTheRowsItReturned()
        {
            var statement = TestPlans.Statement(TestPlans.FunctionInterleaved);
            var call = PlanTableVariables.FunctionsIn(statement).Single();

            Assert.IsTrue(call.IsMultiStatement);
            Assert.IsTrue(call.IsInterleaved);
            Assert.AreEqual("dbo.MSTVF", call.Name);
            CollectionAssert.AreEqual(new[] { 1, 5 }, call.Operators.Select(o => o.NodeId).ToList());

            var card = FunctionCard(statement);
            Assert.IsNotNull(card);
            Assert.AreEqual(PlanWarningSeverity.Information, card!.Severity);
            CollectionAssert.AreEqual(new[] { 1, 5 }, card.Operators.Select(o => o.NodeId).ToList());
            Assert.AreEqual(
                MultiStatementOpening +
                " Interleaved execution ran it before the rest of the plan was compiled, so the plan was built for the 50,000 rows it returned then." +
                " Later runs reuse the plan, however many rows the function returns." + InlineAdvice,
                card.Text);
            Assert.IsNull(TableVariableCard(statement), "The function's return table is not named as a table variable.");
        }

        [TestMethod]
        public void InterleavedFunction_ReusedFromCache_IsStillFlagged()
        {
            var card = FunctionCard(TestPlans.Statement(TestPlans.FunctionInterleavedReused));

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "so the plan was built for the 50,000 rows it returned then.");
            Assert.IsFalse(card.Text.Contains("This run it returned"), "It returned the same 50,000 rows: " + card.Text);
        }

        [TestMethod]
        public void CachedPlan_WithACountThatIsntAFixedGuess_IsReadAsInterleaved()
        {
            var statement = TestPlans.Statement(TestPlans.FunctionCachedInterleaved);
            Assert.IsFalse(statement.IsActualPlan);
            Assert.IsFalse(PlanTableVariables.FunctionsIn(statement).Single().IsInterleaved, "Nothing in a cached plan flags it.");

            var card = FunctionCard(statement);

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, MultiStatementOpening +
                " The plan was compiled for 40,000 rows, not a fixed guess: interleaved execution ran the function before compiling the rest of the plan, and that is how many it returned." +
                " Later runs reuse the plan, however many rows the function returns.");
        }

        [TestMethod]
        public void FunctionNotInterleaved_SaysTheFixedGuessWhatItReturnedAndWhyItWasntInterleaved()
        {
            var card = FunctionCard(TestPlans.Statement(TestPlans.FunctionFixedGuess));

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text,
                " The optimizer used its fixed guess for a multi-statement function, 100 rows (the legacy cardinality estimator, from before SQL Server 2014, guesses 1), and it returned 50,000 rows.");
            StringAssert.Contains(card.Text,
                "It didn't here: it needs level 140 or above, doesn't apply in a statement that modifies data, and can be switched off with the " +
                "INTERLEAVED_EXECUTION_TVF database scoped configuration or the DISABLE_INTERLEAVED_EXECUTION_TVF hint.");
            StringAssert.EndsWith(card.Text, InlineAdvice);
        }

        [TestMethod]
        public void FunctionOnAnEstimatedPlan_SaysInterleavingNeedsARun_AndItsBodyGetsATableVariableCard()
        {
            var plan = TestPlans.Load(TestPlans.FunctionEstimated);

            var card = FunctionCard(StatementStarting(TestPlans.FunctionEstimated, "SELECT"));
            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "The optimizer used its fixed guess for a multi-statement function, 100 rows");
            StringAssert.Contains(card.Text,
                "It only happens when the query runs, and not in a statement that modifies data, so an estimated plan (SET SHOWPLAN_XML, or Display Estimated Execution Plan) " +
                "always shows the fixed guess. A plan from the plan cache or Query Store that shows it was compiled without interleaved execution - unless the " +
                "function happened to return exactly that many rows when interleaved execution ran it, which such a plan can't tell apart, as only a run's figures flag it.");
            Assert.IsFalse(card.Text.Contains("returned"), card.Text);

            var body = plan.Statements.Single(s => s.StatementText!.Contains("INSERT @r"));
            StringAssert.Contains(TableVariableCard(body)!.Text, "This statement changes @r, so it runs on a single thread");
        }

        [TestMethod]
        public void FunctionCalledPerOuterRow_IsNotCalledMultiStatement_AndCountsItsCalls()
        {
            var statement = TestPlans.Statement(TestPlans.FunctionCrossApply);
            var call = PlanTableVariables.FunctionsIn(statement).Single();

            Assert.IsFalse(call.IsMultiStatement, "Streaming its rows, it could as well be a CLR function.");

            var card = FunctionCard(statement);
            Assert.IsNotNull(card);
            Assert.AreEqual(
                "Table-valued function dbo.MSTVF (a multi-statement or CLR function): the work inside it isn't shown in this plan, and the optimizer can't see how many rows it returns." +
                " The optimizer estimated 100 rows per call, and it returned 3,000 rows over 2 calls." +
                " If it is a multi-statement function, interleaved execution can't improve the estimate: it doesn't apply when the function's arguments come from the outer query, as they do when it runs once per outer row." +
                InlineAdvice,
                card.Text);
        }

        [TestMethod]
        public void FunctionOperatorCard_IsOnTheFunctionAndTheReadOfItsReturnTable()
        {
            var statement = TestPlans.Statement(TestPlans.FunctionInterleaved);

            foreach (var node in new[] { 1, 5 })
            {
                Assert.AreEqual(1, PlanInsights.ForOperator(TestPlans.Operator(statement, node), statement).Count(i => i.Title == "Table-valued function"), "Node " + node);
            }

            Assert.AreEqual(0, PlanInsights.ForOperator(TestPlans.Operator(statement, 6), statement).Count(i => i.Title == "Table-valued function"));
        }

        // ---------------------------------------------------------------- built-in functions

        private const string TempTableForFunctions =
            "\nWhere a wrong estimate leads to a poor join or memory grant, putting the function's rows in a temp table first gives the optimizer statistics on them.";

        [TestMethod]
        public void BuiltInFunction_HasItsOwnCard_NotTheUserDefinedOne()
        {
            var statement = TestPlans.Statement(TestPlans.StringSplit);

            Assert.AreEqual(0, PlanTableVariables.FunctionsIn(statement).Count);
            Assert.IsNull(FunctionCard(statement));

            var card = BuiltInCard(statement);
            Assert.IsNotNull(card);
            Assert.AreEqual(PlanWarningSeverity.Information, card!.Severity);
            CollectionAssert.AreEqual(new[] { 5 }, card.Operators.Select(o => o.NodeId).ToList());
            Assert.AreEqual(
                "Built-in table-valued function STRING_SPLIT: the optimizer has no statistics on the rows it returns, so its estimate is often a fixed guess, " +
                "and the work it does isn't shown in this plan. It was estimated at 50 rows and returned 5 rows." + TempTableForFunctions,
                card.Text);
        }

        [TestMethod]
        public void BuiltInFunctions_AreOneCardForTheStatement_AndOneOnEachOperator()
        {
            var statement = TestPlans.Statement(TestPlans.BuiltInFunctions);

            var card = BuiltInCard(statement);
            Assert.IsNotNull(card);
            CollectionAssert.AreEqual(new[] { 3, 4 }, card!.Operators.Select(o => o.NodeId).ToList());
            StringAssert.StartsWith(card.Text, "Built-in table-valued functions SYSREQUESTS and FNGETSQL: the optimizer has no statistics on the rows they return");

            // FNGETSQL ran once per request, estimated at a row each time, and returned one row in all.
            StringAssert.Contains(card.Text, "The furthest out was node 4: estimated at 77 rows over 77 calls, it returned 1 row.");

            var onOperator = PlanInsights.ForOperator(TestPlans.Operator(statement, 3), statement).Single(i => i.Title == "Built-in table-valued functions");
            StringAssert.StartsWith(onOperator.Text, "Built-in table-valued function SYSREQUESTS:");
            Assert.AreEqual(0, PlanInsights.ForOperator(TestPlans.Operator(statement, 2), statement).Count(i => i.Title == "Built-in table-valued functions"));
        }

        [TestMethod]
        public void UserDefinedFunctions_HaveNoBuiltInCard()
        {
            Assert.IsNull(BuiltInCard(TestPlans.Statement(TestPlans.FunctionFixedGuess)));
            Assert.IsNull(BuiltInCard(TestPlans.Statement(TestPlans.FunctionCrossApply)));
        }

        /// <summary>A captured plan with <paramref name="find"/> replaced, for the cases no capture covers.</summary>
        private static PlanStatement Variant(string plan, string find, string replace)
        {
            var xml = TestPlans.Xml(plan);
            StringAssert.Contains(xml, find);
            return PlanParser.Parse(xml.Replace(find, replace)).Statements.Single();
        }

        [TestMethod]
        public void PermanentTableNamedLikeATableVariable_IsNotOne()
        {
            var statement = Variant(TestPlans.TableVariableInsert, "<Object Table=\"[@t]\"", "<Object Database=\"[TVTest]\" Schema=\"[dbo]\" Table=\"[@t]\"");

            Assert.IsFalse(PlanTableVariables.Touches(TestPlans.Operator(statement, 0)));
            Assert.IsNull(TableVariableCard(statement));
        }

        [TestMethod]
        public void TableVariableReadOnTheInnerSideOfALoop_IsComparedPerExecution()
        {
            // Node 4 read every row of @t, ten times: 500,000 estimated in all, but nothing guessed.
            var xml = TestPlans.Xml(TestPlans.TableVariableDeferred);
            var node = new Regex("<RelOp NodeId=\"4\".*?</RunTimeInformation>", RegexOptions.Singleline);
            Assert.IsTrue(node.IsMatch(xml));
            xml = node.Replace(xml, m => m.Value
                .Replace("EstimateRows=\"223.607\"", "EstimateRows=\"50000\"")
                .Replace("ActualExecutions=\"1\"", "ActualExecutions=\"10\""), 1);
            var statement = PlanParser.Parse(xml).Statements.Single(s => s.Operators.Any(op => op.NodeId == 4 && PlanTableVariables.Touches(op)));

            var card = TableVariableCard(statement);
            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "The optimizer knew @t held 50,000 rows when it compiled the plan.");
        }

        [TestMethod]
        public void ReturnTableScanStoppedEarly_DoesNotGiveTheFunctionsRowCount()
        {
            var statement = Variant(TestPlans.FunctionFixedGuess,
                "ActualRowsRead=\"50000\" ActualEndOfScans=\"1\"", "ActualRowsRead=\"50000\" ActualEndOfScans=\"0\"");

            var card = FunctionCard(statement);
            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "The optimizer used its fixed guess for a multi-statement function, 100 rows (the legacy cardinality estimator, from before SQL Server 2014, guesses 1).");
            Assert.IsFalse(card.Text.Contains("it returned 50,000"), card.Text);
        }

        [TestMethod]
        public void InterleavedFunction_WithRecompile_DoesNotSayLaterRunsReuseThePlan()
        {
            var statement = Variant(TestPlans.FunctionInterleaved, "GROUP BY f.Grp\"", "GROUP BY f.Grp OPTION (RECOMPILE)\"");

            var card = FunctionCard(statement);
            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "so the plan was built for the 50,000 rows it returned then.");
            Assert.IsFalse(card.Text.Contains("Later runs reuse the plan"), card.Text);
        }

        [TestMethod]
        public void CachedPlan_CompiledForOneRow_WithTheNewEstimator_IsReadAsInterleaved()
        {
            // One row is the legacy estimator's guess, but this plan used the level 170 estimator, which guesses 100.
            var statement = Variant(TestPlans.FunctionCachedInterleaved, "\"40000\"", "\"1\"");

            var card = FunctionCard(statement);
            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, " The plan was compiled for 1 row, not a fixed guess");
            Assert.IsFalse(card.Text.Contains("legacy"), card.Text);
        }
    }
}
