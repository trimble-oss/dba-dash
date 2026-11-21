using DBADashGUI.CustomReports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.ElasticJobs
{
    /// <summary>
    /// The definition of an elastic job (dbo.ElasticJobSteps_Get): its steps, and the members of the target groups they run against.
    /// </summary>
    internal static class ElasticJobStepsReport
    {
        private sealed class CommandLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                Convert.ToString(drv["command"]).OpenAsTextFile();
            }
        }

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport Instance => new()
        {
            ReportName = "Elastic Job Steps",
            SchemaName = "dbo",
            ProcedureName = "ElasticJobSteps_Get",
            QualifiedProcedureName = "dbo.ElasticJobSteps_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Steps",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["step_id"] = new() { Alias = "Step ID" },
                        ["step_name"] = new() { Alias = "Step Name" },
                        ["target_group_name"] = new() { Alias = "Target Group" },
                        ["command"] = new() { Alias = "Command", Link = new CommandLink() },
                        ["command_type"] = new() { Alias = "Command Type", Visible = false },
                        ["credential_name"] = new() { Alias = "Credential" },
                        ["retry_attempts"] = new() { Alias = "Retry Attempts" },
                        ["initial_retry_interval_seconds"] = new() { Alias = "Initial Retry Interval (sec)", Visible = false },
                        ["maximum_retry_interval_seconds"] = new() { Alias = "Max Retry Interval (sec)", Visible = false },
                        ["retry_interval_backoff_multiplier"] = new() { Alias = "Retry Backoff Multiplier", Visible = false },
                        ["step_timeout_seconds"] = new() { Alias = "Timeout (sec)" },
                        ["max_parallelism"] = new() { Alias = "Max Parallelism", Description = "Most databases in an elastic pool the step runs against at once.  Blank for no limit." },
                        ["output_type"] = new() { Alias = "Output Type", Visible = false },
                        ["output_server_name"] = new() { Alias = "Output Server" },
                        ["output_database_name"] = new() { Alias = "Output Database" },
                        ["output_schema_name"] = new() { Alias = "Output Schema", Visible = false },
                        ["output_table_name"] = new() { Alias = "Output Table" },
                        ["job_version"] = new() { Alias = "Job Version" },
                        ["job_name"] = Hidden(),
                        ["InstanceID"] = Hidden(),
                        ["job_id"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [1] = new CustomReportResult
                {
                    ResultName = "Target Group Members",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["target_group_name"] = new() { Alias = "Target Group" },
                        ["membership_type"] = new() { Alias = "Membership", Description = "Include or Exclude" },
                        ["target_type"] = new() { Alias = "Target Type", Description = "SqlDatabase, SqlServer (each of the server's databases), SqlElasticPool (each database in the pool) or SqlShardMap (each shard in the map)" },
                        ["server_name"] = new() { Alias = "Server" },
                        ["database_name"] = new() { Alias = "Database" },
                        ["elastic_pool_name"] = new() { Alias = "Elastic Pool" },
                        ["shard_map_name"] = new() { Alias = "Shard Map" },
                        ["resource_group_name"] = new() { Alias = "Resource Group" },
                        ["refresh_credential_name"] = new() { Alias = "Refresh Credential", Description = "Credential used to list the databases of a server or pool" },
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@JobID", ParamType = "UNIQUEIDENTIFIER" },
                }
            }
        };
    }
}
