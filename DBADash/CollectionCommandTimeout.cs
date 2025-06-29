using System.Collections.Generic;
using System.IO;
using System;
using System.Data;
using Serilog;
using Newtonsoft.Json;

namespace DBADash
{
    public static class CollectionCommandTimeout
    {
        public static CommandTimeoutSettings Settings { get; private set; } = new();

        public static int GetDefaultCommandTimeout() => Settings.DefaultCommandTimeout;

        private static readonly JsonSerializerSettings serializationOpts = new JsonSerializerSettings
        {
            // Ignore unknown enum values
            Error = (_, args) =>
            {
                if (args.CurrentObject is Dictionary<CollectionType, int> &&
                    args.ErrorContext.Error is JsonSerializationException)
                {
                    args.ErrorContext.Handled = true;
                    Log.Warning($"Invalid collection type '{args.ErrorContext.Member}' in '{FilePath}'");
                }
            }
        };

        static CollectionCommandTimeout()
        {
            LoadCommandTimeouts();
            Log.Debug("Timeout  Settings:\n" + Settings.ToJson());
        }

        private static void LoadCommandTimeouts()
        {
            // Load the default command timeouts
            if (!File.Exists(FilePath))
            {
                Log.Information("Using default command timeouts");
                return;
            }

            try
            {
                var json = File.ReadAllText(FilePath);

                Settings = JsonConvert.DeserializeObject<CommandTimeoutSettings>(json, serializationOpts);

                Log.Information($"Custom command timeouts loaded from {FilePath}");
            }
            catch (JsonException ex)
            {
                // Ignore any errors in the JSON file and use the default command timeouts
                Log.Warning(ex, $"Error loading custom command timeouts from {FilePath}");
                Settings = new CommandTimeoutSettings();
            }
        }

        public static CommandTimeoutSettingsBase GetCustomTimeouts()
        {
            if (!File.Exists(FilePath))
            {
                return new CommandTimeoutSettingsBase();
            }
            var json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<CommandTimeoutSettingsBase>(json, serializationOpts);
        }

        private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commandTimeouts.json");

        public static int GetCommandTimeout(this CollectionType type)
        {
            return Settings.CollectionCommandTimeouts.TryGetValue(type, out var value) ? value : Settings.DefaultCommandTimeout;
        }

        public class CommandTimeoutSettings : CommandTimeoutSettingsBase
        {
            // Defaults supplied in this class will be used unless user supplies value in commandTimeouts.json
            public new Dictionary<CollectionType, int> CollectionCommandTimeouts { get; } = new()
            {
                { CollectionType.TableSize, 900 },
                { CollectionType.DatabasePermissions, 900 },
                { CollectionType.DatabasePrincipals, 900 },
                { CollectionType.DatabaseRoleMembers, 900 },
                { CollectionType.IdentityColumns, 900 },
                { CollectionType.SlowQueries, 90 },
                // Set timeouts for hourly schedules to 120, and midnight and elevenPM schedules to 300
                { CollectionType.ServerProperties, 120 },
                { CollectionType.Databases, 120 },
                { CollectionType.SysConfig, 120 },
                { CollectionType.Drives, 120 },
                { CollectionType.DBFiles, 120 },
                { CollectionType.Backups, 120 },
                { CollectionType.LogRestores, 120 },
                { CollectionType.ServerExtraProperties, 120 },
                { CollectionType.DBConfig, 120 },
                { CollectionType.Corruption, 120 },
                { CollectionType.OSInfo, 120 },
                { CollectionType.TraceFlags, 120 },
                { CollectionType.DBTuningOptions, 120 },
                { CollectionType.AzureDBServiceObjectives, 120 },
                { CollectionType.LastGoodCheckDB, 120 },
                { CollectionType.Alerts, 120 },
                { CollectionType.CustomChecks, 120 },
                { CollectionType.DatabaseMirroring, 120 },
                { CollectionType.Jobs, 120 },
                { CollectionType.AzureDBResourceGovernance, 120 },
                { CollectionType.SchemaSnapshot, 1200 },
                { CollectionType.ServerPrincipals, 300 },
                { CollectionType.ServerRoleMembers, 300 },
                { CollectionType.ServerPermissions, 300 },
                { CollectionType.VLF, 300 },
                { CollectionType.DriversWMI, 300 },
                { CollectionType.OSLoadedModules, 300 },
                { CollectionType.ResourceGovernorConfiguration, 300 },
                { CollectionType.DatabaseQueryStoreOptions, 300 },
                { CollectionType.DatabasesHADR, 120 },
                { CollectionType.AvailabilityGroups, 120 },
                { CollectionType.AvailabilityReplicas, 120 },
                { CollectionType.MemoryUsage, 120 }
            };

            public new int DefaultCommandTimeout { get; } = 60;
        }

        public class CommandTimeoutSettingsBase
        {
            public Dictionary<CollectionType, int> CollectionCommandTimeouts { get; set; } = new();

            public int? DefaultCommandTimeout { get; set; }

            public DataTable CollectionCommandTimeoutsAsDataTable()
            {
                var dt = new DataTable();
                dt.Columns.Add("CollectionType", typeof(string));
                dt.Columns.Add("Timeout", typeof(int));
                dt.PrimaryKey = new[] { dt.Columns[0] };
                if (CollectionCommandTimeouts == null) return dt;
                foreach (var collectionCommandTimeout in CollectionCommandTimeouts)
                {
                    var row = dt.NewRow();
                    row["CollectionType"] = collectionCommandTimeout.Key;
                    row["Timeout"] = collectionCommandTimeout.Value;
                    dt.Rows.Add(row);
                }
                return dt;
            }

            public void SetFromDataTable(DataTable dt)
            {
                CollectionCommandTimeouts = new();
                foreach (DataRow row in dt.Rows)
                {
                    if (!Enum.TryParse<CollectionType>(row["CollectionType"].ToString(), out var collectionType)) continue;
                    CollectionCommandTimeouts.Add(collectionType, (int)row["Timeout"]);
                }
            }

            public string ToJson()
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                return JsonConvert.SerializeObject(this, settings);
            }

            public void Save()
            {
                if (DefaultCommandTimeout == null && CollectionCommandTimeouts.Count == 0)
                {
                    if (File.Exists(FilePath))
                    {
                        File.Delete(FilePath);
                    }
                }
                else
                {
                    File.WriteAllText(FilePath, ToJson());
                }
            }
        }
    }
}