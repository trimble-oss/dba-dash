using System.Linq;
using System.Text.RegularExpressions;
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

            StringAssert.StartsWith(script, "/* Missing indexes the optimizer suggested");
            StringAssert.Contains(script, "/* 1. Sales.dbo.Orders: estimated to reduce this statement's cost by 92.4%");
            StringAssert.Contains(script, "   Equality: CustomerID");
            StringAssert.Contains(script, "   Read by Index Seek (node 1)");
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
        public void MissingIndexes_CannotBeEndedEarlyByAnIdentifier()
        {
            // A delimited identifier can hold anything, the two characters that end a block comment
            // included.  Left as they arrive, the note stops there and the rest of it - a name the
            // reader did not write - runs in whatever window they pasted the script into.
            var statement = Parse(
                """
                <MissingIndexes>
                  <MissingIndexGroup Impact="50">
                    <MissingIndex Database="[Sales]" Schema="[dbo]" Table="[Or*/ders]">
                      <ColumnGroup Usage="EQUALITY"><Column Name="[a/*b]" ColumnId="1" /></ColumnGroup>
                    </MissingIndex>
                  </MissingIndexGroup>
                </MissingIndexes>
                """ +
                Op(0, "Table Scan", "Table Scan", "",
                    """<Object Database="[Sales]" Schema="[dbo]" Table="[Or*/ders]" IndexKind="Heap" />"""));

            var script = PlanScripts.MissingIndexes(statement);

            // Defused where the name is prose inside the comment.
            StringAssert.Contains(script, "Sales.dbo.Or* /ders: estimated");
            StringAssert.Contains(script, "Equality: a/ *b");
            Assert.IsFalse(script.Contains("Or*/ders:"), "The heading would end the comment.");

            // Not where it is T-SQL: a delimited identifier means what it says, comment characters
            // included, so the statement still names the table the plan named.
            StringAssert.Contains(script, statement.MissingIndexes[0].CreateStatement);
            StringAssert.Contains(script, "ON [Sales].[dbo].[Or*/ders] ([a/*b]);");
        }

        [TestMethod]
        public void MissingIndexes_OnATempTable_SuggestDeclaringTheIndexOnTheCreateTable()
        {
            var statement = TempTableMissingIndex();
            var script = PlanScripts.MissingIndexes(statement);

            // The CREATE INDEX still has to be there - the CREATE TABLE is not always the reader's to
            // change - but not on its own, because run as it stands it costs the table its caching.
            StringAssert.Contains(script, "CREATE NONCLUSTERED INDEX [IX_#Ids_Item]");
            StringAssert.Contains(script, "#Ids is a temp table.");
            StringAssert.Contains(script, "stops SQL Server reusing a cached temp table");
            StringAssert.Contains(script, "CREATE TABLE [#Ids] (<columns>, INDEX [IX_#Ids_Item] NONCLUSTERED ([Item]));");

            // The note is inside the comment, so what is left is the statement, runnable as it stands.
            var code = Regex.Replace(script, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            Assert.AreEqual(statement.MissingIndexes[0].CreateStatement, code.Trim());
        }

        [TestMethod]
        public void MissingIndex_OnItsOwn_CarriesTheSameNotesAsTheTab()
        {
            // What a grid row's link and an insight card's View T-SQL open.  The reader who reaches a
            // recommendation that way has not seen the tab, so it cannot be the bare statement.
            var statement = TempTableMissingIndex();
            var one = PlanScripts.MissingIndex(statement, statement.MissingIndexes[0]);

            StringAssert.StartsWith(one, "/* A missing index the optimizer suggested for this statement.");
            StringAssert.Contains(one, "tempdb.dbo.#Ids: estimated to reduce this statement's cost by 99.2%");
            StringAssert.Contains(one, "Read by Table Scan (node 0)");
            StringAssert.Contains(one, "#Ids is a temp table.");
            StringAssert.Contains(one, statement.MissingIndexes[0].CreateStatement);

            // Numbered only on the tab, where there can be more than one to tell apart.
            Assert.IsFalse(one.Contains("/* 1. "), "One recommendation on its own is not numbered.");

            var code = Regex.Replace(one, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            Assert.AreEqual(statement.MissingIndexes[0].CreateStatement, code.Trim());
        }

        [TestMethod]
        public void MissingIndexes_OnAPermanentTable_SayNothingAboutCaching()
        {
            var script = PlanScripts.MissingIndexes(TestPlans.Statement(TestPlans.KeyLookupSeek));

            StringAssert.Contains(script, "CREATE NONCLUSTERED INDEX");
            Assert.IsFalse(script.Contains("temp table"), "The recommendation is on a permanent table.");
        }

        /// <summary>A statement whose one missing index is on a temp table.</summary>
        private static PlanStatement TempTableMissingIndex() =>
            Parse("""
                  <MissingIndexes>
                    <MissingIndexGroup Impact="99.2">
                      <MissingIndex Database="[tempdb]" Schema="[dbo]" Table="[#Ids]">
                        <ColumnGroup Usage="EQUALITY"><Column Name="[Item]" ColumnId="1" /></ColumnGroup>
                      </MissingIndex>
                    </MissingIndexGroup>
                  </MissingIndexes>
                  """ +
                  Op(0, "Table Scan", "Table Scan", "",
                      """<Object Database="[tempdb]" Schema="[dbo]" Table="[#Ids]" IndexKind="Heap" />"""));

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

            StringAssert.StartsWith(both, "/* This execution ran with different parameter values from those the plan was compiled for.");

            // Runtime first - what ran - then compiled, each labelled, each noting the other value.
            var runtime = PlanScripts.DeclareParameters(statement, PlanParameterValues.Runtime);
            var compiled = PlanScripts.DeclareParameters(statement, PlanParameterValues.Compiled);
            Assert.IsTrue(both.IndexOf(runtime, System.StringComparison.Ordinal) < both.IndexOf(compiled, System.StringComparison.Ordinal));

            StringAssert.StartsWith(runtime, "/* Runtime values:");
            StringAssert.StartsWith(compiled, "/* Compiled values:");
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

            StringAssert.StartsWith(script, "/* Compiled values: the parameter values the plan was compiled for.");
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

        private static PlanStatement WithSetOptions(string attributes) =>
            Parse($"<StatementSetOptions {attributes} />" + Op(1, "Table Scan", "Table Scan", ""));

        [TestMethod]
        public void SetOptions_AreScriptedAsSetStatements_InSsmsOrder()
        {
            var statement = WithSetOptions(
                """QUOTED_IDENTIFIER="true" ARITHABORT="false" CONCAT_NULL_YIELDS_NULL="true" ANSI_NULLS="true" ANSI_PADDING="true" ANSI_WARNINGS="true" NUMERIC_ROUNDABORT="false" """);

            var script = PlanScripts.SetOptions(statement);

            var sets = script.Split('\n', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Where(l => l.StartsWith("SET ")).ToList();

            CollectionAssert.AreEqual(
                new[]
                {
                    "SET ANSI_NULLS ON;", "SET ANSI_PADDING ON;", "SET ANSI_WARNINGS ON;", "SET ARITHABORT OFF;",
                    "SET CONCAT_NULL_YIELDS_NULL ON;", "SET NUMERIC_ROUNDABORT OFF;", "SET QUOTED_IDENTIFIER ON;"
                },
                sets);
        }

        [TestMethod]
        public void SetOptions_ExplainArithAbort_OnlyWhenItWasOff()
        {
            StringAssert.Contains(PlanScripts.SetOptions(WithSetOptions("""ARITHABORT="false" """)), "ARITHABORT was OFF");
            Assert.IsFalse(PlanScripts.SetOptions(WithSetOptions("""ARITHABORT="true" """)).Contains("was OFF"));
        }

        [TestMethod]
        public void SetOptions_IgnoreAttributesThatAreNotSetOptions()
        {
            // The names go into a script the reader runs, so only ones known to be SET options do.
            var script = PlanScripts.SetOptions(WithSetOptions("""ANSI_NULLS="true" EVIL="1; DROP TABLE x" """));

            StringAssert.Contains(script, "SET ANSI_NULLS ON;");
            Assert.IsFalse(script.Contains("EVIL") || script.Contains("DROP"));
        }

        [TestMethod]
        public void SetOptions_WithNoneRecorded_AreEmpty()
        {
            Assert.AreEqual(string.Empty, PlanScripts.SetOptions(Parse(Op(1, "Table Scan", "Table Scan", ""))));
        }

        [TestMethod]
        public void SetOptionsProperty_CarriesTheScript_ForTheViewerToOffer()
        {
            var property = TestPlans.Statement(TestPlans.KeyLookupSeek).Properties.Single(p => p.Name == "Statement Set Options");

            StringAssert.Contains(property.Script, "SET ARITHABORT ON;");
            Assert.AreEqual(7, property.Children.Count);
        }
    }
}
