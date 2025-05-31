using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace DBADash
{
    public class ConnectionInfo
    {
        public int EngineEditionValue { get; set; }

        public DatabaseEngineEdition EngineEdition { get; set; }

        public string ProductVersion { get; set; }

        public string DatabaseName { get; set; }

        public string ServerName { get; set; }

        public string ComputerNetBIOSName { get; set; }

        public bool IsLinux { get; set; }

        public int MajorVersion => GetMajorVersion(ProductVersion);

        public bool IsAzureMasterDB => IsAzureDB && DatabaseName == "master";
        public bool IsAzureDB => EngineEdition == DatabaseEngineEdition.SqlDatabase;

        public bool IsRDS => ComputerNetBIOSName.StartsWith("EC2AMAZ-");

        // Extended events only supported on Standard and Enterprise editions for RDS
        public bool IsXESupported => (!IsRDS || EngineEdition is DatabaseEngineEdition.Standard or DatabaseEngineEdition.Enterprise) && GetXESupported(MajorVersion);

        public static int GetMajorVersion(string ProductVersion)
        {
            return int.Parse(ProductVersion[..ProductVersion.IndexOf('.')]);
        }

        public static bool GetXESupported(string ProductVersion)
        {
            return GetXESupported(GetMajorVersion(ProductVersion));
        }

        public static bool GetXESupported(int MajorVersion)
        {
            // Note: Extended events added in SQL 2008 (10.*).  Batch completed not supported in this version & there are other differences like recording durations in ms instead of microseconds
            return MajorVersion > 10;
        }

        public static ConnectionInfo GetConnectionInfo(string connectionString)
        {
            return Task.Run(async () => await GetConnectionInfoAsync(connectionString)).GetAwaiter().GetResult();
        }

        public static async Task<ConnectionInfo> GetConnectionInfoAsync(string connectionString)
        {
            var connectionInfo = new ConnectionInfo();
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("SELECT SERVERPROPERTY('EngineEdition'),SERVERPROPERTY('ProductVersion'),DB_NAME(),@@SERVERNAME,SERVERPROPERTY('ComputerNamePhysicalNetBIOS'),CASE WHEN SERVERPROPERTY('PathSeparator') = '/' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsLinux", cn);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            rdr.Read();
            connectionInfo.EngineEditionValue = rdr.GetInt32(0);
            try
            {
                connectionInfo.EngineEdition = (DatabaseEngineEdition)connectionInfo.EngineEditionValue;
            }
            catch
            {
                connectionInfo.EngineEdition = DatabaseEngineEdition.Unknown;
            }
            connectionInfo.ProductVersion = rdr.GetString(1);
            connectionInfo.DatabaseName = rdr.GetString(2);
            connectionInfo.ServerName = rdr.IsDBNull(3) ? "" : rdr.GetString(3);
            connectionInfo.ComputerNetBIOSName = rdr.IsDBNull(4) ? "" : rdr.GetString(4);  /* ComputerNamePhysicalNetBIOS is NULL for AzureDB */
            connectionInfo.IsLinux = rdr.GetBoolean(5);

            return connectionInfo;
        }
    }
}