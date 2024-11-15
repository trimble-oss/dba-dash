using Newtonsoft.Json;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class DriveSpaceRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.DriveSpace;

        [Description("Use the critical drive threshold already configured. This can be used instead of specifying an alert threshold or in combination with an additional threshold.")]
        [DisplayName("Use Critical Status")]
        public bool UseCriticalStatus { get; set; } = true;

        [DefaultValue('\u0000')]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DisplayName("Drive Letter")]
        [Description("The drive letter to monitor or leave blank for all drives")]
        public char DriveLetter { get; set; }

        [DisplayName("Threshold Is Percentage?")]
        [Description("Set to false to alert on free space in MB")]
        public bool IsThresholdPercentage { get; set; } = true;

        [Description("Set a threshold or leave blank to filter on status only.  20 = 20% if threshold is percent is selected, or 20MB")]
        public override decimal? Threshold { get; set; }

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override string AlertKey => "DRIVE SPACE";

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not (>= 0M or null))
            {
                return (false, "Threshold must be >=0 OR null");
            }
            return (true, string.Empty);
        }
    }
}