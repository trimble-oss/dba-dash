using Microsoft.SqlServer.XEvent.XELite;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Channels;
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
    /// <para>The streamer invokes its handler once per event; we hand each event to a single-consumer channel and
    /// accumulate on the consumer side, flushing a shredded batch on whichever comes first - a count threshold or a
    /// time interval - so the GUI gets batches, not a message per event.  The interval is driven by the consumer's own
    /// wait, so the tail of a bursty-then-idle workload still flushes on time even though no further events arrive to
    /// trigger it (the streamer's handler only runs when an event is delivered).</para>
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

            // The streamer's handler must return quickly and is called serially, so it just posts each event to the
            // channel.  A single consumer owns the buffer, so no locking is needed - the buffer has exactly one writer.
            //
            // The channel is bounded so a slow consumer (or a slow GUI push inside onBatch) can't grow memory without
            // limit.  When it fills we DROP the incoming event rather than block the handler: blocking it would stall
            // ReadEventStream, and the server session runs ALLOW_SINGLE_EVENT_LOSS, so it would shed events on its own
            // anyway - we keep the live tap draining and drop on our side instead.  The ItemDropped callback counts
            // what we shed so the consumer can log it.
            long dropped = 0;
            var channel = Channel.CreateBounded<IXEvent>(
                new BoundedChannelOptions(Math.Max(_batchSize * 16, 8192))
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.DropWrite
                },
                _ => Interlocked.Increment(ref dropped));

            HandleXEvent onEvent = ev =>
            {
                channel.Writer.TryWrite(ev);
                return Task.CompletedTask;
            };

            // A flush failure (the GUI report throwing) should tear the stream down, not be swallowed, so link a CTS the
            // consumer can trip to cancel ReadEventStream.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            async Task ConsumeAsync()
            {
                var reader = channel.Reader;
                var buffer = new List<IXEvent>(_batchSize);

                // Flush against a fixed deadline rather than a delay that restarts each loop.  A restarting delay only
                // fires after a full interval of *silence*, so a steady trickle that never reaches _batchSize would keep
                // resetting it and never flush on time; the deadline guarantees "count threshold OR time interval".
                var nextFlush = DateTime.UtcNow + _batchInterval;
                long reportedDrops = 0;

                async Task FlushAsync()
                {
                    nextFlush = DateTime.UtcNow + _batchInterval;
                    if (buffer.Count == 0) return;
                    var batch = XELiteShredder.Build(buffer);
                    buffer.Clear();
                    await onBatch(batch).ConfigureAwait(false);
                }

                void ReportDrops()
                {
                    var total = Interlocked.Read(ref dropped);
                    if (total <= reportedDrops) return;
                    Log.Warning(
                        "XE live trace on session {session}: dropped {count} event(s) - consumer could not keep up ({total} total this stream)",
                        _sessionName, total - reportedDrops, total);
                    reportedDrops = total;
                }

                try
                {
                    while (true)
                    {
                        // Wait for the next event or the flush deadline, whichever comes first.  The deadline branch is
                        // what flushes a partial tail when the workload has gone quiet - it does not depend on a further
                        // event arriving.
                        var waitTask = reader.WaitToReadAsync().AsTask();
                        var remaining = nextFlush - DateTime.UtcNow;
                        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                        var signalled = await Task.WhenAny(waitTask, Task.Delay(remaining)).ConfigureAwait(false);
                        if (signalled != waitTask)
                        {
                            await FlushAsync().ConfigureAwait(false); // flush deadline reached
                            ReportDrops();
                            continue;
                        }

                        if (!await waitTask.ConfigureAwait(false))
                        {
                            break; // writer completed and the channel is drained
                        }

                        while (reader.TryRead(out var ev))
                        {
                            buffer.Add(ev);
                            if (buffer.Count >= _batchSize)
                            {
                                await FlushAsync().ConfigureAwait(false);
                            }
                        }
                        ReportDrops();
                    }

                    // Flush the final partial batch once the writer has completed.
                    await FlushAsync().ConfigureAwait(false);
                    ReportDrops();
                }
                catch
                {
                    cts.Cancel(); // abort ReadEventStream so the request ends instead of hanging
                    throw;
                }
            }

            var consumer = ConsumeAsync();
            try
            {
                // ReadEventStream runs until the connection is cancelled; a live session never ends on its own, so it
                // ultimately throws (cancellation, or its own "reader aborted") - both mean "we're done streaming".
                await streamer.ReadEventStream(onEvent, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on stop / duration cap / heartbeat loss.
            }
            catch (Exception) when (cts.IsCancellationRequested)
            {
                // The cancelled streaming query can surface as a SqlException rather than OCE - treat as a clean stop.
            }
            finally
            {
                // Signal the consumer to drain whatever is queued, emit the final batch, and finish.  Its exceptions
                // (a failed flush) propagate here rather than being swallowed on teardown.
                channel.Writer.Complete();
                await consumer.ConfigureAwait(false);
            }
        }
    }
}
