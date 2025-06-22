using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public class ProcedureExecutionMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public CommunityProcs? CommunityProc
        {
            get => Enum.TryParse<CommunityProcs>(ProcedureName, true, out var result) ? (CommunityProcs?)result : null;
            set => ProcedureName = value.ToString();
        }

        public List<CustomSqlParameter> Parameters { get; set; }

        public string ProcedureName { get; set; }

        public string SchemaName { get; set; } = "dbo";

        private string QualifiedProcedureName => SchemaName.SqlQuoteName() + '.' + ProcedureName.SqlQuoteName();

        public enum CommunityProcs
        {
            sp_WhoIsActive,
            sp_Blitz,
            sp_BlitzFirst,
            sp_BlitzIndex,
            sp_BlitzCache,
            sp_BlitzWho,
            sp_LogHunter,
            sp_PressureDetector,
            sp_BlitzLock,
            sp_BlitzBackups,
            sp_HumanEvents,
            sp_HealthParser,
            sp_QuickieStore,
            sp_HumanEventsBlockViewer,
            sp_SrvPermissions,
            sp_DBPermissions,
            sp_IndexCleanup
        }

        public static bool IsAllowedCommunityProc(CommunityProcs? proc, string allowedCommunityProcs) => proc != null && allowedCommunityProcs != null && (allowedCommunityProcs.Split(",").Contains(proc.ToString(), StringComparer.OrdinalIgnoreCase) || allowedCommunityProcs == "*");

        public static bool IsAllowedCustomProc(string schema, string proc, string allowedCustomScripts)
        {
            if (string.IsNullOrEmpty(allowedCustomScripts))
                return false;

            var allowedScripts = allowedCustomScripts
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(script => script.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return allowedScripts.Contains($"{schema.SqlQuoteName()}.{proc.SqlQuoteName()}") ||
                   allowedScripts.Contains($"{schema}.{proc}") ||
                   allowedScripts.Contains(proc);
        }

        public bool IsAllowedProc(CollectionConfig cfg) =>
            IsAllowedCustomProc(SchemaName, ProcedureName, cfg.AllowedCustomProcs) ||
            IsAllowedCommunityProc(CommunityProc, cfg.AllowedScripts);

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (!IsAllowedProc(cfg))
            {
                Log.Error("Command {Proc} is not allowed.  Use the service configuration tool to enable access", ProcedureName);
                throw new Exception($"Command {ProcedureName} is not allowed.  Use the service configuration tool to enable access");
            }
            try
            {
                using var op = Operation.Begin("Running {Proc} on {instance} triggered from message {id} with handle {handle}",
                    QualifiedProcedureName,
                    ConnectionID,
                    Id,
                    handle);
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(QualifiedProcedureName, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Lifetime };
                if (Parameters != null)
                    cmd.Parameters.AddRange(Parameters.GetParameters());
                var da = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                await using var registration = cancellationToken.Register(() =>
                {
                    cmd.Cancel();
                });
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
                Log.Error(ex, "Error running {CommunityProc} on {instance} from message {id} with handle {handle}", CommunityProc, ConnectionID, Id, handle);
                throw;
            }
        }
    }
}