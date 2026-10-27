using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace DBADashGUI.DBADashAlerts.Rules
{
    /// <summary>The status a Service Broker check has to reach to raise an alert.</summary>
    public enum ServiceBrokerAlertLevel
    {
        Off,
        Critical,
        WarningOrCritical
    }

    /// <summary>
    /// Alerts on the checks the Service Broker tab shows - see Alert.ServiceBrokerAlert_Upd.  Each check has its own level and raises one alert per
    /// instance listing the queues or databases that fail it, so checks can be turned on or off separately and resolve on their own.  The thresholds
    /// the checks use are configured from the Service Broker tab.
    /// </summary>
    internal class ServiceBrokerRule : AlertRuleBase
    {
        private const string LevelHelp = "  Off, Critical, or WarningOrCritical to alert on a warning as well.";

        public override RuleTypes RuleType => RuleTypes.ServiceBroker;

        // Generated alerts are "ServiceBroker <check>". Blackout copies this key and matches it with LIKE,
        // so the wildcard mutes those alerts. An exact key would skip evaluation and resolve them instead.
        public override string AlertKey => "ServiceBroker%";

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        [Description("A disabled queue - messages can't be added or received.  Usually a poison message (a RECEIVE rolled back 5 times in a row).  Always critical." + LevelHelp)]
        [DisplayName("Disabled"), Category("Queues")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel QueueDisabled { get; set; } = ServiceBrokerAlertLevel.Critical;

        [Description("Activation procedure doesn't exist (critical with messages waiting), activation disabled with messages waiting, or activation notified for 10 mins with messages waiting but nothing receiving from the queue (warnings)." + LevelHelp)]
        [DisplayName("Activation"), Category("Queues")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel QueueActivation { get; set; } = ServiceBrokerAlertLevel.WarningOrCritical;

        [Description("Age of the oldest message waiting to be received, against the queue age thresholds (15/60 mins by default).  Shows a queue that isn't being processed." + LevelHelp)]
        [DisplayName("Oldest Message"), Category("Queues")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel QueueAge { get; set; } = ServiceBrokerAlertLevel.Critical;

        [Description("Messages waiting in the queue, against the queue depth thresholds (10,000 warning by default, no critical threshold)." + LevelHelp)]
        [DisplayName("Queue Depth"), Category("Queues")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel QueueDepth { get; set; } = ServiceBrokerAlertLevel.Critical;

        [Description("Queues disabled more than this many hours ago - or before they were first collected - are left out of all the queue checks.  A queue disabled for months is usually deliberate or already known, where one disabled a minute ago needs attention.  A queue disabled recently while the Disabled Queues alert is open stays in it until the queue is enabled.  0 to alert however long ago the queue was disabled.")]
        [DisplayName("Recently Disabled (Hours)"), Category("Queues")]
        public int RecentlyDisabledHours { get; set; } = 24;

        [Description("Age of the oldest message waiting to be sent, against the transmission queue thresholds (15/60 mins by default).  Messages stay in the transmission queue if they can't be delivered." + LevelHelp)]
        [DisplayName("Transmission Queue"), Category("Databases")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel TransmissionQueue { get; set; } = ServiceBrokerAlertLevel.Critical;

        [Description("Open conversations against the conversation thresholds (100,000 warning by default, no critical threshold).  Usually conversations not being ended." + LevelHelp)]
        [DisplayName("Open Conversations"), Category("Databases")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel OpenConversations { get; set; } = ServiceBrokerAlertLevel.WarningOrCritical;

        [Description("Conversations in the error state.  These stay until the application ends them.  A warning." + LevelHelp)]
        [DisplayName("Error Conversations"), Category("Databases")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel ErrorConversations { get; set; } = ServiceBrokerAlertLevel.Off;

        [Description("Activation tasks aborted in the last hour - the activation procedure ended with an error.  A warning." + LevelHelp)]
        [DisplayName("Aborted Activations"), Category("Databases")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel AbortedActivations { get; set; } = ServiceBrokerAlertLevel.WarningOrCritical;

        [Description("Service Broker endpoint not started, or connections failing to log in.  Only needed for messaging between instances.  A warning.  Not checked by a rule with a Database Name." + LevelHelp)]
        [DisplayName("Endpoint"), Category("Instance")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel Endpoint { get; set; } = ServiceBrokerAlertLevel.WarningOrCritical;

        [Description("Corrupted messages in the last hour.  A warning.  Not checked by a rule with a Database Name." + LevelHelp)]
        [DisplayName("Corrupted Messages"), Category("Instance")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceBrokerAlertLevel CorruptedMessages { get; set; } = ServiceBrokerAlertLevel.WarningOrCritical;

        [Description("Database name to apply to. Supports LIKE syntax. Leave blank to apply to all databases and the instance checks.")]
        [DisplayName("Database Name"), Category("Filters")]
        public string DatabaseName { get; set; }

        public override (bool isValid, string message) Validate()
        {
            return RecentlyDisabledHours < 0
                ? (false, "Recently Disabled (Hours) must be 0 or more")
                : (true, string.Empty);
        }
    }
}
