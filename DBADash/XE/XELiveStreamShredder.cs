using Microsoft.SqlServer.XEvent.XELite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.XE
{
    /// <summary>
    /// Parses the buffer rows returned by <c>sys.fn_MSxe_read_event_stream</c> (the call SSMS uses) into events.
    /// XELite's public readers can't consume this directly, but its internal <c>XEEventStreamerBase.HandleBuffer</c>
    /// does - decompiling it shows the protocol, which we reach by reflection.  For each <c>(type, data)</c> row:
    /// <list type="bullet">
    ///   <item>A <c>LBHT_HEADER</c> row marks a new FILE boundary.  We do NOT parse it (its parse result is discarded
    ///   by XELite, and in file mode the row is truncated before its padded length); instead, at every header after
    ///   the first, we start a FRESH streamer.  Each file carries its own metadata AND clock calibration
    ///   (m_ticksConfig), so a single streamer would decode later files with the first file's clock and produce wrong
    ///   timestamps.  A new streamer per file (what XELite does when reading each .xel with its own
    ///   <c>XEFileEventStreamer</c>) keeps metadata and clock from leaking across files.</item>
    ///   <item>Every other (METADATA / EVENT) buffer is wrapped in a fresh <c>FastBinaryReader</c> and passed to
    ///   <c>HandleBuffer(reader, onMeta, onEvt, ct, liveStreamHeader: false)</c>.  <c>false</c> because each of these
    ///   buffers is self-describing (it carries its own log-buffer header) - the standalone HEADER row is not what
    ///   establishes it.</item>
    /// </list>
    ///
    /// <para><b>Fragile</b>: it binds to XELite internals, so it's pinned to the referenced XELite version and covered
    /// by a smoke test (<see cref="TryInitialise"/>).  Every failure throws so the caller falls back.</para>
    /// </summary>
    internal static class XELiveStreamShredder
    {
        private const int LbhtHeader = 2; // XE_LogBufferType.LBHT_HEADER - used only as a per-file boundary marker

        private static readonly object Sync = new();
        private static bool _initialised;
        private static Type _fileStreamerType;
        private static ConstructorInfo _readerCtor;
        private static MethodInfo _handleBuffer;

        /// <summary>Resolves the XELite internal members once.  Returns false if the internals aren't as expected.</summary>
        public static bool TryInitialise()
        {
            if (_initialised) return IsReady;
            lock (Sync)
            {
                if (_initialised) return IsReady;
                try
                {
                    var asm = typeof(IXEvent).Assembly;
                    _fileStreamerType = typeof(XEFileEventStreamer);
                    var readerType = asm.GetType("Microsoft.SqlServer.XEvent.XELite.Internal.FastBinaryReader", false);
                    var baseType = asm.GetType("Microsoft.SqlServer.XEvent.XELite.Internal.XEEventStreamerBase", false);
                    if (readerType != null && baseType != null)
                    {
                        _readerCtor = readerType.GetConstructor(new[] { typeof(byte[]) });
                        _handleBuffer = baseType.GetMethod("HandleBuffer",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    }
                }
                catch
                {
                    // leave members null -> IsReady false
                }
                _initialised = true;
                return IsReady;
            }
        }

        private static bool IsReady => _readerCtor != null && _handleBuffer != null;

        /// <summary>
        /// Parses the <c>(type, data)</c> rows from <c>fn_MSxe_read_event_stream</c>, invoking <paramref name="onEvent"/>
        /// for each event.  Throws if the XELite internals are unavailable or the buffers don't parse, so the caller
        /// can fall back.
        /// </summary>
        public static async Task ParseRowsAsync(IReadOnlyList<(int Type, byte[] Data)> rows, Action<IXEvent> onEvent,
            CancellationToken ct)
        {
            if (!TryInitialise())
            {
                throw new InvalidOperationException(
                    "XELite's internal buffer parser is unavailable (XELite version mismatch?).");
            }

            HandleMetadata onMeta = () => Task.CompletedTask;
            HandleXEvent onEvt = ev =>
            {
                onEvent(ev);
                return Task.CompletedTask;
            };

            // Each HEADER buffer marks a new FILE, and each file carries its own metadata AND clock calibration
            // (m_ticksConfig - the ticks->wall-clock mapping, which can differ per file/session-start).  A single
            // streamer would MERGE later files' metadata into the first's (keeping file 1's clock), so events from
            // later files decode with the wrong timestamps.  Start a FRESH streamer per file (like XELite does when it
            // reads each .xel file with its own XEFileEventStreamer) so metadata and clock never leak across files.
            object NewStreamer() => Activator.CreateInstance(_fileStreamerType, new object[] { new MemoryStream(), false });
            var streamer = NewStreamer();

            var processed = 0;
            foreach (var (type, data) in rows)
            {
                if (type == LbhtHeader)
                {
                    // New file boundary - reset metadata/clock.  (We don't parse the HEADER itself: its parse result is
                    // discarded by XELite, and in file mode the row is truncated before its padded length anyway.)
                    if (processed > 0) streamer = NewStreamer();
                    continue;
                }

                if (data == null || data.Length == 0) continue;

                var reader = _readerCtor.Invoke(new object[] { data });
                var task = (Task)_handleBuffer.Invoke(streamer,
                    new object[] { reader, onMeta, onEvt, ct, false /* buffer carries its own header */ });
                await task.ConfigureAwait(false);
                processed++;
            }
        }
    }
}
