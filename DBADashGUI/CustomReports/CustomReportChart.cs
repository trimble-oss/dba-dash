namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Lightweight wrapper used for storing chart configuration along with the table index
    /// in custom report metadata.
    /// </summary>
    public record CustomReportChart
    {
        public Charts.ChartConfigurationBase Config { get; init; }
        public int TableIndex { get; init; }
    }
}