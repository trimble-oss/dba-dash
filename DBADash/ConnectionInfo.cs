using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public int MajorVersion
        {
            get
            {
                return GetMajorVersion(ProductVersion);
            }
        }

        public bool IsAzureMasterDB
        {
            get{
                return IsAzureDB && DatabaseName == "master";
            }
        }
        public bool IsAzureDB
        {
            get
            {
                return EngineEdition ==  DatabaseEngineEdition.SqlDatabase;
            }
        }

        public bool IsRDS
        {
            get
            {
                return ComputerNetBIOSName.StartsWith("EC2AMAZ-");
            }
        }

        public bool IsXESupported
        {
            get {
                if (IsRDS && !(EngineEdition == DatabaseEngineEdition.Standard || EngineEdition == DatabaseEngineEdition.Enterprise)) // Extended events only supported on Standard and Enterprise editions for RDS
                {
                    return false;
                }
                else
                {
                    return GetXESupported(MajorVersion);
                }
            }
        }

        public static int GetMajorVersion(string ProductVersion)
        {
            return Int32.Parse(ProductVersion.Substring(0, ProductVersion.IndexOf('.')));
        }

        public static bool GetXESupported(string ProductVersion)
        {
            return GetXESupported(GetMajorVersion(ProductVersion));
        }

        public static bool GetXESupported(int MajorVersion)
        {
            if (MajorVersion <= 10) // Note: Extended events added in SQL 2008 (10.*).  Batch completed not supported in this version & there are other differences like recording durations in ms instead of microseconds
            {
                return false;
            }
            else
            {
                return true;
            }
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
                    catch {
                        connectionInfo.EngineEdition = DatabaseEngineEdition.Unknown;
                    }
                    connectionInfo.ProductVersion = rdr.GetString(1);
                    connectionInfo.DatabaseName = rdr.GetString(2);
                    connectionInfo.ServerName = rdr.GetString(3);
                    connectionInfo.ComputerNetBIOSName = rdr.GetString(4);
                }
            }
            return connectionInfo;
        }
    }
}
