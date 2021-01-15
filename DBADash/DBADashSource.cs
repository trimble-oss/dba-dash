using DBADashService;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class DBADashSource
    {
        private Int32 slowQueryThresholdMs = -1;

        public CollectionConfigSchedule[] Schedules { get; set; }

        public CollectionConfigSchedule[] GetSchedule()
        {
            if (Schedules == null)
            {
                if (SourceConnection.Type == ConnectionType.AWSS3 || SourceConnection.Type == ConnectionType.Directory)
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

        public string SchemaSnapshotCron;
        public string SchemaSnapshotDBs;
        public bool SchemaSnapshotOnServiceStart = true;


        [JsonIgnore]
        public DBADashConnection SourceConnection { get; set; }


        public string GetSource()
        {
            return SourceConnection.ConnectionString;
        }

        // Note if source is SQL connection string, password is encrypted.  Use GetSource() to return with real password
        public string ConnectionString
        {
            get
            {
                if (SourceConnection == null)
                {
                    return "";
                }
                else
                {
                    return SourceConnection.EncryptedConnectionString;
                }
            }
            set
            {
                SourceConnection = new DBADashConnection(value);
            }
        }

        public string GenerateFileName(bool binarySerialization)
        {
            return "DBADash_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + (binarySerialization ? ".bin" : ".json");
        }

        [DefaultValue(false)]
        public bool NoWMI { get; set; }

        [DefaultValue(-1)]
        public Int32 SlowQueryThresholdMs
        {
            get { return slowQueryThresholdMs; }
            set { slowQueryThresholdMs = value; }
        }
        [DefaultValue(false)]
        public bool PersistXESessions { get; set; }

        public DBADashSource(string source)
        {
            this.ConnectionString = source;
        }
        public DBADashSource()
        {

        }


    }

}
