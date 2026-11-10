using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>
    /// How the importer attributes its tables to the collection runs - <see cref="CollectionRunsTable.ApplyImport"/>.
    /// </summary>
    [TestClass]
    public class CollectionRunsTableTests
    {
        private static DataTable Runs(params (string Reference, string Step)[] runs)
        {
            var dt = CollectionRunsTable.Create();
            foreach (var (reference, step) in runs)
            {
                dt.Rows.Add(reference, step, DateTime.UtcNow, (byte)CollectionRunStatus.Succeeded, (byte)1, 10, DBNull.Value, DBNull.Value);
            }
            return dt;
        }

        private static DataRow Run(DataTable dt, string reference, string step = "") =>
            dt.Rows.Cast<DataRow>().Single(r => (string)r["Reference"] == reference && (string)r["Step"] == step);

        [TestMethod]
        public void TablesNamedAfterTheirCollection()
        {
            Assert.AreEqual(("Waits", ""), CollectionRunsTable.GetImportTarget("Waits", _ => false));
            Assert.AreEqual(("UserData.Test", ""), CollectionRunsTable.GetImportTarget("UserData.Test", _ => false));
        }

        [TestMethod]
        public void TablesOfAnotherCollection()
        {
            Assert.AreEqual(("Instance", ""), CollectionRunsTable.GetImportTarget("DBADash", _ => false));
            Assert.AreEqual(("Jobs", ""), CollectionRunsTable.GetImportTarget("JobSteps", _ => false));
            Assert.AreEqual(("RunningQueries", ""), CollectionRunsTable.GetImportTarget("SessionWaits", _ => false));
            Assert.AreEqual(("RunningQueries", "LiveProfiles"), CollectionRunsTable.GetImportTarget(LiveProfileCollector.TableName, _ => false));
            Assert.AreEqual(("SlowQueries", ""), CollectionRunsTable.GetImportTarget("SlowQueriesStats", _ => false));
        }

        /// <summary>Text and plans are shared by a step of running queries and of query stats</summary>
        [TestMethod]
        public void TextAndPlansGoToTheStepThatRan()
        {
            Assert.AreEqual(("RunningQueries", "Text"), CollectionRunsTable.GetImportTarget("QueryText", r => r == ("RunningQueries", "Text")));
            Assert.AreEqual(("QueryStats", "Plans"), CollectionRunsTable.GetImportTarget("QueryPlans", _ => false));
        }

        [TestMethod]
        public void ImportTimeAndCostAreAddedUpPerRun()
        {
            var dt = Runs(("Jobs", ""), ("RunningQueries", ""), ("RunningQueries", "Text"), ("Waits", ""));
            var tables = new[]
            {
                new ImportTableStats("Jobs", 100.4, 5, 50),
                new ImportTableStats("JobSteps", 20, null, null),
                new ImportTableStats("RunningQueries", 30, 1, 10),
                new ImportTableStats("QueryText", 7, 2, 20),
                new ImportTableStats("Snapshot_DB1", 1000, 9, 90) // Belongs to no run
            };
            CollectionRunsTable.ApplyImport(dt, tables, new List<string>());

            Assert.AreEqual(120, Run(dt, "Jobs")["ImportMs"]);
            Assert.AreEqual(5, Run(dt, "Jobs")["ImportCpuMs"]);
            Assert.AreEqual(50L, Run(dt, "Jobs")["ImportLogicalReads"]);
            Assert.AreEqual(30, Run(dt, "RunningQueries")["ImportMs"]);
            Assert.AreEqual(7, Run(dt, "RunningQueries", "Text")["ImportMs"]);
            Assert.AreEqual(DBNull.Value, Run(dt, "Waits")["ImportMs"], "Nothing imported for it");

            // The import of the runs is retried on a transient error - applying it again counts nothing twice
            CollectionRunsTable.ApplyImport(dt, tables, new List<string>());
            Assert.AreEqual(120, Run(dt, "Jobs")["ImportMs"]);
            Assert.AreEqual(5, Run(dt, "Jobs")["ImportCpuMs"]);
            Assert.AreEqual(50L, Run(dt, "Jobs")["ImportLogicalReads"]);
        }

        [TestMethod]
        public void ATableThatFailsToImportFailsItsRun()
        {
            var dt = Runs(("Jobs", ""), ("Waits", ""));
            dt.Rows[1]["Status"] = (byte)CollectionRunStatus.Failed;
            CollectionRunsTable.ApplyImport(dt, Array.Empty<ImportTableStats>(), new List<string> { "JobSteps", "Waits" });
            Assert.AreEqual((byte)CollectionRunStatus.ImportFailed, Run(dt, "Jobs")["Status"]);
            Assert.AreEqual((byte)CollectionRunStatus.Failed, Run(dt, "Waits")["Status"], "Already failed to collect");
        }

        private static CollectionRunStatus Status(bool collectedData, params RunError[] errors) =>
            CollectionRunsTable.GetStatus("Drivers", failed: false, attempts: 1, collectedData, errors);

        /// <summary>Drivers catches its own WMI failure and logs it with a "Collect:WMI" context - nothing was collected</summary>
        [TestMethod]
        public void ACaughtFailureWithNothingCollectedIsAFailure()
        {
            Assert.AreEqual(CollectionRunStatus.Failed, Status(false, new RunError("Drivers", "Collect:WMI", "Error connecting to WMI")));
            Assert.AreEqual(CollectionRunStatus.Failed, Status(false, new RunError("drivers", "Collect", "Error")), "Any context, any case");
        }

        /// <summary>ServerExtraProperties when one of its WMI queries fails - the rest of the data still arrives</summary>
        [TestMethod]
        public void ACaughtErrorWithDataCollectedIsPartial()
        {
            Assert.AreEqual(CollectionRunStatus.Partial, Status(true, new RunError("Drivers", "Collect:WMI", "Error")));
        }

        /// <summary>Another collection's error isn't this run's failure, even with nothing collected (Jobs skipped as unchanged, say)</summary>
        [TestMethod]
        public void AnotherSourcesErrorIsPartial()
        {
            Assert.AreEqual(CollectionRunStatus.Partial, Status(false, new RunError("Instance", "Collect", "Error")));
            Assert.AreEqual(CollectionRunStatus.Succeeded, Status(false), "Nothing to collect and no errors - Jobs unchanged");
        }

        [TestMethod]
        public void RetriesAndWarningsAreNotErrors()
        {
            Assert.AreEqual(CollectionRunStatus.Warning, Status(true, new RunError("Drivers", "Collect", "Warning: over the threshold")));
            Assert.AreEqual(CollectionRunStatus.Warning, Status(false, new RunError("Drivers", "Collect[Warning]", "Not supported")));
            Assert.AreEqual(CollectionRunStatus.Succeeded, Status(true, new RunError("Drivers", "Collect[Retry]", "Succeeded after 2 attempts")));
            Assert.AreEqual(CollectionRunStatus.Retried, CollectionRunsTable.GetStatus("Drivers", false, 2, true, Array.Empty<RunError>()));
            Assert.AreEqual(CollectionRunStatus.Failed, CollectionRunsTable.GetStatus("Drivers", true, 3, false, Array.Empty<RunError>()));
        }

        /// <summary>The table type has the import columns after the collector's - positional, so the order matters</summary>
        [TestMethod]
        public void ImportColumnsFollowTheCollectorsColumns()
        {
            var dt = Runs(("Waits", ""));
            CollectionRunsTable.EnsureImportColumns(dt);
            CollectionRunsTable.EnsureImportColumns(dt);
            CollectionAssert.AreEqual(
                new[] { "Reference", "Step", "SnapshotDate", "Status", "Attempts", "DurationMs", "CpuMs", "LogicalReads", "ImportMs", "ImportCpuMs", "ImportLogicalReads" },
                dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        }
    }
}
