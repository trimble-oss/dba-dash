using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// A temp table is created in tempdb, so a character column declared without a collation takes the server
    /// collation rather than the repository database's.  Where the two differ, comparing that column with a
    /// repository table column fails with "Cannot resolve the collation conflict" (#2210).
    ///
    /// <para>CI's server collation differs from the repository's, but a missing COLLATE DATABASE_DEFAULT only shows up
    /// there if the statement runs - the AGHealthEvents one needs an availability group failover.  This test checks
    /// every CREATE TABLE #... in the database project instead, whichever code path uses it.</para>
    ///
    /// <para>Table variables and SELECT INTO aren't checked: a table variable takes the current database's
    /// collation and SELECT INTO takes the collation of its source columns.</para>
    /// </summary>
    [TestClass]
    public class TempTableCollationTests
    {
        private static readonly HashSet<SqlDataTypeOption> CharacterTypes = new()
        {
            SqlDataTypeOption.Char,
            SqlDataTypeOption.VarChar,
            SqlDataTypeOption.NChar,
            SqlDataTypeOption.NVarChar,
            SqlDataTypeOption.Text,
            SqlDataTypeOption.NText
        };

        [TestMethod]
        public void TempTableCharacterColumnsSpecifyCollation()
        {
            var root = Path.Combine(MessageAuthorizationTests.FindRepositoryRoot(), "DBADashDB");
            var files = Directory.EnumerateFiles(root, "*.sql", SearchOption.AllDirectories).ToList();
            Assert.IsTrue(files.Count > 0, $"No SQL files were found in {root}.");

            var failures = new List<string>();
            foreach (var file in files)
            {
                var parser = new TSql160Parser(true);
                using var reader = new StreamReader(file);
                var fragment = parser.Parse(reader, out var errors);
                if (errors.Count > 0)
                {
                    failures.Add($"{Path.GetRelativePath(root, file)}: doesn't parse ({errors[0].Message})");
                    continue;
                }

                var visitor = new TempTableVisitor();
                fragment.Accept(visitor);
                failures.AddRange(visitor.Columns.Select(c => $"{Path.GetRelativePath(root, file)}: {c}"));
            }

            Assert.AreEqual(0, failures.Count,
                "Character columns in temp tables need COLLATE DATABASE_DEFAULT, otherwise they take the server " +
                "collation and can't be compared with repository columns when the repository database has a " +
                "different collation:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        private class TempTableVisitor : TSqlFragmentVisitor
        {
            public List<string> Columns { get; } = new();

            public override void Visit(CreateTableStatement node)
            {
                var table = node.SchemaObjectName.BaseIdentifier.Value;
                if (!table.StartsWith('#') || node.Definition == null) return;

                foreach (var column in node.Definition.ColumnDefinitions)
                {
                    if (column.Collation == null && IsCharacterType(column.DataType))
                    {
                        Columns.Add($"{table}.{column.ColumnIdentifier.Value} (line {column.StartLine})");
                    }
                }
            }

            private static bool IsCharacterType(DataTypeReference type) => type switch
            {
                SqlDataTypeReference sqlType => CharacterTypes.Contains(sqlType.SqlDataTypeOption),
                // sysname is NVARCHAR(128)
                UserDataTypeReference userType => string.Equals(userType.Name.BaseIdentifier.Value, "sysname", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
