using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    public enum SpaceMeasures
    {
        Used,
        Allocated
    }

    internal class AzureDBSpaceRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.AzureDBSpace;

        [Description("Use the critical % Max Size threshold already configured for the database (based on allocated space). This can be used instead of specifying an alert threshold or in combination with an additional threshold.")]
        [DisplayName("Use Critical Status")]
        public bool UseCriticalStatus { get; set; } = true;

        [Description("Measure used space or allocated space as a percentage of the database max size when evaluating the threshold.  Used space reflects how full the database is.")]
        [DisplayName("Space Measure")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SpaceMeasures SpaceMeasure { get; set; } = SpaceMeasures.Used;

        [Description("Percentage of the database max size at which to alert (e.g. 90 = 90%) or leave blank to filter on status only.  If Use Critical Status is also selected, both conditions need to be met.")]
        public override decimal? Threshold { get; set; }

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override string AlertKey => "AzureDBSpace";

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not (>= 0M and <= 100M or null))
            {
                return (false, "Threshold must be between 0 and 100 OR null");
            }
            if (!UseCriticalStatus && Threshold == null)
            {
                return (false, "Specify a threshold or select Use Critical Status");
            }
            return (true, string.Empty);
        }
    }
}
