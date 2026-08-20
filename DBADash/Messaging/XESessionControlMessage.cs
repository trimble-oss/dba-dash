using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>Start or stop an existing extended-events session.</summary>
    public enum XESessionOperation
    {
        Start,
        Stop
    }

    /// <summary>
    /// Starts or stops an <b>existing</b> extended-events session on a monitored instance
    /// (<c>ALTER EVENT SESSION ... STATE = START|STOP</c>).  Requires <see cref="CollectionConfig.AllowManageXE"/>.
    ///
    /// <para><b>Security</b>: the state keyword comes from the <see cref="XESessionOperation"/> enum (never user
    /// text), and the session name is escaped server-side with <c>QUOTENAME</c> before being inlined into the DDL
    /// (which can't take a variable), so any valid session name is handled without injection risk.</para>
    /// </summary>
    public class XESessionControlMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string SessionName { get; set; }

        public XESessionOperation Operation { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (!cfg.AllowManageXE)
            {
                throw new Exception(
                    "Managing extended events is not enabled on the DBA Dash service.  Use the service configuration tool to enable.");
            }
            if (string.IsNullOrWhiteSpace(SessionName))
            {
                throw new Exception("No session name was supplied.");
            }
            if (!cfg.CanManageXESession(SessionName))
            {
                throw new Exception(
                    $"Starting/stopping the extended-events session '{SessionName}' is not permitted by the DBA Dash " +
                    "service's manageable-sessions list.");
            }

            var src = await cfg.GetSourceConnectionAsync(ConnectionID);
            if (src == null)
            {
                throw new Exception($"Source connection '{ConnectionID}' not found.");
            }
            var connectionString = src.SourceConnection.ConnectionString;
            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);

            var scope = info.IsAzureDB ? "ON DATABASE" : "ON SERVER";
            var state = Operation == XESessionOperation.Start ? "START" : "STOP";
            var runningView = info.IsAzureDB ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";

            // QUOTENAME escapes the identifier (doubles ']' and wraps in brackets) so any valid session name is safe
            // to inline; the STATE keyword is a controlled constant.  Return the resulting running state.
            var sql =
                $"DECLARE @sql NVARCHAR(MAX) = N'ALTER EVENT SESSION ' + QUOTENAME(@name) + N' {scope} STATE = {state};';\n" +
                "EXEC sp_executesql @sql;\n" +
                $"SELECT @name AS Name, CAST(CASE WHEN EXISTS(SELECT 1 FROM {runningView} WHERE name = @name) THEN 1 ELSE 0 END AS bit) AS IsRunning;";

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = SessionName;
            await cn.OpenAsync(cancellationToken);
            var ds = new DataSet();
            var da = new SqlDataAdapter(cmd);
            await using var registration = cancellationToken.Register(() => cmd.Cancel());
            da.Fill(ds);
            if (ds.Tables.Count > 0) ds.Tables[0].TableName = "Result";

            Log.Information("XE session {session} {state} on {instance} (handle {handle})",
                SessionName, state, ConnectionID, handle);
            return ds;
        }
    }
}
