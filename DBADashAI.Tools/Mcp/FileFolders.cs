using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// The folders the local server reads plan and deadlock files from: DBA Dash's own folder under %LOCALAPPDATA%,
    /// where the GUI's Ask button puts the file it hands over, and the folders the client says it is working in (its MCP
    /// roots).  A client that doesn't say gets the user's profile folder instead.  A path can come from text the model
    /// read rather than from the user, so a file anywhere else is for the client to read itself, under its own
    /// permissions, and pass as xml.
    /// </summary>
    internal static partial class FileFolders
    {
        [System.Text.RegularExpressions.GeneratedRegex(@"^file:///([A-Za-z])%3[Aa]", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
        private static partial System.Text.RegularExpressions.Regex EscapedDrive();

        private static readonly TimeSpan RootsTimeout = TimeSpan.FromSeconds(5);

        public static string DbaDashFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash");

        /// <summary>How long a client's folders are kept before it is asked again - the roots can change while a session runs.</summary>
        private static readonly TimeSpan KeepFor = TimeSpan.FromMinutes(1);

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Cached> ByClient = new();

        /// <summary>The key for a call with no client: tests, and tools run outside a server.</summary>
        private static readonly object NoClient = new();

        private sealed record Cached(DateTime Until, IReadOnlyList<string> Folders);

        /// <summary>
        /// The folders for this call, each as written and - where a link leads elsewhere - where it leads.  Kept for a minute
        /// per client connection, so a run of calls asks the client once - and a client that offers roots but never
        /// answers costs its timeout once a minute rather than on every call.
        /// </summary>
        public static async Task<IReadOnlyList<string>> ForCallAsync(McpServer? client, CancellationToken cancellationToken)
        {
            var key = (object?)client ?? NoClient;
            if (ByClient.TryGetValue(key, out var cached) && cached.Until > DateTime.UtcNow)
            {
                return cached.Folders;
            }

            var folders = await LookUpAsync(client, cancellationToken);
            ByClient.AddOrUpdate(key, new Cached(DateTime.UtcNow + KeepFor, folders));
            return folders;
        }

        private static async Task<IReadOnlyList<string>> LookUpAsync(McpServer? client, CancellationToken cancellationToken)
        {
            var folders = new List<string> { DbaDashFolder };
            folders.AddRange(await ClientRootsAsync(client, cancellationToken)
                             ?? [Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)]);
            return folders
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .SelectMany(f => new[] { Path.GetFullPath(f), FinalDirectoryPath(f) })
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>The client's roots as local folders; null when it doesn't offer roots.  One that offers none usable gets none.</summary>
        private static async Task<IReadOnlyList<string>?> ClientRootsAsync(McpServer? client, CancellationToken cancellationToken)
        {
            if (client?.ClientCapabilities?.Roots is null)
            {
                return null;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RootsTimeout);
            try
            {
                var result = await client.RequestRootsAsync(new ListRootsRequestParams(), timeout.Token);
                return Local(result.Roots.Select(r => r.Uri));
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // It said it had roots and didn't give them: only DBA Dash's own folder.
                return [];
            }
        }

        /// <summary>The file: URIs among <paramref name="uris"/>, as local paths.  VS Code escapes the drive's colon (file:///c%3A/...).</summary>
        internal static IReadOnlyList<string> Local(IEnumerable<string?> uris) =>
            uris.Select(u => Uri.TryCreate(EscapedDrive().Replace(u ?? string.Empty, "file:///$1:"), UriKind.Absolute, out var uri) && uri.IsFile ? uri.LocalPath : null)
                .OfType<string>()
                .ToList();

        /// <summary>Whether <paramref name="path"/>, a full path, is inside one of <paramref name="folders"/>.</summary>
        public static bool Contains(IReadOnlyList<string> folders, string path) =>
            folders.Any(folder =>
            {
                var prefix = Path.EndsInDirectorySeparator(folder) ? folder : folder + Path.DirectorySeparatorChar;
                return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            });

        /// <summary>Where an open file really is, after any symbolic link or junction on the way; null when that can't be told.</summary>
        public static string? FinalPath(SafeFileHandle handle)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            var buffer = new char[1024];
            var length = GetFinalPathNameByHandle(handle, buffer, buffer.Length, 0);
            if (length > buffer.Length)
            {
                buffer = new char[length];
                length = GetFinalPathNameByHandle(handle, buffer, buffer.Length, 0);
            }

            return length == 0 || length > buffer.Length ? null : WithoutPrefix(new string(buffer, 0, (int)length));
        }

        /// <summary>\\?\C:\x is C:\x; \\?\UNC\server\share is \\server\share.</summary>
        internal static string WithoutPrefix(string path) =>
            path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase) ? @"\\" + path[8..]
            : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..]
            : path;

        private static string? FinalDirectoryPath(string folder)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            // A folder is opened for its path with FILE_FLAG_BACKUP_SEMANTICS; reading nothing from it.
            using var handle = CreateFile(folder, 0, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open, 0x02000000, IntPtr.Zero);
            return handle.IsInvalid ? null : FinalPath(handle);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetFinalPathNameByHandleW")]
        private static extern uint GetFinalPathNameByHandle(SafeFileHandle file, [Out] char[] path, int length, uint flags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW")]
        private static extern SafeFileHandle CreateFile(string name, uint access, FileShare share, IntPtr security, FileMode mode, uint flags, IntPtr template);
    }
}
