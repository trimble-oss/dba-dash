using System.Text.Json;
using System.Text.Json.Nodes;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// Where an MCP server's <see cref="AiDataMask"/> comes from, read once per tool call.  Fixed for the AI service and
    /// for DBADashMCP started with --data-detail (the GUI's Ask > Claude Code window pins its request's masking that
    /// way).  Otherwise DBADashMCP follows <see cref="UserSetting"/>, so a change in Options > Connect AI Tools reaches
    /// the servers AI tools have already started - they run for as long as the client's session does, which can be days.
    /// </summary>
    public sealed class AiDataMaskSource
    {
        private static readonly AsyncLocal<AiDataMask?> CallMask = new();

        private readonly Func<AiDataMask> _current;

        private AiDataMaskSource(Func<AiDataMask> current, bool followsUserSetting)
        {
            _current = current;
            FollowsUserSetting = followsUserSetting;
        }

        public static AiDataMaskSource Fixed(AiDataMask mask) => new(() => mask, followsUserSetting: false);

        /// <summary>Whatever <paramref name="current"/> says when a call starts - for a setting kept elsewhere, such as the AI service's floor.</summary>
        public static AiDataMaskSource From(Func<AiDataMask> current) => new(current, followsUserSetting: false);

        /// <summary>The user's setting in <paramref name="path"/> (<see cref="UserSetting.DefaultPath"/> when null), re-read when the file changes.</summary>
        public static AiDataMaskSource FromUserSetting(string? path = null)
        {
            var setting = new UserSetting(path ?? UserSetting.DefaultPath);
            return new AiDataMaskSource(setting.Read, followsUserSetting: true);
        }

        public bool FollowsUserSetting { get; }

        public AiDataMask Current => _current();

        /// <summary>The mask the call in progress started with, for the tools to read - so a change mid-call can't mask half a result.</summary>
        public static AiDataMask? ForCall => CallMask.Value;

        /// <summary>Fixes <see cref="ForCall"/> for the rest of the calling async flow.</summary>
        internal static void BeginCall(AiDataMask mask) => CallMask.Value = mask;

        /// <summary>
        /// The user's MCP masking: one word in a small JSON file under %LOCALAPPDATA%\DBADash, written by the GUI.  No file
        /// is <see cref="AiDataMask.Default"/>; so is a word that isn't one of <see cref="AiDataMask.Words"/>.  A file that
        /// can't be read just now - the GUI part way through replacing it - keeps the last value read.
        /// </summary>
        public sealed class UserSetting(string path)
        {
            public static string DefaultPath => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash", "McpDataDetail.json");

            private const string Key = "dataDetail";

            private readonly object _lock = new();
            private (DateTime Written, long Length)? _seen;
            private AiDataMask _value = AiDataMask.Default;

            public AiDataMask Read()
            {
                lock (_lock)
                {
                    try
                    {
                        var file = new FileInfo(path);
                        if (!file.Exists)
                        {
                            _seen = null;
                            return _value = AiDataMask.Default;
                        }

                        var stamp = (file.LastWriteTimeUtc, file.Length);
                        if (_seen != stamp)
                        {
                            _value = AiDataMask.TryParse(ReadWord(path), out var mask) ? mask : AiDataMask.Default;
                            _seen = stamp;
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                    {
                        // Keep the last value.
                    }

                    return _value;
                }
            }

            /// <summary>The word in the file - empty when it holds none - or null when there is no file.</summary>
            public static string? ReadWord(string path)
            {
                if (!File.Exists(path)) return null;
                return JsonNode.Parse(File.ReadAllText(path)) is JsonObject root && root[Key] is JsonValue value && value.TryGetValue<string>(out var word)
                    ? word
                    : string.Empty;
            }

            /// <summary>Writes <paramref name="word"/>, replacing the file in one step so a server never reads half of it.</summary>
            public static void Write(string path, string word)
            {
                if (!AiDataMask.TryParse(word, out _))
                {
                    throw new ArgumentException($"Unknown data detail '{word}'.", nameof(word));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var temp = path + ".tmp";
                File.WriteAllText(temp, new JsonObject { [Key] = word }.ToJsonString());
                File.Move(temp, path, overwrite: true);
            }
        }
    }
}
