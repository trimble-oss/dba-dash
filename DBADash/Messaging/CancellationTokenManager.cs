using System;
using System.Collections.Concurrent;
using System.Threading;
using Serilog;

namespace DBADash.Messaging
{
    public static class CancellationTokenManager
    {
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> Tokens = new();

        // Cancellations that arrived before the operation registered its token - e.g. over the SQS relay a Stop can
        // overtake the slower Start (or be processed while the Start is still queued), so TryCancel runs before Add.
        // Recording the request here lets a late Add honour it immediately, instead of the trace starting and running
        // orphaned on the server after the client has already given up.  Timestamped so stale entries can be pruned.
        private static readonly ConcurrentDictionary<Guid, DateTime> PreCancelled = new();

        // How long a pre-emptive cancellation is retained for a not-yet-registered operation.  Comfortably longer than
        // any realistic Start/Stop reordering window, while still bounding memory if a matching Add never arrives.
        private static readonly TimeSpan PreCancelRetention = TimeSpan.FromMinutes(10);

        // Adds a CancellationTokenSource associated with a Guid (Id of the message)
        public static void Add(Guid Id, CancellationTokenSource cts)
        {
            Tokens.TryAdd(Id, cts);
            // Honour a cancellation that raced ahead of this registration (Stop processed before Start) so the
            // operation is cancelled the moment it starts rather than running orphaned.
            if (PreCancelled.TryRemove(Id, out _))
            {
                Log.Information("Applying pre-emptive cancellation for message {id} registered after its cancel request", Id);
                cts.Cancel();
            }
            PrunePreCancelled();
        }

        // Attempts to cancel the operation associated with the message Id
        public static bool TryCancel(Guid Id)
        {
            if (!Tokens.TryRemove(Id, out var cts))
            {
                // The operation hasn't registered its token yet (e.g. its Start message is still in-flight/queued over
                // the SQS relay).  Record the request so a subsequent Add cancels immediately rather than the trace
                // starting and running orphaned.
                PreCancelled[Id] = DateTime.UtcNow;
                PrunePreCancelled();
                Log.Warning("Cancellation requested for message {id}, but no token found - recorded as a pre-emptive cancellation", Id);
                return false;
            }
            Log.Information("Cancellation trigger for message {id}", Id);
            cts.Cancel();

            return true;
        }

        // Removes the entry associated with the given Guid
        public static void Remove(Guid Id)
        {
            Tokens.TryRemove(Id, out _);
        }

        // Drops pre-emptive cancellations whose matching operation never registered within the retention window.
        private static void PrunePreCancelled()
        {
            if (PreCancelled.IsEmpty) return;
            var cutoff = DateTime.UtcNow - PreCancelRetention;
            foreach (var kvp in PreCancelled)
            {
                if (kvp.Value < cutoff)
                {
                    PreCancelled.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}