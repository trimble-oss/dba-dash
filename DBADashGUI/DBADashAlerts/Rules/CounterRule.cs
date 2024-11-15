using DBADashGUI.Performance;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class CounterRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.Counter;

        public enum Aggregations
        {
            Max,
            Min,
            Avg,
            Sum
        }

        [JsonIgnore]
        public RuleComparisonTypes RuleComparisonType { get; set; } = RuleComparisonTypes.GreaterThanOrEqual;

        [Browsable(false)]
        public string ComparisonSymbol
        {
            get => RuleComparisonType.ToSymbol();
            set => RuleComparisonType = value.FromSymbol();
        }

        [JsonConverter(typeof(StringEnumConverter))]
        [Description("The aggregation to apply to the counter")]
        public Aggregations Aggregation { get; set; }

        [Editor(typeof(CounterSelect), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("Select a counter/metric to alert on.")]
        public AlertCounter Counter { get; set; }

        public override string AlertKey => Counter.ToString();

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
            else if (Counter == null)
            {
                return (false, "Counter must be selected");
            }
            return (true, string.Empty);
        }
    }
}