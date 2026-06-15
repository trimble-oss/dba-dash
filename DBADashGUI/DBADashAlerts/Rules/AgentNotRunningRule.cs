using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class AgentNotRunningRule : AlertRuleBase
    {
        public override string AlertKey => RuleTypes.AgentNotRunning.ToString();
        public override RuleTypes RuleType => RuleTypes.AgentNotRunning;

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        [Browsable(false)]
        public override decimal? Threshold => null;

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}
