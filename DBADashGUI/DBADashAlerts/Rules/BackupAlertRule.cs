using System.ComponentModel;
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DBADashGUI.DBADashAlerts.Rules
{
    internal class BackupAlertRule : AlertRuleBase
    {
        public enum BackupTypes
        {
            Full,
            Diff,
            Log
        }

        public enum AlertModes
        {
            AgeSinceLastBackup,
            Status
        }

        public enum MinimumAlertStatuses
        {
            Critical,
            Warning
        }

        private sealed class BackupAlertTypeDescriptionProvider : TypeDescriptionProvider
        {
            public BackupAlertTypeDescriptionProvider(TypeDescriptionProvider parent) : base(parent) { }

            public override ICustomTypeDescriptor GetTypeDescriptor(System.Type objectType, object instance)
                => new BackupAlertTypeDescriptor(base.GetTypeDescriptor(objectType, instance), instance as BackupAlertRule);

            private sealed class BackupAlertTypeDescriptor : CustomTypeDescriptor
            {
                private readonly BackupAlertRule _rule;

                public BackupAlertTypeDescriptor(ICustomTypeDescriptor parent, BackupAlertRule rule) : base(parent)
                {
                    _rule = rule;
                }

                public override PropertyDescriptorCollection GetProperties()
                    => Filter(base.GetProperties());

                public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
                    => Filter(base.GetProperties(attributes));

                private PropertyDescriptorCollection Filter(PropertyDescriptorCollection props)
                {
                    if (_rule == null)
                    {
                        return props;
                    }

                    var filtered = props.Cast<PropertyDescriptor>()
                        .Where(p => _rule.AlertMode == AlertModes.AgeSinceLastBackup
                            ? !string.Equals(p.Name, nameof(MinimumAlertStatus), StringComparison.Ordinal)
                            : !string.Equals(p.Name, nameof(Threshold), StringComparison.Ordinal))
                        .ToArray();

                    return new PropertyDescriptorCollection(filtered);
                }
            }
        }

        public override RuleTypes RuleType => RuleTypes.BackupAlert;
        private const decimal DefaultThreshold=120;
        private const MinimumAlertStatuses DefaultMinimumAlertStatus = MinimumAlertStatuses.Critical;

        [DisplayName("Database Name")]
        [Description("Database name to apply to. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited). Leave blank to apply to all databases.")]
        public string DatabaseName { get; set; }

        [DisplayName("Exclude Database Name")]
        [Description("Database name to exclude. Supports LIKE syntax. Use multiple lines for multiple patterns (newline-delimited).")]
        public string ExcludeDatabaseName { get; set; } = "model";

        [DisplayName("Backup Type")]
        [Description("Select the backup type to target.")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BackupTypes BackupType { get; set; } = BackupTypes.Log;

        [DisplayName("Alert Mode")]
        [Description("Alert on time since last backup or on the computed backup status.")]
        [RefreshProperties(RefreshProperties.All)]
        [JsonConverter(typeof(StringEnumConverter))]
        public AlertModes AlertMode { get; set; } = AlertModes.AgeSinceLastBackup;

        [DisplayName("Minimum Alert Status")]
        [Description("When Alert Mode is Status, choose whether to alert on warning and critical or only critical.")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MinimumAlertStatuses? MinimumAlertStatus
        {
            get => AlertMode == AlertModes.Status ? field ?? DefaultMinimumAlertStatus : null;
            set;
        }

        [DisplayName("Age Threshold (Mins)")]
        [Description("When Alert Mode is AgeSinceLastBackup, alert when the selected backup is older than this threshold in minutes.")]
        public override decimal? Threshold
        {
            get => AlertMode == AlertModes.AgeSinceLastBackup ? field ?? DefaultThreshold : null;
            set;
        }

        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override string AlertKey => $"BACKUP:{BackupType}";

        public override (bool isValid, string message) Validate()
        {
            if (AlertMode == AlertModes.AgeSinceLastBackup)
            {
                if (Threshold is not (>= 0M))
                {
                    return (false, "Age Threshold (Mins) must be >= 0");
                }
            }

            return (true, string.Empty);
        }

        public static void AttachTypeDescriptorProvider(BackupAlertRule rule)
        {
            TypeDescriptor.AddProviderTransparent(
                new BackupAlertTypeDescriptionProvider(TypeDescriptor.GetProvider(rule)),
                rule);
        }
    }
}
