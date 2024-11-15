using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class CollectionDatesRule : AlertRuleBase
    {
        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override RuleTypes RuleType => RuleTypes.CollectionDates;

        [DisplayName("Collection Reference"), Description("The collection type to alert on. See Collection Dates tab.\ne.g. Instance, CPU.")]
        public string Reference { get; set; }

        public override string AlertKey => "COLLECTION DATES";

        [Description("Set a threshold in minutes or leave blank to rely on critical status only")]
        public override decimal? Threshold { get; set; }

        [Description("Use the critical threshold already configured. This can be used instead of specifying an alert threshold or in combination with an additional threshold.")]
        [DisplayName("Use Critical Status")]
        public bool UseCriticalStatus { get; set; }

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not (>= 0M or null))
            {
                return (false, "Threshold must be >=0 OR null");
            }
            if (Threshold == null && !UseCriticalStatus)
            {
                return (false, "Set a threshold or check use critical status");
            }
            return (true, string.Empty);
        }
    }
}