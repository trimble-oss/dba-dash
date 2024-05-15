using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DBADash
{
    public class DBADashConnection
    {
        private readonly List<int> supportedEngineEditions = new() { 1, 2, 3, 4, 5, 8 }; // Personal, Standard, Enterprise, Express, Azure DB, Azure MI
        private readonly List<int> supportedProductVersions = new() { 9, 10, 11, 12, 13, 14, 15, 16 }; // SQL 2005 to 2022 & Azure

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
        private string _hash;

        public bool WasEncrypted => wasEncryptionPerformed;

        public bool IsEncrypted => isEncrypted;

        public DBADashConnection(string connectionString)
        {
            SetConnectionString(connectionString);
        }

        public DBADashConnection()
        {
        }

        private void SetConnectionString(string value)
        {
            if (GetConnectionType(value) == ConnectionType.SQL)
            {
                var builder = new SqlConnectionStringBuilder(value)
                {
                    ApplicationName = "DBADash"
                };
                value = builder.ToString();
            }
            encryptedConnectionString = GetConnectionStringWithEncryptedPassword(value);
            connectionString = GetDecryptedConnectionString(value);
            connectionType = GetConnectionType(value);
            _hash = EncryptText.GetHash(connectionString);
        }

        [JsonIgnore]
        public string ConnectionString
        {
            get => connectionString;
            set => SetConnectionString(value);
        }

        [JsonIgnore]
        public string MasterConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString)
                {
                    InitialCatalog = "master"
                };
                return builder.ToString();
            }
        }

        public DBADashConnection MasterConnection() => new(MasterConnectionString);

        public string EncryptedConnectionString
        {
            get => encryptedConnectionString;
            set => SetConnectionString(value);
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
                ValidateSQLConnection(); // Open a connection to the DB
            }
        }

        public bool IsXESupported()
        {
            return connectionType == ConnectionType.SQL && ConnectionInfo.IsXESupported;
        }

        public static bool IsXESupported(string productVersion)
        {
            return ConnectionInfo.GetXESupported(productVersion);
        }

        public bool IsAzureDB()
        {
            return ConnectionInfo.IsAzureDB;
        }

        private void ValidateSQLConnection()
        {
            if (!supportedProductVersions.Contains(ConnectionInfo.MajorVersion))
            {
                throw new Exception(string.Format("SQL Server Version {0} isn't supported by DBA Dash.  For testing purposes, it's possible to skip this validation check.", ConnectionInfo.MajorVersion));
            }
            if (!supportedEngineEditions.Contains(ConnectionInfo.EngineEditionValue))
            {
                throw new Exception(string.Format("SQL Server Engine Edition {0} isn't supported by DBA Dash.  For testing purposes, it's possible to skip this validation check.", ConnectionInfo.EngineEditionValue));
            }
        }

        public ConnectionType Type => connectionType;

        public string InitialCatalog()
        {
            if (connectionType == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new(connectionString);
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
                SqlConnectionStringBuilder builder = new(connectionString);
                return builder.DataSource;
            }
            else
            {
                return "";
            }
        }

        private static ConnectionType GetConnectionType(string connectionString)
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
                    SqlConnectionStringBuilder builder = new(connectionString);
                    return ConnectionType.SQL;
                }
                catch
                {
                    return ConnectionType.Invalid;
                }
            }
        }

        private string GetConnectionStringWithEncryptedPassword(string connectionString)
        {
            if (GetConnectionType(connectionString) == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new(connectionString);

                if (builder.Password.StartsWith("¬=!"))
                {
                    isEncrypted = true;
                    return connectionString;
                }
                else if (builder.Password.Length > 0)
                {
                    builder.Password = "¬=!" + builder.Password.EncryptString(myString);
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

        private string GetDecryptedConnectionString(string connectionString)
        {
            if (GetConnectionType(connectionString) == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new(connectionString);
                if (builder.ApplicationName == ".Net SqlClient Data Provider")
                {
                    builder.ApplicationName = "DBADash";
                }
                if (builder.Password.StartsWith("¬=!"))
                {
                    builder.Password = builder.Password[3..].DecryptString(myString);
                }
                return builder.ConnectionString;
            }
            else
            {
                return connectionString;
            }
        }

        [JsonIgnore]
        public string Hash => EncryptText.GetHash(ConnectionString);

        public string ConnectionForFileName
        {
            get
            {
                if (Type == ConnectionType.SQL)
                {
                    SqlConnectionStringBuilder builder = new(connectionString);
                    string connection = builder.DataSource + (builder.InitialCatalog == "" ? "" : "_" + builder.InitialCatalog);
                    string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                    Regex r = new(string.Format("[{0}]", Regex.Escape(regexSearch)));
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
                    SqlConnectionStringBuilder builder = new(connectionString);
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