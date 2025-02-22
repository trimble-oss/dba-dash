using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class AgentJobRule : AlertRuleBase
    {
        public override string AlertKey => "Job:{JobName}";

        public override RuleTypes RuleType => RuleTypes.AgentJob;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [Description("SQL Server agent job category to apply to.  Supports LIKE syntax.")]
        [DisplayName("Category"), Category("Filters")]
        public string Category { get; set; }

        [Description("SQL Server agent job name to apply to.  Supports LIKE syntax.")]
        [DisplayName("Job Name"), Category("Filters")]
        public string JobName { get; set; }

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}