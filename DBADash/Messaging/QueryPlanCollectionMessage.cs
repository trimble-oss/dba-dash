using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public class QueryPlanCollectionMessage : MessageBase
    {
        public List<Plan> PlansToCollect { get; set; }

        public string ConnectionID { get; set; }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            using var op = Operation.Begin(
                "GetPlans from {instance} triggered from message {handle}",
                ConnectionID,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);
                var dt = await Plan.GetPlansAsync(PlansToCollect, src.SourceConnection.ConnectionString);
                var ds = new DataSet();
                ds.Tables.Add(dt);
                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing plan collection message");
                throw;
            }
        }
    }
}