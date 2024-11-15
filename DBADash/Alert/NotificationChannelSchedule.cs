using DBADashGUI;
using System.ComponentModel;

namespace DBADash.Alert
{
    public class NotificationChannelSchedule : ScheduleBase
    {
        public override string ToString() => $"{(ApplyToTag==null || ApplyToTag.TagID == -1 ? "" : $"{ApplyToTag}, ")}{AlertNotificationLevel}, {base.ToString()}";

        [DisplayName("Notification Level")]
        public Alert.Priorities AlertNotificationLevel { get; set; } = Alert.Priorities.Medium10;

        [DisplayName("Retrigger Threshold (mins)")]
        public int RetriggerThresholdMins { get; set; } = 10;

        [Description("Filter the list of instances this rule applies to using Tags")]
        [DisplayName("Apply To (Tag)")]
        public DBADashTag ApplyToTag { get; set; }
    }
}