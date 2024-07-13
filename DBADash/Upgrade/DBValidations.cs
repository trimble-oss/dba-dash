using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace DBADash
{
    public class DBValidations
    {

        public const string DACPackFile = "DBADashDB.dacpac";

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

        }


        public static bool DBExists(string connectionString)
        {
            SqlConnectionStringBuilder builder = new(connectionString);
            var db = builder.InitialCatalog;
            builder.InitialCatalog = "";
            connectionString = builder.ConnectionString;

            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SELECT CASE WHEN EXISTS(SELECT 1 FROM sys.databases WHERE name = @db) THEN CAST(1 as  BIT) ELSE CAST(0 as BIT) END as DBExists", cn))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@db", db);
                return (bool)cmd.ExecuteScalar();
            }

        }

        public static Version GetDBVersion(string connectionString)
        {
            var sql = @"IF EXISTS(
	SELECT * FROM sys.extended_properties
	WHERE major_id=OBJECT_ID('DBVersionHistory')
	AND name = 'AppID'
	AND value='74F9FD8A-DF22-4355-9A7A-6E1F4AE712B9'
)
BEGIN 
	SELECT TOP(1) Version
	FROM dbo.DBVersionHistory
	ORDER BY DeployDate DESC
END
ELSE IF NOT EXISTS(SELECT * 
			FROM sys.objects
			WHERE type NOT IN('S','IT','SQ')          
            )
        AND DB_ID()>4
BEGIN
	SELECT '0.0.0.0' AS Version
END
ELSE
BEGIN 
	RAISERROR('Invalid database',11,1)
END
";
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                var version = (string)cmd.ExecuteScalar();
                version ??= "0.0.0.1";
                return Version.Parse(version);
            }
        }

        public static DBVersionStatus VersionStatus(string connectionString)
        {
            DBVersionStatus status = new()
            {
                DACVersion = DacpacUtility.DacpacService.GetVersion(DACPackFile)
            };
            if (!DBExists(connectionString))
            {
                status.VersionStatus = DBVersionStatusEnum.CreateDB;
            }
            else
            {

                status.DBVersion = GetDBVersion(connectionString);
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

        public static Task UpgradeDBAsync(string connectionString, string db)
        {
            DacpacUtility.DacpacService dac = new();
            var status = VersionStatus(connectionString);
            if (status.VersionStatus is DBVersionStatusEnum.UpgradeRequired or DBVersionStatusEnum.CreateDB)
            {
                return Task.Run(() => dac.ProcessDacPac(connectionString, db, DACPackFile, status.VersionStatus));
            }
            else
            {
                return Task.CompletedTask;
            }
        }


    }
}
