using System.Linq;
using System.Security;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Finding the catch-all query - WHERE (col = @p OR @p IS NULL) - compiled without
    /// OPTION (RECOMPILE), from what its predicates look like in the plan.
    /// </summary>
    [TestClass]
    public class PlanOptionalParametersTests
    {
        [TestMethod]
        public void FindsEachSpelling_AsShowplanWritesIt()
        {
            // As SQL Server 2025 writes them: the first and third are copied from the captured plans,
            // and the second is the first the other way round.
            CollectionAssert.AreEqual(new[] { "@B" },
                PlanOptionalParameters.InPredicate("[CatchAll].[dbo].[T].[B]=[@B] OR [@B] IS NULL").ToList(), "col = @p OR @p IS NULL");
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("[@A] IS NULL OR [CatchAll].[dbo].[T].[A]=[@A]").ToList(), "@p IS NULL OR col = @p");
            CollectionAssert.AreEqual(new[] { "@A", "@B" },
                PlanOptionalParameters.InPredicate(
                    "[CatchAll].[dbo].[T].[A]=isnull([@A],[CatchAll].[dbo].[T].[A]) AND " +
                    "[CatchAll].[dbo].[T].[B]=CASE WHEN [@B] IS NOT NULL THEN [@B] ELSE [CatchAll].[dbo].[T].[B] END").ToList(),
                "ISNULL(@p, col), then COALESCE(@p, col)");
            CollectionAssert.AreEqual(new[] { "@C" },
                PlanOptionalParameters.InPredicate("[@C] IS NULL OR [CatchAll].[dbo].[T].[C] as [T2].[C]>=[@C]").ToList(), "an aliased column, and not equality");
        }

        [TestMethod]
        public void IgnoresParametersThatAreNotOptional()
        {
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]=[@A]").Count);
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[@A] IS NOT NULL OR [db].[dbo].[T].[A]=(1)").Count, "IS NOT NULL is not the pattern");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]=isnull([@A],(0))").Count, "ISNULL(@p, 0) is a constant, and seeks");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A] IS NULL OR [db].[dbo].[T].[B]=[@B]").Count, "a column tested for NULL");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate(null).Count);
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[@A] IS NULL OR [@B] IS NULL").Count, "a startup filter reads no column");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[@A] IS NULL OR [db].[dbo].[T].[B]=[@B]").Count, "@A is never compared to anything");
        }

        [TestMethod]
        public void TheDefaultedSpellings_OnlyCountAsEqualityWithTheSameColumn()
        {
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("isnull([@A],[db].[dbo].[T].[A])=[db].[dbo].[T].[A]").ToList(), "ISNULL(@p, col) = col");
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("CASE WHEN [@A] IS NOT NULL THEN [@A] ELSE [db].[dbo].[T].[A] END=[db].[dbo].[T].[A]").ToList(), "COALESCE(@p, col) = col");
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A] as [x].[A]=isnull([@A],[db].[dbo].[T].[A] as [x].[A])").ToList(), "an aliased column");

            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]>isnull([@A],[db].[dbo].[T].[B])").Count, "not equality, nor the same column");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]>isnull([@A],[db].[dbo].[T].[A])").Count, "not equality");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]=isnull([@A],[db].[dbo].[T].[B])").Count, "another column");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]=isnull([@A],[db].[dbo].[T].[A].[x])").Count, "a longer name");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[db].[dbo].[T].[A]=CASE WHEN [@A] IS NOT NULL THEN [@A] ELSE [db].[dbo].[T].[B] END").Count, "COALESCE with another column");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("isnull([@A],[db].[dbo].[T].[A])>=[db].[dbo].[T].[A]").Count, "reversed, and not equality");
            Assert.AreEqual(0, PlanOptionalParameters.DefaultedInPredicate("[db].[dbo].[T].[A]>isnull([@A],[db].[dbo].[T].[B])").Count);
        }

        [TestMethod]
        public void FindsTableVariableColumns()
        {
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("[@t].[A]=isnull([@A],[@t].[A])").ToList(), "ISNULL(@p, @t.col)");
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("[@t].[A]=CASE WHEN [@A] IS NOT NULL THEN [@A] ELSE [@t].[A] END").ToList(), "COALESCE(@p, @t.col)");
            CollectionAssert.AreEqual(new[] { "@A" },
                PlanOptionalParameters.InPredicate("[@t].[A]=[@A] OR [@A] IS NULL").ToList(), "@t.col = @p OR @p IS NULL");
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate("[@t].[A] IS NULL OR [@t].[B]=[@B]").Count, "a table variable's column tested for NULL");
        }

        [TestMethod]
        public void TheRecompileHint_InACommentOrString_IsNotTheHint()
        {
            Assert.IsFalse(PlanOptionalParameters.HasRecompileHint("SELECT ID FROM dbo.T WHERE (A = @A OR @A IS NULL) -- TODO: OPTION (RECOMPILE)"));
            Assert.IsFalse(PlanOptionalParameters.HasRecompileHint("SELECT ID FROM dbo.T /* OPTION /* nested */ (RECOMPILE) */ WHERE A = @A"));
            Assert.IsFalse(PlanOptionalParameters.HasRecompileHint("SELECT 'OPTION (RECOMPILE)' FROM dbo.T"));
            Assert.IsFalse(PlanOptionalParameters.HasRecompileHint("SELECT 'it''s OPTION (RECOMPILE)' FROM dbo.T"));

            Assert.IsTrue(PlanOptionalParameters.HasRecompileHint("SELECT ID -- a comment\nFROM dbo.T WHERE [x--y] = 'a;b' OPTION (OPTIMIZE FOR (@A = 'x;y'), RECOMPILE)"));
            Assert.IsTrue(PlanOptionalParameters.HasRecompileHint("SELECT ID /* c */ FROM dbo.T OPTION(RECOMPILE)"));
        }

        [TestMethod]
        public void ACatchAllPlan_IsCalledOut_OnTheStatementAndTheOperator()
        {
            // Compatibility level 150: (A = @A OR @A IS NULL) AND (B = @B OR @B IS NULL).  A constructed plan.
            var statement = TestPlans.Statement(TestPlans.OptionalParameters);

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Optional parameters"));
            Assert.AreEqual(PlanWarningSeverity.Warning, insight.Severity);
            CollectionAssert.AreEqual(new[] { 1, 3 }, insight.Operators.Select(op => op.NodeId).ToList());
            StringAssert.Contains(insight.Text, "@A and @B are supplied");
            StringAssert.Contains(insight.Text, "the predicates on nodes 1 and 3 match");

            // Each fix comes with its cost, and none helps without an index to seek on.
            StringAssert.Contains(insight.Text, "adds a compile to every execution");
            StringAssert.Contains(insight.Text, "more complex code");
            StringAssert.Contains(insight.Text, "needs compatibility level 170 and the OPTIONAL_PARAMETER_OPTIMIZATION database scoped configuration on");
            StringAssert.Contains(insight.Text, "None of these helps unless an index supports the conditions");
            Assert.IsFalse(insight.Text.Contains('\r'), "A card's label draws CRLF as one character and misplaces links after it.");

            // What a compile costs, next to what a run costs, for the reader to weigh the hint by.
            StringAssert.Contains(insight.Text, "This plan took 3 ms of CPU to compile, against 2 ms to run.");

            var scan = TestPlans.Operator(statement, 1);
            var own = PlanInsights.ForOperator(scan, statement).Single(i => i.Text.StartsWith("Optional parameters"));
            StringAssert.Contains(own.Text, "this operator's predicate matches whether or not @A is supplied");
            StringAssert.Contains(own.Text, "can't seek on that condition");
            Assert.AreEqual(PlanWarningSeverity.Warning, own.Severity);

            // @B is a residual on the lookup after the scan, but how much that costs depends on how
            // many rows the scan finds, which the plan cannot say for other values - still a warning.
            var lookup = TestPlans.Operator(statement, 3);
            var residual = PlanInsights.ForOperator(lookup, statement).Single(i => i.Text.StartsWith("Optional parameters"));
            Assert.AreEqual(PlanWarningSeverity.Warning, residual.Severity);
        }

        [TestMethod]
        public void AnOptionalParameterPlanVariant_WithOnlyNullsLeft_JustSaysItWasOptimized()
        {
            // The same query at compatibility level 170: SQL Server 2025 compiled a variant for @A,
            // whose seek is fine, and left B = @B OR @B IS NULL as a residual predicate - but @B is
            // NULL, so the residual matches every row and there is nothing left to do.
            var statement = TestPlans.Statement(TestPlans.OptionalParametersVariant);

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Optional parameters"));
            Assert.AreEqual(PlanWarningSeverity.Information, insight.Severity);
            Assert.AreEqual(
                "Optional parameters, optimized by SQL Server: optional parameter plan optimization compiled this plan for whether @A is NULL, removing its IS NULL test from the plan. " +
                "The condition left on @B matches every row, as it is NULL, so neither OPTION (RECOMPILE) nor another variant could do better for these values.",
                insight.Text);
            Assert.AreEqual(0, insight.Operators.Count);

            var lookup = TestPlans.Operator(statement, 4);
            Assert.IsFalse(PlanInsights.ForOperator(lookup, statement).Any(i => i.Text.StartsWith("Optional parameters")), "nothing to say about a residual that matches every row");
        }

        [TestMethod]
        public void AnOptionalParameterPlanVariant_WithNoConditionsLeft_SaysItWasOptimized()
        {
            var statement = Parse(Op(0, "Clustered Index Seek", "Clustered Index Seek", ""));
            statement.StatementText = "SELECT ID FROM dbo.T WHERE (A = @A OR @A IS NULL) option (PLAN PER VALUE(ObjectID = 1, QueryVariantID = 1, optional_predicate(@A IS NULL)))";

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Optional parameters"));
            Assert.AreEqual(PlanWarningSeverity.Information, insight.Severity);
            Assert.AreEqual("Optional parameters, optimized by SQL Server: optional parameter plan optimization compiled this plan for whether @A is NULL, removing its IS NULL test from the plan.", insight.Text);
        }

        [TestMethod]
        public void AnOptionalParameterPlanVariant_WithASuppliedValueLeft_SaysWhatWasLeftInIt()
        {
            // The variant compiled with @B NULL, reused from cache with @B supplied: the runtime value
            // is the one that counts, and the condition on @B can't seek.
            var statement = TestPlans.Statement(TestPlans.OptionalParametersVariantReused);

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Optional parameters"));
            CollectionAssert.AreEqual(new[] { 4 }, insight.Operators.Select(op => op.NodeId).ToList());
            StringAssert.StartsWith(insight.Text, "Optional parameters, optimized by SQL Server: optional parameter plan optimization compiled this plan for whether @A is NULL");
            StringAssert.Contains(insight.Text, "The predicate on node 4 matches whether or not @B is supplied");

            // The query is already being optimized for its optional parameters, so not a warning -
            // and the optimization is not offered as a fix for itself.
            Assert.AreEqual(PlanWarningSeverity.Information, insight.Severity);
            Assert.IsFalse(insight.Text.Contains("compatibility level 170"));
        }

        [TestMethod]
        public void IsNullAndCoalesce_GetNoVariant_SoTheOptimizationIsOfferedAfterARewrite()
        {
            // At compatibility level 170, with the optimization on, these compiled one plan each - a
            // clustered index scan with both conditions in its predicate.
            foreach (var name in new[] { TestPlans.OptionalParametersIsNull, TestPlans.OptionalParametersCoalesce })
            {
                var statement = TestPlans.Statement(name);
                Assert.AreEqual(0, PlanOptionalParameters.OptimizedBy(statement.StatementText).Count, name);

                var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Optional parameters"));
                Assert.AreEqual(PlanWarningSeverity.Warning, insight.Severity, name);
                CollectionAssert.AreEqual(new[] { 0 }, insight.Operators.Select(op => op.NodeId).ToList(), name);
                StringAssert.Contains(insight.Text, "@A and @B are supplied", name);
                StringAssert.Contains(insight.Text,
                    "It only recognizes (col = @p OR @p IS NULL), not ISNULL(@p, col) or COALESCE(@p, col), so the conditions on @A and @B would need rewriting in that form first.", name);
                StringAssert.Contains(insight.Text, "the OR form keeps rows where the column is NULL", name);
            }
        }

        [TestMethod]
        public void TheOrForm_IsNotToldToRewrite()
        {
            var insight = PlanInsights.ForStatement(TestPlans.Statement(TestPlans.OptionalParameters))
                .Single(i => i.Text.StartsWith("Optional parameters"));
            Assert.IsFalse(insight.Text.Contains("would need rewriting"));
        }

        [TestMethod]
        public void OptionRecompile_EmbedsTheValues_AndLeavesNothingToFind()
        {
            // @B NULL: ISNULL(NULL, B) leaves B = B as a residual on the lookup, which is no optional
            // parameter - and the hint is enough to say nothing in any case.
            var nullB = TestPlans.Statement(TestPlans.OptionalParametersRecompile);
            Assert.AreEqual("[CatchAll].[dbo].[T].[B]=[CatchAll].[dbo].[T].[B]", TestPlans.Operator(nullB, 4).Predicate);
            Assert.AreEqual(0, PlanOptionalParameters.InPredicate(TestPlans.Operator(nullB, 4).Predicate).Count);

            // Both supplied: a seek on each index, and no predicate left at all.
            var supplied = TestPlans.Statement(TestPlans.OptionalParametersRecompileSupplied);
            Assert.IsTrue(supplied.Operators.All(op => PlanOptionalParameters.InPredicate(op.Predicate).Count == 0));

            foreach (var statement in new[] { nullB, supplied })
                Assert.IsFalse(PlanInsights.ForStatement(statement).Any(i => i.Text.StartsWith("Optional parameters")));
        }

        [TestMethod]
        public void AnOptionalParameterPlanVariant_WithNullAndSuppliedResidualsLeft_OnlyCallsOutTheSuppliedOne()
        {
            // Chosen by @A; the scan keeps @B, which is NULL and matches every row, and the filter
            // keeps @C, which is supplied.
            var statement = Parse(
                """<ParameterList><ColumnReference Column="@B" ParameterCompiledValue="NULL" /><ColumnReference Column="@C" ParameterCompiledValue="(3)" /></ParameterList>""" +
                Op(0, "Filter", "Filter", "",
                    Predicate("[db].[dbo].[T].[C]=[@C] OR [@C] IS NULL"),
                    Op(1, "Clustered Index Scan", "Clustered Index Scan", "",
                        Predicate("[db].[dbo].[T].[B]=[@B] OR [@B] IS NULL"))));
            statement.StatementText = "SELECT ID FROM dbo.T option (PLAN PER VALUE(ObjectID = 1, QueryVariantID = 1, optional_predicate(@A IS NULL)))";

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Optional parameters"));
            CollectionAssert.AreEqual(new[] { 0 }, insight.Operators.Select(op => op.NodeId).ToList());
            StringAssert.Contains(insight.Text, "The predicate on node 0 matches whether or not @C is supplied");
            Assert.IsFalse(insight.Text.Contains("@B"), "the NULL condition has nothing to improve");

            Assert.IsTrue(PlanInsights.ForOperator(TestPlans.Operator(statement, 0), statement).Any(i => i.Text.StartsWith("Optional parameters")));
            Assert.IsFalse(PlanInsights.ForOperator(TestPlans.Operator(statement, 1), statement).Any(i => i.Text.StartsWith("Optional parameters")),
                "nothing to say about a residual that matches every row, whatever the other operators have");
        }

        [TestMethod]
        public void OptimizedBy_ReadsTheParametersAVariantWasChosenBy()
        {
            CollectionAssert.AreEqual(new[] { "@C", "@A" }, PlanOptionalParameters.OptimizedBy(
                "SELECT ... option (PLAN PER VALUE(ObjectID = 1317579732, QueryVariantID = 3, optional_predicate(@C IS NULL),optional_predicate(@A IS NULL)))").ToList());
            Assert.AreEqual(0, PlanOptionalParameters.OptimizedBy("SELECT ID FROM dbo.T WHERE (A = @A OR @A IS NULL)").Count);
        }

        [TestMethod]
        public void TheRecompileHint_IsEnoughToSayNothing()
        {
            // An estimated plan has no values to embed, so the pattern survives the hint.
            var statement = Parse(Op(0, "Clustered Index Scan", "Clustered Index Scan", "",
                Predicate("[db].[dbo].[T].[A]=[@A] OR [@A] IS NULL")));

            Assert.AreEqual(1, PlanOptionalParameters.In(statement).Count);

            statement.StatementText = "SELECT ID FROM dbo.T WHERE (A = @A OR @A IS NULL) OPTION (MAXDOP 1, OPTIMIZE FOR (@A = 1), RECOMPILE)";
            Assert.AreEqual(0, PlanOptionalParameters.In(statement).Count);
            Assert.IsFalse(PlanInsights.ForStatement(statement).Any(i => i.Text.StartsWith("Optional parameters")));

            statement.StatementText = "SELECT ID FROM dbo.T WHERE (A = @A OR @A IS NULL) OPTION (MAXDOP 1)";
            Assert.AreEqual(1, PlanOptionalParameters.In(statement).Count, "Other hints are not the one that helps.");
        }

        private static string Predicate(string expression) =>
            "<Predicate><ScalarOperator ScalarString=\"" + SecurityElement.Escape(expression) + "\" /></Predicate>";
    }
}
