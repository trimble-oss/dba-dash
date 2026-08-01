using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class FailedLoginsRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.FailedLogins;

        public override string AlertKey => "FailedLogins";

        [Description("Evaluation period to apply the threshold over in minutes")]
        [DisplayName("Evaluation Period (Mins)")]
        public override int? EvaluationPeriodMins { get; set; } = 60;

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not >= 1M)
            {
                return (false, "Threshold must be >= 1");
            }
            else if (Threshold != decimal.Truncate(Threshold.Value))
            {
                return (false, "Threshold must be a whole number");
            }
            else if (Threshold > long.MaxValue)
            {
                return (false, "Threshold is too large");
            }
            else if (EvaluationPeriodMins is not (>= 1 and <= 1440))
            {
                return (false, "Evaluation Period must be between 1 and 1440");
            }
            return (true, string.Empty);
        }
    }
}
