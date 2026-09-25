using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;

namespace DBADashGUI.ShellIntegration
{
    /// <summary>
    /// Makes files opened from Explorer join the viewer already running, rather than each starting a
    /// viewer of its own.
    ///
    /// Explorer starts a process per file: select four plans and press Enter, and four copies of
    /// DBA Dash start, each with one plan in a window of its own - so the plans could not share the
    /// tabbed plan window, which is only shared within a process.  The first copy to start becomes the
    /// primary, holding a mutex and listening on a named pipe; every later one sends its files down the
    /// pipe and exits, and the primary opens them - plans as tabs in its plan window.
    ///
    /// Per user, per session and per copy of DBA Dash: files are never handed to another user's
    /// process, to one in another Windows session whose windows could not be seen from this one, or to
    /// a different install that may be another version.
    /// </summary>
    public sealed class ViewerInstance : IDisposable
    {
        /// <summary>
        /// How long a copy waits to reach the primary.  The primary may itself have only just started,
        /// in which case its pipe does not exist yet; the client keeps retrying until this runs out.
        /// </summary>
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How long a copy waits for the primary to say it has opened the files.  Generous, as a large
        /// plan takes a while to parse; if it runs out, the copy opens the files itself.
        /// </summary>
        private static readonly TimeSpan OpenedTimeout = TimeSpan.FromSeconds(60);

        private const byte Opened = 1;

        private readonly Mutex _mutex;
        private readonly string _pipeName;
        private readonly CancellationTokenSource _stop = new();
        private bool _ownsMutex;
        private Control _uiThread;
        private Func<IReadOnlyList<string>, bool> _open;

        private ViewerInstance(string name)
        {
            // Local\ scopes the mutex to this session.  Pipe names are machine wide, so the pipe's name
            // carries the session and the user's SID as well.
            _mutex = new Mutex(false, @"Local\" + name);
            _pipeName = name + "." + Process.GetCurrentProcess().SessionId + "." + WindowsIdentity.GetCurrent().User?.Value;
            _ownsMutex = TryTakeMutex();
        }

        /// <summary>True when this is the first copy running, which opens files for the others.</summary>
        public bool IsPrimary => _ownsMutex;

        /// <summary>This copy's claim to be the viewer that files join.</summary>
        public static ViewerInstance Claim() => new(NameForThisCopy());

        /// <summary>
        /// Hand <paramref name="files"/> to the primary.  True when it has opened them and this copy has
        /// nothing left to do; false when it could not be reached or did not answer, and this copy
        /// should open them itself.
        /// </summary>
        public bool Forward(IReadOnlyList<string> files)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly);
                client.Connect((int)ConnectTimeout.TotalMilliseconds);

                // Explorer lets the process it starts come to the front, and nothing else.  Passing that on
                // to the primary is what lets its window come to the front rather than just flash.
                if (GetNamedPipeServerProcessId(client.SafePipeHandle.DangerousGetHandle(), out var primary))
                {
                    AllowSetForegroundWindow(primary);
                }

                var request = Encoding.UTF8.GetBytes(string.Join("\n", files) + "\n\n");
                client.Write(request, 0, request.Length);
                client.Flush();

                var answer = new byte[1];
                var read = client.ReadAsync(answer, 0, 1).Wait(OpenedTimeout) ? answer[0] : (byte)0;
                return read == Opened;
            }
            catch (Exception ex)
            {
                // The primary exited, or is too busy to answer.  Opening the files here is the fallback.
                Log.Warning(ex, "Could not hand {Count} file(s) to the DBA Dash viewer already running", files.Count);
                return false;
            }
        }

        /// <summary>
        /// Open files sent by later copies, for as long as this one runs.  <paramref name="open"/> runs
        /// on the UI thread, and returns false once the viewer is closing and can open nothing more -
        /// the sender then opens the files itself.  Only the primary listens; a copy that could not
        /// reach it, and opened its files itself, takes over if the primary has gone.
        /// </summary>
        public void Listen(Func<IReadOnlyList<string>, bool> open)
        {
            if (!_ownsMutex) _ownsMutex = TryTakeMutex();
            if (!_ownsMutex) return;

            // A control created here, on the UI thread, for the pipe's thread to hand files to it by.
            _uiThread = new Control();
            _uiThread.CreateControl();
            _open = open;

            Task.Run(ServeAsync);
        }

        private async Task ServeAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        _pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                    await server.WaitForConnectionAsync(_stop.Token);

                    var files = await ReadFilesAsync(server, _stop.Token);
                    var opened = files.Count > 0 && OpenOnUiThread(files);

                    await server.WriteAsync(new[] { opened ? Opened : (byte)0 }, _stop.Token);
                    await server.FlushAsync(_stop.Token);
                    server.WaitForPipeDrain();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // One sender going wrong - it gave up, or sent nonsense - is no reason to stop
                    // opening files for the next.
                    Log.Warning(ex, "Error receiving files for the DBA Dash viewer");
                }
            }
        }

        /// <summary>The paths sent, one to a line, ended by an empty line.</summary>
        private static async Task<IReadOnlyList<string>> ReadFilesAsync(Stream stream, CancellationToken token)
        {
            var buffer = new StringBuilder();
            var bytes = new byte[4096];
            var chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];

            // One decoder for the whole request rather than one per read.  A character outside
            // ASCII - a user folder named for someone whose name has one - is more than one byte,
            // and a read boundary can fall inside it.  Decoded a chunk at a time, each half would
            // come out as a replacement character and the path would no longer name a file: it
            // would be dropped silently, since all that is checked afterwards is that it exists.
            var decoder = Encoding.UTF8.GetDecoder();

            while (!EndsWithBlankLine(buffer))
            {
                var read = await stream.ReadAsync(bytes, token);
                if (read == 0) break;

                buffer.Append(chars, 0, decoder.GetChars(bytes, 0, read, chars, 0));
            }

            // Only files that exist: whatever sent them, all the viewer does with a path is open it.
            return buffer.ToString().Split('\n').Where(f => f.Length > 0 && File.Exists(f)).ToList();
        }

        /// <summary>
        /// The end of a request: the line that ends the last path, then a line with nothing on it.
        /// Checked against the buffer itself rather than a copy of it, which on every read of a long
        /// request would cost the whole request again.
        /// </summary>
        private static bool EndsWithBlankLine(StringBuilder buffer) =>
            buffer.Length >= 2 && buffer[^1] == '\n' && buffer[^2] == '\n';

        private bool OpenOnUiThread(IReadOnlyList<string> files)
        {
            try
            {
                return _uiThread is { IsDisposed: false, IsHandleCreated: true } &&
                       (bool)_uiThread.Invoke(() => _open(files));
            }
            catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
            {
                // The viewer has closed down between the connection and the open.
                return false;
            }
        }

        private bool TryTakeMutex()
        {
            try
            {
                return _mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // The previous primary ended without releasing it - it crashed, or was killed.  The
                // mutex is ours now all the same.
                return true;
            }
        }

        /// <summary>
        /// A name for this copy of DBA Dash: installs in different folders - which may be different
        /// versions - each have their own primary.
        /// </summary>
        private static string NameForThisCopy()
        {
            var path = Application.ExecutablePath.ToUpperInvariant();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..16];
            return "DBADash.Viewer." + hash;
        }

        public void Dispose()
        {
            _stop.Cancel();

            if (_ownsMutex)
            {
                _mutex.ReleaseMutex();
                _ownsMutex = false;
            }

            _mutex.Dispose();
            _uiThread?.Dispose();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNamedPipeServerProcessId(IntPtr pipe, out uint serverProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(uint processId);
    }
}
