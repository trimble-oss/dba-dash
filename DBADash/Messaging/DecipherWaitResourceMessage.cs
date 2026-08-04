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
    /// Deciphers a wait resource string on the source instance.  Runs a fixed embedded script that uses the
    /// documented sys.dm_db_page_info function (SQL 2019+) rather than the undocumented DBCC PAGE command.
    /// If the function isn't available the script returns a row with RequiresScript = 1 so the GUI can fall back
    /// to prompting the user to run the manual script.
    /// </summary>
    public class DecipherWaitResourceMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string WaitResource { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            using var op = Operation.Begin(
                "Decipher wait resource on {instance} triggered from message {id} with handle {handle}",
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);
                var sql = SqlStrings.GetSqlString("DecipherWaitResource");

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text, CommandTimeout = Lifetime };
                cmd.Parameters.Add("@WaitResource", SqlDbType.NVarChar, 256).Value = WaitResource ?? string.Empty;

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

                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deciphering wait resource on {instance} from message {id} with handle {handle}",
                    ConnectionID, Id, handle);
                throw;
            }
        }
    }
}
