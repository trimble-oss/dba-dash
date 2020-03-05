using System;
using System.ComponentModel;
using System.Data.SqlClient;

namespace DBAChecksService
{


    public class CollectionConfig
    {

        public enum ConnectionType
        {
            SQL,
            Directory,
            AWSS3,
            Invalid
        }

        private string myString = "g&hAs2&mVOLwE6DqO!I5";
        private string _source;
        private string _destination;
        private bool wasEncryptionPerformed = false;
        private bool isEncrypted = false;

        public CollectionConfigSchedule[] Schedules { get; set; }


        public CollectionConfigSchedule[] GetSchedule()
        {
            if (Schedules == null)
            {
                if (SourceConnectionType() == ConnectionType.AWSS3 || SourceConnectionType() == ConnectionType.Directory)
                {
                    return CollectionConfigSchedule.DefaultImportSchedule();
                }
                else
                {
                    return CollectionConfigSchedule.DefaultSchedules();
                }
            }
            else
            {
                return Schedules;
            }
        }

        public bool Validate()
        {
            return ValidateDestination() && ValidateSource();
        }

        private bool validate(string connectionString, ConnectionType typ)
        {
            if (typ == ConnectionType.Directory)
            {
                if (System.IO.Directory.Exists(connectionString) == false)
                {
                    return false;
                }
            }
            if (typ == ConnectionType.SQL)
            {
                if (validateSQLConnection(connectionString) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public bool ValidateSource()
        {
            return validate(_source, SourceConnectionType());
        }
        public bool ValidateDestination()
        {
            return validate(_destination, DestinationConnectionType());
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

        public ConnectionType SourceConnectionType()
        {
            return getConnectionType(_source);
        }

        public ConnectionType DestinationConnectionType()
        {
            return getConnectionType(_destination);
        }

     
        // Note if source is SQL connection string, password is encrypted.  Use GetSource() to return with real password
        public string Source
        {
            get
            {
                return _source;
            }
            set
            {
                 _source = getConnectionStringWithEncryptedPassword(value);   
            }
        }

        // Note if destination is SQL connection string, password is encrypted.  Use GetDestination() to return with real password
        public string Destination
        {
            get
            {
                return _destination;
            }
            set
            {
                   _destination = getConnectionStringWithEncryptedPassword(value);
            }
        }

        // Return if encryption was performed when source/destination was set.  e.g. User supplied connection string with password that we need to encrypt and save back encrypted
        public bool WasEncrypted()
        {
            return wasEncryptionPerformed;
        }

        // Return if source or destination have an encrypted password.  
        public bool IsEncrypted()
        {
            return isEncrypted;
        }

        public string GetSource()
        {
            return getDecryptedConnectionString(_source);
        }
        public string GetDestination()
        {
            return getDecryptedConnectionString(_destination);
        }

        public string AWSProfile { get; set; }



        public string GenerateFileName()
        {
            return "DBAChecks_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + ".json";
        }

        [DefaultValue(false)]
        public bool NoWMI { get; set; }

        public CollectionConfig(string source, string destination)
        {
            this.Source = source;
            this.Destination = destination;
        }
        public CollectionConfig()
        {

        }

        private ConnectionType getConnectionType(string connectionString)
        {
            if (connectionString == null || connectionString.Length<3)
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
                if (builder.Password.StartsWith("¬=!"))
                {
                    builder.Password = EncryptText.DecryptString(builder.Password.Substring(3), myString);
                    return builder.ConnectionString;
                }
                else
                {
                    return connectionString;
                }
            }
            else
            {
                return connectionString;
            }
        }

    }


}
