using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DBADash.QueryStats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// SqlClient binds a DataTable to a table-valued parameter <b>by ordinal</b>, not by name.  A column
    /// added to one side of the query stats contract and not the other therefore does not fail loudly - it
    /// writes each value into the neighbouring column, and surfaces much later as a report where the CPU
    /// column holds a row count.
    ///
    /// The same guard the deadlock collection has, over the two types this collection sends.  Source only:
    /// the definitions are read out of the database project, so nothing here needs a repository.
    /// </summary>
    [TestClass]
    public class QueryStatsTableContractTests
    {
        [TestMethod]
        public void QueryStatsTableMatchesTableType()
        {
            AssertMatchesTableType(QueryStatsTables.GetQueryStatsSchema(), "QueryStats");
        }

        [TestMethod]
        public void QueryStatsCollectionTableMatchesTableType()
        {
            AssertMatchesTableType(QueryStatsTables.GetCollectionSchema(), "QueryStatsCollection");
        }

        /// <summary>
        /// The import procedure inserts the fact table's columns by name, but the columns it reads come from
        /// the table type, so a measure added to the type and forgotten in the table would import as a
        /// silent zero.  Compares the two lists rather than their order, since the table carries keys and
        /// computed columns the type does not.
        /// </summary>
        [TestMethod]
        public void EveryMeasureOnTheTableTypeIsStoredByTheTable()
        {
            var typeColumns = ReadTypeColumns("QueryStats");
            var tableColumns = ReadTableColumns("QueryStats");

            // Identity columns are resolved to a StatementID by the import and are not stored as they arrive.
            var resolvedByImport = new[]
            {
                "StatementType", "database_name", "schema_name", "object_name", "object_id", "sql_handle",
                "statement_start_offset", "statement_end_offset", "query_hash"
            };
            // An ad hoc shape's template and example batch belong to the statement rather than to an interval:
            // stored on dbo.QueryStatements and in dbo.QueryText - see the test below.
            var storedWithTheStatement = new[] { "StatementTemplate", "ExampleBatchText" };
            // A plan belongs to the statement and plan shape: stored in dbo.QueryStatsPlans - see the test below.
            var storedWithThePlanShape = new[] { "query_plan_compressed" };

            var missing = typeColumns
                .Where(c => !resolvedByImport.Contains(c, StringComparer.OrdinalIgnoreCase))
                .Where(c => !storedWithTheStatement.Contains(c, StringComparer.OrdinalIgnoreCase))
                .Where(c => !storedWithThePlanShape.Contains(c, StringComparer.OrdinalIgnoreCase))
                .Where(c => !tableColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            Assert.AreEqual(0, missing.Count,
                "dbo.QueryStats (the type) carries columns dbo.QueryStats (the table) has nowhere to put: "
                + string.Join(", ", missing));
        }

        /// <summary>
        /// The hourly rollup is added to as the raw rows are stored, and the reports read whole hours from it in
        /// place of the raw rows - so a measure added to dbo.QueryStats and not to dbo.QueryStats_60MIN would read
        /// as nothing over any window long enough to span an hour.
        /// </summary>
        [TestMethod]
        public void EveryMeasureIsRolledUpByTheHour()
        {
            var raw = ReadTableColumns("QueryStats");
            var hourly = ReadTableColumns("QueryStats_60MIN");

            // Not carried as they are: an interval's length and its ends become the span the hour's work was done
            // in, and the cache entry count is kept at its largest rather than summed.
            var replaced = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["PeriodTime"] = "FirstPeriodStart",
                ["PeriodStartTime"] = "FirstPeriodStart",
                ["PeriodEndTime"] = "LastSnapshotDate",
                ["PlanCount"] = "MaxPlanCount"
            };
            // Worked out from the others wherever they are read
            var derived = new[] { "AvgElapsedTime", "AvgWorkerTime" };

            var missing = raw
                .Where(c => !derived.Contains(c, StringComparer.OrdinalIgnoreCase))
                .Select(c => replaced.TryGetValue(c, out var hourlyColumn) ? hourlyColumn : c)
                .Where(c => !hourly.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            Assert.AreEqual(0, missing.Count,
                "dbo.QueryStats has columns dbo.QueryStats_60MIN does not roll up: " + string.Join(", ", missing));
        }

        [TestMethod]
        public void ShapeTemplateHasSomewhereToGo()
        {
            CollectionAssert.Contains(ReadTypeColumns("QueryStats"), "StatementTemplate");
            CollectionAssert.Contains(ReadTableColumns("QueryStatements"), "StatementTemplate",
                "the template the type carries is stored on the statement it belongs to");
        }

        [TestMethod]
        public void PlanHasSomewhereToGo()
        {
            CollectionAssert.Contains(ReadTypeColumns("QueryStats"), "query_plan_compressed");
            CollectionAssert.Contains(ReadTableColumns("QueryStatsPlans"), "query_plan_compressed",
                "the plan the type carries is stored for the statement and plan shape it belongs to");
        }

        private static void AssertMatchesTableType(DataTable dt, string typeName)
        {
            var expected = ReadTypeColumns(typeName);
            var actual = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            Assert.AreEqual(
                string.Join(", ", expected),
                string.Join(", ", actual),
                $"dbo.{typeName} and QueryStatsTables have drifted.  Table-valued parameters bind by "
                + "ordinal, so both sides must list the same columns in the same order.");
        }

        private static List<string> ReadTypeColumns(string typeName)
        {
            var path = Path.Combine(FindRepositoryRoot(), "DBADashDB", "dbo", "User Defined Types", typeName + ".sql");
            if (!File.Exists(path)) Assert.Inconclusive($"Table type definition not found at {path}.");
            return DeadlockTableContractTests.ParseTableTypeColumns(File.ReadAllText(path));
        }

        /// <summary>
        /// Column names from a CREATE TABLE definition.  Deliberately loose - it only has to spot a measure
        /// that has nowhere to land, so constraints, computed columns and indexes are noise either way.
        /// </summary>
        private static List<string> ReadTableColumns(string tableName)
        {
            var path = Path.Combine(FindRepositoryRoot(), "DBADashDB", "dbo", "Tables", tableName + ".sql");
            if (!File.Exists(path)) Assert.Inconclusive($"Table definition not found at {path}.");
            var sql = System.Text.RegularExpressions.Regex.Replace(
                File.ReadAllText(path), @"/\*.*?\*/", string.Empty,
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var start = sql.IndexOf('(', sql.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase));
            var columns = new List<string>();
            foreach (var rawLine in sql[(start + 1)..].Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("--", StringComparison.Ordinal)) continue;
                if (line.StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith(")", StringComparison.Ordinal)) break;

                var name = System.Text.RegularExpressions.Regex
                    .Match(line, @"^\[?([A-Za-z_][A-Za-z0-9_]*)\]?").Groups[1].Value;
                if (name.Length > 0) columns.Add(name);
            }

            Assert.IsTrue(columns.Count > 0, $"No columns parsed from {tableName}.");
            return columns;
        }

        private static string FindRepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "DBADash.sln"))) return dir.FullName;
                dir = dir.Parent;
            }

            Assert.Inconclusive("Repository root (the directory containing DBADash.sln) was not found.");
            return string.Empty;
        }
    }
}
