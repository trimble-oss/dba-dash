using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;

namespace DBADash
{
    /// <summary>
    /// The options a user wants new source connections to start with, saved per user so the service configuration tool
    /// and the GUI's Add Connections dialog both use them.  Without a saved file the app's own defaults
    /// (<see cref="SourceConnectionOptions.AppDefaults"/>) are used.  Only new connections are affected - existing
    /// connections keep the settings they were added with.
    /// </summary>
    public class NewConnectionDefaults
    {
        /// <summary>The current user's defaults, in %LocalAppData%\DBADash</summary>
        public static NewConnectionDefaults User { get; } = new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash", "NewConnectionDefaults.json"));

        public string FilePath { get; }

        public NewConnectionDefaults(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>True if the user has saved defaults of their own</summary>
        public bool IsCustomized => File.Exists(FilePath);

        /// <summary>
        /// The user's saved defaults, or the app defaults if none are saved or the file can't be read.  The saved options are
        /// applied over the app defaults, so an option added in a later version starts at its app default.  That goes for
        /// the fields of the thresholds too: they're populated rather than replaced, so a field left out keeps its app
        /// default.  An explicit null still switches the option off.
        /// </summary>
        public SourceConnectionOptions Load()
        {
            var options = SourceConnectionOptions.AppDefaults;
            try
            {
                if (!File.Exists(FilePath)) return options;
                JsonConvert.PopulateObject(File.ReadAllText(FilePath), options, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Reuse });
                options.CustomCollections = null;
                return options.Validate() == null ? options : SourceConnectionOptions.AppDefaults;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error reading new connection defaults from {path}.  Using the app defaults.", FilePath);
                return SourceConnectionOptions.AppDefaults;
            }
        }

        /// <summary>
        /// Saves <paramref name="options"/> as the defaults for new connections.  Custom collections aren't saved - they run
        /// SQL with the service's credentials, so they are chosen for each set of connections in the service configuration tool.
        /// </summary>
        public void Save(SourceConnectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var error = options.Validate();
            if (error != null) throw new ArgumentException(error, nameof(options));

            var json = JsonConvert.SerializeObject(options, Formatting.Indented,
                new JsonSerializerSettings { ContractResolver = new IgnoreCustomCollectionsResolver() });
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, json);
        }

        /// <summary>Removes the saved defaults, so new connections use the app defaults</summary>
        public void Reset()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        private sealed class IgnoreCustomCollectionsResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (property.PropertyName == nameof(SourceConnectionOptions.CustomCollections)) property.ShouldSerialize = _ => false;
                return property;
            }
        }
    }
}
