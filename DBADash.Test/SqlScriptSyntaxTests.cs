using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBADash;
using DBADash.Deadlocks;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Parses the T-SQL DBA Dash ships, so a syntax error is a failing build rather than a collection that
    /// fails against every instance it runs on.
    ///
    /// <para>Syntax only.  The parser has no instance to resolve names against, so an undeclared variable, a
    /// table that does not exist or a permission that is missing all pass here - what it catches is T-SQL
    /// that no server would accept whatever it was run against.</para>
    /// </summary>
    [TestClass]
    public class SqlScriptSyntaxTests
    {
        /// <summary>
        /// Every collection script, straight out of the assembly rather than off disk, so a script that was
        /// never embedded is not silently skipped.
        /// </summary>
        [TestMethod]
        public void EveryEmbeddedSqlScriptParses()
        {
            var assembly = typeof(SqlStrings).Assembly;
            var scripts = assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith("DBADash.SQL.", StringComparison.Ordinal)
                            && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.IsTrue(scripts.Count > 0, "No embedded SQL scripts were found to parse.");

            var failures = new List<string>();
            foreach (var name in scripts)
            {
                using var stream = assembly.GetManifestResourceStream(name);
                using var reader = new StreamReader(stream!);
                var errors = Parse(reader.ReadToEnd());
                if (errors.Count > 0) failures.Add($"{name}: {Describe(errors)}");
            }

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// The statements the deadlock session scripts assemble at run time and hand to sp_executesql.
        ///
        /// <para>Worth its own test because <see cref="EveryEmbeddedSqlScriptParses"/> cannot see them: to a
        /// parser reading the script, a statement built by concatenation is a string literal.  That is how an
        /// ALTER EVENT SESSION combining an ADD and a DROP - which T-SQL does not allow in one statement -
        /// reached an instance.</para>
        ///
        /// <para>The statements are rebuilt from the script rather than written out here, so this is testing
        /// what the script really produces rather than a copy of it that can quietly fall out of step.  The
        /// expected count is asserted so that extraction finding nothing fails rather than passes.</para>
        /// </summary>
        [TestMethod]
        public void DeadlockSessionScriptsBuildParseableStatements()
        {
            // Three statements on Azure - create, drop to resize, start - and two on-premises, which has no
            // buffer to resize.
            AssertBuiltStatementsParse("DeadlockSessionAzure", SqlStrings.GetSqlString("DeadlockSessionAzure"), 3);
            AssertBuiltStatementsParse("DeadlockSession", SqlStrings.GetSqlString("DeadlockSession"), 2);
        }

        /// <summary>
        /// The stop/start that empties a ring buffer.  Built in C# rather than in a script, so it is taken
        /// from the collector itself - both the batch that is sent and the statements that batch assembles.
        /// </summary>
        [TestMethod]
        public void RingBufferFlushBuildsParseableStatements()
        {
            foreach (var databaseScoped in new[] { true, false })
            {
                var batch = DeadlockCollector.BuildRingBufferFlushSql(databaseScoped);
                var label = databaseScoped ? "flush (database scoped)" : "flush (server scoped)";

                AssertParses(label, batch);
                AssertBuiltStatementsParse(label, batch, 1);
            }
        }

        /// <summary>
        /// Rebuilds every statement <paramref name="script"/> assembles by concatenation and parses each.
        /// </summary>
        private static void AssertBuiltStatementsParse(string label, string script, int expectedCount)
        {
            var statements = BuildableStatements(script).ToList();

            Assert.AreEqual(expectedCount, statements.Count,
                $"{label} builds {statements.Count} statement(s) where {expectedCount} were expected.  If the " +
                "script gained or lost one, update the count; if it went to nothing, the extraction is no " +
                "longer finding the statements and is checking nothing.");

            for (var i = 0; i < statements.Count; i++)
            {
                AssertParses($"{label} statement {i + 1}: {Summarise(statements[i])}", statements[i]);
            }
        }

        /// <summary>
        /// The value of every variable the script assigns from an expression containing a string literal -
        /// which is the shape of a statement being assembled, as against a variable simply being given a
        /// name or a number.
        /// </summary>
        private static IEnumerable<string> BuildableStatements(string script)
        {
            var parser = new TSql160Parser(true);
            using var reader = new StringReader(script);
            var fragment = parser.Parse(reader, out var errors);
            Assert.AreEqual(0, errors.Count, $"The script itself does not parse: {Describe(errors)}");

            var visitor = new AssignedExpressionVisitor();
            fragment.Accept(visitor);

            return visitor.Expressions.Where(ContainsStringLiteral).Select(Rebuild);
        }

        /// <summary>Collects what a script assigns, whether by SET or by DECLARE with a value.</summary>
        private sealed class AssignedExpressionVisitor : TSqlFragmentVisitor
        {
            public List<ScalarExpression> Expressions { get; } = new();

            public override void Visit(SetVariableStatement node)
            {
                if (node.Expression != null) Expressions.Add(node.Expression);
            }

            public override void Visit(DeclareVariableElement node)
            {
                if (node.Value != null) Expressions.Add(node.Value);
            }
        }

        /// <summary>
        /// Turns a concatenation back into the statement it produces.  Literals contribute their text; what
        /// the server would substitute at run time becomes a stand-in, because the point is the shape of the
        /// statement rather than the values in it.
        ///
        /// <para>QUOTENAME yields a bracketed identifier, since that is what it is there for and a number
        /// would not be a legal object name.  Everything else - a CAST of a size, a REPLACE inside a quoted
        /// file name - is a value, and a number is legal wherever one of those appears.</para>
        /// </summary>
        private static string Rebuild(ScalarExpression expression) => expression switch
        {
            StringLiteral literal => literal.Value,
            BinaryExpression { BinaryExpressionType: BinaryExpressionType.Add } concat =>
                Rebuild(concat.FirstExpression) + Rebuild(concat.SecondExpression),
            FunctionCall call when string.Equals(call.FunctionName?.Value, "QUOTENAME",
                StringComparison.OrdinalIgnoreCase) => "[DBADashSyntaxTest]",
            _ => "1"
        };

        private static bool ContainsStringLiteral(ScalarExpression expression) => expression switch
        {
            StringLiteral => true,
            BinaryExpression concat => ContainsStringLiteral(concat.FirstExpression)
                                       || ContainsStringLiteral(concat.SecondExpression),
            _ => false
        };

        private static void AssertParses(string label, string sql)
        {
            var errors = Parse(sql);
            Assert.AreEqual(0, errors.Count, $"{label}: {Describe(errors)}");
        }

        /// <summary>
        /// Parses as T-SQL, quoted identifiers on - which is how SqlClient connects, and what the scripts are
        /// written for.  TSql160Parser is SQL Server 2022; it accepts everything earlier versions do, and
        /// Azure SQL Database is ahead of it, so it is the right grammar for scripts that run on both.
        /// </summary>
        private static IList<ParseError> Parse(string sql)
        {
            var parser = new TSql160Parser(true);
            using var reader = new StringReader(sql);
            parser.Parse(reader, out var errors);
            return errors;
        }

        private static readonly char[] WhiteSpace = { ' ', '\t', '\r', '\n' };

        /// <summary>Enough of a statement to recognise it in a failure message.</summary>
        private static string Summarise(string sql)
        {
            var flattened = string.Join(" ", sql.Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries));
            return flattened.Length > 60 ? flattened[..60] + "..." : flattened;
        }

        private static string Describe(IEnumerable<ParseError> errors) =>
            string.Join("; ", errors.Take(3).Select(e => $"line {e.Line}: {e.Message}"));
    }
}
