using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Test
{
    /// <summary>
    /// Backup history matched to databases by name and family_guid, from a real collection imported by DBImporter into a new
    /// repository:
    /// <list type="bullet">
    /// <item>SQLBackups.sql drops history of a previous database with the same name, and never gives a restored copy's backups to
    /// the database it was restored from (they share family_guid).</item>
    /// <item>LastBackup/BackupStatus don't count stored backups of a previous database, but only once the Databases collection has
    /// seen the new database - the two collections run separately.  The warning is only raised for backup types that are checked.</item>
    /// <item>DatabasesHADR_Upd matches databases by name (in a contained AG the DMV's database_id for master isn't 1), falling back to
    /// database_id for older agents.</item>
    /// </list>
    /// Three imports: everything backed up, then (after databases are recreated/restored/taken offline) backups collected before
    /// the Databases collection sees the changes, then both.
    ///
    /// <para>Same server as <see cref="MessageAuthorizationSqlTests"/>: the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string, or LocalDB.  The repository and the test databases are dropped afterwards.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class BackupMappingSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Suffix = Guid.NewGuid().ToString("N")[..8];
        private static readonly string Repository = "DBADashBackupRepoTest_" + Suffix;
        private static readonly string ConnectionID = "BackupMappingTest_" + Suffix;
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string RepositoryConnectionString => ConnectionStringFor(Repository);

        /* Source databases.  All start in FULL recovery with a full and log backup */
        private static readonly string Normal = DB("Normal");                 // Unchanged
        private static readonly string Recreated = DB("Recreated");           // Dropped and recreated, not backed up since
        private static readonly string RecreatedFull = DB("RecreatedFull");   // Recreated (FULL) with a full backup, but no log backup yet
        private static readonly string RecreatedSimple = DB("RecreatedSimple"); // Recreated in SIMPLE recovery with a full backup
        private static readonly string Offline = DB("Offline");               // Taken offline - no family_guid
        private static readonly string Source = DB("Source");                 // Restored as Copy
        private static readonly string Copy = DB("Copy");                     // Restored from Source's backup, backed up, then dropped
        private static readonly string[] Databases = { Normal, Recreated, RecreatedFull, RecreatedSimple, Offline, Source, Copy };

        private static string DB(string name) => $"DBADashBk{Suffix}_{name}";

        private static DataTable statusBeforeDatabasesCollection = new();
        private static DataTable collectedBackups = new();
        private static DateTime sourceBackupStart;
        private static DateTime copyBackupStart;
        private static int instanceID;

        private static string ConnectionStringFor(string database)
        {
            var builder = ServerConnectionString == null
                ? new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }
                : new SqlConnectionStringBuilder(ServerConnectionString);
            builder.InitialCatalog = database;
            return builder.ConnectionString;
        }

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            try
            {
                await using var test = new SqlConnection(MasterConnectionString);
                await test.OpenAsync();
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
                return;
            }
            new DacpacUtility.DacpacService().ProcessDacPac(MasterConnectionString, Repository, MessageAuthorizationSqlTests.FindDacpac(), DBValidations.DBVersionStatusEnum.CreateDB);

            await using var master = new SqlConnection(MasterConnectionString);
            await master.OpenAsync();

            /* 1. Everything backed up.  Copy is restored from Source's backup (so it shares Source's family_guid) and backed up later */
            foreach (var db in Databases.Where(d => d != Copy))
            {
                CreateDatabase(master, db, "FULL");
                Execute(master, $"BACKUP DATABASE [{db}] TO DISK = 'NUL'; BACKUP LOG [{db}] TO DISK = 'NUL';");
            }
            var dataFile = (string)Scalar(master, $"SELECT physical_name FROM sys.master_files WHERE database_id = DB_ID('{Source}') AND type = 0");
            var folder = dataFile[..(dataFile.LastIndexOfAny(new[] { '\\', '/' }) + 1)];
            var backupFile = folder + Source + ".bak";
            Execute(master, $"BACKUP DATABASE [{Source}] TO DISK = N'{backupFile}' WITH INIT");
            sourceBackupStart = LastBackupStart(master, Source);
            Thread.Sleep(1100); // backup_start_date is to the second - make Copy's backup later than Source's
            Execute(master, $@"RESTORE DATABASE [{Copy}] FROM DISK = N'{backupFile}' WITH
                                MOVE N'{Source}' TO N'{folder}{Copy}.mdf', MOVE N'{Source}_log' TO N'{folder}{Copy}_log.ldf'");
            Execute(master, $"BACKUP DATABASE [{Copy}] TO DISK = 'NUL'");
            copyBackupStart = LastBackupStart(master, Copy);
            try
            {
                Execute(master, $"EXEC master.dbo.xp_delete_file 0, N'{backupFile}'");
            }
            catch (SqlException)
            {
                // Only tidying up
            }
            await CollectAndImportAsync(CollectionType.Databases, CollectionType.Backups);
            Thread.Sleep(1100); // Backups below must start after the Databases collection above, to the second

            /* 2. Databases recreated/restored/taken offline.  Backups collected before the Databases collection sees the changes */
            Execute(master, $"DROP DATABASE [{Recreated}]");
            CreateDatabase(master, Recreated, "FULL");
            Execute(master, $"DROP DATABASE [{RecreatedFull}]");
            CreateDatabase(master, RecreatedFull, "FULL");
            Execute(master, $"BACKUP DATABASE [{RecreatedFull}] TO DISK = 'NUL'");
            Execute(master, $"DROP DATABASE [{RecreatedSimple}]");
            CreateDatabase(master, RecreatedSimple, "SIMPLE");
            Execute(master, $"BACKUP DATABASE [{RecreatedSimple}] TO DISK = 'NUL'");
            Execute(master, $"ALTER DATABASE [{Offline}] SET OFFLINE WITH ROLLBACK IMMEDIATE");
            Execute(master, $"DROP DATABASE [{Copy}]");
            await CollectAndImportAsync(CollectionType.Backups);
            statusBeforeDatabasesCollection = BackupStatus();

            /* 3. Both collections */
            collectedBackups = (await CollectAndImportAsync(CollectionType.Databases, CollectionType.Backups)).Tables["Backups"]!;

            await using var repo = new SqlConnection(RepositoryConnectionString);
            await repo.OpenAsync();
            instanceID = (int)Scalar(repo, $"SELECT InstanceID FROM dbo.Instances WHERE ConnectionID = N'{ConnectionID}'");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                foreach (var db in Databases.Append(Repository))
                {
                    Execute(master, $@"IF DB_ID('{db}') IS NOT NULL
                                        BEGIN
                                            ALTER DATABASE [{db}] SET ONLINE;
                                            ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                            DROP DATABASE [{db}];
                                        END");
                }
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        private static async Task<DataSet> CollectAndImportAsync(params CollectionType[] types)
        {
            var collector = await DBCollector.CreateAsync(new DBADashSource(MasterConnectionString) { ConnectionID = ConnectionID }, "DBADashTest", disableRetry: true);
            await collector.CollectAsync(types);
            await new DBImporter(collector.Data, RepositoryConnectionString, DBADashAgent.GetCurrent()).UpdateAsync();
            return collector.Data;
        }

        private static void CreateDatabase(SqlConnection master, string db, string recoveryModel) =>
            Execute(master, $"CREATE DATABASE [{db}]; ALTER DATABASE [{db}] SET RECOVERY {recoveryModel};");

        private static DateTime LastBackupStart(SqlConnection master, string db) =>
            (DateTime)Scalar(master, $"SELECT MAX(backup_start_date) FROM msdb.dbo.backupset WHERE database_name = N'{db}'");

        private static DataTable BackupStatus()
        {
            using var repo = new SqlConnection(RepositoryConnectionString);
            repo.Open();
            using var da = new SqlDataAdapter(@"SELECT BS.name,BS.LastFull,BS.LastLog,BS.FullBackupStatus,BS.LogBackupStatus,BS.LogBackupExcludedReason,
                                                    BS.HasPreviousDatabaseBackups,BS.SnapshotBackups
                                                FROM dbo.BackupStatus BS
                                                JOIN dbo.Instances I ON I.InstanceID = BS.InstanceID
                                                WHERE I.ConnectionID = @ConnectionID", repo);
            da.SelectCommand.Parameters.AddWithValue("@ConnectionID", ConnectionID);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private static DataRow Status(DataTable status, string db) => status.Rows.Cast<DataRow>().Single(r => (string)r["name"] == db);

        private static DataRow[] Collected(string db) => collectedBackups.Rows.Cast<DataRow>().Where(r => (string)r["database_name"] == db).ToArray();

        private static void Execute(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 120 };
            cmd.ExecuteNonQuery();
        }

        private static object Scalar(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 120 };
            return cmd.ExecuteScalar()!;
        }

        [TestMethod]
        public void UnchangedDatabaseIsBackedUp()
        {
            var normal = Status(BackupStatus(), Normal);
            Assert.AreEqual(4, normal["FullBackupStatus"]);
            Assert.AreEqual(4, normal["LogBackupStatus"]);
            Assert.IsFalse((bool)normal["HasPreviousDatabaseBackups"]);
        }

        /// <summary>The collector doesn't send history of a previous database with the same name</summary>
        [TestMethod]
        public void CollectionDropsPreviousDatabaseHistory()
        {
            Assert.AreEqual(0, Collected(Recreated).Length, "Recreated hasn't been backed up since it was recreated");
            var recreatedFull = Collected(RecreatedFull);
            Assert.AreEqual("D", recreatedFull.Single()["type"], "Only the new full backup - not the previous database's log backup");
        }

        /// <summary>A restored copy shares family_guid with the database it was restored from - its backups aren't given to it</summary>
        [TestMethod]
        public void RestoredCopyBackupsAreNotGivenToSource()
        {
            Assert.IsTrue(copyBackupStart > sourceBackupStart);
            var sourceFull = Collected(Source).Where(r => (string)r["type"] == "D" && !(bool)r["is_copy_only"]).ToArray();
            Assert.AreEqual(sourceBackupStart, (DateTime)sourceFull.Single()["backup_start_date"]);
            Assert.AreEqual(0, Collected(Copy).Length);
        }

        /// <summary>An offline database has no family_guid - its history is matched by name, as before</summary>
        [TestMethod]
        public void OfflineDatabaseKeepsBackups()
        {
            var offline = Status(BackupStatus(), Offline);
            Assert.IsFalse(offline.IsNull("LastFull"));
            Assert.IsFalse((bool)offline["HasPreviousDatabaseBackups"]);
        }

        /// <summary>Stored backups of a previous database are kept, but flagged and not counted</summary>
        [TestMethod]
        public void PreviousDatabaseBackupsAreFlaggedNotCounted()
        {
            var recreated = Status(BackupStatus(), Recreated);
            Assert.IsTrue(recreated.IsNull("LastFull"));
            Assert.AreEqual(1, recreated["FullBackupStatus"]);
            Assert.IsTrue((bool)recreated["HasPreviousDatabaseBackups"]);
            Assert.IsTrue(recreated.IsNull("SnapshotBackups"), "No valid backups - as for a database never backed up");

            using var repo = new SqlConnection(RepositoryConnectionString);
            repo.Open();
            using var cmd = new SqlCommand(@"SELECT COUNT(*), SUM(CAST(LB.IsPreviousDatabaseBackup AS INT))
                                            FROM dbo.LastBackup LB
                                            JOIN dbo.Databases D ON D.DatabaseID = LB.DatabaseID
                                            WHERE D.InstanceID = @InstanceID AND D.name = @name AND D.IsActive = 1", repo);
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            cmd.Parameters.AddWithValue("@name", Recreated);
            using var rdr = cmd.ExecuteReader();
            rdr.Read();
            Assert.AreEqual(2, rdr.GetInt32(0), "The previous database's full and log backups are kept");
            Assert.AreEqual(2, rdr.GetInt32(1), "Both are flagged");
        }

        /// <summary>
        /// Backups collected before the Databases collection has seen a recreated database are checked against the old GUID -
        /// they can't be judged yet, so they count
        /// </summary>
        [TestMethod]
        public void BackupsNewerThanDatabasesCollectionCount()
        {
            var recreatedFull = Status(statusBeforeDatabasesCollection, RecreatedFull);
            Assert.AreEqual(4, recreatedFull["FullBackupStatus"]);
            Assert.IsFalse((bool)recreatedFull["HasPreviousDatabaseBackups"]);
        }

        /// <summary>The warning is only raised for a backup type that's checked</summary>
        [TestMethod]
        public void WarningOnlyForCheckedBackupTypes()
        {
            var status = BackupStatus();
            var recreatedFull = Status(status, RecreatedFull);
            Assert.AreEqual(4, recreatedFull["FullBackupStatus"], "The new full backup counts");
            Assert.AreEqual(1, recreatedFull["LogBackupStatus"], "The previous database's log backup doesn't");
            Assert.IsTrue((bool)recreatedFull["HasPreviousDatabaseBackups"]);

            var recreatedSimple = Status(status, RecreatedSimple);
            Assert.AreEqual(4, recreatedSimple["FullBackupStatus"]);
            StringAssert.Contains((string)recreatedSimple["LogBackupExcludedReason"], "SIMPLE");
            Assert.IsFalse((bool)recreatedSimple["HasPreviousDatabaseBackups"], "The stale log backup isn't relevant in SIMPLE recovery");
        }

        /// <summary>
        /// HADR rows are matched to databases by name.  In a contained AG the DMV's database_id for master is the node's id for
        /// &lt;AG&gt;_master, not 1.  Older agents don't send the name - matched on database_id.
        /// </summary>
        [TestMethod]
        public void DatabasesHADRMatchedByName()
        {
            var masterGroup = Guid.NewGuid();
            var msdbGroup = Guid.NewGuid();
            UpdateHADR((16375, "master", masterGroup), (99999, "msdb", msdbGroup), (12345, "DBADashNoSuchDatabase", Guid.NewGuid()));
            var hadr = HADRGroups();
            Assert.AreEqual(2, hadr.Count);
            Assert.AreEqual(masterGroup, hadr["master"]);
            Assert.AreEqual(msdbGroup, hadr["msdb"]);

            var legacyGroup = Guid.NewGuid();
            UpdateHADR((1, null, legacyGroup));
            hadr = HADRGroups();
            Assert.AreEqual(1, hadr.Count);
            Assert.AreEqual(legacyGroup, hadr["master"], "Older agent - matched on database_id");
        }

        private static void UpdateHADR(params (int DatabaseID, string? Name, Guid Group)[] rows)
        {
            var dt = new DataTable();
            dt.Columns.Add("database_id", typeof(int));
            dt.Columns.Add("group_database_id", typeof(Guid));
            dt.Columns.Add("is_primary_replica", typeof(bool));
            dt.Columns.Add("synchronization_state", typeof(byte));
            dt.Columns.Add("synchronization_health", typeof(byte));
            dt.Columns.Add("is_suspended", typeof(bool));
            dt.Columns.Add("suspend_reason", typeof(byte));
            dt.Columns.Add("replica_id", typeof(Guid));
            dt.Columns.Add("group_id", typeof(Guid));
            dt.Columns.Add("is_commit_participant", typeof(bool));
            dt.Columns.Add("database_state", typeof(byte));
            dt.Columns.Add("is_local", typeof(bool));
            dt.Columns.Add("secondary_lag_seconds", typeof(long));
            foreach (var col in new[] { "last_sent_time", "last_received_time", "last_hardened_time", "last_redone_time" })
            {
                dt.Columns.Add(col, typeof(DateTimeOffset));
            }
            foreach (var col in new[] { "log_send_queue_size", "log_send_rate", "redo_queue_size", "redo_rate", "filestream_send_rate" })
            {
                dt.Columns.Add(col, typeof(long));
            }
            dt.Columns.Add("last_commit_time", typeof(DateTimeOffset));
            dt.Columns.Add("database_name", typeof(string));
            foreach (var (databaseID, name, group) in rows)
            {
                var row = dt.NewRow();
                row["database_id"] = databaseID;
                row["group_database_id"] = group;
                row["is_primary_replica"] = true;
                row["is_local"] = true;
                row["database_name"] = (object?)name ?? DBNull.Value;
                dt.Rows.Add(row);
            }

            using var repo = new SqlConnection(RepositoryConnectionString);
            repo.Open();
            using var cmd = new SqlCommand("dbo.DatabasesHADR_Upd", repo) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@DatabasesHADR", SqlDbType.Structured) { TypeName = "dbo.DatabasesHADR", Value = dt });
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            cmd.Parameters.AddWithValue("@SnapshotDate", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
            Thread.Sleep(20); // A later call must have a later snapshot date
        }

        private static Dictionary<string, Guid> HADRGroups()
        {
            using var repo = new SqlConnection(RepositoryConnectionString);
            repo.Open();
            using var cmd = new SqlCommand(@"SELECT D.name, H.group_database_id
                                            FROM dbo.DatabasesHADR H
                                            JOIN dbo.Databases D ON D.DatabaseID = H.DatabaseID
                                            WHERE H.InstanceID = @InstanceID", repo);
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            using var rdr = cmd.ExecuteReader();
            var result = new Dictionary<string, Guid>();
            while (rdr.Read()) result.Add(rdr.GetString(0), rdr.GetGuid(1));
            return result;
        }
    }
}
