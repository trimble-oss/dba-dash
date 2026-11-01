#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Mcp
{
    /// <summary>How an MCP client starts the local DBA Dash MCP server: the exe and its arguments.</summary>
    public sealed record McpServerEntry(string Command, IReadOnlyList<string> Args)
    {
        public const string ServerName = "dbadash";

        /// <summary>
        /// The entry for DBADashMCP.exe serving the saved GUI connection called <paramref name="connectionName"/>.
        /// <paramref name="dataDetail"/>, when given, is a <c>--data-detail</c> level fixed for the server's life - for a
        /// server started for one request.  A registration leaves it out: the server then follows the user's setting in
        /// Options > Connect AI Tools on each call, so a change reaches sessions the AI tool already has open.
        /// </summary>
        public static McpServerEntry ForConnection(string exePath, string? connectionName, string? dataDetail = null)
        {
            var args = new List<string>();
            if (!string.IsNullOrWhiteSpace(connectionName))
            {
                args.AddRange(["--connection", connectionName]);
            }

            if (!string.IsNullOrWhiteSpace(dataDetail))
            {
                args.AddRange(["--data-detail", dataDetail]);
            }

            return new McpServerEntry(exePath, args);
        }

        public bool SameAs(string? command, IEnumerable<string>? args) =>
            !string.IsNullOrWhiteSpace(command)
            && string.Equals(Path.GetFullPath(command), Path.GetFullPath(Command), StringComparison.OrdinalIgnoreCase)
            && (args ?? []).SequenceEqual(Args, StringComparer.Ordinal);

        /// <summary>The usual <c>mcpServers</c> JSON, for a client the GUI doesn't register itself.</summary>
        public string ToConfigJson() =>
            new JsonObject
            {
                ["mcpServers"] = new JsonObject { [ServerName] = McpClient.ServerObject(this, includeType: false) }
            }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public enum McpClientState
    {
        /// <summary>The client isn't installed, as far as can be told.</summary>
        NotInstalled,
        NotRegistered,
        Registered,
        /// <summary>Registered, but with another exe or another connection - an older copy of DBA Dash, say.</summary>
        RegisteredDifferently
    }

    public sealed record McpClientStatus(McpClientState State, string Detail);

    /// <summary>
    /// An AI tool that can use the DBA Dash MCP server, and how to register the server with it.  Where the tool has a
    /// command line for this it is used; otherwise its JSON config file is edited, keeping a backup.
    /// </summary>
    public abstract class McpClient
    {
        public abstract string Name { get; }

        /// <summary>What to do after registering for the client to pick it up.</summary>
        public abstract string AfterChange { get; }

        public abstract McpClientStatus GetStatus(McpServerEntry entry);

        /// <summary>Registers <paramref name="entry"/>, replacing a registration of the same name.  Returns what to tell the user.</summary>
        public abstract Task<string> RegisterAsync(McpServerEntry entry, CancellationToken cancellationToken);

        public abstract Task<string> UnregisterAsync(CancellationToken cancellationToken);

        internal static JsonObject ServerObject(McpServerEntry entry, bool includeType)
        {
            var server = new JsonObject();
            if (includeType) server["type"] = "stdio";
            server["command"] = entry.Command;
            server["args"] = new JsonArray(entry.Args.Select(a => (JsonNode?)JsonValue.Create(a)).ToArray());
            return server;
        }

        /// <summary>The status of a server entry as a config file holds it: absent, this one, or another.</summary>
        internal static McpClientStatus StatusOf(JsonObject? server, McpServerEntry entry, string where)
        {
            if (server is null)
            {
                return new McpClientStatus(McpClientState.NotRegistered, "Not registered");
            }

            var command = server["command"]?.GetValue<string>();
            var args = (server["args"] as JsonArray)?.Select(a => a?.GetValue<string>() ?? string.Empty).ToList();
            return entry.SameAs(command, args)
                ? new McpClientStatus(McpClientState.Registered, $"Registered ({where})")
                : new McpClientStatus(McpClientState.RegisteredDifferently,
                    $"Registered with {Path.GetFileName(command)} {string.Join(' ', args ?? [])} - update it to this copy and connection");
        }

        /// <summary>The clients the GUI knows how to register with, looking in the given folders.</summary>
        public static IReadOnlyList<McpClient> All(McpClientPaths? paths = null)
        {
            paths ??= McpClientPaths.ForCurrentUser();
            return
            [
                new ClaudeCodeMcpClient(paths),
                new JsonFileMcpClient("Claude Desktop", paths.ClaudeDesktopConfig, "mcpServers", includeType: false,
                    installed: Directory.Exists(Path.GetDirectoryName(paths.ClaudeDesktopConfig)), "Quit Claude Desktop from the tray and start it again."),
                new JsonFileMcpClient("Cursor", paths.CursorConfig, "mcpServers", includeType: false,
                    installed: Directory.Exists(Path.GetDirectoryName(paths.CursorConfig)), "Cursor picks it up in Settings > MCP; restart Cursor if it doesn't appear."),
                new JsonFileMcpClient("VS Code (Copilot agent mode)", paths.VsCodeConfig, "servers", includeType: true,
                    installed: Directory.Exists(Path.GetDirectoryName(paths.VsCodeConfig)), "Start the server from the MCP view, or reload the window."),
                new JsonFileMcpClient("Windsurf", paths.WindsurfConfig, "mcpServers", includeType: false,
                    installed: Directory.Exists(Path.GetDirectoryName(paths.WindsurfConfig)), "Refresh the MCP servers in Windsurf's Cascade settings.")
            ];
        }
    }

    /// <summary>Where each client keeps its MCP config.  Separate so tests can point them at a scratch folder.</summary>
    public sealed record McpClientPaths(
        string ClaudeCodeConfig,
        string ClaudeDesktopConfig,
        string CursorConfig,
        string VsCodeConfig,
        string WindsurfConfig,
        string? ClaudeCli)
    {
        public static McpClientPaths ForCurrentUser()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Claude Code keeps its user-scope servers in .claude.json, in CLAUDE_CONFIG_DIR when that is set.
            var claudeConfigDir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            var claudeCode = Path.Combine(string.IsNullOrWhiteSpace(claudeConfigDir) ? home : claudeConfigDir, ".claude.json");

            return new McpClientPaths(
                claudeCode,
                FindClaudeDesktopConfig(roaming, local),
                Path.Combine(home, ".cursor", "mcp.json"),
                Path.Combine(roaming, "Code", "User", "mcp.json"),
                Path.Combine(home, ".codeium", "windsurf", "mcp_config.json"),
                FindOnPath("claude") ?? FirstExisting(
                    // Where Claude Code's installers put it, for a GUI started without them on its PATH.
                    Path.Combine(home, ".local", "bin", "claude.exe"),
                    Path.Combine(roaming, "npm", "claude.cmd")));
        }

        /// <summary>
        /// The Microsoft Store build of Claude Desktop is an MSIX package: its %APPDATA% writes are redirected into the
        /// package, so that is where its config is read from.  The installer build uses %APPDATA%\Claude.
        /// </summary>
        private static string FindClaudeDesktopConfig(string roaming, string local)
        {
            var packages = Path.Combine(local, "Packages");
            var package = Directory.Exists(packages)
                ? Directory.GetDirectories(packages, "Claude_*").FirstOrDefault()
                : null;
            var folder = package is not null
                ? Path.Combine(package, "LocalCache", "Roaming", "Claude")
                : Path.Combine(roaming, "Claude");
            return Path.Combine(folder, "claude_desktop_config.json");
        }

        private static string? FirstExisting(params string[] paths) => paths.FirstOrDefault(File.Exists);

        /// <summary>The first match for a command on PATH: an .exe before a .cmd, as Windows would run it.</summary>
        public static string? FindOnPath(string command)
        {
            var folders = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var extension in new[] { ".exe", ".cmd" })
            {
                foreach (var folder in folders)
                {
                    try
                    {
                        var candidate = Path.Combine(folder.Trim('"'), command + extension);
                        if (File.Exists(candidate)) return candidate;
                    }
                    catch (ArgumentException)
                    {
                        // A malformed PATH entry.
                    }
                }
            }

            return null;
        }
    }

    /// <summary>A client whose MCP servers are in a JSON file under one key - Cursor, Claude Desktop, Windsurf, VS Code.</summary>
    public sealed class JsonFileMcpClient(string name, string configPath, string serversKey, bool includeType, bool installed, string afterChange) : McpClient
    {
        public override string Name => name;

        public override string AfterChange => afterChange;

        public string ConfigPath => configPath;

        public override McpClientStatus GetStatus(McpServerEntry entry)
        {
            if (!installed && !File.Exists(configPath))
            {
                return new McpClientStatus(McpClientState.NotInstalled, "Not found");
            }

            try
            {
                var root = McpConfigFile.Read(configPath).Root;
                return StatusOf(root[serversKey]?[McpServerEntry.ServerName] as JsonObject, entry, configPath);
            }
            catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
            {
                return new McpClientStatus(McpClientState.NotRegistered, $"Can't read {configPath}: {ex.Message}");
            }
        }

        public override Task<string> RegisterAsync(McpServerEntry entry, CancellationToken cancellationToken)
        {
            var note = McpConfigFile.Update(configPath, root =>
            {
                if (root[serversKey] is not JsonObject servers)
                {
                    root[serversKey] = servers = new JsonObject();
                }

                servers[McpServerEntry.ServerName] = ServerObject(entry, includeType);
            });
            return Task.FromResult($"Registered in {configPath}.{note}  {afterChange}");
        }

        public override Task<string> UnregisterAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(configPath))
            {
                return Task.FromResult("It wasn't registered.");
            }

            var note = McpConfigFile.Update(configPath, root => (root[serversKey] as JsonObject)?.Remove(McpServerEntry.ServerName));
            return Task.FromResult($"Removed from {configPath}.{note}  {afterChange}");
        }
    }

    /// <summary>
    /// Claude Code, through its own command line (claude mcp add/remove), which owns its config.  The status is read
    /// from that config, which is quicker than asking the CLI and needs nothing to be running.
    /// </summary>
    public sealed class ClaudeCodeMcpClient(McpClientPaths paths) : McpClient
    {
        public override string Name => "Claude Code";

        public override string AfterChange => "Start a new Claude Code session to use it.";

        public override McpClientStatus GetStatus(McpServerEntry entry)
        {
            if (paths.ClaudeCli is null)
            {
                return new McpClientStatus(McpClientState.NotInstalled, "Not found - the claude command isn't on PATH");
            }

            try
            {
                var root = File.Exists(paths.ClaudeCodeConfig) ? McpConfigFile.Read(paths.ClaudeCodeConfig).Root : new JsonObject();
                return StatusOf(root["mcpServers"]?[McpServerEntry.ServerName] as JsonObject, entry, "user scope");
            }
            catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
            {
                return new McpClientStatus(McpClientState.NotRegistered, $"Can't read {paths.ClaudeCodeConfig}: {ex.Message}");
            }
        }

        public override async Task<string> RegisterAsync(McpServerEntry entry, CancellationToken cancellationToken)
        {
            // add refuses a name that is already there, so a registration with another exe or connection goes first.
            await RunCliAsync(["mcp", "remove", McpServerEntry.ServerName, "--scope", "user"], allowFailure: true, cancellationToken);
            await RunCliAsync(["mcp", "add", "--scope", "user", McpServerEntry.ServerName, "--", entry.Command, .. entry.Args], allowFailure: false, cancellationToken);
            return $"Registered with Claude Code for your user.  {AfterChange}";
        }

        public override async Task<string> UnregisterAsync(CancellationToken cancellationToken)
        {
            await RunCliAsync(["mcp", "remove", McpServerEntry.ServerName, "--scope", "user"], allowFailure: false, cancellationToken);
            return "Removed from Claude Code.";
        }

        private async Task RunCliAsync(IReadOnlyList<string> args, bool allowFailure, CancellationToken cancellationToken)
        {
            var cli = paths.ClaudeCli ?? throw new InvalidOperationException("The claude command isn't on PATH.");
            var start = McpCli.StartInfo(cli, args);

            using var process = Process.Start(start) ?? throw new InvalidOperationException("claude didn't start.");
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                throw new TimeoutException("claude didn't finish within a minute.");
            }

            if (process.ExitCode != 0 && !allowFailure)
            {
                var message = (await error).Trim();
                if (message.Length == 0) message = (await output).Trim();
                throw new InvalidOperationException($"claude {string.Join(' ', args.Take(2))} failed: {message}");
            }
        }
    }

    internal static class McpCli
    {
        /// <summary>
        /// How to run a CLI: an .exe directly, with each argument passed as it is; an npm .cmd shim through cmd, whose
        /// quoting needs the whole command line wrapped once more (/s /c "...").
        /// </summary>
        public static ProcessStartInfo StartInfo(string cli, IReadOnlyList<string> args)
        {
            ProcessStartInfo start;
            if (cli.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
            {
                var line = string.Join(' ', new[] { cli }.Concat(args).Select(Quote));
                start = new ProcessStartInfo("cmd.exe") { Arguments = $"/d /s /c \"{line}\"" };
            }
            else
            {
                start = new ProcessStartInfo(cli);
                foreach (var arg in args) start.ArgumentList.Add(arg);
            }

            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            return start;
        }

        /// <summary>An argument quoted for cmd: in quotes when it needs them.  DBA Dash's own arguments - paths and connection names - have no quotes in them.</summary>
        internal static string Quote(string arg) =>
            arg.Length > 0 && arg.IndexOfAny([' ', '\t', '&', '|', '<', '>', '^', '(', ')']) < 0 ? arg : $"\"{arg}\"";
    }

    /// <summary>Reads and rewrites a client's JSON config, keeping what else is in it and a backup of the original.</summary>
    public static class McpConfigFile
    {
        private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        /// <summary>The file's root object - empty when there is no file - and whether it had comments, which a rewrite can't keep.</summary>
        public static (JsonObject Root, bool HadComments) Read(string path)
        {
            if (!File.Exists(path))
            {
                return (new JsonObject(), false);
            }

            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
            {
                return (new JsonObject(), false);
            }

            var root = JsonNode.Parse(text, documentOptions: Lenient) as JsonObject
                       ?? throw new InvalidOperationException("The file isn't a JSON object.");

            bool hadComments;
            try
            {
                using var strict = JsonDocument.Parse(text);
                hadComments = false;
            }
            catch (JsonException)
            {
                hadComments = true;
            }

            return (root, hadComments);
        }

        /// <summary>Applies <paramref name="change"/> and writes the file, copying the original to .dbadash-backup first.  Returns a note for the user, or empty.</summary>
        public static string Update(string path, Action<JsonObject> change)
        {
            var (root, hadComments) = Read(path);
            change(root);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var backup = path + ".dbadash-backup";
            if (File.Exists(path))
            {
                File.Copy(path, backup, overwrite: true);
            }

            var temp = path + ".dbadash-tmp";
            File.WriteAllText(temp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, overwrite: true);

            return hadComments
                ? $"  The file's comments couldn't be kept; the original is in {backup}."
                : string.Empty;
        }
    }
}
