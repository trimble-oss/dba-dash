using DBADash;
using DBADashService;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Runtime.InteropServices;

namespace DBADashConfig
{
    public class Helper
    {
        public static void CommandLineUpgrade()
        {
            try
            {
                var latest = Upgrade.GetLatestVersionAsync().GetAwaiter().GetResult();
                if (Upgrade.IsUpgradeIncomplete)
                {
                    Log.Warning(Upgrade.IncompleteUpgradeMessage);
                    Log.Information("Upgrade will be attempted");
                }
                if (Upgrade.IsUpgradeAvailable(latest) || Upgrade.IsUpgradeIncomplete)
                {
                    if (Upgrade.IsAdministrator)
                    {
                        Log.Information($"Upgrade is available to {latest.TagName}.  Initiating upgrade.");
                        Upgrade.UpgradeDBADashAsync(noExit: false).Wait();
                    }
                    else
                    {
                        Log.Information($"Upgrade is available to {latest.TagName}.  Please re-run as Administrator.");
                    }
                }
                else
                {
                    Log.Information($"Latest version is {latest.TagName}.  Upgrade is not available at this time. ");
                }
            }
            catch (AggregateException ex) when (ex.InnerException != null &&
                                                ex.InnerException.GetType() == typeof(Octokit.NotFoundException))
            {
                Log.Error("Upgrade script is not available.  Please check the upgrade instructions on the GitHub page");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running upgrade");
                Environment.Exit(1);
            }
        }

        public static async Task<DBADashSource?> GetSourceConnectionAsync(Options o, CollectionConfig config)
        {
            if (!string.IsNullOrEmpty(o.ConnectionString))
            {
                return config.GetSourceFromConnectionString(o.ConnectionString);
            }
            else if (!string.IsNullOrEmpty(o.ConnectionID))
            {
                try
                {
                    return await config.GetSourceConnectionAsync(o.ConnectionID);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ConnectionID not found: {ConnectionID}", o.ConnectionID);
                    Environment.Exit(1);
                }
            }
            else
            {
                Log.Error("ConnectionString or ConnectionID required");
                Environment.Exit(1);
            }

            return null;
        }

        public static async Task RemoveSourceConnectionAsync(Options o, CollectionConfig config, bool delete)
        {
            var sourceToRemove = await GetSourceConnectionAsync(o, config);
            if (sourceToRemove == null)
            {
                Log.Error("Source connection not found.");
                Environment.Exit(1);
                return;
            }

            Log.Information("Remove existing connection: {Connection}", sourceToRemove.SourceConnection.ConnectionForPrint);
            config.SourceConnections.Remove(sourceToRemove);
            if (delete)
            {
                MarkInstanceDeleted(config, sourceToRemove.ConnectionID);
            }
            SaveConfig(config, o);
        }

        public static void MarkInstanceDeleted(CollectionConfig config, string connectionId, bool isActive = false)
        {
            var status = isActive ? "active" : "deleted";
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentException("ConnectionID required");
            }
            foreach (var dest in config.AllDestinations.Where(d => d.Type == DBADashConnection.ConnectionType.SQL))
            {
                Log.Information("Marking instance {status} in {Destination}", status, dest.ConnectionForPrint);
                try
                {
                    if (isActive)
                    {
                        SharedData.RestoreInstance(connectionId, dest.ConnectionString);
                    }
                    else
                    {
                        SharedData.MarkInstanceDeleted(connectionId, dest.ConnectionString);
                    }
                }
                catch (SqlException ex) when (ex.Message == "Instance not found")
                {
                    Log.Warning("Instance with ConnectionID {ConnectionID} not found in {Destination}", connectionId, dest.ConnectionForPrint);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error marking instance {status} in {Destination}", status, dest.ConnectionForPrint);
                    Environment.Exit(1);
                }
            }
        }

        public static void CheckForUpdates()
        {
            var latest = Upgrade.GetLatestVersionAsync().GetAwaiter().GetResult();
            Log.Information("Upgrade Available: : {0}", Upgrade.IsUpgradeAvailable(latest));
            Log.Information("Current Version: {0}", Upgrade.CurrentVersion().ToString());
            Log.Information("Latest Version: {0}", latest.TagName);
            Log.Information("Release Date: {0}", latest.PublishedAt.ToString());
            Log.Information("URL: {0}", latest.Url);
            Console.WriteLine(latest.Body);
        }

        /// <summary>
        /// The extended events session the Deadlocks collection reads for this connection.  --CaptureDeadlocks
        /// is the shorthand for the session DBA Dash manages; --DeadlockXESessionName names any session,
        /// including system_health or one the DBA runs.  A name given explicitly wins, so passing both is not
        /// a conflict.  Blank switches the collection off - see DBADashSource.DeadlockXESessionName.
        /// </summary>
        private static string GetDeadlockXESessionName(Options o) =>
            o.CaptureDeadlocks && string.IsNullOrWhiteSpace(o.DeadlockXESessionName)
                ? DBADashSource.ManagedDeadlockXESessionName
                : o.DeadlockXESessionName;

        public static async Task AddSourceConnectionAsync(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ConnectionString))
            {
                Log.Error("ConnectionString required");
                Environment.Exit(1);
                return;
            }

            Log.Information("Add new connection: {Connection}", o.ConnectionString);

            var source = new DBADashSource()
            {
                IOCollectionLevel = (DBADashSource.IOCollectionLevels)o.IOCollectionLevel,
                ConnectionString = o.ConnectionString,
                NoWMI = o.NoWMI,
                CollectSessionWaits = !o.NoCollectSessionWaits,
                PlanCollectionEnabled = o.PlanCollectionEnabled,
                SlowQueryThresholdMs = o.SlowQueryThresholdMs,
                DeadlockXESessionName = GetDeadlockXESessionName(o),
                FlushDeadlockXERingBuffer = o.FlushDeadlockXERingBuffer,
                SlowQuerySessionMaxMemoryKB = o.SlowQuerySessionMaxMemoryKB,
                SlowQueryTargetMaxMemoryKB = o.SlowQueryTargetMaxMemoryKB,
                UseDualEventSession = o.UseDualEventSession ?? true,
                PersistXESessions = o.PersistXESessions ?? false,
                CollectTempDB = o.CollectTempDB ?? false,
                CollectTranBeginTime = o.CollectTranBeginTime ?? true,
                WriteToSecondaryDestinations = o.WriteToSecondaryDestinations ?? true,
                ScriptAgentJobs = o.ScriptAgentJobs ?? true,
                CollectTaskWaits = o.CollectTaskWaits ?? false,
                CollectCursors = o.CollectCursors ?? false
            };
            // Table size collection options (nullable: omit to use defaults)
            if (o.TableSizeCollectionThresholdMB.HasValue)
            {
                source.TableSizeCollectionThresholdMB = o.TableSizeCollectionThresholdMB.Value;
            }
            if (o.TableSizeDatabases != null)
            {
                source.TableSizeDatabases = o.TableSizeDatabases;
            }
            if (o.TableSizeMaxTableThreshold.HasValue)
            {
                source.TableSizeMaxTableThreshold = o.TableSizeMaxTableThreshold.Value;
            }
            if (o.TableSizeMaxDatabaseThreshold.HasValue)
            {
                source.TableSizeMaxDatabaseThreshold = o.TableSizeMaxDatabaseThreshold.Value;
            }
            if (!o.SkipValidation)
            {
                Log.Information("Validating connection...");
                source.SourceConnection.Validate();
                Log.Information("Validated");
            }
            if (o.ConnectionID != string.Empty)
            {
                source.ConnectionID = o.ConnectionID;
            }
            else if (!o.SkipValidation)
            {
                source.ConnectionID = await source.GetGeneratedConnectionIDAsync();
            }
            else if (source.SourceConnection.Type == DBADashConnection.ConnectionType.SQL)
            {
                source.SetConnectionIDFromBuilderIfNotSet();
                Log.Warning("Validation skipped & ConnectionID not specified. ConnectionID set to {ConnectionID} based on Data Source", source.ConnectionID);
            }
            if (!string.IsNullOrEmpty(o.SchemaSnapshotDBs) && o.SchemaSnapshotDBs != "<null>") // <null> was added for PowerShell script as passing a blank string results in an error with commandline parser
            {
                source.SchemaSnapshotDBs = o.SchemaSnapshotDBs;
            }
            if (o.PlanCollectionEnabled)
            {
                source.PlanCollectionCountThreshold = o.PlanCollectionCountThreshold;
                source.PlanCollectionCPUThreshold = o.PlanCollectionCPUThreshold;
                source.PlanCollectionDurationThreshold = o.PlanCollectionDurationThreshold;
                source.PlanCollectionMemoryGrantThreshold = o.PlanCollectionMemoryGrantThreshold;
            }
            // Naming a session says which session to read, not when to read it.  The Deadlocks collection is
            // disabled in the default schedule, so without a schedule the connection is configured for
            // deadlocks and still collects nothing.
            if (source.IsDeadlockCollectionEnabled &&
                !(config.GetSchedules().TryGetValue(CollectionType.Deadlocks, out var deadlockSchedule) &&
                  !string.IsNullOrWhiteSpace(deadlockSchedule?.Schedule)))
            {
                Log.Warning(
                    "Deadlock capture is set to read the {SessionName} session, but the Deadlocks collection doesn't have a schedule so no deadlocks will be collected.  Set a schedule for the Deadlocks collection.",
                    source.DeadlockXESessionName);
            }

            // check if connection exists before adding a new connection
            var oldSource = await config.FindSourceConnectionAsync(o.ConnectionString, source.ConnectionID);
            if (oldSource != null)
            {
                if (o.Replace)
                {
                    Log.Information("Replace existing connection");
                    config.SourceConnections.Remove(oldSource);
                }
                else
                {
                    Log.Warning("Source connection already exists.  Use --Replace to update the existing connection.");
                    Environment.Exit(0);
                }
            }
            config.SourceConnections.Add(source);
            SaveConfig(config, o);
        }

        public static void ListConnections(CollectionConfig config)
        {
            foreach (var cn in config.SourceConnections)
            {
                Console.WriteLine(cn.SourceConnection.EncryptedConnectionString);
            }
        }

        public static void ListConnections2(CollectionConfig config)
        {
            foreach (var cn in config.SourceConnections)
            {
                Console.WriteLine(cn.ConnectionID + "\t" + cn.SourceConnection.EncryptedConnectionString);
            }
        }

        public static async Task AddDestination(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ConnectionString))
            {
                Log.Error("ConnectionString required");
                Environment.Exit(1);
                return;
            }
            if (string.IsNullOrEmpty(config.Destination) || o.Option == Options.CommandLineActionOption.SetDestination) // Set primary destination
            {
                Log.Information("Setting destination connection");
                config.Destination = o.ConnectionString;
                if (!o.SkipValidation)
                {
                    Log.Information("Validating connection...");
                    await config.ValidateDestinationAsync();
                    Log.Information("Validated");
                }
            }
            else // Add additional destination
            {
                if (config.AllDestinations.Any(d => d.ConnectionString == o.ConnectionString))
                {
                    Log.Information("Destination connection already exists");
                    return;
                }

                var con = new DBADashConnection(o.ConnectionString);
                if (!o.SkipValidation)
                {
                    Log.Information("Validating connection...");

                    await CollectionConfig.ValidateDestinationAsync(con);
                    Log.Information("Validated");
                }

                Log.Information("Adding secondary destination");
                config.SecondaryDestinationConnections.Add(con);
            }
            SaveConfig(config, o);
        }

        public static void RemoveDestination(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ConnectionString))
            {
                Log.Error("ConnectionString required");
                Environment.Exit(1);
                return;
            }
            var toRemove = new DBADashConnection(o.ConnectionString);
            if (config.Destination == o.ConnectionString || config.DestinationConnection.ConnectionForPrint == toRemove.ConnectionForPrint)
            {
                config.Destination = string.Empty;
                Log.Warning("Warning: Primary Destination removed.");
            }
            else
            {
                var found = config.SecondaryDestinationConnections.FirstOrDefault(d =>
                    d.ConnectionString == o.ConnectionString ||
                    d.ConnectionForPrint == toRemove.ConnectionForPrint);
                if (found == null)
                {
                    Log.Error("Destination connection not found");
                    Environment.Exit(1);
                    return;
                }

                config.SecondaryDestinationConnections.Remove(found);
            }
            SaveConfig(config, o);
        }

        public static void SetServiceName(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ServiceName))
            {
                Log.Error("ServiceName is required with SetServiceName action");
                Environment.Exit(1);
            }
            else if (o.ServiceName == config.ServiceName)
            {
                Log.Information("ServiceName is already set to {ServiceName}", config.ServiceName);
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.Error("SetServiceName is only supported on Windows");
                Environment.Exit(1);
            }
            else if (ServiceTools.IsServiceInstalledByName(o.ServiceName)) // Check if a service exists with the specified name
            {
                Log.Error("ServiceName is already in use");
                Environment.Exit(1);
            }
            else if (ServiceTools.IsServiceInstalledByPath()) // Check if the service is already installed by location on disk
            {
                Log.Error("Service is already installed.  Please uninstall before setting a new service name");
                Environment.Exit(1);
            }
            else
            {
                Log.Information("Setting service name to {ServiceName}", o.ServiceName);
                config.ServiceName = o.ServiceName;
                SaveConfig(config, o);
            }
        }

        public static async Task SetPerfmonCounters(CollectionConfig config, Options o)
        {
            // A connection target (-c or --ConnectionID) switches to a per-connection override; otherwise
            // we edit the global list.  --PerfmonInherit only makes sense per-connection.
            var perConnection = !string.IsNullOrEmpty(o.ConnectionString) || !string.IsNullOrEmpty(o.ConnectionID);

            if (o.PerfmonInherit && !perConnection)
            {
                Log.Error("--PerfmonInherit requires a connection (-c or --ConnectionID).  The global list has nothing to inherit from.");
                Environment.Exit(1);
                return;
            }

            // Resolve the requested action into a tri-state result:
            //   null  = inherit the global list (per-connection only)
            //   empty = collect nothing (disabled)
            //   list  = these counters
            List<PerfmonCounter>? result;
            if (o.PerfmonInherit)
            {
                if (o.PerfmonDefaults || o.PerfmonClear || !string.IsNullOrWhiteSpace(o.PerfmonCounters))
                {
                    Log.Error("--PerfmonInherit cannot be combined with --PerfmonDefaults, --PerfmonCounters or --PerfmonClear.");
                    Environment.Exit(1);
                    return;
                }
                result = null;
            }
            else if (o.PerfmonClear)
            {
                if (o.PerfmonDefaults || !string.IsNullOrWhiteSpace(o.PerfmonCounters))
                {
                    Log.Error("--PerfmonClear cannot be combined with --PerfmonDefaults or --PerfmonCounters.");
                    Environment.Exit(1);
                    return;
                }
                result = new List<PerfmonCounter>();
            }
            else if (o.PerfmonDefaults || !string.IsNullOrWhiteSpace(o.PerfmonCounters))
            {
                try
                {
                    result = PerfmonCounter.BuildList(o.PerfmonDefaults, o.PerfmonCounters);
                }
                catch (ArgumentException ex)
                {
                    Log.Error("Invalid --PerfmonCounters value: {Message}", ex.Message);
                    Environment.Exit(1);
                    return;
                }
            }
            else
            {
                Log.Error("Nothing to do.  Specify --PerfmonDefaults and/or --PerfmonCounters, --PerfmonClear to disable{0}.",
                    perConnection ? ", or --PerfmonInherit to use the global list" : string.Empty);
                Environment.Exit(1);
                return;
            }

            if (perConnection)
            {
                var source = await GetSourceConnectionAsync(o, config);
                if (source == null)
                {
                    Log.Error("Source connection not found.");
                    Environment.Exit(1);
                    return;
                }
                source.PerfmonCounters = result;
                Log.Information("Perfmon counters for {Connection} set to {State}", source.SourceConnection.ConnectionForPrint,
                    result == null ? "inherit global" : result.Count == 0 ? "disabled" : $"{result.Count} counter(s)");
            }
            else
            {
                config.PerfmonCounters = result!; // never null on the global path (--PerfmonInherit is blocked above)
                Log.Information("Global perfmon counter list set to {State}",
                    result!.Count == 0 ? "disabled" : $"{result.Count} counter(s)");
            }

            SaveConfig(config, o);
        }


        /// <summary>
        /// Sets how often one collection runs, at service level or for a single connection.
        ///
        /// <para>Both levels, because they answer different questions: the service level schedule is how
        /// often this service collects something, and a connection override is how often it does so for the
        /// one instance that needs a different cadence.  The service combines the two, so an override
        /// replaces the schedule for that collection on that connection and leaves every other collection,
        /// and every other connection, alone.</para>
        ///
        /// <para>Only the collection named is written.  A schedule this does not mention keeps whatever it
        /// had, which is what makes it safe to run against a configured service.</para>
        /// </summary>
        public static async Task SetScheduleAsync(CollectionConfig config, Options o)
        {
            // A connection target (-c or --ConnectionID) switches to a per-connection override; otherwise
            // the service level schedule is edited.  Mirrors SetPerfmonCounters.
            var perConnection = !string.IsNullOrEmpty(o.ConnectionString) || !string.IsNullOrEmpty(o.ConnectionID);

            if (!CollectionTypeLegacyNames.TryParse(o.CollectionType ?? string.Empty, out var collectionType))
            {
                Log.Error("--CollectionType is required and must name a collection.  Valid values: {Types}",
                    string.Join(", ", Enum.GetNames<CollectionType>()));
                Environment.Exit(1);
                return;
            }

            if (o.ScheduleClear && (o.Schedule != null || o.RunOnServiceStart.HasValue))
            {
                Log.Error("--ScheduleClear cannot be combined with --Schedule or --RunOnServiceStart.");
                Environment.Exit(1);
                return;
            }

            if (!o.ScheduleClear && o.Schedule == null && !o.RunOnServiceStart.HasValue)
            {
                Log.Error("Nothing to do.  Specify --Schedule and/or --RunOnServiceStart, or --ScheduleClear to remove the override.");
                Environment.Exit(1);
                return;
            }

            if (o.Schedule != null && !IsValidSchedule(o.Schedule))
            {
                Log.Error("Invalid --Schedule value {Schedule}.  Expected a cron expression such as \"0 0/5 * * * ?\", a whole number of seconds, or an empty value to disable the collection.",
                    o.Schedule);
                Environment.Exit(1);
                return;
            }

            DBADashSource? source = null;
            if (perConnection)
            {
                source = await GetSourceConnectionAsync(o, config);
                if (source == null)
                {
                    Log.Error("Source connection not found.");
                    Environment.Exit(1);
                    return;
                }
            }

            var target = perConnection ? source!.CollectionSchedules : config.CollectionSchedules;
            var scope = perConnection ? source!.SourceConnection.ConnectionForPrint : "the service";

            if (o.ScheduleClear)
            {
                if (target?.Remove(collectionType) != true)
                {
                    Log.Information("{CollectionType} had no schedule override for {Scope} - nothing to clear",
                        collectionType, scope);
                    return;
                }
                // An override object holding nothing says nothing, so it is dropped rather than left behind.
                if (target!.Count == 0) target = null;
                Apply(config, source, perConnection, target);
                Log.Information("{CollectionType} schedule override removed for {Scope}", collectionType, scope);
                SaveConfig(config, o);
                return;
            }

            // What this level would run without an override of its own: the shipped defaults for the
            // service, and the service level schedule for a connection.  Used so --RunOnServiceStart on its
            // own keeps the schedule already in effect rather than writing a blank one, which would read as
            // "disabled".
            var inherited = perConnection ? config.GetSchedules() : CollectionSchedules.DefaultSchedules;
            CollectionSchedule? current = null;
            if (target?.TryGetValue(collectionType, out current) != true)
            {
                inherited.TryGetValue(collectionType, out current);
            }

            var schedule = o.Schedule ?? current?.Schedule ?? string.Empty;
            var runOnServiceStart = o.RunOnServiceStart ?? current?.RunOnServiceStart ?? true;

            target ??= new CollectionSchedules();
            target[collectionType] = new CollectionSchedule { Schedule = schedule, RunOnServiceStart = runOnServiceStart };
            Apply(config, source, perConnection, target);

            Log.Information("{CollectionType} schedule for {Scope} set to {Schedule} (RunOnServiceStart={RunOnServiceStart})",
                collectionType, scope,
                string.IsNullOrWhiteSpace(schedule) ? "disabled" : schedule, runOnServiceStart);

            WarnIfCollectionDisabledByConfiguration(config, source, perConnection, collectionType, schedule);

            SaveConfig(config, o);
        }

        private static void Apply(CollectionConfig config, DBADashSource? source, bool perConnection,
            CollectionSchedules? schedules)
        {
            if (perConnection)
            {
                source!.CollectionSchedules = schedules;
            }
            else
            {
                config.CollectionSchedules = schedules;
            }
        }

        /// <summary>
        /// A schedule says when a collection runs, not that it will: deadlocks need a session to read and
        /// slow queries a threshold to capture against.  The mirror of the warning <see cref="AddSourceConnectionAsync"/>
        /// gives for the opposite mistake, and worth saying here because scheduling something that then
        /// collects nothing looks like a bug in the collection rather than a gap in the configuration.
        /// </summary>
        private static void WarnIfCollectionDisabledByConfiguration(CollectionConfig config, DBADashSource? source,
            bool perConnection, CollectionType collectionType, string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule)) return;

            var affected = perConnection
                ? new[] { source! }
                : config.SourceConnections.ToArray();
            if (affected.Length == 0 || !affected.All(src => src.IsCollectionDisabledByConfiguration(collectionType))) return;

            Log.Warning("{CollectionType} is scheduled but switched off by configuration for {Scope}, so nothing will be collected.  {Advice}",
                collectionType,
                perConnection ? source!.SourceConnection.ConnectionForPrint : "every connection",
                collectionType == CollectionType.Deadlocks
                    ? "Set a session to read with --CaptureDeadlocks or --DeadlockXESessionName."
                    : "Set a threshold with --SlowQueryThresholdMs.");
        }

        /// <summary>
        /// Empty disables the collection.  Otherwise the service takes either a whole number of seconds or a
        /// cron expression - see SchedulerService - so both are accepted rather than only cron.
        /// </summary>
        private static bool IsValidSchedule(string schedule) =>
            string.IsNullOrWhiteSpace(schedule)
            || (int.TryParse(schedule, out var seconds)
                ? seconds > 0
                : Quartz.CronExpression.IsValidExpression(schedule));

        public static void SaveConfig(CollectionConfig config, Options o)
        {
            Log.Information("Saving config");

            var backup = config.ConfigBackupRetentionDays > 0 && (!o.NoBackupConfig);
            config.Save(backup);

            Log.Information("Complete.  Restart the service to apply the config change");
        }

        public static void EncryptConfig(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.EncryptionPassword))
            {
                Log.Error("EncryptionPassword parameter not supplied");
                Environment.Exit(1);
                return;
            }
            EncryptedConfig.SetPassword(o.EncryptionPassword, true);
            config.EncryptionOption = BasicConfig.EncryptionOptions.Encrypt;
            SaveConfig(config, o);
        }

        public static async Task PopulateConnectionIDAsync(CollectionConfig config, Options o, bool force)
        {
            var errors = 0;
            var succeeded = 0;
            foreach (var source in config.SourceConnections.Where(src => src.SourceConnection.Type == DBADashConnection.ConnectionType.SQL && string.IsNullOrEmpty(src.ConnectionID)))
            {
                try
                {
                    source.ConnectionID = await source.GetGeneratedConnectionIDAsync();
                    Log.Information("ConnectionID {ConnectionID} generated", source.ConnectionID);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    if (force)
                    {
                        source.SetConnectionIDFromBuilderIfNotSet();
                        succeeded++;
                        Log.Warning(ex, "Error generating ConnectionID for {ConnectionString}.  Connection set to {ConnectionID} from connection string builder.", source.SourceConnection.ConnectionForPrint, source.ConnectionID);
                    }
                    else
                    {
                        Log.Error(ex, "Error generating ConnectionID for {ConnectionString}", source.SourceConnection.ConnectionForPrint);
                        errors++;
                    }
                }
            }
            if (succeeded > 0)
            {
                Log.Information("ConnectionID generated for {Count} source connections.", succeeded);
                SaveConfig(config, o);
            }
            if (errors == 0 && succeeded == 0)
            {
                Log.Information("No source connections found without a ConnectionID");
            }
            if (errors > 0)
            {
                Log.Error("{Count} errors occurred generating ConnectionID.  Please review the log for details.", errors);
                Environment.Exit(1);
            }
        }

        public static void SetAWS(CollectionConfig config, Options o)
        {
            if (!string.IsNullOrEmpty(o.AWSAccessKey))
            {
                if (string.IsNullOrEmpty(o.AWSSecretKey))
                {
                    Log.Error("AWSSecretKey is required when setting AWSAccessKey");
                    Environment.Exit(1);
                }
                else if (!string.IsNullOrEmpty(o.AWSProfile))
                {
                    Log.Error("Please set either using --AWSProfile or both --AWSAccessKey and --AWSSecretKey");
                    Environment.Exit(1);
                }
                config.AccessKey = o.AWSAccessKey;
                config.SecretKey = o.AWSSecretKey;
                config.AWSProfile = null;
            }
            else if (!string.IsNullOrEmpty(o.AWSProfile))
            {
                config.AWSProfile = o.AWSProfile;
                config.AccessKey = null;
                config.SecretKey = null;
            }
            else
            {
                Log.Information("Clearing AWS configuration.  Use --AWSAccessKey and --AWSSecretKey or --AWSProfile with -a SetAWS to configure.");
                config.AWSProfile = null;
                config.AccessKey = null;
                config.SecretKey = null;
            }
            SaveConfig(config, o);
        }
    }
}