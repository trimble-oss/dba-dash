using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class BackupChainRiskRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.BackupChainRisk;

        [Description("Database name to apply to. Supports LIKE syntax. Leave blank to apply to all eligible databases.")]
        [DisplayName("Database Name"), Category("Filters")]
        public string DatabaseName { get; set; }

        [Description("Database name pattern to exclude. Supports LIKE syntax. Leave blank for no exclusions.")]
        [DisplayName("Exclude Database Name"), Category("Filters")]
        public string ExcludedDatabaseName { get; set; }

        [Description("Suppress alerts for databases created more recently than this many minutes ago. Leave blank to disable.")]
        [DisplayName("Minimum Database Age (Mins)"), Category("Filters")]
        public int? MinimumDatabaseAgeMins { get; set; }

        public override string AlertKey => "Backup Chain Risk: {DatabaseName}";

        [Description("Maximum allowed age in minutes for the last log backup before the database is considered at risk.")]
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
