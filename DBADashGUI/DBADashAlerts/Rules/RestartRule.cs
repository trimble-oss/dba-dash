using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class RestartRule : AlertRuleBase
    {
        public override string AlertKey => "RESTART";

        public override RuleTypes RuleType => RuleTypes.Restart;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        public override int? EvaluationPeriodMins { get; set; } = 10;

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}