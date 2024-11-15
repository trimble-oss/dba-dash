namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class CPURule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.CPU;

        public override string AlertKey => "CPU";

        public override (bool isValid, string message) Validate()
        {
            if (Threshold is not (>= 0M and <= 100M))
            {
                return (false, "Threshold must be between 0 and 100");
            }
            else if (EvaluationPeriodMins is not (>= 1 and <= 60))
            {
                return (false, "Evaluation Period must be between 1 and 60");
            }
            return (true, string.Empty);
        }
    }
}