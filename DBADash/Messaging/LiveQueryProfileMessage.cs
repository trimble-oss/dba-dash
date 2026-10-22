using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// The operator counters of a request that is still running, from sys.dm_exec_query_profiles - what the plan viewer
    /// needs to refresh a live plan it already has (see <see cref="LivePlanMessage"/>) without fetching the whole plan
    /// again.  A few small rows per operator rather than the showplan XML, so it is cheap enough to send every few
    /// seconds.<br/>
    /// Returns a Request table: Status is Running, Completed when the session is no longer running the request with the
    /// expected start time - an expected outcome when following a query, so not an error - or Moving when the request
    /// changed statement while the counters were read, so they couldn't be put down to one statement.  While it is
    /// running the table also identifies the statement, so the GUI can tell the request has moved on to another one and
    /// fetch its plan, and a Profiles table has the counters for each operator and thread.
    /// </summary>
    public class LiveQueryProfileMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public int SessionID { get; set; }

        /// <summary>The request start time (UTC) from the snapshot.  Compared with the live value to confirm it's the same request.</summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// The instance UTC offset, in minutes, collected for the snapshot.  See <see cref="LivePlanMessage.SnapshotUtcOffset"/>.
        /// </summary>
        public int SnapshotUtcOffset { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            // Not logged as a timed operation: it runs every few seconds while a live plan is open.
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(SqlStrings.GetSqlString("LiveQueryProfile"), cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = SessionID;
                cmd.Parameters.Add("@ExpectedStartUtc", SqlDbType.DateTime).Value = StartTimeUtc;
                cmd.Parameters.Add("@SnapshotUtcOffset", SqlDbType.Int).Value = SnapshotUtcOffset;

                await cn.OpenAsync(cancellationToken);
                var ds = new DataSet();
                await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    var request = new DataTable("Request");
                    request.Load(reader);
                    ds.Tables.Add(request);

                    if (!reader.IsClosed)
                    {
                        var profiles = new DataTable("Profiles");
                        profiles.Load(reader);
                        ds.Tables.Add(profiles);
                    }
                }

                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting live query profile for session {session} on {instance} from message {id} with handle {handle}",
                    SessionID, ConnectionID, Id, handle);
                throw;
            }
        }
    }
}
