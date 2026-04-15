using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class AgentJobRule : AlertRuleBase
    {
        public override string AlertKey => "Job:{JobName}";

        public override RuleTypes RuleType => RuleTypes.AgentJob;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [Description("SQL Server agent job category to apply to. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
        [DisplayName("Category"), Category("Filters")]
        public string Category { get; set; }

        [Description("SQL Server agent job name to apply to. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
        [DisplayName("Job Name"), Category("Filters")]
        public string JobName { get; set; }

        [Description("SQL Server agent job category to exclude. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
        [DisplayName("Exclude Category"), Category("Filters")]
        public string ExcludeCategory { get; set; }

        [Description("SQL Server agent job name to exclude. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
        [DisplayName("Exclude Job Name"), Category("Filters")]
        public string ExcludeJobName { get; set; }

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}