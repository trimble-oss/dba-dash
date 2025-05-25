using System;
using System.Data;

namespace DBADash.Messaging
{
    public class HelpJobInfo
    {
        private const int IDLE_STATUS = 4;

        public int LastRunTime { get; set; }
        public int LastRunDate { get; set; }

        public string CurrentStep { get; set; }

        public int ExecutionStatus { get; set; }

        public int LastRunOutcome { get; set; }

        public int CurrentRetryAttempt { get; set; }

        public string LastRunOutcomeDescription => GetLastRunOutcomeDescription(LastRunOutcome);

        public string ExecutionStatusDescription => GetExecutionStatusDescription(ExecutionStatus);

        public bool IsIdle => ExecutionStatus == IDLE_STATUS;

        public string StatusInfo => ExecutionStatusDescription + Environment.NewLine + "Current Step: " + CurrentStep + (CurrentRetryAttempt > 0 ? $" (Retry {CurrentRetryAttempt})" : string.Empty);

        public string Category { get; set; }

        public string Name { get; set; }

        public Guid JobId { get; set; }

        public static string GetLastRunOutcomeDescription(int? value)
        {
            return value switch
            {
                0 => "Failed",
                1 => "Succeeded",
                3 => "Cancelled",
                5 => "Unknown",
                _ => $"Unknown outcome {value}"
            };
        }

        public static string GetExecutionStatusDescription(int? value)
        {
            return value switch
            {
                1 => "Executing.",
                2 => "Waiting for thread.",
                3 => "Between retries.",
                4 => "Idle.",
                5 => "Suspended.",
                7 => "Performing completion actions.",
                _ => $"Unknown status {value}"
            };
        }

        public HelpJobInfo(DataTable dtJob)
        {
            LastRunTime = dtJob.Rows[0].Field<int>("last_run_time");
            LastRunDate = dtJob.Rows[0].Field<int>("last_run_date");
            CurrentStep = dtJob.Rows[0].Field<string>("current_execution_step");
            ExecutionStatus = dtJob.Rows[0].Field<int>("current_execution_status");
            LastRunOutcome = dtJob.Rows[0].Field<int>("last_run_outcome");
            CurrentRetryAttempt = dtJob.Rows[0].Field<int>("current_retry_attempt");
            Category = dtJob.Rows[0].Field<string>("category");
            Name = dtJob.Rows[0].Field<string>("name");
            JobId = dtJob.Rows[0].Field<Guid>("job_id");
        }
    }
}