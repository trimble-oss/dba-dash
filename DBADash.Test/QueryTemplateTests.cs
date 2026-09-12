using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBADash.QueryStats;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The template an ad hoc query shape is shown as.  What these protect is the one promise it makes: the literal
    /// values its variants differ in are never shown, and nothing else about the statement is lost.
    /// </summary>
    [TestClass]
    public class QueryTemplateTests
    {
        [TestMethod]
        public void LiteralValuesAreReplaced()
        {
            Assert.AreEqual(
                "SELECT * FROM dbo.Orders WHERE CustomerID = ? AND Status = N'?' AND Code = '?' AND Total > ? AND Flags = 0x? AND Price < $?",
                QueryTemplate.Create(
                    "SELECT * FROM dbo.Orders WHERE CustomerID = 42 AND Status = N'Open' AND Code = 'it''s' AND Total > 12.5E-3 AND Flags = 0x1F AND Price < $9.99"));
        }

        [TestMethod]
        public void DigitsInsideIdentifiersAreNotNumbers()
        {
            const string sql = "SELECT col1, @p2, #t3, [Order Details 4], $action FROM Table5 t6";
            Assert.AreEqual(sql, QueryTemplate.Create(sql));
        }

        [TestMethod]
        public void CommentsAreRemovedWithoutJoiningTheTokensEitherSide()
        {
            Assert.AreEqual("SELECT a \nFROM t", QueryTemplate.Create("/* user 42 */ SELECT/*x*/a -- trace 7\nFROM t"));
        }

        [TestMethod]
        public void NestedBlockCommentsAreOneComment()
        {
            Assert.AreEqual("SELECT ?  FROM t", QueryTemplate.Create("SELECT 1 /* a /* b */ 'c */ FROM t"));
        }

        [TestMethod]
        public void QuotesInCommentsAndCommentsInStringsAreNeitherOne()
        {
            Assert.AreEqual("SELECT '?' \n, '?'", QueryTemplate.Create("SELECT '--not a comment' -- it's a comment\n, '/* nor this */'"));
        }

        [TestMethod]
        public void DoubleQuotesFollowQuotedIdentifier()
        {
            const string sql = "SELECT \"Order Total\" FROM t WHERE a = \"x\"\"y\"";
            Assert.AreEqual(sql, QueryTemplate.Create(sql, quotedIdentifier: true), "on, they delimit identifiers");
            Assert.AreEqual("SELECT \"?\" FROM t WHERE a = \"?\"", QueryTemplate.Create(sql, quotedIdentifier: false),
                "off, they delimit strings, which vary like any other");
        }

        [TestMethod]
        public void AnUnclosedStringOrCommentRunsToTheEnd()
        {
            Assert.AreEqual("SELECT '?'", QueryTemplate.Create("SELECT 'abc"));
            Assert.AreEqual("SELECT ?", QueryTemplate.Create("SELECT 1 /* abc"));
        }

        [TestMethod]
        public void StatementIsCutTheWayTheRepositoryCutsIt()
        {
            const string batch = "SELECT 1; SELECT 2;";
            Assert.AreEqual("SELECT 2;", QueryTemplate.GetStatementText(batch, 20, 36), "byte offsets into an NVARCHAR batch");
            Assert.AreEqual("SELECT 2;", QueryTemplate.GetStatementText(batch, 20, -1), "an end of -1 is the end of the batch");
            Assert.AreEqual(batch, QueryTemplate.GetStatementText(batch, -1, 16), "a start of -1 is the whole batch");
            Assert.AreEqual("", QueryTemplate.GetStatementText(batch, 400, 410), "offsets past the text are nothing rather than an error");
        }

        /// <summary>
        /// Statements of the kinds that reach the plan cache as ad hoc SQL, each checked against SQL Server's own
        /// tokenizer: every literal token replaced, every comment gone, every other token exactly as it was.
        /// </summary>
        [TestMethod]
        public void AgreesWithTheTsqlTokenizer()
        {
            var statements = new[]
            {
                "SELECT TOP (10) o.OrderID, o.Total FROM dbo.Orders o WHERE o.CustomerID = 42 AND o.Status = N'Open' ORDER BY o.OrderDate DESC",
                "SELECT [Extent1].[Id], [Extent1].[Name] FROM [dbo].[Customers] AS [Extent1] WHERE [Extent1].[Region] = 'North' AND [Extent1].[Score] >= 0.75",
                "UPDATE dbo.Stock SET Qty = Qty - 3, Updated = '2026-10-05T12:00:00' WHERE Sku IN ('A1', 'B22', 'C333')",
                "INSERT INTO #t1 (a, b, c) VALUES (1, -2.5, 1e10), (0x00FF, $12.34, N'x''y')",
                "SELECT CONVERT(varchar(10), GETDATE(), 120), DATEADD(day, -7, SYSUTCDATETIME()), CAST(1.5E+3 AS float)",
                "/* app=orders;user=alice */ SELECT a FROM t -- trace 0af7651916cd43dd8448eb211c80319c\nWHERE b = 'x' /* nested /* comment */ here */ AND c = 5",
                "SELECT x.value('(/root/item)[1]', 'int') AS v FROM dbo.Docs WHERE Body LIKE '%[_]done%' ESCAPE '\\'",
                "MERGE dbo.T AS t USING (SELECT 1 AS id) AS s ON t.id = s.id WHEN MATCHED THEN UPDATE SET n = 'x' OUTPUT $action;",
                "SELECT {ts '2020-01-01 00:00:00'}, £5, ¥7, 0X1f, .5, 5., 1e FROM t WHERE @p1 = 3 AND @@ROWCOUNT > 0",
                "SELECT Größe1, \"Quoted Col\" FROM tbl2 WHERE n = n'unicode' AND d BETWEEN '20260101' AND '20261231' OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY",
                "SELECT 'it''s', [it's], \"say \"\"hi\"\"\" FROM t WHERE a = -1 AND b = - 1.0 AND c = +2"
            };

            foreach (var quotedIdentifier in new[] { true, false })
            {
                foreach (var sql in statements)
                {
                    var expected = Normalize(TemplateFromTokens(sql, quotedIdentifier));
                    var actual = Normalize(QueryTemplate.Create(sql, quotedIdentifier));
                    Assert.AreEqual(expected, actual, $"QUOTED_IDENTIFIER {(quotedIdentifier ? "ON" : "OFF")}: {sql}");
                }
            }
        }

        /// <summary>The template built from ScriptDom's tokens - see <see cref="QueryTemplate"/> for what each becomes.</summary>
        private static string TemplateFromTokens(string sql, bool quotedIdentifier)
        {
            var parser = new TSql160Parser(quotedIdentifier);
            var tokens = parser.GetTokenStream(new StringReader(sql), out IList<ParseError> errors);
            Assert.AreEqual(0, errors.Count, $"the test statement should tokenize: {sql}");

            var sb = new StringBuilder();
            foreach (var token in tokens)
            {
                sb.Append(token.TokenType switch
                {
                    TSqlTokenType.AsciiStringLiteral => "'?'",
                    TSqlTokenType.UnicodeStringLiteral => token.Text[0] + "'?'",
                    TSqlTokenType.Integer or TSqlTokenType.Numeric or TSqlTokenType.Real => "?",
                    TSqlTokenType.Money => token.Text[0] + "?",
                    TSqlTokenType.HexLiteral => token.Text[..2] + "?",
                    TSqlTokenType.AsciiStringOrQuotedIdentifier => quotedIdentifier ? token.Text : "\"?\"",
                    TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment => " ",
                    TSqlTokenType.EndOfFile => "",
                    _ => token.Text
                });
            }
            return sb.ToString();
        }

        /// <summary>Layout aside: where a comment was taken out is spacing, not content.</summary>
        private static string Normalize(string sql) => Regex.Replace(sql, @"\s+", " ").Trim();
    }
}
