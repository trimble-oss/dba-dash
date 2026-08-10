using System.ComponentModel;
using System.Drawing.Design;
using DBADashGUI.Pickers;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class FailedLoginsRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.FailedLogins;

        public override string AlertKey => "FailedLogins";

        [Description("Evaluation period to apply the threshold over. Enter days / hours / minutes.")]
        [DisplayName("Evaluation Period")]
        [TypeConverter(typeof(MinuteDurationConverter))]
        [Editor(typeof(MinuteDurationEditor), typeof(UITypeEditor))]
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
