using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DBADash.Messaging
{
    /// <summary>
    /// Sent periodically by the GUI while an ad-hoc XE trace is running to tell the service the client is still alive.
    /// Targets the running trace by its conversation group id (<see cref="TraceMessageId"/> = the trace message's
    /// <see cref="MessageBase.Id"/>), exactly as <see cref="CancellationMessage"/> does, and is processed immediately
    /// (before the concurrency semaphore) so a beat is never queued behind a busy service.
    /// </summary>
    public class HeartbeatMessage : MessageBase
    {
        /// <summary>The <see cref="MessageBase.Id"/> (conversation group) of the running trace this heartbeat is for.</summary>
        public Guid TraceMessageId { get; set; }

        public override Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            Log.Debug("Heartbeat for message {id}", TraceMessageId);
            HeartbeatManager.Beat(TraceMessageId);
            return Task.FromResult(new DataSet());
        }
    }
}
