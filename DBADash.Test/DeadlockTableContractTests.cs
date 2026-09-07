using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DBADash.Deadlocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// SqlClient binds a DataTable to a table-valued parameter <b>by ordinal</b>, not by name.  A column
    /// added to one side of the deadlock collection contract and not the other therefore does not fail
    /// loudly - it silently writes each value into the neighbouring column, and only shows up as
    /// nonsense in a report much later.
    ///
    /// These tests read the CREATE TYPE definitions out of the database project and compare them, in
    /// order, with the DataTables the collector builds.
    /// </summary>
    [TestClass]
    public class DeadlockTableContractTests
    {
        [TestMethod]
        public void DeadlocksTableMatchesTableType()
        {
            AssertMatchesTableType(DeadlockTables.CreateDeadlocksTable(), "Deadlocks");
        }

        [TestMethod]
        public void DeadlockProcessesTableMatchesTableType()
        {
            AssertMatchesTableType(DeadlockTables.CreateProcessesTable(), "DeadlockProcesses");
        }

        [TestMethod]
        public void DeadlockResourcesTableMatchesTableType()
        {
            AssertMatchesTableType(DeadlockTables.CreateResourcesTable(), "DeadlockResources");
        }

        private static void AssertMatchesTableType(DataTable dt, string typeName)
        {
            var path = Path.Combine(
                FindRepositoryRoot(), "DBADashDB", "dbo", "User Defined Types", typeName + ".sql");
            if (!File.Exists(path))
            {
                Assert.Inconclusive($"Table type definition not found at {path}.");
            }

            var expected = ParseTableTypeColumns(File.ReadAllText(path));
            var actual = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            Assert.AreEqual(
                string.Join(", ", expected),
                string.Join(", ", actual),
                $"dbo.{typeName} and DeadlockTables have drifted.  Table-valued parameters bind by " +
                "ordinal, so both sides must list the same columns in the same order.");
        }

        /// <summary>
        /// Column names, in declaration order, from a CREATE TYPE ... AS TABLE definition.  Deliberately
        /// simple: it takes the first token of each line inside the body, skipping comments and the
        /// table level PRIMARY KEY constraint, which is all these files contain.
        /// </summary>
        internal static List<string> ParseTableTypeColumns(string sql)
        {
            // Block comments can contain anything, including text that looks like a column.
            var body = Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            var start = body.IndexOf("AS TABLE", StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(start >= 0, "No 'AS TABLE' found - is this a table type definition?");
            start = body.IndexOf('(', start);
            Assert.IsTrue(start >= 0, "No opening parenthesis found after 'AS TABLE'.");

            var columns = new List<string>();
            foreach (var rawLine in body[(start + 1)..].Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("--", StringComparison.Ordinal)) continue;
                if (line.StartsWith(")", StringComparison.Ordinal)) break;
                if (line.StartsWith("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("UNIQUE", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("CHECK", StringComparison.OrdinalIgnoreCase)) continue;

                var name = Regex.Match(line, @"^\[?([A-Za-z_][A-Za-z0-9_]*)\]?").Groups[1].Value;
                if (name.Length > 0) columns.Add(name);
            }

            Assert.IsTrue(columns.Count > 0, "No columns parsed from the table type definition.");
            return columns;
        }

        /// <summary>
        /// Walks up from the test binaries to the directory holding DBADash.sln.  The database project is
        /// not built into the test output - and shouldn't be - so the definitions are read from source.
        /// </summary>
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
