using DBADash.XE;
using DBADashGUI.CustomReports;
using Microsoft.SqlServer.XEvent.XELite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Loads XE trace data from a file on disk into the dynamic-schema <see cref="DataTable"/> the grid
    /// (<see cref="XEResultsControl"/>) consumes.  Two kinds of file are handled:
    /// <list type="bullet">
    ///   <item><b>.xel</b> - a native Extended Events file (e.g. captured by SSMS, or a <c>.xel</c> previously saved
    ///   from DBA Dash).  Parsed with XELite via <see cref="XELiteShredder"/> - the same path the service uses - so the
    ///   grid matches a live/history view.  Timestamps come out in UTC.</item>
    ///   <item><b>.json</b> / <b>.xml</b> - a DBA Dash-native save produced by <see cref="GridSerializer"/>.  These hold
    ///   the grid contents as already displayed, so their timestamps are already in the app time zone.</item>
    /// </list>
    /// The returned <c>TimestampsAreUtc</c> flag lets the caller tell <see cref="XEResultsControl.LoadEvents"/> whether
    /// the <c>timestamp</c> column still needs the UTC-&gt;local conversion (only the raw <c>.xel</c> does).
    /// </summary>
    internal static class XEFileLoader
    {
        public const string XelExtension = ".xel";

        /// <summary>Open-dialog filter covering .xel and the native save formats.</summary>
        public const string OpenFilter =
            "XE trace files (*.xel;*.json;*.json.gz;*.xml;*.xml.gz)|*.xel;*.json;*.json.gz;*.xml;*.xml.gz|" +
            "Extended Events (*.xel)|*.xel|JSON (*.json)|*.json|Compressed JSON (*.json.gz)|*.json.gz|" +
            "DataTable XML (*.xml)|*.xml|Compressed XML (*.xml.gz)|*.xml.gz";

        public readonly record struct LoadResult(DataTable Table, bool TimestampsAreUtc);

        /// <summary>Loads the file at <paramref name="path"/>, dispatching on its extension.</summary>
        public static async Task<LoadResult> LoadAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            var ext = Path.GetExtension(path);
            if (string.Equals(ext, XelExtension, StringComparison.OrdinalIgnoreCase))
            {
                return new LoadResult(await ReadXelAsync(path, ct), TimestampsAreUtc: true);
            }
            if (GridSerializer.IsNativeExtension(path))
            {
                // Read the potentially-large file off the UI thread.
                var table = await Task.Run(() => GridSerializer.LoadDataTable(path), ct);
                return new LoadResult(table, TimestampsAreUtc: false);
            }
            throw new NotSupportedException(
                $"Unsupported file type '{ext}'.  Open a {XelExtension}, {GridSerializer.JsonExtension} or {GridSerializer.XmlExtension} file.");
        }

        /// <summary>Reads and shreds a .xel file with XELite (native binary path - no fn_xe_file_target_read_file).</summary>
        private static async Task<DataTable> ReadXelAsync(string path, CancellationToken ct)
        {
            var events = new List<IXEvent>();
            await using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var streamer = new XEFileEventStreamer(fs, false);
                await streamer.ReadEventStream(ev =>
                {
                    events.Add(ev);
                    return Task.CompletedTask;
                }, ct);
            }
            return XELiteShredder.Build(events);
        }
    }
}
