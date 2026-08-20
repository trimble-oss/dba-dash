using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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
            int maxDurationSeconds, string filtersJson)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.XETraceSession_Start", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
            cmd.Parameters.AddWithValue("@RequestedBy", Environment.UserName);
            cmd.Parameters.AddWithValue("@EventTypes", eventTypes);
            cmd.Parameters.AddWithValue("@MaxDurationSeconds", maxDurationSeconds);
            cmd.Parameters.AddWithValue("@FiltersJson", (object)filtersJson ?? DBNull.Value);
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
            await using var cmd = new SqlCommand("dbo.XETraceSession_AddEvents", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID);
            var p = cmd.Parameters.AddWithValue("@Events", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.XETraceEvents";
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

        public static async Task CompleteAsync(long sessionID, byte status, byte? targetType, string generatedDDL,
            byte[] xelData, string errorMessage)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.XETraceSession_Complete", cn) { CommandType = CommandType.StoredProcedure };
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
            await using var cmd = new SqlCommand("dbo.XETraceSession_CancelRunning", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceID);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static Task<DataTable> GetRunningAsync(IEnumerable<int> instanceIDs) =>
            FillAsync("dbo.XETraceSession_GetRunning", cmd =>
                cmd.Parameters.AddWithValue("@InstanceIDs", instanceIDs.AsDataTable()));

        public static Task<DataTable> GetHistoryAsync(IEnumerable<int> instanceIDs, int days) =>
            FillAsync("dbo.XETraceSession_Get", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstanceIDs", instanceIDs.AsDataTable());
                cmd.Parameters.AddWithValue("@Days", days);
            });

        public static Task<DataTable> GetEventsAsync(long sessionID) =>
            FillAsync("dbo.XETraceEvents_Get", cmd => cmd.Parameters.AddWithValue("@XETraceSessionID", sessionID));

        public static async Task<byte[]> GetXelAsync(long sessionID)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.XETraceSession_GetXel", cn) { CommandType = CommandType.StoredProcedure };
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
