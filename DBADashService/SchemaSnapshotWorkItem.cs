using DBADash;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    internal class SchemaSnapshotWorkItem : IWorkItem
    {
        public DBADashSource Source { get; set; }

        public string Schedule { get; set; }

        public string DedupKey => "SchemaSnapshot_" + Source.ConnectionString;

        public WorkItemPriority Priority => WorkItemPriority.Low;

        public string Description => $"Schema Snapshot {Source.ConnectionID} for databases {Source.SchemaSnapshotDBs} on schedule {Schedule}";

        public async Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken)
        {
            if (OfflineInstances.IsOffline(Source))
            {
                Log.Warning("Connection to {Connection} is offline.  Skipping schema snapshot", Source.ConnectionID ?? Source.SourceConnection.ConnectionForPrint);
                return;
            }
            await SchemaSnapshotDB.GenerateSchemaSnapshots(SchedulerServiceConfig.Config, Source);
        }
    }
}