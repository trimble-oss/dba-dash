using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class ElasticPoolSpaceRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.ElasticPoolSpace;

        [Description("Use the critical elastic pool storage threshold already configured (based on allocated space). This can be used instead of specifying an alert threshold or in combination with an additional threshold.")]
        [DisplayName("Use Critical Status")]
        public bool UseCriticalStatus { get; set; } = true;

        [Description("Measure allocated space or used space as a percentage of the pool storage limit when evaluating the threshold.  Databases can't grow once allocated space reaches the pool storage limit.")]
        [DisplayName("Space Measure")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SpaceMeasures SpaceMeasure { get; set; } = SpaceMeasures.Allocated;

        [Description("Percentage of the elastic pool storage limit at which to alert (e.g. 90 = 90%) or leave blank to filter on status only.  If Use Critical Status is also selected, both conditions need to be met.  Applies to elastic pools collected via the master database connection.")]
        public override decimal? Threshold { get; set; }

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override string AlertKey => "ElasticPoolSpace";

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
