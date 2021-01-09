using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashServiceConfig
{
    class DBValidations
    {
        public static bool DBExists(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            var db = builder.InitialCatalog;
            builder.InitialCatalog = "";
            connectionString = builder.ConnectionString;
            SqlConnection cn = new SqlConnection(connectionString);

            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("SELECT CASE WHEN EXISTS(SELECT 1 FROM sys.databases WHERE name = @db) THEN CAST(1 as  BIT) ELSE CAST(0 as BIT) END as DBExists", cn);
                cmd.Parameters.AddWithValue("@db", db);
                return (bool)cmd.ExecuteScalar();
            }

        }

        public static System.Version GetDBVersion(string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"IF EXISTS(
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
			WHERE type NOT IN('S','IT','SQ'))
BEGIN
	SELECT '0.0.0.0' AS Version
END
ELSE
BEGIN 
	RAISERROR('Invalid database',11,1)
END
", cn);
                string version = (string)cmd.ExecuteScalar();
                if (version == null)
                {
                    version = "0.0.0.1";
                }
                return System.Version.Parse(version);
            }
        }
    }
}
