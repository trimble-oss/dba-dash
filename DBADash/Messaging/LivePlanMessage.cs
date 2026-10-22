using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Gets the live query plan (actual row counts etc. so far) for a request that is still running, from
    /// sys.dm_exec_query_statistics_xml.  The plan is only returned if the same request is still running on the session -
    /// the request whose start time matches the snapshot, so a reused SPID or another request on a MARS session doesn't
    /// return a plan for a different query.<br/>
    /// Needs lightweight query profiling: on by default from SQL 2019, trace flag 7412 on SQL 2016 SP1 and 2017.  Without it
    /// a plan is only available if the query was started with SET STATISTICS XML/PROFILE ON or an XE session capturing
    /// query_post_execution_showplan was running.<br/>
    /// The plan isn't stored in the repository - it's a point in time view of a request in progress.
    /// </summary>
    public class LivePlanMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public int SessionID { get; set; }

        /// <summary>The request start time (UTC) from the snapshot.  Compared with the live value to confirm it's the same request.</summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// The instance UTC offset, in minutes, collected for the snapshot (<c>DATEDIFF</c> style, added to local time to
        /// get UTC).  A live start time may differ by exactly the change in this offset since the snapshot - a daylight
        /// saving change - and by nothing else.
        /// </summary>
        public int SnapshotUtcOffset { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            using var op = Operation.Begin(
                "Get live plan for session {session} on {instance} triggered from message {id} with handle {handle}",
                SessionID,
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(SqlStrings.GetSqlString("LivePlan"), cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = SessionID;
                cmd.Parameters.Add("@ExpectedStartUtc", SqlDbType.DateTime).Value = StartTimeUtc;
                cmd.Parameters.Add("@SnapshotUtcOffset", SqlDbType.Int).Value = SnapshotUtcOffset;

                await cn.OpenAsync(cancellationToken);
                var ds = new DataSet();
                var dt = new DataTable("LivePlan");
                ds.Tables.Add(dt);
                await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    dt.Load(reader);
                }

                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting live plan for session {session} on {instance} from message {id} with handle {handle}",
                    SessionID, ConnectionID, Id, handle);
                throw;
            }
        }
    }
}
