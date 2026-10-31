#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// What the DBA Dash GUI is showing, for the local MCP server's get-gui-context tool - so a user can ask
    /// Claude Code about "this instance" or "the last hour" and have it mean what is on their screen.
    /// </summary>
    public sealed class GuiContextSnapshot
    {
        public DateTime CapturedUtc { get; set; }

        /// <summary>The GUI's repository, as <see cref="RepositoryFingerprint"/> identifies it.</summary>
        public string? RepositoryFingerprint { get; set; }

        /// <summary>The selected tree node's path, e.g. "Production\SQL01\Databases\Sales".</summary>
        public string? NodePath { get; set; }

        public string? NodeType { get; set; }

        /// <summary>The tab open on the right.</summary>
        public string? Tab { get; set; }

        /// <summary>The instance the node is for, when it is for one.</summary>
        public string? Instance { get; set; }

        public int? InstanceId { get; set; }

        /// <summary>Every instance the node covers - one for an instance's node, all of them for a folder.</summary>
        public List<int> InstanceIds { get; set; } = [];

        public string? Database { get; set; }

        /// <summary>schema.object, when the node is an object in a database.</summary>
        public string? Object { get; set; }

        /// <summary>The time window the GUI's charts and reports are showing.</summary>
        public DateTime FromUtc { get; set; }

        public DateTime ToUtc { get; set; }

        /// <summary>The deadlock open in the GUI's viewer, when there is one.</summary>
        public GuiContextDeadlock? Deadlock { get; set; }

        /// <summary>The query plan open in the GUI's viewer, when there is one.</summary>
        public GuiContextPlan? Plan { get; set; }
    }

    /// <summary>
    /// An artifact open in one of the GUI's viewers.  It carries its XML rather than only a key: what is open may
    /// never have been in the repository - a file, a Query Store plan, a live plan - and the user still means it.
    /// </summary>
    public abstract class GuiContextArtifact
    {
        /// <summary>The instance it came from, when the GUI opened it from one.</summary>
        public string? Instance { get; set; }

        public int? InstanceId { get; set; }

        /// <summary>The file it was opened from, if any.</summary>
        public string? FileName { get; set; }

        /// <summary>The viewer's tab title.</summary>
        public string? Title { get; set; }

        public string Xml { get; set; } = string.Empty;
    }

    public sealed class GuiContextDeadlock : GuiContextArtifact
    {
        /// <summary>
        /// DeadlockHash of the graph shown, as dbo.Deadlocks stores it - "0x..." - so a deadlock that was collected
        /// can be found in the repository.
        /// </summary>
        public string? DeadlockHash { get; set; }
    }

    public sealed class GuiContextPlan : GuiContextArtifact
    {
        /// <summary>The query_plan_hash of the statement shown.</summary>
        public string? QueryPlanHash { get; set; }

        /// <summary>The statement shown, by its position among the plan's statements.</summary>
        public int StatementIndex { get; set; }
    }

    /// <summary>
    /// The named pipe the GUI publishes a <see cref="GuiContextSnapshot"/> on.  A client connects, the GUI writes
    /// one snapshot as a line of JSON and hangs up.  The name is per Windows user and logon session, and the GUI
    /// creates the pipe for its own user only, so it can't be read from another account or session.  When several
    /// copies of the GUI are open, the first to start serves it.
    /// </summary>
    public static class GuiContextPipe
    {
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static string PipeName =>
            $"DBADashGUI.Context.{WindowsIdentity.GetCurrent().User?.Value}.{Process.GetCurrentProcess().SessionId}";

        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        /// <summary>The GUI's snapshot, or null when no GUI is open (or it didn't answer in time).</summary>
        public static async Task<GuiContextSnapshot?> TryReadAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            await using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await pipe.ConnectAsync(timeoutSource.Token);
                using var reader = new StreamReader(pipe);
                var line = await reader.ReadLineAsync(timeoutSource.Token);
                return string.IsNullOrWhiteSpace(line) ? null : JsonSerializer.Deserialize<GuiContextSnapshot>(line, JsonOptions);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
