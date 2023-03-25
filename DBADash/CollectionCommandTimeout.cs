using System.Collections.Generic;
using System.IO;
using System;
using Serilog;
using Newtonsoft.Json;

namespace DBADash
{
    internal static class CollectionCommandTimeout
    {
        private static CommandTimeoutSettings settings = new();

        public static int GetDefaultCommandTimeout()=>settings.DefaultCommandTimeout;

        static CollectionCommandTimeout()
        {
            LoadCommandTimeouts();
            Log.Debug("Timeout  settings:\n" + JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static void LoadCommandTimeouts()
        {
            // Load the default command timeouts
            string filePath = GetFilePath();
            if (!File.Exists(filePath))
            {
                Log.Information("Using default command timeouts");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);

                var serializationOpts = new JsonSerializerSettings
                {
                    // Ignore unknown enum values
                    Error = (_, args) =>
                    {
                        if (args.CurrentObject is Dictionary<CollectionType, int> &&
                            args.ErrorContext.Error is JsonSerializationException)
                        {
                            args.ErrorContext.Handled = true;
                            Log.Warning($"Invalid collection type '{args.ErrorContext.Member}' in '{filePath}'");
                        }
                    }
                };
                settings = JsonConvert.DeserializeObject<CommandTimeoutSettings>(json, serializationOpts);
                
                Log.Information($"Custom command timeouts loaded from {filePath}");
            }
            catch (JsonException ex)
            {
                // Ignore any errors in the JSON file and use the default command timeouts
                Log.Warning(ex, $"Error loading custom command timeouts from {filePath}");
            }
        }

        private static string GetFilePath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commandTimeouts.json");

        public static int GetCommandTimeout(this CollectionType type)
        {
            return settings.CollectionCommandTimeouts.TryGetValue(type, out var value) ? value : settings.DefaultCommandTimeout;
        }

        private class CommandTimeoutSettings
        {
            // Defaults supplied in this class will be used unless user supplies value in commandTimeouts.json
            public Dictionary<CollectionType, int> CollectionCommandTimeouts { get; } = new()
            {
                { CollectionType.DatabasePermissions, 900 },
                { CollectionType.DatabasePrincipals, 900 },
                { CollectionType.DatabaseRoleMembers, 900 },
                { CollectionType.IdentityColumns, 900 },
                { CollectionType.SlowQueries, 90 }
            };
            public int DefaultCommandTimeout { get; } = 60;

        }
    }


}