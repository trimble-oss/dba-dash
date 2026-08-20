using Microsoft.SqlServer.XEvent.XELite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.XE
{
    /// <summary>
    /// Streams a running XE session's events <b>live</b> via XELite's public <see cref="XELiveEventStreamer"/> (which
    /// runs <c>sys.fn_MSxe_read_event_stream(@session, 0)</c> under the covers) and delivers them to the caller in
    /// batches.  Unlike reading a target (event_file / ring_buffer), this taps the live event stream directly - no
    /// per-event XML conversion, no target to manage, real-time, and it stops as soon as the streaming connection is
    /// cancelled.  A single live session has one clock, so none of the per-file metadata handling the captured-file
    /// reader needs applies here - XELite parses the stream into <see cref="IXEvent"/>s for us.
    ///
    /// <para>The streamer invokes its handler once per event; we accumulate and flush a shredded batch on whichever
    /// comes first - a count threshold or a time interval - so the GUI gets batches, not a message per event.</para>
    /// </summary>
    public sealed class XELiveTraceReader
    {
        private readonly string _connectionString;
        private readonly string _sessionName;
        private readonly int _batchSize;
        private readonly TimeSpan _batchInterval;

        public XELiveTraceReader(string connectionString, string sessionName, int batchSize, TimeSpan batchInterval)
        {
            _connectionString = connectionString;
            _sessionName = sessionName;
            _batchSize = batchSize > 0 ? batchSize : 500;
            _batchInterval = batchInterval > TimeSpan.Zero ? batchInterval : TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Streams the session until <paramref name="ct"/> is cancelled, invoking <paramref name="onBatch"/> with each
        /// shredded batch of events (and a final partial batch when the stream ends).
        /// </summary>
        public async Task StreamAsync(Func<DataTable, Task> onBatch, CancellationToken ct)
        {
            var streamer = new XELiveEventStreamer(_connectionString, _sessionName);
            var buffer = new List<IXEvent>(_batchSize);
            var lastFlush = DateTime.UtcNow;

            async Task FlushAsync()
            {
                if (buffer.Count == 0) return;
                var batch = XELiteShredder.Build(buffer);
                buffer.Clear();
                lastFlush = DateTime.UtcNow;
                await onBatch(batch).ConfigureAwait(false);
            }

            HandleXEvent onEvent = async ev =>
            {
                buffer.Add(ev);
                if (buffer.Count >= _batchSize || DateTime.UtcNow - lastFlush >= _batchInterval)
                {
                    await FlushAsync().ConfigureAwait(false);
                }
            };

            try
            {
                // ReadEventStream runs until the connection is cancelled; a live session never ends on its own, so it
                // ultimately throws (cancellation, or its own "reader aborted") - both mean "we're done streaming".
                await streamer.ReadEventStream(onEvent, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on stop / duration cap / heartbeat loss.
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                // The cancelled streaming query can surface as a SqlException rather than OCE - treat as a clean stop.
            }
            finally
            {
                // Flush any events buffered since the last batch (best-effort - the request is over).
                try { await FlushAsync().ConfigureAwait(false); } catch { /* ignore on teardown */ }
            }
        }
    }
}
