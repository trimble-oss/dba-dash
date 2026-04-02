using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class PerformanceReport
    {
        public static SystemReport Instance => new()
        {
            ReportName = "Performance",
            Description = "System performance charts (CPU, IO, Waits, Blocking, Object Execution)",
            SchemaName = "dbo",
            ProcedureName = string.Empty,
            QualifiedProcedureName = string.Empty,
            CanEditReport = false,
            // Instance level report - allow Instance selection
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" }
                }
            },
            // Charts are metric-based (no data table required)
            Charts = new List<CustomReportChart>
            {
                new() { Metric = new CPUMetric(), Title = "CPU" },
                new() { Metric = new WaitMetric(), Title = "Waits" },
                new() { Metric = new IOMetric() { PointSize = 10 }, Title = "IO"},
                new() { Metric = new BlockingMetric(), Title = "Blocking" },
                new() { Metric = new ObjectExecutionMetric() { Measure = "duration_ms_per_sec" }, Title = "Object Execution" }
            },
            ChartVisible = true,
            TableVisible = false,
            // Prefer a single column layout (1 column x up to 5 rows) for the performance report
            ChartLayoutColumns = 1
        };
    }
}