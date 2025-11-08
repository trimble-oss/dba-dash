using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Octokit;
using System.Threading;

namespace DBADash
{
    public class DBValidations
    {
        public const string DACPackFile = "DBADashDB.dacpac";

        public static string DACPackPath => System.IO.Path.Combine(AppContext.BaseDirectory, DACPackFile);

        public enum DBVersionStatusEnum
        {
            OK,
            UpgradeRequired,
            AppUpgradeRequired,
            CreateDB
        }

        public class DBVersionStatus
        {
            public DBVersionStatusEnum VersionStatus;
            public Version DACVersion;
            public Version DBVersion;
            public bool DeployInProgress;
        }

        private static async Task TestConnectionAsync(string connectionString, CancellationToken ct = default)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(ct);
        }

        /// <summary>
        /// Used as part of DBExistsAsync to check for existence of the initial catalog database.  Connects to master database to check if the DB specified in the initial catalog exists.
        /// The DBExistsAsync method will first attempt to connect directly to the specified database, and if that fails, it will call this method to check for existence.
        /// </summary>
        /// <param name="connectionString">Connection string to test</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task<bool> InitialCatalogDatabaseExistsAsync(string connectionString, CancellationToken ct = default)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var db = builder.InitialCatalog;
            // Connect to master to check if DB exists
            // Note: This may fail for contained databases or if the user has insufficient permissions to access 'master'.
            builder.InitialCatalog = "master";
            await using var cn = new SqlConnection(builder.ConnectionString);
            await using var cmd = new SqlCommand(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM sys.databases WHERE name = @db) THEN 1 ELSE 0 END",
                cn);
            cmd.Parameters.AddWithValue("@db", db);
            await cn.OpenAsync(ct);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is int i && i == 1;
        }


        /// <summary>
        /// Checks if the initial catalog database exists by checking sys.databases in master or by attempting to connect directly.
        /// Master connection may fail for contained databases or if the user has insufficient permissions to access 'master'.
        /// Connecting directly to the DB might be slower if the DB doesn't exist
        /// </summary>
        /// <param name="connectionString">Connection string to test</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<bool> DBExistsAsync(string connectionString, CancellationToken ct = default)
        {
            // Edge case: no initial catalog provided -> let direct connection decide (likely points to default DB).
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                await TestConnectionAsync(connectionString, ct);
                return true;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // Timeout after 30 seconds
            linkedCts.CancelAfter(TimeSpan.FromSeconds(30));

            var connectionTask = TestConnectionAsync(connectionString, linkedCts.Token);
            var dbExistsTask = InitialCatalogDatabaseExistsAsync(connectionString, linkedCts.Token);

            var first = await Task.WhenAny(connectionTask, dbExistsTask);

            if (first == connectionTask)
            {
                // If we can connect directly, the DB exists  
                if (first.IsCompletedSuccessfully)
                {
                    await first;
                    linkedCts.Cancel();
                    dbExistsTask.ObserveFault(); // Avoid unobserved task exception
                    return true;
                }
                // Connection failed -> rely on catalog existence (may throw).
                return await dbExistsTask;
            }
            else
            {
                try
                {
                    var exists = await dbExistsTask;
                    linkedCts.Cancel();
                    connectionTask.ObserveFault(); // Avoid unobserved task exception
                    return exists;
                }
                catch
                {
                    // Catalog check failed (permissions / connectivity). If direct connection works, treat as exists.
                    await connectionTask;
                    return true;
                }
            }
        }

        public static (Version Version, bool DeployInProgress) GetDBVersion(string connectionString)
        {
            var sql = @"
DECLARE @DeployInProgress BIT = 0
IF EXISTS(  SELECT 1
            FROM sys.extended_properties
            WHERE name = 'IsDBUpgradeInProgress'
            AND value ='Y'
            AND class = 0
            )
BEGIN
    SET @DeployInProgress = 1
END

IF EXISTS(
	SELECT * FROM sys.extended_properties
	WHERE major_id=OBJECT_ID('DBVersionHistory')
	AND name = 'AppID'
	AND value='74F9FD8A-DF22-4355-9A7A-6E1F4AE712B9'
)
BEGIN
	SELECT TOP(1) Version, @DeployInProgress AS DeployInProgress
	FROM dbo.DBVersionHistory
	ORDER BY DeployDate DESC
END
ELSE IF NOT EXISTS(SELECT *
			FROM sys.objects
			WHERE type NOT IN('S','IT','SQ')
            AND is_ms_shipped=0
            )
        AND DB_ID()>4
BEGIN
	SELECT '0.0.0.0' AS Version,CAST(0 AS BIT) AS DeployInProgress
END
ELSE
BEGIN
	RAISERROR('Invalid database',11,1)
END
";
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, cn);

            cn.Open();
            using var rdr = cmd.ExecuteReader();
            string version = null;
            var deployInProgress = false;
            if (rdr.Read())
            {
                version = (string)rdr["Version"];
                deployInProgress = (bool)rdr["DeployInProgress"];
            }
            else // First time deployment is likely in progress
            {
                deployInProgress = true;
                version = "0.0.0.1";
            }
            return (Version.Parse(version), deployInProgress);
        }

        public static async Task<DBVersionStatus> VersionStatusAsync(string connectionString)
        {
            DBVersionStatus status = new()
            {
                DACVersion = DacpacUtility.DacpacService.GetVersion(DACPackPath)
            };
            if (!await DBExistsAsync(connectionString))
            {
                status.VersionStatus = DBVersionStatusEnum.CreateDB;
            }
            else
            {
                var dbVersion = GetDBVersion(connectionString);
                status.DBVersion = dbVersion.Version;
                status.DeployInProgress = dbVersion.DeployInProgress;
                var compare = status.DBVersion.CompareTo(status.DACVersion);
                if (compare == 0)
                {
                    status.VersionStatus = DBVersionStatusEnum.OK;
                }
                else if (compare > 0)
                {
                    status.VersionStatus = DBVersionStatusEnum.AppUpgradeRequired;
                }
                else
                {
                    status.VersionStatus = DBVersionStatusEnum.UpgradeRequired;
                }
            }
            return status;
        }

        public static Task UpgradeDBAsync(string connectionString)
        {
            SqlConnectionStringBuilder builder = new(connectionString);
            if (builder.InitialCatalog.Length > 0)
            {
                return UpgradeDBAsync(connectionString, builder.InitialCatalog);
            }
            else
            {
                throw new Exception("Initial catalog not specified");
            }
        }

        public static async Task UpgradeDBAsync(string connectionString, string db)
        {
            DacpacUtility.DacpacService dac = new();
            var status = await VersionStatusAsync(connectionString);
            if (status.VersionStatus is DBVersionStatusEnum.UpgradeRequired or DBVersionStatusEnum.CreateDB || status.DeployInProgress)
            {
                await Task.Run(() => dac.ProcessDacPac(connectionString, db, DACPackPath, status.VersionStatus));
            }
        }
    }
}