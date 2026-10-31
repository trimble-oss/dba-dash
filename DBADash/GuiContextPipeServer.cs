#nullable enable
using Serilog;
using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// Publishes what the GUI is showing on <see cref="GuiContextPipe"/>, for the local MCP server's
    /// get-gui-context tool.  Each connection gets one snapshot, captured on the UI thread at that moment, so
    /// nothing is kept in step with the tree as it changes.  The pipe is created for the current user only.
    ///
    /// Best effort throughout: a second copy of the GUI finds the pipe taken and simply doesn't serve, and a
    /// failure here never reaches the user.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class GuiContextPipeServer : IDisposable
    {
        private readonly Func<GuiContextSnapshot> _capture;
        private readonly Func<string?> _connectionString;
        private readonly CancellationTokenSource _stop = new();

        /// <summary>The fingerprint is a repository query, so it is looked up once per connection string rather than per request (and again until it succeeds).</summary>
        private (string ConnectionString, string? Fingerprint)? _fingerprint;

        private GuiContextPipeServer(Func<GuiContextSnapshot> capture, Func<string?> connectionString)
        {
            _capture = capture;
            _connectionString = connectionString;
        }

        /// <param name="capture">Reads the snapshot - called on a background thread, so it must marshal to the UI thread itself.</param>
        /// <param name="connectionString">The repository the GUI is connected to, for the snapshot's fingerprint.</param>
        public static GuiContextPipeServer Start(Func<GuiContextSnapshot> capture, Func<string?> connectionString)
        {
            var server = new GuiContextPipeServer(capture, connectionString);
            _ = Task.Run(server.RunAsync);
            return server;
        }

        private async Task RunAsync()
        {
            PipeSecurity security;
            try
            {
                security = new PipeSecurity();
                security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!,
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "GUI context pipe not started");
                return;
            }

            NamedPipeServerStream Create(bool first) => NamedPipeServerStreamAcl.Create(GuiContextPipe.PipeName,
                PipeDirection.Out, 2, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | (first ? PipeOptions.FirstPipeInstance : PipeOptions.None), 0, 0, security);

            // The next instance is always created before the one being served is released, so the name is never
            // free for a moment: a second copy of the GUI, whose first instance has to be the pipe's first, can't
            // take it over between requests.
            NamedPipeServerStream next;
            try
            {
                next = Create(first: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Another copy of the GUI is serving it.
                Log.Debug(ex, "GUI context pipe is already served by another copy of the GUI");
                return;
            }

            while (true)
            {
                var pipe = next;
                await using (pipe)
                {
                    try
                    {
                        await pipe.WaitForConnectionAsync(_stop.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    try
                    {
                        next = Create(first: false);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Log.Debug(ex, "GUI context pipe could not be renewed");
                        return;
                    }

                    try
                    {
                        var json = JsonSerializer.Serialize(Snapshot(), GuiContextPipe.JsonOptions);
                        await using var writer = new StreamWriter(pipe) { AutoFlush = true };
                        await writer.WriteLineAsync(json.AsMemory(), _stop.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        await next.DisposeAsync();
                        return;
                    }
                    catch (Exception ex)
                    {
                        // A client that went away mid-write, or a capture that failed - serve the next one.
                        Log.Debug(ex, "GUI context pipe request failed");
                    }
                }
            }
        }

        private GuiContextSnapshot Snapshot()
        {
            var snapshot = _capture();
            snapshot.CapturedUtc = DateTime.UtcNow;

            var connectionString = _connectionString() ?? string.Empty;
            if (_fingerprint is not { } cached || cached.ConnectionString != connectionString || cached.Fingerprint is null)
            {
                cached = (connectionString, RepositoryFingerprint.GetFingerprint(connectionString));
                _fingerprint = cached;
            }

            snapshot.RepositoryFingerprint = cached.Fingerprint;
            return snapshot;
        }

        public void Dispose()
        {
            _stop.Cancel();
            _stop.Dispose();
        }
    }
}
