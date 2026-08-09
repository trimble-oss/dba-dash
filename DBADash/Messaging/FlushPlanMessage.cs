using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Flushes a single cached plan from the plan cache on the source instance via a targeted
    /// <c>DBCC FREEPROCCACHE(&lt;plan_handle&gt;)</c> (not a cache-wide flush).  Guarded on the service side: plan
    /// flushing reuses the plan-forcing gate (<see cref="CollectionConfig.AllowPlanForcing"/>) and must be explicitly
    /// enabled.  The plan handle is validated as a well-formed varbinary literal before it is inlined into the DBCC
    /// statement (FREEPROCCACHE takes a literal, not a parameter, so it can't be parameterised).  A short command
    /// timeout is used so a blocked DBCC can't hang the service.
    /// </summary>
    public class FlushPlanMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        /// <summary>The plan handle to flush, as the 0x… varbinary literal from the RunningQueries snapshot.</summary>
        public string PlanHandle { get; set; }

        /// <summary>The session the plan was flushed from (for logging/audit context only).</summary>
        public int SessionID { get; set; }

        // Short timeout - a targeted FREEPROCCACHE should be near instant. 
        private const int FlushCommandTimeoutSeconds = 5;

        // A valid plan_handle is a varbinary literal: 0x followed by an even number of hex digits (up to 64 bytes).
        private static readonly Regex PlanHandlePattern = new("^0x([0-9A-Fa-f]{2})+$", RegexOptions.Compiled);

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            if (!cfg.AllowPlanForcing)
            {
                throw new Exception("Plan forcing is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }

            if (string.IsNullOrEmpty(PlanHandle) || !PlanHandlePattern.IsMatch(PlanHandle) || PlanHandle.Length > 130)
            {
                throw new Exception($"'{PlanHandle}' is not a valid plan handle.");
            }

            using var op = Operation.Begin(
                "Flush plan {planHandle} (session {session}) on {instance} triggered from message {id} with handle {handle}",
                PlanHandle,
                SessionID,
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                // FREEPROCCACHE can't take a variable/parameter - the (already validated) plan handle is inlined.
                await using var cmd = new SqlCommand($"DBCC FREEPROCCACHE({PlanHandle});", cn)
                { CommandType = CommandType.Text, CommandTimeout = FlushCommandTimeoutSeconds };

                await cn.OpenAsync(cancellationToken);
                await using var registration = cancellationToken.Register(() => cmd.Cancel());
                try
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
                finally
                {
                    registration.Unregister();
                }

                op.Complete();

                var ds = new DataSet();
                var dt = new DataTable();
                dt.Columns.Add("PlanHandle", typeof(string));
                dt.Columns.Add("Flushed", typeof(bool));
                dt.Columns.Add("Message", typeof(string));
                dt.Rows.Add(PlanHandle, true, "Plan flushed from cache.");
                ds.Tables.Add(dt);
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error flushing plan {planHandle} (session {session}) on {instance} from message {id} with handle {handle}",
                    PlanHandle, SessionID, ConnectionID, Id, handle);
                throw;
            }
        }
    }
}
