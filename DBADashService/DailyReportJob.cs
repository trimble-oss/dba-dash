using DBADash.DailyReport;
using Quartz;
using Serilog;
using SerilogTimings;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// Sends the daily reports that are due.  Runs on a short interval; a report is only sent once per local day
    /// (see dbo.DailyReport_Claim), so the interval only sets how late a report can start.
    /// </summary>
    [DisallowConcurrentExecution]
    internal class DailyReportJob : IJob
    {
        public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var connectionString = context.JobDetail.JobDataMap.GetString("ConnectionString");
            try
            {
                using Operation op = Operation.Begin("DailyReport");
                var nowUtc = DateTime.UtcNow;
                var dueIDs = DailyReportRepository.ClaimDueReports(connectionString);
                // The claim has marked every report in the batch as run, so one that fails mustn't stop the rest - and is left to retry
                foreach (var reportID in dueIDs)
                {
                    try
                    {
                        var config = DailyReportRepository.GetReports(connectionString, reportID).FirstOrDefault();
                        if (config == null) continue;
                        Log.Information("Sending daily report {ReportName}", config.ReportName);
                        await DailyReportRunner.RunScheduledAsync(config, connectionString, nowUtc);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Daily report {DailyReportID} failed", reportID);
                        try
                        {
                            DailyReportRepository.SetRunResult(connectionString, reportID, ex.Message, retry: true);
                        }
                        catch (Exception setEx)
                        {
                            Log.Error(setEx, "Daily report {DailyReportID}: run result not recorded", reportID);
                        }
                    }
                }
                op.Complete();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DailyReport error");
            }
        }
    }
}
