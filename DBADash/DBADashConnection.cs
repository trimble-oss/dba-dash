using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;

namespace DBADash
{
    public class DBADashConnection
    {
        private readonly List<int> supportedEngineEditions = new() { 1, 2, 3, 4, 5, 8 }; // Personal, Standard, Enterprise, Express, Azure DB, Azure MI
        private readonly List<int> supportedProductVersions = new() { 9, 10, 11, 12, 13, 14, 15, 16, 17 }; // SQL 2005 to 2025 & Azure

        public enum ConnectionType
        {
            SQL,
            Directory,
            AWSS3,
            Invalid
        }


        private bool wasEncryptionPerformed;
        private bool isEncrypted;
        private string encryptedConnectionString = "";
        private string connectionString = "";
        private ConnectionType connectionType;

        public bool WasEncrypted => wasEncryptionPerformed;

        public bool IsEncrypted => isEncrypted;

        /// <summary>
        /// Set if the password couldn't be decrypted, e.g. it was protected with DPAPI on another machine.  The connection string is used without a password.
        /// </summary>
        [JsonIgnore]
        public string SecretError { get; private set; }

        public bool IsIntegratedSecurity { private set; get; }

        public string UserName { private set; get; }

        public DBADashConnection(string connectionString)
        {
            SetConnectionString(connectionString);
        }

        public DBADashConnection()
        {
        }

        private void SetConnectionString(string value)
        {
            SecretError = null;
            if (GetConnectionType(value) == ConnectionType.SQL)
            {
                var builder = new SqlConnectionStringBuilder(value)
                {
                    ApplicationName = "DBADash"
                };
                IsIntegratedSecurity = builder.IntegratedSecurity || builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                UserName = builder.UserID;
#pragma warning disable CS0618 // ActiveDirectoryPassword is deprecated but still supported
                // A User ID without a password is also how a user-assigned managed identity is specified
                usesPassword = !IsIntegratedSecurity && builder.Authentication is SqlAuthenticationMethod.NotSpecified or SqlAuthenticationMethod.SqlPassword
                    or SqlAuthenticationMethod.ActiveDirectoryPassword or SqlAuthenticationMethod.ActiveDirectoryServicePrincipal;
#pragma warning restore CS0618
                value = builder.ToString();
            }
            encryptedConnectionString = GetConnectionStringWithEncryptedPassword(value);
            connectionString = GetDecryptedConnectionString(value);
            connectionType = GetConnectionType(value);
            protectedPassword = connectionType == ConnectionType.SQL ? new SqlConnectionStringBuilder(encryptedConnectionString).Password : "";
        }

        // Password from encryptedConnectionString - saves parsing it again each time the config is serialized
        private string protectedPassword = "";

        internal bool HasStoredPassword => protectedPassword.Length > 0;

        // The authentication method takes a password - SQL authentication, Entra Password or Entra Service Principal
        private bool usesPassword;

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
                if (Directory.Exists(connectionString) == false)
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

        /// <summary>
        /// True when the connection metadata has already been retrieved and cached, so platform
        /// checks (e.g. <see cref="IsAzureDB()"/>) can be answered without opening a connection.
        /// </summary>
        [JsonIgnore]
        public bool IsConnectionInfoKnown => _connectionInfo != null;

        /// <summary>
        /// Asynchronously determines whether the connection targets Azure SQL Database.
        /// Uses cached connection metadata when available; otherwise opens a connection off the
        /// calling thread. Throws if the connection metadata can't be retrieved.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> IsAzureDBAsync()
        {
            if (_connectionInfo == null && connectionType == ConnectionType.SQL)
            {
                _connectionInfo = await ConnectionInfo.GetConnectionInfoAsync(connectionString);
            }
            return ConnectionInfo.IsAzureDB;
        }

        private void ValidateSQLConnection()
        {
            if (!supportedProductVersions.Contains(ConnectionInfo.MajorVersion))
            {
                throw new Exception(
                    $"SQL Server Version {ConnectionInfo.MajorVersion} isn't supported by DBA Dash.  For testing purposes, it's possible to skip this validation check.");
            }
            if (!supportedEngineEditions.Contains(ConnectionInfo.EngineEditionValue))
            {
                throw new Exception(
                    $"SQL Server Engine Edition {ConnectionInfo.EngineEditionValue} isn't supported by DBA Dash.  For testing purposes, it's possible to skip this validation check.");
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

        public ApplicationIntent? ApplicationIntent()
        {
            if (connectionType == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new(connectionString);
                return builder.ApplicationIntent;
            }
            else
            {
                return null;
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
            else if (connectionString.StartsWith("s3://") || connectionString.StartsWith("https://") || connectionString.StartsWith("http://"))
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

        private string GetConnectionStringWithEncryptedPassword(string _connectionString)
        {
            if (GetConnectionType(_connectionString) == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new(_connectionString);

                if (SecretProtection.IsProtected(builder.Password))
                {
                    isEncrypted = true;
                    return _connectionString;
                }
                else if (builder.Password.Length > 0)
                {
                    builder.Password = SecretProtection.Protect(builder.Password);
                    wasEncryptionPerformed = true;
                    isEncrypted = true;
                    return builder.ConnectionString;
                }
                else
                {
                    return _connectionString;
                }
            }
            else
            {
                return _connectionString;
            }
        }

        private string GetDecryptedConnectionString(string _connectionString)
        {
            if (GetConnectionType(_connectionString) == ConnectionType.SQL)
            {
                SqlConnectionStringBuilder builder = new(_connectionString);
                if (builder.ApplicationName == ".Net SqlClient Data Provider")
                {
                    builder.ApplicationName = "DBADash";
                }
                if (SecretProtection.IsProtected(builder.Password))
                {
                    try
                    {
                        builder.Password = SecretProtection.Unprotect(builder.Password);
                    }
                    catch (SecretUnprotectException ex)
                    {
                        SecretError = ex.Message;
                        builder.Password = "";
                    }
                }
                return builder.ConnectionString;
            }
            else
            {
                return _connectionString;
            }
        }

        /// <summary>
        /// Replace the password.  It's protected as it's set.
        /// </summary>
        public void SetPassword(string password)
        {
            SetConnectionString(new SqlConnectionStringBuilder(encryptedConnectionString) { Password = password }.ConnectionString);
        }

        /// <summary>
        /// True for a SQL connection that has a password (or uses SQL authentication) - one <see cref="SetPassword"/> applies to
        /// </summary>
        [JsonIgnore]
        public bool HasPassword => connectionType == ConnectionType.SQL && (protectedPassword.Length > 0 || SecretError != null || (!string.IsNullOrEmpty(UserName) && usesPassword));

        /// <summary>
        /// Re-protect the password with <paramref name="key"/> (or DPAPI/obfuscation if null) if it isn't already in that format
        /// </summary>
        /// <param name="key">Key to protect with</param>
        /// <param name="seen">Protected passwords already used by other connections.  If this connection's protected password is a duplicate
        /// (e.g. the connection was copied) it's re-protected so connections with the same password don't have the same value.</param>
        internal void ProtectSecret(ConfigKey key, HashSet<string> seen)
        {
            ProtectSecret(key);
            if (connectionType != ConnectionType.SQL || SecretError != null) return;
            if (protectedPassword.Length == 0 || seen.Add(protectedPassword)) return;
            ProtectSecret(key, force: true);
            seen.Add(protectedPassword);
        }

        internal void ProtectSecret(ConfigKey key, bool force = false)
        {
            if (!force && !SecretNeedsProtection(key)) return;
            var builder = new SqlConnectionStringBuilder(encryptedConnectionString)
            {
                Password = SecretProtection.Protect(new SqlConnectionStringBuilder(connectionString).Password, key)
            };
            encryptedConnectionString = builder.ConnectionString;
            protectedPassword = builder.Password;
            isEncrypted = true;
        }

        /// <summary>
        /// True if the password isn't in the format <see cref="ProtectSecret(ConfigKey, bool)"/> would produce.  False for secrets that can't be decrypted, as they can't be re-protected.
        /// </summary>
        internal bool SecretNeedsProtection(ConfigKey key)
        {
            if (connectionType != ConnectionType.SQL || SecretError != null) return false;
            return protectedPassword.Length > 0 && !SecretProtection.IsProtectedWith(protectedPassword, key);
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
                    Regex r = new($"[{Regex.Escape(regexSearch)}]");
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