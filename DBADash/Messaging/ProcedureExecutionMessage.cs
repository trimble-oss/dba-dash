using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;

namespace DBADash.Messaging
{
    public class ProcedureExecutionMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        public CommandNames CommandName { get; set; }

        public List<CustomSqlParameter> Parameters { get; set; }

        public enum CommandNames
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
            sp_HumanEvents
        }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle)
        {
            ThrowIfExpired();
            if (cfg.AllowedScripts == null || (!cfg.AllowedScripts.Split(",").Contains(CommandName.ToString()) && cfg.AllowedScripts != "*"))
            {
                Log.Error("Command {CommandName} is not allowed.  Use the service configuration tool to enable access", CommandName);
                throw new Exception($"Command {CommandName} is not allowed.  Use the service configuration tool to enable access");
            }
            try
            {
                using var op = Operation.Begin("Running {CommandName} on {instance} triggered from message {handle}",
                    CommandName,
                    ConnectionID,
                    handle);
                var src = cfg.GetSourceConnection(ConnectionID);

                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(CommandName.ToString(), cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Lifetime };
                if (Parameters != null)
                    cmd.Parameters.AddRange(Parameters.GetParameters());
                var da = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                da.Fill(ds);

                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running {CommandName} on {instance} from message {handle}", CommandName, ConnectionID, handle);
                throw;
            }
        }
    }
}