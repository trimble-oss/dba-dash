namespace DBADash.Messaging
{
    /// <summary>
    /// Shared timing for the ad-hoc XE trace heartbeat (the GUI→service keep-alive).  Kept in one place, referenced by
    /// both the service (which enforces the timeout, see <see cref="XETraceMessage"/>) and the GUI (which sends the
    /// beats), so the two can't drift apart.
    ///
    /// <para>Every trace is monitored: the GUI sends a beat every <see cref="IntervalSeconds"/>; the service stops a
    /// trace after <see cref="TimeoutSeconds"/> of silence (the client is presumed gone).  The timeout is deliberately
    /// several intervals, so a single missed beat - a transient hiccup or an SQS relay delay - doesn't kill a healthy
    /// trace.  A trace shorter than the timeout is safe with no beats at all: it finishes before it could go stale.</para>
    /// </summary>
    public static class XETraceHeartbeat
    {
        /// <summary>How often the GUI sends a heartbeat while a trace runs.</summary>
        public const int IntervalSeconds = 30;

        /// <summary>How long the service waits without a heartbeat before stopping the trace.</summary>
        public const int TimeoutSeconds = 90;
    }
}
