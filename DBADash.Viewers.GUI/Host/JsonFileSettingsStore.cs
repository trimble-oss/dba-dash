using Serilog;
using System.IO;
using System.Text.Json;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Keeps the viewers' settings in a JSON file - what a host with no user settings of its own, such as the stand-alone
    /// viewer, passes to <see cref="ViewerSettings.Store"/>.
    ///
    /// A file that is missing, unreadable or not what it should be gives the defaults rather than an error: the
    /// settings are a convenience, and never a reason a plan doesn't open.  The file is written to a temporary name and
    /// moved into place, so a crash part way through a save can't leave half a file behind.
    /// </summary>
    public sealed class JsonFileSettingsStore : IViewerSettingsStore
    {
        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        private readonly string _path;
        private readonly object _lock = new();
        private Dictionary<string, object> _values;

        public JsonFileSettingsStore(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public object Get(string name)
        {
            lock (_lock)
            {
                return Values.GetValueOrDefault(name);
            }
        }

        public void Set(string name, object value)
        {
            lock (_lock)
            {
                Values[name] = value;
            }
        }

        public void Save()
        {
            try
            {
                lock (_lock)
                {
                    var folder = Path.GetDirectoryName(_path);
                    if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

                    var temp = _path + ".tmp";
                    File.WriteAllText(temp, JsonSerializer.Serialize(Values, WriteOptions));
                    File.Move(temp, _path, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to save the viewer settings to {path}", _path);
            }
        }

        private Dictionary<string, object> Values => _values ??= Load();

        private Dictionary<string, object> Load()
        {
            var values = new Dictionary<string, object>();
            try
            {
                if (!File.Exists(_path)) return values;

                using var document = JsonDocument.Parse(File.ReadAllText(_path));
                if (document.RootElement.ValueKind != JsonValueKind.Object) return values;

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    // Plain values only, as that is all a setting is: the typed reader converts to what it wants.
                    object value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Number when property.Value.TryGetInt64(out var whole) => whole,
                        JsonValueKind.Number => property.Value.GetDouble(),
                        _ => null
                    };
                    if (value != null) values[property.Name] = value;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to read the viewer settings from {path} - using the defaults", _path);
            }

            return values;
        }
    }
}
