using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class DatabaseStatusRule : AlertRuleBase
    {
        public override string AlertKey => "DB:{DatabaseName}";

        public override RuleTypes RuleType => RuleTypes.DatabaseStatus;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        [Description("Database name to apply to. Supports LIKE syntax. Leave blank to apply to all databases.")]
        [DisplayName("Database Name"), Category("Filters")]
        public string DatabaseName { get; set; }

        [Description("States to exclude from alerting. Empty list uses defaults: ONLINE (0), RESTORING (1), COPYING (7), OFFLINE_SECONDARY (10). " +
                     "State values: 0=ONLINE, 1=RESTORING, 2=RECOVERING, 3=RECOVERY_PENDING, " +
                     "4=SUSPECT, 5=EMERGENCY, 6=OFFLINE, 7=COPYING, 10=OFFLINE_SECONDARY")]
        [DisplayName("Excluded States"), Category("Filters")]
        public List<int> ExcludedStates { get; set; }

        /// <summary>New rule: pre-populate defaults so the user sees them in the PropertyGrid.</summary>
        public DatabaseStatusRule()
        {
            ExcludedStates = new List<int> { 0, 1, 7, 10 };
        }

        /// <summary>Deserialization: receives ExcludedStates directly from JSON, bypassing the new-rule default.</summary>
        [JsonConstructor]
        private DatabaseStatusRule(List<int> excludedStates)
        {
            ExcludedStates = excludedStates ?? new List<int>();
        }

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}
