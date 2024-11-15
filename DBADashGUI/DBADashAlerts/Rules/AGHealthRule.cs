using DBADashGUI.DBADashAlerts.Rules;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts
{
    internal class AGHealthRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.AGHealth;
        public override string AlertKey => "AG HEALTH";

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}