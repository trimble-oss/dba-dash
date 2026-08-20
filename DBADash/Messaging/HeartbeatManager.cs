using System;
using System.Collections.Concurrent;
using Serilog;

namespace DBADash.Messaging
{
    /// <summary>
    /// Tracks the last heartbeat received from the GUI for long-running messages (an ad-hoc XE trace).  The GUI sends
    /// a <see cref="HeartbeatMessage"/> periodically while a trace runs; if the beats stop (the GUI crashed, was
    /// killed, or lost connectivity) the trace's own loop notices the entry has gone stale and stops itself, so a
    /// long trace can't run on unattended after the client that started it has gone away.
    ///
    /// <para>In-memory per-process, exactly like <see cref="CancellationTokenManager"/> - a heartbeat only <b>keeps
    /// a trace alive</b>, so if a beat lands on the wrong process (e.g. a duplicate service) the real trace-owner
    /// simply sees silence and stops.  Missing beats can only ever stop a trace, never prolong one.</para>
    /// </summary>
    public static class HeartbeatManager
    {
        private static readonly ConcurrentDictionary<Guid, DateTime> Beats = new();

        /// <summary>Starts monitoring the message: records an initial beat so it isn't immediately considered stale.</summary>
        public static void Register(Guid id) => Beats[id] = DateTime.UtcNow;

        /// <summary>Records a heartbeat for a monitored message.  Ignored for messages that aren't being monitored.</summary>
        public static void Beat(Guid id)
        {
            if (Beats.ContainsKey(id))
            {
                Beats[id] = DateTime.UtcNow;
                Log.Debug("Heartbeat received for message {id}", id);
            }
            else
            {
                Log.Debug("Heartbeat received for message {id}, but it is not being monitored", id);
            }
        }

        /// <summary>
        /// True when the message is monitored and no beat has arrived within <paramref name="timeout"/> - the client
        /// is presumed gone and the operation should stop.  False for unmonitored messages (never expires).
        /// </summary>
        public static bool IsExpired(Guid id, TimeSpan timeout) =>
            Beats.TryGetValue(id, out var last) && DateTime.UtcNow - last > timeout;

        /// <summary>Stops monitoring the message (called when the operation ends, however it ends).</summary>
        public static void Remove(Guid id) => Beats.TryRemove(id, out _);
    }
}
