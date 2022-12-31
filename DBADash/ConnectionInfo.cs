using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System;

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

        public int MajorVersion => GetMajorVersion(ProductVersion);

        public bool IsAzureMasterDB => IsAzureDB && DatabaseName == "master";
        public bool IsAzureDB => EngineEdition == DatabaseEngineEdition.SqlDatabase;

        public bool IsRDS => ComputerNetBIOSName.StartsWith("EC2AMAZ-");

        // Extended events only supported on Standard and Enterprise editions for RDS
        public bool IsXESupported => IsRDS && !(EngineEdition == DatabaseEngineEdition.Standard || EngineEdition == DatabaseEngineEdition.Enterprise) ? false : GetXESupported(MajorVersion);

        public static int GetMajorVersion(string ProductVersion)
        {
            return Int32.Parse(ProductVersion[..ProductVersion.IndexOf('.')]);
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
            var connectionInfo = new ConnectionInfo();
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SELECT SERVERPROPERTY('EngineEdition'),SERVERPROPERTY('ProductVersion'),DB_NAME(),@@SERVERNAME,SERVERPROPERTY('ComputerNamePhysicalNetBIOS')", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
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
                    connectionInfo.ServerName = rdr.GetString(3);
                    connectionInfo.ComputerNetBIOSName = rdr.IsDBNull(4) ? "" : rdr.GetString(4);  /* ComputerNamePhysicalNetBIOS is NULL for AzureDB */
                }
            }
            return connectionInfo;
        }
    }
}