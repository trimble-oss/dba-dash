using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class BackupFreshnessRule : AlertRuleBase
    {
        public enum BackupTypes
        {
            Full,
            Diff,
            Log
        }

        public override RuleTypes RuleType => RuleTypes.BackupFreshness;

        [JsonConverter(typeof(StringEnumConverter))]
        [DisplayName("Backup Type")]
        [Description("The backup type to monitor for freshness.")]
        public BackupTypes BackupType { get; set; } = BackupTypes.Full;

        [Description("Database name to apply to. Supports LIKE syntax. Leave blank to apply to all eligible databases.")]
        [DisplayName("Database Name"), Category("Filters")]
        public string DatabaseName { get; set; }

        [Description("Database name pattern to exclude. Supports LIKE syntax. Leave blank for no exclusions.")]
        [DisplayName("Exclude Database Name"), Category("Filters")]
        public string ExcludedDatabaseName { get; set; }

        [Description("Suppress alerts for databases created more recently than this many minutes ago. Leave blank to disable.")]
        [DisplayName("Minimum Database Age (Mins)"), Category("Filters")]
        public int? MinimumDatabaseAgeMins { get; set; }

        public override string AlertKey => "Backup " + BackupType + ": {DatabaseName}";

        [Description("Threshold in minutes since the last successful backup of the selected type.")]
        public override decimal? Threshold { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not >= 0M)
            {
                return (false, "Threshold must be >=0");
            }

            if (MinimumDatabaseAgeMins is < 0)
            {
                return (false, "Minimum Database Age (Mins) must be >=0");
            }

            return (true, string.Empty);
        }
    }
}
