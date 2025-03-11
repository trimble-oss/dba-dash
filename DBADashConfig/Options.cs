using CommandLine;

namespace DBADashConfig
{
    public class Options
    {
        [Option('c', "connection", Required = false, HelpText = "The connection string for the SQL Instance you want to monitor.  e.g. \"Data Source = MYSERVER;Integrated Security=True;Encrypt=True;Trust Server Certificate=True\"")]
        public string ConnectionString { get; set; } = "";

        [Option('a', "action", Required = true, HelpText = @"Actions:

Add - Add a source connection to monitor. (Use -c to specify a connection string)
Remove - Remove a source connection
RemoveAndDelete - Remove a source connection and mark as deleted in the repository database
Delete - mark connection as deleted in the repository database.  Use RemoveAndDelete to also remove the connection from the config.
Restore - Mark instance active in the repository database.
SetDestination - Connection to the repository database
List - List source connections
List2 - List source connections with ConnectionID
Count - Count source connections
GetServiceName - Return the name of the service
CheckForUpdates - Check if a new version of DBA Dash is available
Update - Update to the latest version of DBA Dash
SetServiceName - Change the name of the DBA Dash service
Encrypt - Encrypt the config file with a password (--EncryptionPassword)
Decrypt - Decrypt the config file. --DecryptionPassword can be used if required.
SetConfigFileBackupRetention - Specify how long to keep config file backups. --RetentionDays
PopulateConnectionID - Add ConnectionID to source connections without a ConnectionID
PopulateConnectionID2 - Add ConnectionID to source connections without a ConnectionID.  If connection fails, set ConnectionID based on Data Source in connection string.")]
        public CommandLineActionOption Option { get; set; }

        [Option('r', "Replace", Required = false, HelpText = "Option to replace the existing connection if it already exists", Default = false)]
        public bool Replace { get; set; }

        [Option("NoWMI", Required = false, HelpText = "Don't collect any data via Windows Management Instruction (WMI). All data is collected via SQL queries. (WMI allows us to collect data for ALL drives and some other info we might not be able to get via SQL.  Service account needs permissions for WMI though)")]
        public bool NoWMI { get; set; }

        [Option("SlowQueryThresholdMs", Default = -1, Required = false, HelpText = "Set to -1 to disable extended event capture of rpc/batch completed events.  Set to 1000 to capture queries that take longer than 1second to run (or other value in milliseconds as required)")]
        public int SlowQueryThresholdMs { get; set; }

        [Option("PlanCollectionEnabled", Default = false, Required = false, HelpText = "Set this switch to enable plan collection.")]
        public bool PlanCollectionEnabled { get; set; }

        [Option("PlanCollectionCountThreshold", Default = 2, Required = false, HelpText = "Collect plan if we have >=$PlanCollectionCountThreshold running queries with the same plan")]
        public int PlanCollectionCountThreshold { get; set; }

        [Option("PlanCollectionCPUThreshold", Default = 1000, Required = false, HelpText = "Collect plan if CPU usage is higher than the specified threshold (ms)")]
        public int PlanCollectionCPUThreshold { get; set; }

        [Option("PlanCollectionDurationThreshold", Default = 10000, Required = false, HelpText = "Collect plan if duration is higher than the specified threshold (ms)")]
        public int PlanCollectionDurationThreshold { get; set; }

        [Option("PlanCollectionMemoryGrantThreshold", Default = 6400, Required = false, HelpText = "Collect plan if memory grant is higher than the specified threshold (in pages)")]
        public int PlanCollectionMemoryGrantThreshold { get; set; }

        [Option("SchemaSnapshotDBs", Default = "", Required = false, HelpText = "Comma-separated list of databases to include in a schema snapshot.  Use \" * \" to snapshot all databases.")]
        public string? SchemaSnapshotDBs { get; set; }

        [Option("SkipValidation", Default = false, Required = false, HelpText = "Option to skip the validation check on the source connection string")]
        public bool SkipValidation { get; set; }

        [Option("NoBackupConfig", Default = false, Required = false, HelpText = "Default is to save a copy of the config when modifying.  Use this switch to disable.")]
        public bool NoBackupConfig { get; set; }

        [Option("NoCollectSessionWaits", Default = false, Required = false, HelpText = "Default is to collect session waits for running queries.  Use this switch to disable.")]
        public bool NoCollectSessionWaits { get; set; }

        [Option("SlowQuerySessionMaxMemoryKB", Default = 4096, Required = false, HelpText = "Max memory for extended events session")]
        public int SlowQuerySessionMaxMemoryKB { get; set; }

        [Option("SlowQueryTargetMaxMemoryKB", Default = -1, Required = false, HelpText = "Max memory target parameter for ring_buffer")]
        public int SlowQueryTargetMaxMemoryKB { get; set; }

        [Option("UseDualEventSession", Default = true, Required = false, HelpText = "Use two event sessions turned on/off alternatively.  Used to limit event loss while session is turned off/on to flush events.")]
        public bool? UseDualEventSession { get; set; }

        [Option("PersistXESessions", Default = false, Required = false, HelpText = "Allows customization of event sessions by persisting them instead of removing them when the service is shutdown.")]
        public bool? PersistXESessions { get; set; }

        [Option("ConnectionID", Default = "", Required = false, HelpText = "The ConnectionID is used to uniquely identify the SQL Instance in the repository database.  The ConnectionID is automatically assigned to @@SERVERNAME but you can override this with a custom value.  If you change the ConnectionID for an existing server it will appear as a new instance in the repository database.")]
        public string ConnectionID { get; set; } = "";

        [Option("ServiceName", Default = "", Required = false, HelpText = "Use with -a SetServiceName to set the service name for the DBA Dash service.")]
        public string ServiceName { get; set; } = "";

        [Option("IOCollectionLevel", Default = 1, Required = false,
            HelpText = "IO Collection Level.  1 = Full, 2 = InstanceOnly, 3 = Drive, 4= Database, 5 = DriveAndDatabase")]
        public int IOCollectionLevel { get; set; } = 1;

        [Option("EncryptionPassword", Required = false,
            HelpText = "To be used with the Encrypt option")]
        public string? EncryptionPassword { get; set; }

        [Option("DecryptionPassword", Required = false,
            HelpText = "Password to decrypt current config")]
        public string? DecryptionPassword { get; set; }

        [Option("SavePassword", Required = false,
            HelpText = "Use in combination with --DecryptionPassword.  Password will be saved for the current user, protected with DPAPI. ")]
        public bool SavePassword { get; set; }

        [Option("RetentionDays", Required = false,
            HelpText = "Use with action SetConfigFileBackupRetention.")]
        public int RetentionDays { get; set; }

        public enum CommandLineActionOption
        {
            Add,
            Remove,
            SetDestination,
            GetDestination,
            List,
            List2,
            Count,
            GetServiceName,
            CheckForUpdates,
            Update,
            SetServiceName,
            Encrypt,
            Decrypt,
            SetConfigFileBackupRetention,
            AddDestination,
            RemoveDestination,
            RemoveAndDelete,
            Delete,
            Restore,
            PopulateConnectionID,
            PopulateConnectionID2
        }
    }
}