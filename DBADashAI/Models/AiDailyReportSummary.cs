namespace DBADashAI.Models
{
    /// <summary>
    /// A daily health report to summarise.  Sent by the DBA Dash service when it builds a report with the AI summary turned on.  The report
    /// is the plain text version with the query text left out, so it holds names and figures only - metadata - and says so in DataDetail.
    /// </summary>
    public class AiDailyReportSummaryRequest
    {
        public const int MaxReportLength = 60_000;

        public string ReportName { get; set; } = string.Empty;

        public string Report { get; set; } = string.Empty;

        /// <summary>How the caller masked the report.  It has to cover the service's data setting - see AiDataDetailSetting.Refusal</summary>
        public string? DataDetail { get; set; }

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Report)) return "Report is required.";
            if (Report.Length > MaxReportLength) return $"Report is {Report.Length:N0} characters; the limit is {MaxReportLength:N0}.";
            if (ReportName?.Length > 200) return "ReportName is too long.";
            return null;
        }
    }

    public class AiDailyReportSummaryResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string? Model { get; set; }
    }
}
