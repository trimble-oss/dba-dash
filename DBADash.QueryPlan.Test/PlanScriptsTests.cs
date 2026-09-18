using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>The T-SQL the viewer writes from a plan: its missing indexes, and its parameters as variables.</summary>
    [TestClass]
    public class PlanScriptsTests
    {
        private static PlanStatement WithParameters(string parameters, string runtime = "") =>
            Parse("<ParameterList>" + parameters + "</ParameterList>" + Op(1, "Table Scan", "Table Scan", runtime));

        [TestMethod]
        public void MissingIndexes_AreScriptedBestFirst_WithWhatTheOptimizerExpected()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);
            var script = PlanScripts.MissingIndexes(statement);

            StringAssert.StartsWith(script, "-- Missing indexes the optimizer suggested");
            StringAssert.Contains(script, "-- 1. Sales.dbo.Orders: estimated to reduce this statement's cost by 92.4%");
            StringAssert.Contains(script, "--    Equality: CustomerID");
            StringAssert.Contains(script, "--    Read by Index Seek (node 1)");
            StringAssert.Contains(script, statement.MissingIndexes[0].CreateStatement);

            Assert.AreEqual(string.Empty, PlanScripts.MissingIndexes(TestPlans.Statement(TestPlans.Batch)));
        }

        [TestMethod]
        public void MissingIndex_OnOneLine_IsTheSameStatement()
        {
            var index = TestPlans.Statement(TestPlans.KeyLookupSeek).MissingIndexes[0];

            Assert.AreEqual(
                "CREATE NONCLUSTERED INDEX [IX_Orders_CustomerID_OrderDate] ON [Sales].[dbo].[Orders] ([CustomerID], [OrderDate]) INCLUDE ([Total]);",
                index.CreateStatementOneLine);
        }

        [TestMethod]
        public void Parameters_AreDeclaredWithTheValuesTheyRanWith_AndTheCompiledValueBeside()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);

            StringAssert.Contains(
                PlanScripts.DeclareParameters(statement),
                "DECLARE @CustomerID int = 9999; -- compiled for 1");
        }

        [TestMethod]
        public void Parameters_ThatRanWithOtherValues_AreScriptedBothWays()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);
            var both = PlanScripts.DeclareParameters(statement);

            StringAssert.StartsWith(both, "-- This execution ran with different parameter values from those the plan was compiled for.");

            // Runtime first - what ran - then compiled, each labelled, each noting the other value.
            var runtime = PlanScripts.DeclareParameters(statement, PlanParameterValues.Runtime);
            var compiled = PlanScripts.DeclareParameters(statement, PlanParameterValues.Compiled);
            Assert.IsTrue(both.IndexOf(runtime, System.StringComparison.Ordinal) < both.IndexOf(compiled, System.StringComparison.Ordinal));

            StringAssert.StartsWith(runtime, "-- Runtime values:");
            StringAssert.StartsWith(compiled, "-- Compiled values:");
            StringAssert.Contains(compiled, "DECLARE @CustomerID int = 1; -- ran with 9999");
        }

        [TestMethod]
        public void Parameters_ThatRanWithTheirCompiledValues_AreScriptedOnce()
        {
            var statement = WithParameters(
                """<ColumnReference Column="@Id" ParameterDataType="int" ParameterCompiledValue="(3)" ParameterRuntimeValue="(3)" />""",
                Rows(1));

            var script = PlanScripts.DeclareParameters(statement);

            Assert.IsFalse(PlanScripts.RuntimeValuesDiffer(statement));
            Assert.AreEqual(PlanScripts.DeclareParameters(statement, PlanParameterValues.Runtime), script);
            StringAssert.Contains(script, "DECLARE @Id int = 3;");
        }

        [TestMethod]
        public void Parameters_OnAnEstimatedPlan_AreDeclaredWithTheirCompiledValues()
        {
            var statement = WithParameters(
                """<ColumnReference Column="@Name" ParameterDataType="nvarchar(50)" ParameterCompiledValue="N'O''Brien'" />""");

            var script = PlanScripts.DeclareParameters(statement);

            StringAssert.StartsWith(script, "-- Compiled values: the parameter values the plan was compiled for.");
            StringAssert.Contains(script, "DECLARE @Name nvarchar(50) = N'O''Brien';");
        }

        [TestMethod]
        public void Parameters_WithNoTypeOrValue_StillMakeAScriptThatRuns()
        {
            var statement = WithParameters("""<ColumnReference Column="@Old" ParameterCompiledValue="(5)" /><ColumnReference Column="@Tvp" ParameterDataType="int" />""");

            var script = PlanScripts.DeclareParameters(statement);

            StringAssert.Contains(script, "DECLARE @Old sql_variant = 5; -- the plan does not record its type");
            StringAssert.Contains(script, "DECLARE @Tvp int; -- no value recorded in the plan");
            Assert.AreEqual(string.Empty, PlanScripts.DeclareParameters(Parse(Op(1, "Table Scan", "Table Scan", ""))));
        }

        [TestMethod]
        public void Literals_LoseOnlyTheBracketsRoundTheWholeValue()
        {
            Assert.AreEqual("42", PlanScripts.Literal("(42)"));
            Assert.AreEqual("-1.5", PlanScripts.Literal("(-1.5)"));
            Assert.AreEqual("(1)+(2)", PlanScripts.Literal("(1)+(2)"));
            Assert.AreEqual("N'x'", PlanScripts.Literal("N'x'"));
            Assert.AreEqual("NULL", PlanScripts.Literal("NULL"));
        }
    }
}
