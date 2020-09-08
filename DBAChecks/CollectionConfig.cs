using DBAChecksService;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DBAChecks
{

    public class CollectionConfig
    {
        private string _secretKey;
        private bool wasEncryptionPerformed = false;
        private bool isEncrypted = false;
        private string myString = "g&hAs2&mVOLwE6DqO!I5";
        public SchemaSnapshotDBOptions SchemaSnapshotOptions=null;
        public bool BinarySerialization { get; set; } = false;

        public List<DBAChecksSource> SourceConnections = new List<DBAChecksSource>();

        public CollectionConfig()
        {
            DestinationConnection = new DBAChecksConnection();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,

            });
        }

        public static CollectionConfig Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<CollectionConfig>(json);
        }

        public string AWSProfile { get; set; }

        public string AccessKey { get; set; }
        public string SecretKey
        {
            get
            {
                return _secretKey;
            }
            set
            {
                if (value == "")
                {
                    _secretKey = null;
                }
                else if (value == null || value.StartsWith("¬=!"))
                {
                    _secretKey = value;
                }
                else
                {
                    _secretKey = "¬=!" + EncryptText.EncryptString(value, myString);
                    wasEncryptionPerformed = true;
                }
            }
        }


        public string GetSecretKey()
        {

            if (_secretKey != null && _secretKey.StartsWith("¬=!"))
            {
                return EncryptText.DecryptString(_secretKey.Substring(3), myString);
            }
            else
            {
                return _secretKey;
            }

        }

        [JsonIgnore]
        public DBAChecksConnection DestinationConnection { get; set; }

        private string defaultMaintenanceCron = " 0 0 0 ? * * *";

        public string MaintenanceScheduleCron { get; set; }

        public string GetMaintenanceCron()
        {
            if (MaintenanceScheduleCron == null || MaintenanceScheduleCron == "")
            {
                return defaultMaintenanceCron;
            }
            else
            {
                return MaintenanceScheduleCron;
            }
        }


        // Note if destination is SQL connection string, password is encrypted.  Use GetDestination() to return with real password
        public string Destination
        {
            get
            {
                return DestinationConnection.EncryptedConnectionString;
            }
            set
            {
                DestinationConnection = new DBAChecksConnection(value);
            }
        }

        public bool WasEncrypted()
        {
            if (wasEncryptionPerformed)
            {
                return wasEncryptionPerformed;
            }
            if (DestinationConnection.WasEncrypted)
            {
                wasEncryptionPerformed = true;
            }
            else
            {
                foreach (var c in SourceConnections)
                {
                    if (c.SourceConnection.WasEncrypted)
                    {
                        wasEncryptionPerformed = true;
                    }
                }
            }
            return wasEncryptionPerformed;
        }
    }
}