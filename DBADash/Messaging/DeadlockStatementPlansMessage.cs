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
    /// Lists the cached plans, and their execution stats, for a statement captured in a deadlock graph.
    ///
    /// A deadlock graph identifies statements by sql handle, so this can't reuse
    /// <see cref="QueryPlanCollectionMessage"/>, which goes straight to sys.dm_exec_text_query_plan with a
    /// plan handle.  It doesn't return plan XML either: the rows carry the plan handle and offsets that the
    /// existing plan collection path takes, so only the plan the user actually asks for gets fetched.
    /// </summary>
    public class DeadlockStatementPlansMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        /// <summary>The sqlhandle attribute from the deadlock graph's execution stack frame.</summary>
        public byte[] SqlHandle { get; set; }

        /// <summary>
        /// The frame's stmtstart offset.  Used only to flag which of the returned statements is the one that
        /// deadlocked - every statement under the handle is returned either way.
        /// </summary>
        public int StatementStart { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (SqlHandle is not { Length: > 0 })
            {
                throw new ArgumentException("No sql handle supplied", nameof(SqlHandle));
            }
            using var op = Operation.Begin(
                "Get deadlock statement plans from {instance} triggered from message {id} with handle {handle}",
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);
                var sql = SqlStrings.GetSqlString("DeadlockStatementPlans");

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@SqlHandle", SqlDbType.VarBinary, 64).Value = SqlHandle;
                cmd.Parameters.Add("@StatementStart", SqlDbType.Int).Value = StatementStart;

                var da = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                await using var registration = cancellationToken.Register(() => cmd.Cancel());
                try
                {
                    da.Fill(ds);
                }
                finally
                {
                    registration.Unregister();
                }
                if (ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "DeadlockStatementPlans";
                }

                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Error getting deadlock statement plans from {instance} from message {id} with handle {handle}",
                    ConnectionID, Id, handle);
                throw;
            }
        }
    }
}
