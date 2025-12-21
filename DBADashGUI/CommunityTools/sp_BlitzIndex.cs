using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_BlitzIndex
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzIndex.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzIndex.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
            Description = "SQL Server Index Analysis Stored Procedure",
            DatabaseNameParameter = "@DatabaseName",
            ObjectParameterName = "@TableName",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@DatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SchemaName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@TableName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@Mode", ParamType = "TINYINT" },
                    new() { ParamName = "@Filter", ParamType = "TINYINT" },
                    new() { ParamName = "@SkipPartitions", ParamType = "BIT" },
                    new() { ParamName = "@SkipStatistics", ParamType = "BIT" },
                    new() { ParamName = "@GetAllDatabases", ParamType = "BIT" },
                    new() { ParamName = "@ShowColumnstoreOnly", ParamType = "BIT" },
                    new() { ParamName = "@BringThePain", ParamType = "BIT" },
                    new() { ParamName = "@IgnoreDatabases", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ThresholdMB", ParamType = "INT" },
                    new() { ParamName = "@IncludeInactiveIndexes", ParamType = "BIT" },
                    new() { ParamName = "@ShowAllMissingIndexRequests", ParamType = "BIT" },
                    new() { ParamName = "@ShowPartitionRanges", ParamType = "BIT" },
                    new() { ParamName = "@SortOrder", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SortDirection", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Mode",
                    Name = "Mode",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 0, "0 - Diagnose" },
                        { 1, "1 - Summarize" },
                        { 2, "2 - Index Usage Detail" },
                        { 3, "3 - Missing Index Detail" },
                        { 4, "4 - Diagnose Details" }
                    }
                },
                new()
                {
                    ParameterName = "@SkipPartitions",
                    Name = "Skip Partitions",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@SkipStatistics",
                    Name = "Skip Statistics",
                    DefaultValue = true,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@GetAllDatabases",
                    Name = "Get All Databases",
                    DefaultValue = true,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ShowColumnstoreOnly",
                    Name = "Show Columnstore Only",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@BringThePain",
                    Name = "Bring The Pain",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ThresholdMB",
                    Name = "Threshold",
                    DefaultValue = 256,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 128, "128 MB" },
                        { 256, "256 MB" },
                        { 512, "512 MB" },
                        { 1024, "1 GB" },
                        { 2048, "2 GB" },
                        { 4096, "4 GB" },
                        { 8192, "8 GB" },
                        { 16384, "16 GB" },
                        { 32768, "32 GB" },
                        { 65536, "64 GB" },
                        { 131072, "128 GB" },
                    }
                },
                new()
                {
                    ParameterName = "@IncludeInactiveIndexes",
                    Name = "Include Inactive Indexes",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ShowAllMissingIndexRequests",
                    Name = "Show All Missing Index Requests",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ShowPartitionRanges",
                    Name = "Show Partition Ranges",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes (Add partition range values to columnstore visualization)" },
                    }
                },
                new()
                {
                    ParameterName = "@SortOrder",
                    Name = "Sort Order (For Mode=2)",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { "rows", "rows" },
                        { "reserved_mb", "reserved_mb" },
                        { "size", "size" },
                        { "reserved_lob_mb", "reserved_lob_mb" },
                        { "lob", "lob" },
                        { "total_row_lock_wait_in_ms", "total_row_lock_wait_in_ms" },
                        { "total_page_lock_wait_in_ms", "total_page_lock_wait_in_ms" },
                        { "lock_time", "lock_time" },
                        { "total_reads", "total_reads" },
                        { "reads", "reads" },
                        { "user_updates", "user_updates" },
                        { "writes", "writes" },
                        { "reads_per_write", "reads_per_write" },
                        { "ratio", "ratio" },
                        { "forward_fetches", "forward_fetches" },
                        { "fetches", "fetches" },
                        { "create_date", "create_date" },
                        { "modify_date", "modify_date" },
                    }
                },
                new()
                {
                    ParameterName = "@SortDirection",
                    Name = "Sort Direction (For Mode=2)",
                    DefaultValue = "DESC",
                    PickerItems = new()
                    {
                        { "DESC", "DESC" },
                        { "ASC", "ASC" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "Create TSQL",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Create TSQL",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "Drop TSQL",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Drop TSQL",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "More Info",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "More Info",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL",
                                }
                            },
                        }
                    }
                }
            },
        };
    }
}