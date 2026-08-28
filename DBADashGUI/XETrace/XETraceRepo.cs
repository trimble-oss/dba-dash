using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Repository data-access for ad-hoc XE traces.  All persistence is done from the GUI (which always has a repo
    /// connection) - the collecting service may not, e.g. in the S3/SQS relay topology - mirroring how plan forcing
    /// is logged (<see cref="DBADashGUI.Messaging.MessagingHelper"/>).
    /// </summary>
    internal static class XETraceRepo
    {
        public static async Task<long> StartAsync(int instanceID, Guid messageGroup, string eventTypes,
            int maxDurationSeconds, string filtersJson, Guid? runGroupID = null, string notes = null)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("XE.XETraceSession_Start", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
            cmd.Parameters.AddWithValue("@EventTypes", eventTypes);
            cmd.Parameters.AddWithValue("@MaxDurationSeconds", maxDurationSeconds);
            cmd.Parameters.AddWithValue("@FiltersJson", (object)filtersJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RunGroupID", (object)runGroupID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
            var pId = cmd.Parameters.Add("@XETraceSessionID", SqlDbType.BigInt);
            pId.Direction = ParameterDirection.Output;
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            return Convert.ToInt64(pId.Value);
        }

        public static async Task AddEventsAsync(long sessionID, DataTable events)
        {
            if (events == null || events.Rows.Count == 0) return;
            var tvp = BuildEventTvp(events);
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("XE.XETraceSession_AddEvents", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            var p = cmd.Parameters.AddWithValue("@Events", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "XE.XETraceEvents";
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Projects the dynamic (variable-column) event table into the fixed TVP shape: event_type + timestamp
        /// promoted, and all remaining event-specific fields serialized to a JSON object.  This is what lets the
        /// repo store an open-ended set of events without a fixed column schema.
        /// </summary>
        private static DataTable BuildEventTvp(DataTable events)
        {
            var tvp = new DataTable();
            tvp.Columns.Add("event_type", typeof(string));
            tvp.Columns.Add("timestamp", typeof(DateTime));
            tvp.Columns.Add("Fields", typeof(string));

            var hasEventType = events.Columns.Contains("event_type");
            var hasTimestamp = events.Columns.Contains("timestamp");

            foreach (DataRow row in events.Rows)
            {
                var fields = new Dictionary<string, object>();
                foreach (DataColumn c in events.Columns)
                {
                    if (c.ColumnName is "event_type" or "timestamp") continue;
                    if (row[c] != DBNull.Value) fields[c.ColumnName] = row[c];
                }
                var eventType = hasEventType && row["event_type"] != DBNull.Value ? row["event_type"] : "";
                // timestamp is NOT NULL in the TVP / table (every event has a non-nullable timestamp_utc); backstop
                // with now (UTC) in the rare case the shredder could not produce one.
                object timestamp = hasTimestamp && row["timestamp"] != DBNull.Value ? row["timestamp"] : DateTime.UtcNow;
                tvp.Rows.Add(eventType, timestamp, JsonConvert.SerializeObject(fields));
            }
            return tvp;
        }

        /// <summary>
        /// Records the service-generated DDL and resolved target on a still-Running session, as soon as the "trace
        /// running" reply arrives - so the audit DDL is durable even if the trace is later force-cancelled (Stop) or
        /// abandoned before its completion summary lands.  A no-op once the row is terminal.
        /// </summary>
        public static async Task SetDefinitionAsync(long sessionID, string generatedDDL, byte? targetType)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            // No @Status: a definition-only update that records the DDL / target without terminating the trace.
            await using var cmd = new SqlCommand("XE.XETraceSession_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            cmd.Parameters.AddWithValue("@GeneratedDDL", (object)generatedDDL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TargetType", (object)targetType ?? DBNull.Value);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task CompleteAsync(long sessionID, byte status, byte? targetType, string generatedDDL,
            byte[] xelData, string errorMessage)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("XE.XETraceSession_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@TargetType", (object)targetType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GeneratedDDL", (object)generatedDDL ?? DBNull.Value);
            // Type VARBINARY explicitly: AddWithValue(DBNull) infers NVarChar, and nvarchar -> varbinary(max) is a
            // disallowed implicit conversion (fails even when the value is NULL).
            cmd.Parameters.Add("@XelData", SqlDbType.VarBinary, -1).Value = (object)xelData ?? DBNull.Value;
            cmd.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task CancelRunningAsync(int instanceID)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("XE.XETraceSession_CancelRunning", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Soft-deletes a persisted trace (Trace History report's Delete link): removes the captured event data + .xel
        /// but retains the session row (flagged deleted) for audit, until retention hard-deletes it.
        /// </summary>
        public static async Task DeleteAsync(long sessionID)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await cn.OpenAsync();
            await DeleteCoreAsync(cn, sessionID);
        }

        /// <summary>
        /// Soft-deletes several traces (the report's bulk "Delete Selected" / "Delete All" actions) over a single
        /// connection.  Each is deleted independently via <c>XE.XETraceSession_Del</c>.
        /// </summary>
        public static async Task DeleteManyAsync(IEnumerable<long> sessionIDs)
        {
            var ids = sessionIDs?.ToList();
            if (ids == null || ids.Count == 0) return;
            await using var cn = new SqlConnection(Common.ConnectionString);
            await cn.OpenAsync();
            foreach (var id in ids)
            {
                await DeleteCoreAsync(cn, id);
            }
        }

        /// <summary>Executes the soft-delete proc for one session on an already-open connection.  Ownership and the
        /// DeletedBy audit value are enforced/captured server-side (SUSER_SNAME()), so no identity is passed.</summary>
        private static async Task DeleteCoreAsync(SqlConnection cn, long sessionID)
        {
            await using var cmd = new SqlCommand("XE.XETraceSession_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Sets/updates the free-text note on a persisted trace (Trace History report's editable Notes link).  A blank
        /// note is stored as NULL.  Ownership is enforced server-side (<c>XE.XETraceSession_Notes_Upd</c> rejects editing
        /// another user's trace unless the caller is db_owner), matching the delete link.
        /// </summary>
        public static async Task UpdateNotesAsync(long sessionID, string notes)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("XE.XETraceSession_Notes_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            cmd.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static Task<DataTable> GetRunningAsync(IEnumerable<int> instanceIDs) =>
            FillAsync("XE.XETraceSession_GetRunning", cmd =>
                cmd.Parameters.AddWithValue("@InstanceIDs", instanceIDs.AsDataTable()));

        /// <summary>
        /// Returns the other monitored instances that share an availability group with <paramref name="instanceID"/>
        /// (columns InstanceID, InstanceName), so the trace UI can offer to trace every AG replica at once.
        /// Empty when the instance isn't in an AG or has no other monitored replicas.
        /// </summary>
        public static Task<DataTable> GetAgInstancesAsync(int instanceID) =>
            FillAsync("dbo.AvailabilityGroupInstances_Get", cmd => cmd.Parameters.AddWithValue("@InstanceID", instanceID));

        /// <summary>
        /// Recent trace history for the QuickXETrace dropdown.  The proc scopes results to the caller's own traces
        /// server-side (RequestedBy = SUSER_SNAME()), so no requester needs to be passed - the full cross-user view
        /// is the Trace History report.
        /// </summary>
        public static Task<DataTable> GetHistoryAsync(IEnumerable<int> instanceIDs, int days) =>
            FillAsync("XE.XETraceSession_Get", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstanceIDs", instanceIDs.AsDataTable());
                cmd.Parameters.AddWithValue("@Days", days);
            });

        public static Task<DataTable> GetEventsAsync(long sessionID) =>
            FillAsync("XE.XETraceEvents_Get", cmd => cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID));

        /// <summary>
        /// Returns the merged events of every per-instance session of a multi-instance run (each event's source
        /// instance is carried inside its Fields JSON), in time order.  Used to reload an AG-wide trace as one grid.
        /// </summary>
        public static Task<DataTable> GetEventsByRunGroupAsync(Guid runGroupID) =>
            FillAsync("XE.XETraceEvents_GetByRunGroup", cmd => cmd.Parameters.AddWithValue("@RunGroupID", runGroupID));

        public static async Task<byte[]> GetXelAsync(long sessionID)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("XE.XETraceSession_GetXel", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            await cn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : (byte[])result;
        }

        private static async Task<DataTable> FillAsync(string procName, Action<SqlCommand> addParams)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand(procName, cn) { CommandType = CommandType.StoredProcedure };
            addParams(cmd);
            await cn.OpenAsync();
            var dt = new DataTable();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }
    }
}
