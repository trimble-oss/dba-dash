using System;
using System.Collections.Concurrent;
using System.Threading;
using Serilog;

namespace DBADash.Messaging
{
    public static class CancellationTokenManager
    {
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> Tokens = new();

        // Adds a CancellationTokenSource associated with a Guid (Id of the message)
        public static void Add(Guid Id, CancellationTokenSource cts)
        {
            Tokens.TryAdd(Id, cts);
        }

        // Attempts to cancel the operation associated with the message Id
        public static bool TryCancel(Guid Id)
        {
            if (!Tokens.TryRemove(Id, out var cts))
            {
                Log.Warning("Cancellation requested for message {id}, but no token found", Id);
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
    }
}