using System;

namespace DBADashGUI.AgentJobs
{
    public class SqlJobSchedule
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public int FrequencyType { get; set; }
        public int FrequencyInterval { get; set; }
        public int FrequencySubdayType { get; set; }
        public int FrequencySubdayInterval { get; set; }
        public int FrequencyRelativeInterval { get; set; }
        public int FrequencyRecurrenceFactor { get; set; }
        public int ActiveStartDate { get; set; }
        public int ActiveEndDate { get; set; }
        public int ActiveStartTime { get; set; }
        public int ActiveEndTime { get; set; }
        public string ScheduleUid { get; set; }

        /// <summary>The schedule for describing, planning and scripting moves (DBADash.JobSchedules)</summary>
        public DBADash.JobSchedules.AgentSchedule ToAgentSchedule() => new()
        {
            Name = Name,
            Enabled = Enabled,
            FrequencyType = FrequencyType,
            FrequencyInterval = FrequencyInterval,
            FrequencySubdayType = FrequencySubdayType,
            FrequencySubdayInterval = FrequencySubdayInterval,
            FrequencyRelativeInterval = FrequencyRelativeInterval,
            FrequencyRecurrenceFactor = FrequencyRecurrenceFactor,
            ActiveStartDate = ActiveStartDate,
            ActiveEndDate = ActiveEndDate,
            ActiveStartTime = ActiveStartTime,
            ActiveEndTime = ActiveEndTime,
            ScheduleUid = ScheduleUid
        };

        public string GetFrequencyDescription() => ToAgentSchedule().Describe();

        public DateTime? ScheduleStartDateTime => IntToDate(ActiveStartDate);
        public DateTime? ScheduleEndDateTime => IntToDate(ActiveEndDate);

        private static DateTime? IntToDate(int date)
        {
            if (date == 99991231) return null;
            var year = date / 10000;
            var month = (date % 10000) / 100;
            var day = date % 100;
            return new DateTime(year, month, day);
        }
    }
}
