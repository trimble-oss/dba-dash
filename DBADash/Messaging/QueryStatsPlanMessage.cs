using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Fetches the plan of a query stats row from the plan cache, for a plan shape the collection did not capture a
    /// plan for - past its cap, or with plan capture switched off.
    ///
    /// The repository keeps no cache entry for a row, because an ad hoc shape's is a different variant almost every
    /// interval, so this can't go straight to the plan the way <see cref="QueryPlanCollectionMessage"/> does.  It
    /// finds an entry under the row's plan shape first - see SQLQueryStatsPlan.sql - then fetches its plan the same
    /// way.  Returns the table that message returns, with no rows when the plan is no longer cached.
    /// </summary>
    public class QueryStatsPlanMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public byte[] QueryPlanHash { get; set; }

        /// <summary>The statement's handle, found with its offsets.  Null for an ad hoc shape, which has a handle per
        /// variant and is found by <see cref="QueryHash"/> instead.</summary>
        public byte[] SqlHandle { get; set; }

        public int StatementStart { get; set; }

        public int StatementEnd { get; set; }

        /// <summary>The query hash an ad hoc shape is found by.  Null for any other statement.</summary>
        public byte[] QueryHash { get; set; }

        /// <summary>The statement's database, where it is known.  The same text run in two databases is two
        /// statements, each with its own plans.</summary>
        public string DatabaseName { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (QueryPlanHash is not { Length: 8 })
            {
                throw new ArgumentException("No plan hash supplied", nameof(QueryPlanHash));
            }
            if (SqlHandle is not { Length: > 0 } && QueryHash is not { Length: > 0 })
            {
                throw new ArgumentException("Neither a sql handle nor a query hash was supplied");
            }
            using var op = Operation.Begin(
                "Get query stats plan from {instance} triggered from message {id} with handle {handle}",
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);
                var connectionString = src.SourceConnection.ConnectionString;

                Plan entry = null;
                await using (var cn = new SqlConnection(connectionString))
                await using (var cmd = new SqlCommand(SqlStrings.GetSqlString("QueryStatsPlan"), cn) { CommandTimeout = Lifetime })
                {
                    cmd.Parameters.Add("@QueryPlanHash", SqlDbType.Binary, 8).Value = QueryPlanHash;
                    cmd.Parameters.Add("@SqlHandle", SqlDbType.VarBinary, 64).Value = (object)SqlHandle ?? DBNull.Value;
                    cmd.Parameters.Add("@StatementStart", SqlDbType.Int).Value = StatementStart;
                    cmd.Parameters.Add("@StatementEnd", SqlDbType.Int).Value = StatementEnd;
                    cmd.Parameters.Add("@QueryHash", SqlDbType.Binary, 8).Value = (object)QueryHash ?? DBNull.Value;
                    cmd.Parameters.Add("@DatabaseName", SqlDbType.NVarChar, 128).Value = (object)DatabaseName ?? DBNull.Value;
                    await cn.OpenAsync(cancellationToken);
                    await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                    if (await rdr.ReadAsync(cancellationToken))
                    {
                        entry = new Plan((byte[])rdr["plan_handle"], QueryPlanHash, rdr.GetInt32(1), rdr.GetInt32(2));
                    }
                }

                var ds = new DataSet();
                ds.Tables.Add(entry == null
                    ? new DataTable("QueryPlans")
                    : await Plan.GetPlansAsync(new List<Plan> { entry }, connectionString));
                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting query stats plan from {instance} from message {id} with handle {handle}",
                    ConnectionID, Id, handle);
                throw;
            }
        }
    }
}
