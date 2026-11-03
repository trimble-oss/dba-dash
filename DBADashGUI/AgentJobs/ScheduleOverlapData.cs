using DBADash.JobSchedules;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.AgentJobs
{
    /// <summary>How long each job is expected to take, from its successful runs</summary>
    internal enum DurationBasis
    {
        Average,
        P95,
        Longest
    }

    /// <summary>The loaded data worked out with a duration basis: the jobs, how they overlap, and what they ran</summary>
    /// <param name="Jobs">The jobs in the category asked for, with their schedules and expected duration</param>
    /// <param name="AllJobs">Every job on the instance - a job in another category can share a schedule with one in it</param>
    /// <param name="Report">How the jobs in the category overlap, in the server's local time</param>
    /// <param name="UtcOffset">Minutes to add to the server's local time for UTC</param>
    /// <param name="ActualRuns">What each job ran in the period, in server time</param>
    /// <param name="Stats">Duration statistics by job</param>
    /// <param name="JobRows">The jobs in the category as loaded</param>
    /// <param name="History">Each job's schedules as collected over the period, in server time - to tell when they changed</param>
    /// <param name="ClockChange">A clock change found in the runs, which they've been corrected for.  Null when there's none.</param>
    /// <param name="ParseErrors">Why a job's schedules couldn't be read, by job</param>
    internal sealed record ScheduleAnalysis(
        List<JobScheduleInput> Jobs,
        List<JobScheduleInput> AllJobs,
        JobOverlapReport Report,
        int UtcOffset,
        IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> ActualRuns,
        Dictionary<Guid, DataRow> Stats,
        List<DataRow> JobRows,
        IReadOnlyDictionary<Guid, IReadOnlyList<ScheduleVersion>> History,
        ClockChange? ClockChange,
        IReadOnlyDictionary<Guid, string> ParseErrors);

    /// <summary>
    /// An instance's jobs, their schedules (parsed from each job's scripted definition), how long they take and what they
    /// ran in a period - from dbo.JobScheduleOverlap_Get.  Used by the job timeline's plan mode.
    /// </summary>
    internal sealed class ScheduleOverlapData
    {
        public DataSet Data { get; private init; }
        public Dictionary<Guid, SqlJobInfo> JobInfo { get; private init; }

        /// <summary>Why a job's schedules couldn't be read, by job</summary>
        public Dictionary<Guid, string> ParseErrors { get; private init; }

        public DateTime FromUtc { get; private init; }
        public DateTime ToUtc { get; private init; }

        /// <summary>Each job's schedules from each definition collected over the period, by when (UTC).  Versions that couldn't be read are left out.</summary>
        public Dictionary<Guid, List<(DateTime CollectedUtc, List<SqlJobSchedule> Schedules)>> Versions { get; private init; } = new();

        /// <summary>Loads from the repository - call off the UI thread</summary>
        public static ScheduleOverlapData Load(int instanceId, DateTime fromUtc, DateTime toUtc, string category)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobScheduleOverlap_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.Add("@FromDate", SqlDbType.DateTime2).Value = fromUtc;
            cmd.Parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toUtc;
            cmd.Parameters.AddStringIfNotNullOrEmpty("@category", category);
            var ds = new DataSet();
            da.Fill(ds);

            // Schedules come from the job's scripted definition
            var info = new Dictionary<Guid, SqlJobInfo>();
            var errors = new Dictionary<Guid, string>();
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                var jobId = (Guid)r["job_id"];
                if (r["DDL"] == DBNull.Value)
                {
                    errors[jobId] = "No scripted definition collected for this job";
                    continue;
                }
                try
                {
                    info[jobId] = SqlJobInfo.GetJobInfo(DBADash.SMOBaseClass.Unzip((byte[])r["DDL"]));
                }
                catch (Exception ex)
                {
                    errors[jobId] = "Couldn't read the job's definition: " + ex.Message;
                }
            }
            // Earlier definitions, to tell when schedules changed
            var versions = new Dictionary<Guid, List<(DateTime, List<SqlJobSchedule>)>>();
            foreach (DataRow r in ds.Tables[3].Rows)
            {
                if (r["DDL"] == DBNull.Value) continue;
                var jobId = (Guid)r["job_id"];
                try
                {
                    var schedules = SqlJobInfo.GetJobInfo(DBADash.SMOBaseClass.Unzip((byte[])r["DDL"])).Schedules;
                    if (!versions.TryGetValue(jobId, out var list)) versions[jobId] = list = new List<(DateTime, List<SqlJobSchedule>)>();
                    list.Add(((DateTime)r["SnapshotDate"], schedules));
                }
                catch (Exception)
                {
                    // A version that can't be read says nothing about when the schedules changed
                }
            }
            return new ScheduleOverlapData { Data = ds, JobInfo = info, ParseErrors = errors, FromUtc = fromUtc, ToUtc = toUtc, Versions = versions };
        }

        /// <summary>Works the data out with the chosen duration basis.  Quick enough to repeat on the UI thread.</summary>
        public ScheduleAnalysis Analyze(DurationBasis basis)
        {
            var allRows = Data.Tables[0].Rows.Cast<DataRow>().ToList();
            // Every job comes back, to tell which schedules are shared
            var jobRows = allRows.Where(r => r["InCategory"] is true).ToList();
            var utcOffset = UtcOffset;
            // In the server's time as its clock showed it, like the runs - corrected for a clock change since
            var change = ClockChange;
            var fromLocal = FromUtc.AddMinutes(-utcOffset);
            var toLocal = ToUtc.AddMinutes(-utcOffset);
            if (change is { } c) (fromLocal, toLocal) = (c.Correct(fromLocal), c.Correct(toLocal));

            var stats = Data.Tables[1].Rows.Cast<DataRow>().ToDictionary(r => (Guid)r["job_id"]);
            var allInputs = allRows.Select(r =>
            {
                var jobId = (Guid)r["job_id"];
                int? expected = null;
                if (stats.TryGetValue(jobId, out var s))
                {
                    var value = basis switch
                    {
                        DurationBasis.Average => s["AvgSec"],
                        DurationBasis.P95 => s["P95Sec"],
                        _ => s["MaxSec"]
                    };
                    if (value != DBNull.Value) expected = (int)Math.Ceiling(Convert.ToDouble(value));
                }
                return new JobScheduleInput
                {
                    JobId = jobId,
                    Name = (string)r["name"],
                    Enabled = r["enabled"] is true,
                    Schedules = Schedules.TryGetValue(jobId, out var schedules) ? schedules : Array.Empty<AgentSchedule>(),
                    ExpectedDurationSec = expected
                };
            }).ToList();
            var inCategory = jobRows.Select(r => (Guid)r["job_id"]).ToHashSet();
            var inputs = allInputs.Where(j => inCategory.Contains(j.JobId)).ToList();

            var report = JobOverlapAnalyzer.Analyze(inputs, ActualRuns, fromLocal, toLocal);
            return new ScheduleAnalysis(inputs, allInputs, report, utcOffset, ActualRuns, stats, jobRows, History, ClockChange, ParseErrors);
        }

        /// <summary>Minutes to add to the server's local time for UTC</summary>
        private int UtcOffset => Data.Tables[0].Rows.Cast<DataRow>().Select(r => r["UTCOffset"]).FirstOrDefault(o => o != DBNull.Value) is int o ? o : 0;

        // What doesn't depend on the duration basis is worked out once, and the same each time: the plan matches runs to
        // schedules and finds schedule changes only when it's given different ones

        private Dictionary<Guid, IReadOnlyList<AgentSchedule>> _schedules;
        private IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> _actualRuns;
        private IReadOnlyDictionary<Guid, IReadOnlyList<ScheduleVersion>> _history;

        /// <summary>Each job's schedules, from its scripted definition</summary>
        private Dictionary<Guid, IReadOnlyList<AgentSchedule>> Schedules =>
            _schedules ??= JobInfo.ToDictionary(j => j.Key, j => (IReadOnlyList<AgentSchedule>)j.Value.Schedules.Select(s => s.ToAgentSchedule()).ToList());

        private ClockChange? _clockChange;

        /// <summary>
        /// The server's clocks changed since some of the runs (daylight saving), found from the runs themselves - the
        /// instance's UTC offset is only known as it is now.  Those runs, and the definitions collected then, are corrected.
        /// </summary>
        public ClockChange? ClockChange
        {
            get
            {
                _ = ActualRuns;
                return _clockChange;
            }
        }

        /// <summary>What each job ran, in server time, in start order</summary>
        private IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> ActualRuns
        {
            get
            {
                if (_actualRuns != null) return _actualRuns;
                var utcOffset = UtcOffset;
                var converted = Data.Tables[2].Rows.Cast<DataRow>()
                    .GroupBy(r => (Guid)r["job_id"])
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<JobRun>)g
                        .Select(r => new JobRun(((DateTime)r["RunDateTime"]).AddMinutes(-utcOffset), ((DateTime)r["FinishDateTime"]).AddMinutes(-utcOffset))
                        {
                            // How the run ended, so the plan colours it as the timeline does, and what started it
                            Status = r["run_status"] == DBNull.Value ? null : Convert.ToInt32(r["run_status"]),
                            InvokedBy = r["InvokedBy"] as string
                        })
                        .OrderBy(r => r.Start)
                        .ToList());
                var jobs = Schedules.Select(s => new JobScheduleInput { JobId = s.Key, Schedules = s.Value }).ToList();
                _clockChange = ClockChanges.Find(jobs, converted);
                if (_clockChange is not { } change) return _actualRuns = converted;
                return _actualRuns = converted.ToDictionary(r => r.Key, r => (IReadOnlyList<JobRun>)r.Value
                    .Select(run => run with { Start = change.Correct(run.Start), End = change.Correct(run.Start) + (run.End - run.Start) })
                    .OrderBy(run => run.Start)
                    .ToList());
            }
        }

        /// <summary>Each job's schedules as collected over the period, in server time</summary>
        private IReadOnlyDictionary<Guid, IReadOnlyList<ScheduleVersion>> History
        {
            get
            {
                if (_history != null) return _history;
                var utcOffset = UtcOffset;
                var change = ClockChange;
                return _history = Versions.ToDictionary(v => v.Key, v => (IReadOnlyList<ScheduleVersion>)v.Value
                    .Select(x =>
                    {
                        var collected = x.CollectedUtc.AddMinutes(-utcOffset);
                        return new ScheduleVersion(change?.Correct(collected) ?? collected, x.Schedules.Select(s => s.ToAgentSchedule()).ToList());
                    })
                    .ToList());
            }
        }

        public static string BasisText(DurationBasis basis) => basis switch
        {
            DurationBasis.Average => "Average",
            DurationBasis.P95 => "95th percentile",
            _ => "Longest"
        };
    }
}
