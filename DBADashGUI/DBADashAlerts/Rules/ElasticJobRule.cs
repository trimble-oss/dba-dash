using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    /// <summary>
    /// Alert when an elastic job fails (Alert.ElasticJobAlert_Upd) - the counterpart of <see cref="AgentJobRule"/> for the jobs of an
    /// elastic job agent.  The alert is raised against the agent's job database, and lists the databases the execution failed against.
    /// </summary>
    internal class ElasticJobRule : AlertRuleBase
    {
        public override string AlertKey => "ElasticJob:{JobName}";

        public override RuleTypes RuleType => RuleTypes.ElasticJob;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [Description("Elastic job name to apply to. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
        [DisplayName("Job Name"), Category("Filters")]
        public string JobName { get; set; }

        [Description("Elastic job name to exclude. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
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
