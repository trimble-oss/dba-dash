using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class DatabaseMailRule : AlertRuleBase
    {
        public override string AlertKey => "DatabaseMail";

        public override RuleTypes RuleType => RuleTypes.DatabaseMail;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        [Description("Database Mail status values to exclude from alerting. Supports LIKE syntax. " +
                     "The alert triggers when the collected Database Mail status is not 'STARTED' and doesn't match an excluded value. " +
                     "Empty list uses defaults: '15281|%' (Database Mail XPs disabled) and '229|%' (EXECUTE permission denied on sysmail_help_status_sp). " +
                     "Add 'STOPPED' to exclude instances where Database Mail is intentionally stopped.")]
        [DisplayName("Excluded Statuses"), Category("Filters")]
        public List<string> ExcludedStatuses { get; set; }

        /// <summary>New rule: pre-populate defaults so the user sees them in the PropertyGrid.</summary>
        public DatabaseMailRule()
        {
            ExcludedStatuses = new List<string> { "15281|%", "229|%" };
        }

        /// <summary>Deserialization: receives ExcludedStatuses directly from JSON, bypassing the new-rule default.</summary>
        [JsonConstructor]
        private DatabaseMailRule(List<string> excludedStatuses)
        {
            ExcludedStatuses = excludedStatuses ?? new List<string>();
        }

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}
