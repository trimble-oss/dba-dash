using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DBADash.Messaging
{
    public class CancellationMessage : MessageBase
    {
        public Guid CancelMessageId { get; set; }

        public override Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            Log.Information("Cancellation requested for message {id}", CancelMessageId);
            CancellationTokenManager.TryCancel(CancelMessageId);
            return Task.FromResult(new DataSet());
        }
    }
}