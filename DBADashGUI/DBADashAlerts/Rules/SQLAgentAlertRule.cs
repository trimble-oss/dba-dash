using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class SQLAgentAlertRule : AlertRuleBase
    {
        public override string AlertKey => "Alert: {AlertName}";

        public override RuleTypes RuleType => RuleTypes.SQLAgentAlert;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        [DisplayName("Message IDs"), Category("Filters"),
         Description("Message IDs to filter on.  Leave empty to apply to all Message IDs")]
        public List<int> MessageIDList { get; set; } = new();

        [DisplayName("Severity From"), Category("Filters"), Description("Minimum Severity.  Leave blank to apply to all severities")]
        public int? SeverityFrom { get; set; }

        [DisplayName("Severity To"), Category("Filters"), Description("Maximum Severity.  Leave blank to apply to all severities")]
        public int? SeverityTo { get; set; }

        [DisplayName("Alert Name"), Category("Filters"), Description("Name of the SQL Agent alert to filter on.  Leave blank to apply to all.")]
        public string AlertName { get; set; }

        public override (bool isValid, string message) Validate()
        {
            return (true, string.Empty);
        }
    }
}