using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class OfflineRule : AlertRuleBase
    {
        public override string AlertKey => "OFFLINE";

        public override RuleTypes RuleType => RuleTypes.Offline;

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