using System.ComponentModel;

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

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}
