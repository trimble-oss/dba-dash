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

        private const int MaxOfflineDurationMins = 1440;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [DisplayName("Offline Duration")]
        [Description("How long the instance must be offline before the alert is triggered.  Use this to avoid alerts for short connectivity blips.  0 = alert as soon as the instance is detected as offline.  Max 1 day.")]
        public override int? EvaluationPeriodMins { get; set; } = 0;

        public override (bool isValid, string message) Validate()
        {
            return EvaluationPeriodMins switch
            {
                < 0 => (false, "Offline Duration can't be negative"),
                > MaxOfflineDurationMins => (false, $"Offline Duration can't be more than {MaxOfflineDurationMins} mins (1 day)"),
                _ => (true, string.Empty)
            };
        }
    }
}