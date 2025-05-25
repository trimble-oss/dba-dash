using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DBADashGUI.AgentJobs
{
    public class AgentJobThreshold
    {
        public bool IsInherited;
        public int InstanceID { get; set; }
        public Guid JobID { get; set; } = Guid.Empty;
        public int? TimeSinceLastFailureWarning { get; set; }
        public int? TimeSinceLastFailureCritical { get; set; }
        public int? TimeSinceLastSucceededCritical { get; set; }
        public int? TimeSinceLastSucceededWarning { get; set; }
        public int? FailCount24HrsWarning { get; set; }
        public int? FailCount24HrsCritical { get; set; }
        public int? FailCount7DaysWarning { get; set; }
        public int? FailCount7DaysCritical { get; set; }
        public int? JobStepFails24HrsWarning { get; set; }
        public int? JobStepFails24HrsCritical { get; set; }
        public int? JobStepFails7DaysWarning { get; set; }
        public int? JobStepFails7DaysCritical { get; set; }
        public bool LastFailIsCritical { get; set; }
        public bool LastFailIsWarning { get; set; }
        public bool AgentIsRunningCheck { get; set; }

        public string ConfiguredAt { get; set; }

        public bool IsRootLevel => InstanceID == -1 && JobID == Guid.Empty;

        public static AgentJobThreshold GetAgentJobThreshold(int InstanceID, Guid JobID, string connectionString)
        {
            var threshold = new AgentJobThreshold
            {
                InstanceID = InstanceID,
                JobID = JobID
            };

            using var cn = new SqlConnection(connectionString);
            using SqlCommand cmd = new("AgentJobThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();

            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("JobID", JobID);
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                threshold.TimeSinceLastFailureCritical = ColumnToNullableInt32(rdr, "TimeSinceLastFailureCritical");
                threshold.TimeSinceLastFailureWarning = ColumnToNullableInt32(rdr, "TimeSinceLastFailureWarning");
                threshold.TimeSinceLastSucceededCritical = ColumnToNullableInt32(rdr, "TimeSinceLastSucceededCritical");
                threshold.TimeSinceLastSucceededWarning = ColumnToNullableInt32(rdr, "TimeSinceLastSucceededWarning");
                threshold.FailCount24HrsCritical = ColumnToNullableInt32(rdr, "FailCount24HrsCritical");
                threshold.FailCount24HrsWarning = ColumnToNullableInt32(rdr, "FailCount24HrsWarning");
                threshold.FailCount7DaysCritical = ColumnToNullableInt32(rdr, "FailCount7DaysCritical");
                threshold.FailCount7DaysWarning = ColumnToNullableInt32(rdr, "FailCount7DaysWarning");
                threshold.JobStepFails24HrsCritical = ColumnToNullableInt32(rdr, "JobStepFails24HrsCritical");
                threshold.JobStepFails24HrsWarning = ColumnToNullableInt32(rdr, "JobStepFails24HrsWarning");
                threshold.JobStepFails7DaysCritical = ColumnToNullableInt32(rdr, "JobStepFails7DaysCritical");
                threshold.JobStepFails7DaysWarning = ColumnToNullableInt32(rdr, "JobStepFails7DaysWarning");
                threshold.LastFailIsCritical = rdr["LastFailIsCritical"] != DBNull.Value && (bool)rdr["LastFailIsCritical"];
                threshold.LastFailIsWarning = rdr["LastFailIsWarning"] != DBNull.Value && (bool)rdr["LastFailIsWarning"];
                threshold.AgentIsRunningCheck = rdr["AgentIsRunningCheck"] != DBNull.Value && (bool)rdr["AgentIsRunningCheck"];
            }
            else if (JobID != Guid.Empty) // No threshold at Job level.  Get threshold for instance, marking as inherited
            {
                threshold = GetAgentJobThreshold(InstanceID, Guid.Empty, connectionString);
                threshold.JobID = JobID;
                threshold.IsInherited = true;
            }
            else if (InstanceID != -1) // No threshold at instance level. Get root level threshold, marking as inherited
            {
                threshold = GetAgentJobThreshold(-1, Guid.Empty, connectionString);
                threshold.JobID = JobID;
                threshold.InstanceID = InstanceID;
                threshold.IsInherited = true;
            }

            if (JobID == Guid.Empty && InstanceID == -1)
            {
                threshold.ConfiguredAt = "{Root}";
            }
            else if (JobID != Guid.Empty)
            {
                var job = CommonData.GetJobs(InstanceID, JobID);
                var instance = CommonData.GetDBADashContext(InstanceID);
                if (job.Rows.Count == 1)
                {
                    threshold.ConfiguredAt = instance.InstanceName + " \\ " + job.Rows[0].Field<string>("name");
                }
                else
                {
                    threshold.ConfiguredAt = instance.InstanceName + " \\ ??";
                }
            }
            else
            {
                var instance = CommonData.GetDBADashContext(InstanceID);
                threshold.ConfiguredAt = instance.InstanceName;
            }

            return threshold;
        }

        public void Save(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("AgentJobThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("job_id", JobID);
            cmd.Parameters.AddWithNullableValue("TimeSinceLastFailureWarning", TimeSinceLastFailureWarning);
            cmd.Parameters.AddWithNullableValue("TimeSinceLastFailureCritical", TimeSinceLastFailureCritical);
            cmd.Parameters.AddWithNullableValue("TimeSinceLastSucceededWarning", TimeSinceLastSucceededWarning);
            cmd.Parameters.AddWithNullableValue("TimeSinceLastSucceededCritical", TimeSinceLastSucceededCritical);
            cmd.Parameters.AddWithNullableValue("FailCount24HrsWarning", FailCount24HrsWarning);
            cmd.Parameters.AddWithNullableValue("FailCount24HrsCritical", FailCount24HrsCritical);
            cmd.Parameters.AddWithNullableValue("FailCount7DaysCritical", FailCount7DaysCritical);
            cmd.Parameters.AddWithNullableValue("FailCount7DaysWarning", FailCount7DaysWarning);
            cmd.Parameters.AddWithNullableValue("JobStepFails24HrsWarning", JobStepFails24HrsWarning);
            cmd.Parameters.AddWithNullableValue("JobStepFails24HrsCritical", JobStepFails24HrsCritical);
            cmd.Parameters.AddWithNullableValue("JobStepFails7DaysWarning", JobStepFails7DaysWarning);
            cmd.Parameters.AddWithNullableValue("JobStepFails7DaysCritical", JobStepFails7DaysCritical);
            cmd.Parameters.AddWithValue("LastFailIsCritical", LastFailIsCritical);
            cmd.Parameters.AddWithValue("LastFailIsWarning", LastFailIsWarning);
            cmd.Parameters.AddWithValue("Inherit", IsInherited);
            cmd.Parameters.AddWithValue("AgentIsRunningCheck", AgentIsRunningCheck);
            cmd.ExecuteNonQuery();
        }

        private static int? ColumnToNullableInt32(SqlDataReader rdr, string columnName)
        {
            return rdr[columnName] == DBNull.Value ? null : (int?)rdr[columnName];
        }
    }
}