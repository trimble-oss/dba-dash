using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Force-stops and drops the ad-hoc XE session on a monitored instance, regardless of whether a trace is
    /// actively being processed.  This is the recovery path for an <b>abandoned</b> trace - e.g. the service
    /// restarted or the GUI closed, leaving the <c>DBADash_AdHoc</c> session running on the source and blocking new
    /// traces.  Unlike <see cref="CancellationMessage"/> (which cancels a live in-process message by id), this
    /// works even when nothing is tracking the session in memory.
    /// </summary>
    public class XETraceStopMessage : MessageBase
    {
        public string ConnectionID { get; set; }

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

            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);
            var scope = info.IsAzureDB ? "ON DATABASE" : "ON SERVER";
            var catalog = info.IsAzureDB ? "sys.database_event_sessions" : "sys.server_event_sessions";

            // Session name is a fixed, validated constant - safe to inline.  DROP works on a started or stopped
            // session, so no separate STOP is needed; a running in-process trace ends when its reader then fails.
            var sql = $"IF EXISTS(SELECT 1 FROM {catalog} WHERE name = N'{XETraceMessage.SessionName}') " +
                      $"DROP EVENT SESSION [{XETraceMessage.SessionName}] {scope};";

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            await cn.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            Log.Information("Ad-hoc XE session {session} stopped/removed on {instance} (handle {handle})",
                XETraceMessage.SessionName, ConnectionID, handle);

            var dt = new DataTable("StopResult");
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("Message", typeof(string));
            dt.Rows.Add(ConnectionID,
                $"Ad-hoc XE session removed on {ConnectionID} (if one existed).");
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }
    }
}
