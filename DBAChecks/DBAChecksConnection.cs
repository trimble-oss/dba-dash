using DBAChecksService;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;

namespace DBAChecks
{
    public class DBAChecksConnection
    {

        public enum ConnectionType
        {
            SQL,
            Directory,
            AWSS3,
            Invalid
        }
        private string myString = "g&hAs2&mVOLwE6DqO!I5";
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

        public DBAChecksConnection(string connectionString)
        {
            setConnectionString(connectionString);
        }
        public DBAChecksConnection()
        {

        }

        private void setConnectionString(string value)
        {
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



        public bool Validate()
        {

            if (connectionType == ConnectionType.Directory)
            {
                if (System.IO.Directory.Exists(connectionString) == false)
                {
                    return false;
                }
            }
            if (connectionType == ConnectionType.SQL)
            {
                if (validateSQLConnection(connectionString) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsAzureDB()
        {
            if (connectionType == ConnectionType.SQL)
            {
                SqlConnection cn = new SqlConnection(connectionString);
                try
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('EditionID') AS BIGINT)", cn);
                    Int64 editionID = (Int64)cmd.ExecuteScalar();
                    if (editionID == 1674378470)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        private bool validateSQLConnection(string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            try
            {
                cn.Open();
                return true;
            }
            catch
            {
                return false;
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
                    builder.ApplicationName = "DBAChecks";
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

    }

}
