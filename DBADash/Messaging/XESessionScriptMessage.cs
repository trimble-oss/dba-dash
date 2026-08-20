using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.XEventDbScoped;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Returns the <c>CREATE EVENT SESSION</c> DDL for an <b>existing</b> extended-events session on a monitored
    /// instance, so the GUI can show a session's definition.  Read-only.  Requires <see cref="CollectionConfig.AllowManageXE"/>.
    ///
    /// <para>Uses SMO's XEvent management API (<see cref="XEStore"/> / <see cref="DatabaseXEStore"/>) to script the
    /// session, which produces the exact, SSMS-equivalent DDL (field quoting, predicates, targets and WITH options are
    /// all handled by SMO).  Scope-aware: <see cref="XEStore"/> server-scoped on-prem / Managed Instance,
    /// <see cref="DatabaseXEStore"/> database-scoped for Azure SQL Database.</para>
    /// </summary>
    public class XESessionScriptMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public string SessionName { get; set; }

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

            var src = await cfg.GetSourceConnectionAsync(ConnectionID);
            if (src == null)
            {
                throw new Exception($"Source connection '{ConnectionID}' not found.");
            }
            var connectionString = src.SourceConnection.ConnectionString;
            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);

            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(cancellationToken);

            // SMO is synchronous - run it off the async path.  Returns null when the session doesn't exist.
            var ddl = await Task.Run(() =>
            {
                var storeConnection = new SqlStoreConnection(cn);
                if (info.IsAzureDB)
                {
                    var store = new DatabaseXEStore(storeConnection);
                    return store.Sessions[SessionName]?.ScriptCreate().ToString();
                }
                else
                {
                    var store = new XEStore(storeConnection);
                    return store.Sessions[SessionName]?.ScriptCreate().ToString();
                }
            }, cancellationToken);

            var dt = new DataTable("Script");
            dt.Columns.Add("Ddl", typeof(string));
            if (!string.IsNullOrEmpty(ddl)) dt.Rows.Add(ddl);
            var ds = new DataSet();
            ds.Tables.Add(dt);

            Log.Information("Scripted XE session {session} for {instance} (handle {handle})",
                SessionName, ConnectionID, handle);
            return ds;
        }
    }
}
