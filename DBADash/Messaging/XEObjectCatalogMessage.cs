using DBADash.XE;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Returns the extended-events catalog for a monitored instance - the capturable events, each event's data
    /// columns, and the global actions - so the GUI can populate the event/field pickers and validate a request.
    /// Read-only metadata; the GUI caches it per SQL Server version.  See <see cref="XEObjectCatalog"/>.
    /// </summary>
    public class XEObjectCatalogMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        private const string CatalogSql = @"
SELECT p.name AS package_name, o.name AS event_name, o.description
FROM sys.dm_xe_objects o
JOIN sys.dm_xe_packages p ON o.package_guid = p.guid
WHERE o.object_type = 'event' AND (o.capabilities IS NULL OR (o.capabilities & 1) = 0)
ORDER BY o.name;

SELECT p.name AS package_name, oc.object_name AS event_name, oc.name AS field_name, oc.type_name
FROM sys.dm_xe_object_columns oc
JOIN sys.dm_xe_packages p ON oc.object_package_guid = p.guid
WHERE oc.column_type = 'data'
AND EXISTS (SELECT 1 FROM sys.dm_xe_objects o
            WHERE o.name = oc.object_name AND o.object_type = 'event' AND o.package_guid = oc.object_package_guid);

SELECT p.name AS package_name, o.name AS field_name, o.type_name
FROM sys.dm_xe_objects o
JOIN sys.dm_xe_packages p ON o.package_guid = p.guid
WHERE o.object_type = 'pred_source' AND (o.capabilities IS NULL OR (o.capabilities & 1) = 0)
ORDER BY o.name;

SELECT p.name AS package_name, o.name AS field_name, o.type_name
FROM sys.dm_xe_objects o
JOIN sys.dm_xe_packages p ON o.package_guid = p.guid
WHERE o.object_type = 'action' AND (o.capabilities IS NULL OR (o.capabilities & 1) = 0)
ORDER BY o.name;

SELECT p.name AS package_name, oc.object_name AS event_name, oc.name AS field_name, oc.type_name,
       oc.column_value AS default_value
FROM sys.dm_xe_object_columns oc
JOIN sys.dm_xe_packages p ON oc.object_package_guid = p.guid
WHERE oc.column_type = 'customizable'
AND EXISTS (SELECT 1 FROM sys.dm_xe_objects o
            WHERE o.name = oc.object_name AND o.object_type = 'event' AND o.package_guid = oc.object_package_guid);";

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (!cfg.AllowAdhocXE)
            {
                throw new Exception(
                    "Ad-hoc XE tracing is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            var src = await cfg.GetSourceConnectionAsync(ConnectionID);
            if (src == null)
            {
                throw new Exception($"Source connection '{ConnectionID}' not found.");
            }
            var connectionString = src.SourceConnection.ConnectionString;

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(CatalogSql, cn)
            { CommandType = CommandType.Text, CommandTimeout = Lifetime };
            await cn.OpenAsync(cancellationToken);
            var ds = new DataSet();
            var da = new SqlDataAdapter(cmd);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            da.Fill(ds);

            if (ds.Tables.Count >= 3)
            {
                ds.Tables[0].TableName = "Events";
                ds.Tables[1].TableName = "EventFields";
                ds.Tables[2].TableName = "PredSources"; // global predicate sources usable in the WHERE clause
            }
            if (ds.Tables.Count >= 4)
            {
                ds.Tables[3].TableName = "Actions"; // global actions capturable as "global fields"
            }
            if (ds.Tables.Count >= 5)
            {
                ds.Tables[4].TableName = "Customizations"; // per-event customizable columns (SET toggles)
            }
            Log.Information("Returned XE catalog for {instance}: {events} events (handle {handle})",
                ConnectionID, ds.Tables.Contains("Events") ? ds.Tables["Events"].Rows.Count : 0, handle);
            return ds;
        }
    }
}
