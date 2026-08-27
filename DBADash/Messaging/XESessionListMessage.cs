using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Lists the <b>existing</b> extended-events sessions defined on a monitored instance, with their running state,
    /// target types and event count - so the GUI's Extended Events node can show them and offer start/stop/watch.
    /// Read-only.  Requires <see cref="CollectionConfig.AllowManageXE"/> or <see cref="CollectionConfig.AllowWatchXE"/>
    /// (a manage-only user still needs to enumerate sessions to start/stop them).  Scope-aware: server-scoped views on-prem /
    /// Managed Instance, database-scoped views for Azure SQL Database.
    /// </summary>
    public class XESessionListMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        // Server-scoped query; the database-scoped equivalents are swapped in for Azure SQL DB.
        private const string ServerSql = @"
SELECT s.name AS Name,
       CAST(CASE WHEN r.name IS NULL THEN 0 ELSE 1 END AS bit) AS IsRunning,
       r.create_time AS StartTime,
       (SELECT COUNT(*) FROM sys.server_event_session_events e
        WHERE e.event_session_id = s.event_session_id) AS EventCount,
       STUFF((SELECT N',' + t.name FROM sys.server_event_session_targets t
              WHERE t.event_session_id = s.event_session_id ORDER BY t.name FOR XML PATH('')), 1, 1, N'') AS TargetTypes
FROM sys.server_event_sessions s
LEFT JOIN sys.dm_xe_sessions r ON r.name = s.name
ORDER BY s.name;";

        private static string DatabaseSql => ServerSql
            .Replace("sys.server_event_sessions", "sys.database_event_sessions")
            .Replace("sys.server_event_session_events", "sys.database_event_session_events")
            .Replace("sys.server_event_session_targets", "sys.database_event_session_targets")
            .Replace("sys.dm_xe_sessions", "sys.dm_xe_database_sessions");

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (!cfg.AllowManageXE && !cfg.AllowWatchXE)
            {
                throw new Exception(
                    "Viewing extended events is not enabled on the DBA Dash service.  Enable Manage XE or Watch XE in the service configuration tool.");
            }

            var src = await cfg.GetSourceConnectionAsync(ConnectionID);
            if (src == null)
            {
                throw new Exception($"Source connection '{ConnectionID}' not found.");
            }
            var connectionString = src.SourceConnection.ConnectionString;
            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(info.IsAzureDB ? DatabaseSql : ServerSql, cn)
            { CommandType = CommandType.Text, CommandTimeout = Lifetime };
            await cn.OpenAsync(cancellationToken);
            var ds = new DataSet();
            var da = new SqlDataAdapter(cmd);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            da.Fill(ds);
            if (ds.Tables.Count > 0) ds.Tables[0].TableName = "Sessions";

            Log.Information("Returned {count} XE sessions for {instance} (handle {handle})",
                ds.Tables.Count > 0 ? ds.Tables[0].Rows.Count : 0, ConnectionID, handle);
            return ds;
        }
    }
}
