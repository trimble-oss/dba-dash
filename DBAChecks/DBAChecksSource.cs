using DBAChecksService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DBAChecks.DBAChecksConnection;

namespace DBAChecks
{
    public class DBAChecksSource
    {
        private Int32 slowQueryThresholdMs=-1;

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


        [JsonIgnore]
        public DBAChecksConnection SourceConnection { get; set; }


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
                SourceConnection = new DBAChecksConnection(value);
            }
        }

        public string GenerateFileName()
        {
            return "DBAChecks_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + ".json";
        }

        [DefaultValue(false)]
        public bool NoWMI { get; set; }

        [DefaultValue(-1)]
        public Int32 SlowQueryThresholdMs { 
            get { return slowQueryThresholdMs; }
            set { slowQueryThresholdMs = value; }
        }
        [DefaultValue(false)]
        public bool PersistXESessions { get; set; }

        public DBAChecksSource(string source)
        {
            this.ConnectionString = source;
        }
        public DBAChecksSource()
        {

        }


    }

}
