using DBADash;
using DBADashGUI.CustomReports;
using System.Collections.Generic;

namespace DBADashGUI.DBFiles
{
    internal class TempDBConfigView : CustomReportView
    {
        public TempDBConfigView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
        }

        #region Report definition

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(TempDBConfigView),
            ReportName = "TempDB Config",
            SchemaName = "dbo",
            ProcedureName = "TempDBConfigReport_Get",
            QualifiedProcedureName = "dbo.TempDBConfigReport_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.DBFiles.ToString(),
                CollectionType.TraceFlags.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "TempDB Config",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance", Description = "Name of instance" },
                        ["NumberOfDataFiles"] = new()
                        {
                            Alias = "#Data Files",
                            Description = "Number of tempdb data files",
                            Highlighting = new CellHighlightingRuleSet("InsufficientFilesStatus") { IsStatusColumn = true }
                        },
                        ["InsufficientFiles"] = new()
                        {
                            Alias = "Insufficient Files",
                            Description = "Indicates the number of tempdb data files is below the recommended minimum",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                            Highlighting = new CellHighlightingRuleSet("InsufficientFilesStatus") { IsStatusColumn = true }
                        },
                        ["MinimumRecommendedFiles"] = new() { Alias = "Recommended Files (Minimum)", Description = "Minimum recommended number of tempdb data files based on CPU core count" },
                        ["NumberOfLogFiles"] = new()
                        {
                            Alias = "#Log Files",
                            Description = "Number of tempdb log files.  Should always be 1",
                            Highlighting = new CellHighlightingRuleSet("NumberOfLogFilesStatus") { IsStatusColumn = true }
                        },
                        ["IsEvenlySized"] = new()
                        {
                            Alias = "Even Sized?",
                            Description = "Should be evenly sized",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                            Highlighting = new CellHighlightingRuleSet("IsEvenlySizedStatus") { IsStatusColumn = true }
                        },
                        ["IsEvenGrowth"] = new()
                        {
                            Alias = "Even Growth",
                            Description = "Should be even growth",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                            Highlighting = new CellHighlightingRuleSet("IsEvenGrowthStatus") { IsStatusColumn = true }
                        },
                        ["TotalSizeMB"] = new() { Alias = "Total Size (MB)", FormatString = "N0", Description = "Total size of tempdb data files in MB" },
                        ["LogMB"] = new() { Alias = "Log MB", FormatString = "N0", Description = "Size of tempdb log file in MB" },
                        ["FileSizeMB"] = new() { Alias = "File Size (MB)", FormatString = "N0", Description = "Size of the largest tempdb data file in MB" },
                        ["MaxGrowthMB"] = new() { Alias = "Max Growth (MB)", FormatString = "N0", Description = "Largest fixed autogrowth setting (MB) amongst the tempdb data files" },
                        ["MaxLogGrowthMB"] = new() { Alias = "Max Log Growth (MB)", FormatString = "N0", Description = "Fixed autogrowth setting (MB) for the tempdb log file" },
                        ["MaxGrowthPct"] = new() { Alias = "Max Growth %", Description = "Largest percentage autogrowth setting amongst the tempdb data files" },
                        ["MaxLogGrowthPct"] = new() { Alias = "Max Log Growth %", Description = "Percentage autogrowth setting for the tempdb log file" },
                        ["TempDBVolumes"] = new() { Alias = "TempDB Volume(s)", Description = "Drive volume(s) hosting the tempdb data files" },
                        ["cpu_core_count"] = new() { Alias = "CPU Core Count", Description = "Up to 8 cores tempdb should match core count.  Then start at 8 files and add more if needed." },
                        ["T1117"] = new()
                        {
                            Alias = "T1117",
                            Description = "Recommended trace flag prior to SQL 2016",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                            Highlighting = new CellHighlightingRuleSet("T1117Status") { IsStatusColumn = true }
                        },
                        ["T1118"] = new()
                        {
                            Alias = "T1118",
                            Description = "Recommended trace flag prior to SQL 2016",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                            Highlighting = new CellHighlightingRuleSet("T1118Status") { IsStatusColumn = true }
                        },
                        ["IsTraceFlagRequired"] = new()
                        {
                            Alias = "Trace Flag Required?",
                            Description = "SQL 2016 and later don't require T1118 & T1117",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                        },
                        ["IsTempDBMetadataMemoryOptimized"] = new()
                        {
                            Alias = "Memory Optimized TempDB?",
                            Description = "Valid from SQL 2019",
                            BooleanTrueValue = "True",
                            BooleanFalseValue = "False",
                            NullValue = "N/A",
                            Highlighting = new CellHighlightingRuleSet("TempDBMemoryOptStatus") { IsStatusColumn = true }
                        },
                        // Hidden technical/status columns
                        ["InstanceID"] = Hidden(),
                        ["Instance"] = new ColumnMetadata { Visible = false, Alias = "Instance (Connection)" },
                        ["InsufficientFilesStatus"] = Hidden(),
                        ["NumberOfLogFilesStatus"] = Hidden(),
                        ["IsEvenlySizedStatus"] = Hidden(),
                        ["IsEvenGrowthStatus"] = Hidden(),
                        ["T1117Status"] = Hidden(),
                        ["T1118Status"] = Hidden(),
                        ["TempDBMemoryOptStatus"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@ShowHidden", ParamType = "BIT" },
                }
            }
        };

        #endregion Report definition
    }
}
