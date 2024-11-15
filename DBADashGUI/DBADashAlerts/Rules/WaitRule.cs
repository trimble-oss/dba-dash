using Newtonsoft.Json;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class WaitRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.Wait;

        [Description("The type of wait to alert on.  A LIKE filter is applied supporting % wildcard etc.")]
        [DisplayName("Wait Type")]
        public string WaitType { get; set; }

        [Description("Wait is normalized to per second per core.  Set to false if you don't want the wait divided by the number of CPU cores.")]
        [DisplayName("Per Core")]
        public bool IsPerCore { get; set; } = true;

        public override string AlertKey => "WAIT - " + WaitType;

        [JsonIgnore]
        [Description("Threshold (ms/sec or ms/sec/core) at which to trigger the alert")]
        [DisplayName("Threshold (ms/sec)")]
        public override decimal? Threshold { get; set; }

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not >= 0M)
            {
                return (false, "Threshold must be >=0");
            }
            else if (EvaluationPeriodMins is not (>= 1 and <= 60))
            {
                return (false, "Evaluation Period must be between 1 and 60");
            }
            else if (string.IsNullOrEmpty(WaitType))
            {
                return (false, "Wait type must be specified");
            }
            return (true, string.Empty);
        }
    }
}