using DBADashService;
using Newtonsoft.Json;
using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using Serilog;
using System.Collections.Generic;

namespace DBADash
{
    public class DBADashConnection
    {

        private List<int> supportedEngineEditions = new List<int> { 1, 2, 3, 4, 5, 8 }; // Personal, Standard, Enterprise, Express, Azure DB, Azure MI
        private List<int> supportedProductVersions = new List<int> { 9, 10, 11, 12, 13, 14, 15 }; // SQL 2005 to 2019 & Azure

        public enum ConnectionType
        {
            SQL,
            Directory,
            AWSS3,
            Invalid
        }
        private readonly string myString = "g&hAs2&mVOLwE6DqO!I5";
        private bool wasEncryptionPerformed = false;
        private bool isEncrypted = false;
        private string encryptedConnectionString = "";
        private string connectionString = "";
        private ConnectionType connectionType;

        public bool WasEncrypted
        {
            get
            {
                return wasEncryptionPerformed;
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return isEncrypted;
            }
        }

        public DBADashConnection(string connectionString)
        {
            setConnectionString(connectionString);
        }
        public DBADashConnection()
        {

        }

        private void setConnectionString(string value)
        {
            if (getConnectionType(value) == ConnectionType.SQL){
                var builder = new SqlConnectionStringBuilder(value)
                {
                    ApplicationName = "DBADash"
                };
                value = builder.ToString();
            }
            encryptedConnectionString = getConnectionStringWithEncryptedPassword(value);
            connectionString = getDecryptedConnectionString(value);
            connectionType = getConnectionType(value);
        }

        [JsonIgnore]
        public string ConnectionString
        {
            get
            {
                return connectionString;
            }
            set
            {
                setConnectionString(value);
            }
        }

        [JsonIgnore]
        public string MasterConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                builder.InitialCatalog = "master";
                return builder.ToString();
            }
        }

        public DBADashConnection MasterConnection()
        {
            return new DBADashConnection(MasterConnectionString);
        }

        public string EncryptedConnectionString
        {
            get
            {
                return encryptedConnectionString;
            }
            set
            {
                setConnectionString(value);
            }
        }

        private ConnectionInfo _connectionInfo;

        [JsonIgnore]
        public ConnectionInfo ConnectionInfo
        {
            get
            {
                if (_connectionInfo == null && connectionType == ConnectionType.SQL)
                {
                    _connectionInfo = ConnectionInfo.GetConnectionInfo(connectionString);
                }
                return _connectionInfo;
            }
        }

        public void Validate()
        {

            if (connectionType == ConnectionType.Directory)
            {
                if (System.IO.Directory.Exists(connectionString) == false)
                {
                    throw new Exception("Directory does not exist");
                }
            }
           else if (connectionType == ConnectionType.SQL)
            {
                validateSQLConnection(); // Open a connection to the DB
            }
        }


        public bool IsXESupported()
        {
            if (connectionType == ConnectionType.SQL)
            {
                return ConnectionInfo.IsXESupported;
            }
            else
            {
                return false;   
            }
        }

        public static bool IsXESupported(string productVersion)
        {
           return ConnectionInfo.GetXESupported(productVersion);
        }

        public bool IsAzureDB()
        {
            return ConnectionInfo.IsAzureDB;
        }


        private void validateSQLConnection()
        {         
            if (!supportedProductVersions.Contains(ConnectionInfo.MajorVersion)){
                throw new Exception(string.Format("SQL Server Version {0} isn't supported by DBA Dash.  For testing purposes, it's possible to skip this validation check.",ConnectionInfo.MajorVersion));
            }
            if(!supportedEngineEditions.Contains(ConnectionInfo.EngineEditionValue)){
                throw new Exception(string.Format("SQL Server Engine Edition {0} isn't supported by DBA Dash.  For testing purposes, it's possible to skip this validation check.", ConnectionInfo.EngineEditionValue));
            }       
        }

        public ConnectionType Type
        {
            get
            {
                return connectionType;
            }
        }

        public string InitialCatalog()
        {
            if (connectionType == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                return builder.InitialCatalog;
            }
            else
            {
                return "";
            }
        }

        public string DataSource()
        {
            if (connectionType == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                return builder.DataSource;
            }
            else
            {
                return "";
            }
        }

        private ConnectionType getConnectionType(string connectionString)
        {
            if (connectionString == null || connectionString.Length < 3)
            {
                return ConnectionType.Invalid;
            }
            else if (connectionString.StartsWith("s3://") || connectionString.StartsWith("https://"))
            {
                return ConnectionType.AWSS3;
            }
            else if (connectionString.StartsWith("\\\\") || connectionString.StartsWith("//") || connectionString.Substring(1, 2) == ":\\")
            {
                return ConnectionType.Directory;
            }
            else
            {
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                    return ConnectionType.SQL;
                }
                catch
                {
                    return ConnectionType.Invalid;
                }
            }
        }

        private string getConnectionStringWithEncryptedPassword(string connectionString)
        {
            if (getConnectionType(connectionString) == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

                if (builder.Password.StartsWith("¬=!"))
                {
                    isEncrypted = true;
                    return connectionString;
                }
                else if (builder.Password.Length > 0)
                {
                    builder.Password = "¬=!" + EncryptText.EncryptString(builder.Password, myString);
                    wasEncryptionPerformed = true;
                    isEncrypted = true;
                    return builder.ConnectionString;
                }
                else
                {
                    return connectionString; ;
                }
            }
            else
            {
                return connectionString;
            }
        }


        private string getDecryptedConnectionString(string connectionString)
        {
            if (getConnectionType(connectionString) == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                if (builder.ApplicationName == ".Net SqlClient Data Provider")
                {
                    builder.ApplicationName = "DBADash";
                }
                if (builder.Password.StartsWith("¬=!"))
                {
                    builder.Password = EncryptText.DecryptString(builder.Password.Substring(3), myString);
                }
                return builder.ConnectionString;
            }
            else
            {
                return connectionString;
            }
        }

        public string ConnectionForFileName
        {
            get
            {
                if (Type == ConnectionType.SQL)
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                    string connection = builder.DataSource + (builder.InitialCatalog == "" ? "" : "_" + builder.InitialCatalog);
                    string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                    Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                    return r.Replace(connection, "");
                }
                else
                {
                    throw new Exception("Invalid connection type for filename generation");
                }
            }
        }

        public string ConnectionForPrint
        {
            get
            {
                if (Type == ConnectionType.SQL)
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                    return builder.DataSource + (builder.InitialCatalog == "" ? "" : "|" + builder.InitialCatalog);
                }
                else
                {
                    return connectionString;
                }
            
            }
        }

    }

}
