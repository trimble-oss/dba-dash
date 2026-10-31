#nullable enable
using DBADash.Mcp;
using DBADashAI.Mcp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// What the GUI's Ask button hands to an AI tool: which of the MCP server's analyse prompts, with which arguments,
    /// and the deadlock graph or plan it is about.
    /// </summary>
    internal sealed record AgentAsk(string Prompt, string? ArtifactFile, string? Artifact, Func<string, Dictionary<string, string>> Arguments)
    {
        public static AgentAsk Deadlock(string graphXml, string deadlockHash, string? instance) =>
            new("analyse-deadlock", $"deadlock-{AgentHandoff.Safe(deadlockHash)}.xdl", graphXml,
                path => instance is null ? new() { ["path"] = path } : new() { ["path"] = path, ["instance"] = instance });

        /// <param name="statement">The statement shown, numbered from 1 among those with plans.</param>
        public static AgentAsk Plan(string planXml, string title, int statement) =>
            new("analyse-query-plan", $"plan-{AgentHandoff.Safe(title)}-{DateTime.Now:yyyyMMddHHmmss}.sqlplan", planXml,
                path => new() { ["path"] = path, ["statement"] = statement.ToString() });

        public static AgentAsk Performance(string instance, DateTime fromUtc, DateTime toUtc, bool latchesAndSpinlocks)
        {
            var hours = Math.Clamp((int)Math.Ceiling((toUtc - fromUtc - TimeSpan.FromMinutes(1)).TotalHours), 1, 168);
            var args = new Dictionary<string, string>
            {
                ["instance"] = instance,
                ["hoursBack"] = hours.ToString(),
                ["to"] = toUtc.ToString("yyyy-MM-ddTHH:mm:ss")
            };
            if (latchesAndSpinlocks) args["focus"] = "latches-and-spinlocks";
            return new AgentAsk("analyse-performance", null, null, _ => args);
        }
    }

    /// <summary>An <see cref="AgentAsk"/> written to the workspace: the request file and the prompt text in it.</summary>
    internal sealed record PreparedAsk(string RequestPath, string RequestRelativePath, string Text);

    /// <summary>
    /// Hands a deadlock, a plan or an instance's performance to an AI tool that has DBA Dash's MCP server: the GUI's Ask
    /// button.
    ///
    /// Whichever tool is picked, the handoff is prepared the same way, in a workspace folder of DBA Dash's own.  What to
    /// analyse is written there as a file - the deadlock graph or plan the user clicked on, which the tool reads with
    /// describe-deadlock-graph or describe-query-plan - rather than pointed at in the GUI, so the answer is about that one
    /// even if the GUI has moved on by the time it looks.  The prompt is the MCP server's own analyse prompt, so it asks
    /// exactly what /mcp__dbadash__analyse-* would, and it goes in a request file the tool is told to read, which keeps a
    /// long prompt off command lines and out of URLs.
    ///
    /// For Claude Code the workspace also carries the server itself, so it needs nothing registered: an MCP config
    /// starting DBADashMCP.exe with the GUI's repository connection, settings allowing its tools (all read-only), and a
    /// CLAUDE.md about DBA Dash.  The other tools use the dbadash server registered with them.
    /// </summary>
    internal static class AgentHandoff
    {
        /// <summary>The workspace's MCP config, which a Claude Code session is started with.</summary>
        internal const string McpConfigFile = "dbadash.mcp.json";

        internal static string McpExe => Path.Combine(Application.StartupPath, "DBADashMCP.exe");

        internal static string Workspace => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash", "ClaudeCode");

        /// <summary>Writes the workspace, the artifact and the request; returns where the request is and what it says.</summary>
        public static PreparedAsk Prepare(AgentAsk ask, string? workspace = null, string? connectionName = null)
        {
            workspace ??= Workspace;
            PrepareWorkspace(workspace, connectionName ?? CurrentConnectionName(), McpExe);

            var artifactPath = string.Empty;
            if (ask.ArtifactFile is not null)
            {
                artifactPath = Path.Combine(workspace, "artifacts", ask.ArtifactFile);
                File.WriteAllText(artifactPath, ask.Artifact);
            }

            var text = Render(ask.Prompt, ask.Arguments(artifactPath));
            var relative = Path.Combine("requests", $"{ask.Prompt}-{DateTime.Now:yyyyMMdd-HHmmss}.md");
            var path = Path.Combine(workspace, relative);
            File.WriteAllText(path, text);
            return new PreparedAsk(path, relative, text);
        }

        /// <summary>The MCP server's own analyse prompt for these arguments - the same text /mcp__dbadash__ would give.</summary>
        internal static string Render(string prompt, Dictionary<string, string> arguments)
        {
            var values = arguments.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));
            return AnalysisMcpPrompt.All(guiContext: true).Single(p => p.ProtocolPrompt.Name == prompt).Render(values);
        }

        /// <summary>
        /// Writes the workspace's own files, every time - so a moved install or a different repository connection is
        /// picked up - and clears out artifacts and requests more than a week old.  Anything else in the folder is left.
        /// </summary>
        internal static void PrepareWorkspace(string workspace, string? connectionName, string mcpExe)
        {
            Directory.CreateDirectory(Path.Combine(workspace, ".claude"));
            Directory.CreateDirectory(Path.Combine(workspace, "artifacts"));
            Directory.CreateDirectory(Path.Combine(workspace, "requests"));

            // Not .mcp.json: Claude Code loads it with --mcp-config, and a project .mcp.json would clash with a dbadash the
            // user has registered for themselves if they started claude here on their own.
            var entry = McpServerEntry.ForConnection(mcpExe, connectionName);
            File.WriteAllText(Path.Combine(workspace, McpConfigFile), entry.ToConfigJson());

            // The server's tools - all read-only - allowed without asking.
            var settings = new JsonObject
            {
                ["permissions"] = new JsonObject { ["allow"] = new JsonArray($"mcp__{McpServerEntry.ServerName}") }
            };
            File.WriteAllText(Path.Combine(workspace, ".claude", "settings.json"), settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            File.WriteAllText(Path.Combine(workspace, "CLAUDE.md"), ClaudeMd(connectionName));

            foreach (var folder in new[] { "artifacts", "requests" })
            {
                foreach (var file in Directory.GetFiles(Path.Combine(workspace, folder)))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-7)) File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // In use: next time.
                    }
                }
            }
        }

        private static string ClaudeMd(string? connectionName)
        {
            var text = new StringBuilder();
            text.AppendLine("# DBA Dash workspace");
            text.AppendLine();
            text.AppendLine("The DBA Dash GUI opens AI tools here when its user clicks Ask.  DBA Dash rewrites this file, `dbadash.mcp.json` and `.claude/settings.json` each time, and clears `artifacts/` and `requests/` of files more than a week old.");
            text.AppendLine();
            text.AppendLine("DBA Dash monitors SQL Server instances and keeps what it collects in a repository database.  The `dbadash` MCP server reads that repository " +
                            $"({(connectionName is null ? "the GUI's default connection" : $"the GUI connection `{connectionName}`")}) as the user, read-only - it never connects to the monitored instances and changes nothing.  Its tools are allowed here without asking.");
            text.AppendLine();
            text.AppendLine("- `requests/` holds what the user asked for, written by the GUI.  Do what the request says, using the `dbadash` tools.");
            text.AppendLine("- `artifacts/` holds the deadlock graph or plan the request is about, for `describe-deadlock-graph` or `describe-query-plan`.");
            text.AppendLine("- Times from the tools are UTC.  The user sees the GUI in their own time zone, so say UTC when you give a time.");
            text.AppendLine("- `get-gui-context` says what the user has open in the GUI now - for a follow-up question about \"this instance\" or \"now\".");
            text.AppendLine("- When the user asks for a change - an index, a setting, a code fix - write it out for them to review and run.  Nothing here can run it.");
            return text.ToString();
        }

        /// <summary>The saved GUI connection the repository is open on, which the workspace's MCP server is started with.</summary>
        private static string? CurrentConnectionName() =>
            Common.ConnectionString is { } cs ? RepositoryConnectionList.GetRepositoryConnectionList().FindByConnectionString(cs)?.Name : null;

        /// <summary>A name safe for a file: a hash or a tab title, with anything a path can't hold replaced.</summary>
        internal static string Safe(string value)
        {
            var safe = new string(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray());
            return safe.Length > 40 ? safe[..40] : safe;
        }

        /// <summary>
        /// Starts a CLI in its own console window: an .exe directly, each argument as it is; a .cmd shim through cmd, with
        /// the line quoted for it.  The arguments here are DBA Dash's own - flags, paths and a fixed sentence - with no
        /// quotes in them.
        /// </summary>
        internal static void StartCli(string cli, IReadOnlyList<string> args, string workingDirectory, bool keepWindow)
        {
            ProcessStartInfo start;
            if (cli.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
            {
                var line = string.Join(' ', new[] { cli }.Concat(args).Select(a => a.Length > 0 && a.IndexOfAny([' ', '&', '|', '<', '>', '^', '(', ')']) < 0 ? a : $"\"{a}\""));
                start = new ProcessStartInfo("cmd.exe") { Arguments = $"/d /s {(keepWindow ? "/k" : "/c")} \"{line}\"" };
            }
            else
            {
                start = new ProcessStartInfo(cli);
                foreach (var arg in args) start.ArgumentList.Add(arg);
            }

            start.WorkingDirectory = workingDirectory;
            start.UseShellExecute = false;
            start.CreateNoWindow = !keepWindow;
            Process.Start(start)?.Dispose();
        }
    }

    /// <summary>An AI tool the Ask button can hand to.</summary>
    internal abstract class AgentTarget
    {
        public abstract string Name { get; }

        public abstract string ToolTip { get; }

        /// <summary>The menu item's text.</summary>
        public virtual string MenuText => $"Ask {Name}";

        /// <summary>
        /// The tool's installed program, whose icon the menu item shows.  DBA Dash ships no third-party artwork: the icon
        /// comes from the user's own copy of the tool, as it does in Explorer's Open with menu.  Null for none.
        /// </summary>
        public virtual string? ProgramPath => null;

        /// <summary>Hands <paramref name="ask"/> over.  Returns a message for the user, or null when the tool's own window says enough.</summary>
        public abstract string? Launch(PreparedAsk ask);

        /// <summary>The program registered to open <paramref name="scheme"/>:// links, e.g. Cursor.exe for cursor.</summary>
        internal static string? ProtocolHandler(string scheme)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"{scheme}\shell\open\command");
                if (key?.GetValue(null) is not string command) return null;
                command = command.Trim();
                var path = command.StartsWith('"')
                    ? command[1..Math.Max(1, command.IndexOf('"', 1))]
                    : command.Split(' ')[0];
                return File.Exists(path) ? path : null;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// The tools that can take a handoff now: Claude Code when it is installed, which needs nothing registered; Claude
        /// Code in the Claude desktop app when that is installed and DBA Dash is registered with Claude Code; Cursor
        /// and VS Code when DBA Dash is registered with them; and copying the prompt for any other tool it is registered
        /// with.  Empty when there is no tool to hand to, which hides the button.
        /// </summary>
        public static IReadOnlyList<AgentTarget> Available()
        {
            if (!File.Exists(AgentHandoff.McpExe)) return [];

            var paths = McpClientPaths.ForCurrentUser();
            var entry = McpServerEntry.ForConnection(AgentHandoff.McpExe, null);
            var registered = McpClient.All(paths)
                .Where(c => c.GetStatus(entry).State is McpClientState.Registered or McpClientState.RegisteredDifferently)
                .Select(c => c.Name)
                .ToHashSet();

            var targets = new List<AgentTarget>();
            if (paths.ClaudeCli is { } claude) targets.Add(new ClaudeCodeTarget(claude));
            if (registered.Remove("Claude Code") && ClaudeDesktopCodeTarget.Find() is { } desktop) targets.Add(desktop);
            if (registered.Contains("Cursor")) targets.Add(new CursorTarget());
            if (registered.Any(n => n.StartsWith("VS Code")) && VsCodeCli() is { } code) targets.Add(new VsCodeTarget(code));
            if (targets.Count > 0 || registered.Count > 0) targets.Add(new CopyPromptTarget(registered));
            return targets;
        }

        /// <summary>VS Code's code command: on PATH, or where its user installer puts it.</summary>
        private static string? VsCodeCli() =>
            McpClientPaths.FindOnPath("code") is { } onPath
                ? onPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "bin", "code.cmd") is var local && File.Exists(local)
                    ? local
                    : null;
    }

    /// <summary>
    /// Claude Code, in its own window in the workspace, with only the workspace's MCP server (--strict-mcp-config): a user
    /// who has registered dbadash themselves - an older copy, another connection - would otherwise have theirs win.  Its
    /// tools are allowed on the command line as well as in the workspace settings, which Claude Code ignores until the
    /// folder has been trusted.
    /// </summary>
    internal sealed class ClaudeCodeTarget(string cli) : AgentTarget
    {
        public override string Name => "Claude Code";

        public override string ToolTip => "Open Claude Code in a new window to analyse this, using DBA Dash's MCP server";

        public override string? ProgramPath => cli;

        public override string? Launch(PreparedAsk ask)
        {
            AgentHandoff.StartCli(cli, Arguments($"Read {ask.RequestRelativePath} and do what it asks."), AgentHandoff.Workspace, keepWindow: true);
            return null;
        }

        /// <summary>--mcp-config and --allowedTools each take a list, so -- ends them: without it the prompt is read as one more.</summary>
        internal static IReadOnlyList<string> Arguments(string prompt) =>
            ["--strict-mcp-config", "--mcp-config", AgentHandoff.McpConfigFile, "--allowedTools", $"mcp__{McpServerEntry.ServerName}", "--", prompt];
    }

    /// <summary>
    /// Claude Code in the Claude desktop app, through its claude://code/new link, which opens a Code session in the
    /// workspace with the prompt filled in for the user to send.  The app asks the user to confirm a folder that came from
    /// a link every time, and it can't be given an MCP config, so this is only offered when dbadash is registered with
    /// Claude Code itself (user scope), which its sessions share.  The whole prompt goes in the link when it fits the
    /// app's limit of about 14,000 characters; otherwise the link asks it to read the request file.
    /// </summary>
    internal sealed class ClaudeDesktopCodeTarget(string? app) : AgentTarget
    {
        private const int MaxPrompt = 14000;

        /// <summary>The Microsoft Store / MSIX package's family name, which is Anthropic's.</summary>
        private const string PackageFamily = "Claude_pzs8sxrjxfjjc";

        /// <summary>
        /// The desktop app when it handles claude:// links, or null.  An older, non-packaged install registers the link with
        /// a command, which names its program.  The packaged app registers it in its manifest instead - outside the package
        /// the claude key has no command - so its program is found from the package.
        /// </summary>
        internal static ClaudeDesktopCodeTarget? Find()
        {
            if (ProtocolHandler("claude") is { } program) return new ClaudeDesktopCodeTarget(program);
            if (!IsUrlProtocol("claude")) return null;
            return new ClaudeDesktopCodeTarget(PackagedProgram());
        }

        private static bool IsUrlProtocol(string scheme)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(scheme);
                return key?.GetValue("URL Protocol") is not null;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>claude.exe in the installed package, for its icon.  Null when it can't be found: the item shows text only.</summary>
        private static string? PackagedProgram()
        {
            try
            {
                var package = new Windows.Management.Deployment.PackageManager().FindPackagesForUser(string.Empty, PackageFamily).FirstOrDefault();
                var exe = package is null ? null : Path.Combine(package.InstalledLocation.Path, "app", "claude.exe");
                return exe is not null && File.Exists(exe) ? exe : null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Couldn't find the Claude desktop app's package");
                return null;
            }
        }

        public override string Name => "Claude desktop app";

        public override string MenuText => "Ask Claude Code in the Claude app";

        public override string ToolTip => "Open a Claude Code session in the Claude desktop app with the prompt filled in, using the DBA Dash MCP server registered with Claude Code - confirm the folder, then press send";

        public override string? ProgramPath => app;

        public override string? Launch(PreparedAsk ask)
        {
            // No message: the app's own folder confirmation comes first and says enough.
            Process.Start(new ProcessStartInfo(Link(ask, AgentHandoff.Workspace)) { UseShellExecute = true })?.Dispose();
            return null;
        }

        internal static string Link(PreparedAsk ask, string folder)
        {
            var prompt = ask.Text.Length <= MaxPrompt ? ask.Text : $"Read {ask.RequestRelativePath} and do what it asks, using the dbadash MCP tools.";
            return $"claude://code/new?q={Uri.EscapeDataString(prompt)}&folder={Uri.EscapeDataString(folder)}";
        }
    }

    /// <summary>
    /// Cursor, through its prompt deep link, which opens Cursor with the prompt filled in for the user to send - Cursor
    /// never runs a deep-linked prompt by itself.  The whole prompt goes in the link when it fits Cursor's 8,000 character
    /// limit; otherwise the link asks Cursor to read the request file.
    /// </summary>
    internal sealed class CursorTarget : AgentTarget
    {
        private const int MaxLink = 8000;

        public override string Name => "Cursor";

        public override string ToolTip => "Open Cursor with the prompt filled in, using the DBA Dash MCP server registered with it - press send to run it";

        public override string? ProgramPath => ProtocolHandler("cursor");

        public override string? Launch(PreparedAsk ask)
        {
            Process.Start(new ProcessStartInfo(Link(ask)) { UseShellExecute = true })?.Dispose();
            return "Cursor opens with the prompt filled in - review it and press send.";
        }

        internal static string Link(PreparedAsk ask)
        {
            static string For(string text) => "cursor://anysphere.cursor-deeplink/prompt?text=" + Uri.EscapeDataString(text);
            var link = For(ask.Text);
            return link.Length <= MaxLink ? link : For($"Read the file {ask.RequestPath} and do what it asks, using the dbadash MCP tools.");
        }
    }

    /// <summary>VS Code's Copilot agent mode, through code chat, with the request file attached.</summary>
    internal sealed class VsCodeTarget(string cli) : AgentTarget
    {
        public override string Name => "VS Code";

        public override string ToolTip => "Start a Copilot agent mode chat in VS Code with the request attached, using the DBA Dash MCP server registered with it";

        /// <summary>Code.exe, which the code command in its bin folder runs.</summary>
        public override string? ProgramPath =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cli)!, "..", "Code.exe")) is var exe && File.Exists(exe)
                ? exe
                : ProtocolHandler("vscode");

        public override string? Launch(PreparedAsk ask)
        {
            AgentHandoff.StartCli(cli, Arguments(ask), AgentHandoff.Workspace, keepWindow: false);
            return null;
        }

        internal static IReadOnlyList<string> Arguments(PreparedAsk ask) =>
            ["chat", "--mode", "agent", "--add-file", ask.RequestPath, "Do what the attached file asks, using the dbadash MCP tools."];
    }

    /// <summary>The prompt on the clipboard, for a tool that can't be started with one - Claude Desktop, Windsurf.</summary>
    internal sealed class CopyPromptTarget(IReadOnlyCollection<string> registered) : AgentTarget
    {
        public override string Name => "Copy prompt";

        public override string MenuText => "Copy prompt";

        public override string ToolTip => registered.Count > 0
            ? $"Copy the prompt to paste into an AI tool DBA Dash is registered with ({string.Join(", ", registered)})"
            : "Copy the prompt to paste into an AI tool that has DBA Dash's MCP server";

        public override string? Launch(PreparedAsk ask)
        {
            Clipboard.SetText(ask.Text);
            return "Copied the prompt.  Paste it into an AI tool that has DBA Dash's MCP server registered.";
        }
    }
}
