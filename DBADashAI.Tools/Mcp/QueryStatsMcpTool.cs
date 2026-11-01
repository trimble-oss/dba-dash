using System.Data;
using System.Text;
using System.Text.Json;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Data.SqlClient;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// The heaviest queries over a window, from the Query Stats collection - the same figures as the GUI's Query
    /// Stats tab, because it is the same procedure (dbo.QueryStats_Get).  That procedure already does the hard parts:
    /// blending raw intervals with the hourly rollup, rates against the time actually collected rather than the
    /// window asked for, ad hoc shapes shown as templates, and a second result that says how complete the window is.
    /// That second result goes to the model too, as the error bars on the first.
    /// </summary>
    public sealed class TopQueriesTool(IConfiguration? config) : RepositoryMcpTool(
        "top-queries",
        "The queries that used the most CPU over a window, from the DBA Dash Query Stats collection - the figures the " +
        "GUI's Query Stats tab shows: executions, CPU, duration, reads, writes, spills, memory grants, and the share of " +
        "the instance's CPU, with rates per second of time actually collected.  Rows are query families (one per query " +
        "shape) by default.  {Other ...} rows are the rest of the workload rolled up, so the figures add up to the " +
        "instance's total.  coverage says how much of the window was collected and how much work couldn't be attributed - " +
        "read it before drawing conclusions.  A row with PlanStored true can be passed to get-query-plan by its PlanHash.  " +
        "Times are UTC.",
        $$"""
        {
          "type": "object",
          "properties": {
            "instance": { "type": "string", "description": "One instance's DBA Dash display name.  Leave it out for every instance." },
            "hoursBack": { "type": "integer", "minimum": 1, "maximum": {{MaxHours}}, "description": "How many hours the window covers, ending at to.  Defaults to 24." },
            "to": { "type": "string", "description": "When the window ends, in UTC.  Defaults to now." },
            "groupBy": { "type": "string", "enum": ["family", "statement", "plan"], "description": "family (the default) is one row per query shape, statement one per statement, plan one per plan shape - use plan to compare plans of one query." },
            "maxRows": { "type": "integer", "minimum": 1, "maximum": {{MaxTop}}, "description": "How many rows, heaviest by CPU first.  Defaults to 20." },
            "excludeRollups": { "type": "boolean", "description": "Leave out the {Other ...} rollup rows.  Defaults to false, so the figures add up to the total." }
          },
          "additionalProperties": false
        }
        """,
        config)
    {
        private const int MaxHours = 31 * 24;
        private const int MaxTop = 100;

        /// <summary>Columns that drive the GUI's links rather than describe the query.</summary>
        private static readonly HashSet<string> LinkColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "TextStatementID", "ViewBatch", "RunningQueries", "ViewPlan", "PlanStatementID", "StatementID", "InstanceID",
            "DatabaseID", "CacheEntries", "FirstPeriodStart", "LastSnapshotDate"
        };

        private readonly AiPromptDataSerializer _serializer = new(
            config?.GetValue<int?>("Mcp:MaxOutputChars") ?? AiToolMcpAdapter.DefaultMaxOutputChars,
            MaxTop,
            config?.GetValue<int?>("Mcp:MaxStringValueLength") ?? AiToolMcpAdapter.DefaultMaxStringValueLength);

        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var instanceName = args.String("instance");
            var hours = args.Int("hoursBack", 1, MaxHours) ?? 24;
            var toUtc = args.UtcDateTime("to") ?? DateTime.UtcNow;
            var groupBy = args.String("groupBy") switch
            {
                null or "family" => "Family",
                "statement" => "Statement",
                "plan" => "Plan",
                var other => throw new McpToolException($"groupBy must be family, statement or plan, not '{other}'.")
            };
            var top = args.Int("maxRows", 1, MaxTop) ?? 20;
            var excludeRollups = args.Bool("excludeRollups") ?? false;

            var instances = new DataTable();
            instances.Columns.Add("ID", typeof(int));
            int? instanceId = null;
            if (instanceName is not null)
            {
                instanceId = (await ResolveInstanceAsync(sql, instanceName, cancellationToken)).InstanceId;
                instances.Rows.Add(instanceId.Value);
            }
            else
            {
                var all = await sql.QueryAsync("AI.Instance_Get",
                [
                    new SqlParameter("@InstanceFilter", SqlDbType.NVarChar, 256) { Value = string.Empty },
                    new SqlParameter("@MaxRows", SqlDbType.Int) { Value = 100_000 }
                ], cancellationToken);
                foreach (var row in all)
                {
                    instances.Rows.Add(Convert.ToInt32(row["InstanceID"]));
                }
            }

            var results = await sql.QueryMultiAsync("dbo.QueryStats_Get",
            [
                new SqlParameter("@InstanceIDs", SqlDbType.Structured) { TypeName = "dbo.IDs", Value = instances },
                new SqlParameter("@InstanceID", SqlDbType.Int) { Value = (object?)instanceId ?? DBNull.Value },
                new SqlParameter("@FromDate", SqlDbType.DateTime2) { Scale = 3, Value = toUtc.AddHours(-hours) },
                new SqlParameter("@ToDate", SqlDbType.DateTime2) { Scale = 3, Value = toUtc },
                new SqlParameter("@GroupBy", SqlDbType.VarChar, 20) { Value = groupBy },
                new SqlParameter("@ExcludeRollups", SqlDbType.Bit) { Value = excludeRollups },
                new SqlParameter("@Top", SqlDbType.Int) { Value = top }
            ], cancellationToken);

            // Not collecting: the procedure answers with a message saying how to turn it on, and nothing else.
            if (results.Count > 0 && results[0].Count > 0 && results[0][0].ContainsKey("Message"))
            {
                throw new McpToolException(
                    $"Query Stats isn't collected for {(instanceName ?? "these instances")}.  {results[0][0]["Message"]} {results[0][0].GetValueOrDefault("Url")}");
            }

            var rows = results.Count > 0 ? results[0].Select(Describe).ToList() : [];
            // One coverage row per instance in scope, which for the whole estate is a lot of rows about instances that
            // aren't in the answer.  Only the instances the rows are from are kept.
            var instancesShown = rows.Select(r => Convert.ToString(r.GetValueOrDefault("Instance"))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var coverage = results.Count > 1
                ? results[1].Where(c => instancesShown.Contains(Convert.ToString(c.GetValueOrDefault("Instance")))).ToList()
                : [];

            var execution = new AiToolExecutionResult
            {
                Tool = ProtocolTool.Name,
                Data = McpResultNotes.RoundNumbers(AiDataMasker.MaskJson(JsonSerializer.SerializeToElement(new { rows, coverage }), Mask)),
                RowCount = rows.Count,
                Evidence = []
            };

            var text = new StringBuilder();
            text.AppendLine($"Top {top} by CPU, grouped by {groupBy.ToLowerInvariant()}, {instanceName ?? "every instance"}, " +
                            $"{toUtc.AddHours(-hours):yyyy-MM-dd HH:mm} to {toUtc:yyyy-MM-dd HH:mm} UTC.");
            text.Append(_serializer.Serialize([execution]));
            if (rows.Count == top)
            {
                text.AppendLine($"Note: there are {top} rows, the maxRows limit, so there are probably more below them.");
            }

            if (rows.Count == 0)
            {
                text.AppendLine("Note: no query stats in this window - the collection may not have run in it, or it is outside the retention.");
            }

            return text.ToString();
        }

        /// <summary>A row as the model needs it: the link columns dropped, and whether its plan is stored said plainly.</summary>
        private static Dictionary<string, object?> Describe(Dictionary<string, object?> row)
        {
            var described = row.Where(c => !LinkColumns.Contains(c.Key))
                .ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);

            // View: the plan is stored.  Example: an ad hoc shape's, one variant's plan.  Find: not stored.
            var viewPlan = Convert.ToString(row.GetValueOrDefault("ViewPlan"));
            described["PlanStored"] = viewPlan is "View" or "Example";
            return described;
        }
    }
}
