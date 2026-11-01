using System.Data;
using System.Text;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Analysis;
using DBADash.QueryPlan.Model;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Data.SqlClient;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// A captured plan, as the AI service's plan analysis would be shown it: the statement with that plan hash,
    /// summarised by the same rules as the plan viewer's - context, statistics, insights, missing indexes,
    /// operators ranked by their own cost - and optionally the showplan XML.  The text is
    /// <see cref="AiPlanPromptBuilder.Data"/>.
    /// </summary>
    public sealed class GetQueryPlanTool(IConfiguration? config) : RepositoryMcpTool(
        "get-query-plan",
        "A captured execution plan, summarised for analysis: the statement, plan context and statistics, the " +
        "warnings DBA Dash's plan rules raise, missing indexes, the operators ranked by their own cost, waits and " +
        "parameters, and optionally the showplan XML.  Find a query_plan_hash with running-queries-summary.  Plans " +
        "come from the running queries and query stats collections.",
        """
        {
          "type": "object",
          "properties": {
            "queryPlanHash": { "type": "string", "description": "The plan's query_plan_hash, 0x followed by 16 hex digits." },
            "instance": { "type": "string", "description": "The instance it ran on.  Leave it out for the newest capture on any instance." },
            "includePlanXml": { "type": "boolean", "description": "Include the showplan XML, which can be large.  Defaults to false - the summary carries most of what an analysis needs." }
          },
          "required": ["queryPlanHash"],
          "additionalProperties": false
        }
        """,
        config)
    {
        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var hash = args.Hex("queryPlanHash", 8, required: true)!;
            var instanceName = args.String("instance");
            var includePlanXml = args.Bool("includePlanXml") ?? false;

            int? instanceId = instanceName is null
                ? null
                : (await ResolveInstanceAsync(sql, instanceName, cancellationToken)).InstanceId;

            var rows = await sql.QueryAsync("AI.QueryPlan_Get",
            [
                new SqlParameter("@QueryPlanHash", SqlDbType.VarChar, 18) { Value = hash },
                new SqlParameter("@InstanceID", SqlDbType.Int) { Value = (object?)instanceId ?? DBNull.Value }
            ], cancellationToken);

            if (rows.Count == 0)
            {
                throw new McpToolException(
                    $"No plan with hash {hash} has been captured{(instanceName is null ? "" : $" on {instanceName}")}.  " +
                    "Plans are kept for long running queries and for query stats, and are purged over time.");
            }

            var row = rows[0];
            var instance = Convert.ToString(row["InstanceDisplayName"]);
            ExecutionPlan plan;
            try
            {
                plan = PlanParser.Parse(AiDataMasker.MaskXml(Convert.ToString(row["QueryPlan"]), Mask));
            }
            catch (PlanParseException ex)
            {
                throw new McpToolException($"The stored plan could not be read: {ex.Message}");
            }

            var withPlans = plan.StatementsWithPlans.ToList();
            var statement = withPlans.FirstOrDefault(s => string.Equals(s.QueryPlanHash, hash, StringComparison.OrdinalIgnoreCase))
                            ?? withPlans.FirstOrDefault()
                            ?? throw new McpToolException("The stored plan has no statement with a plan in it.");

            var heading = $"Plan {hash} on {instance}, from the {Convert.ToString(row["Source"])?.ToLowerInvariant()} collection, " +
                          $"captured {row["CaptureDate"]:yyyy-MM-dd HH:mm} UTC.";

            return Describe(plan, statement, instance, Convert.ToInt32(row["InstanceID"]), heading, includePlanXml, MaxOutputChars, Mask);
        }

        /// <summary>
        /// A plan statement as get-query-plan describes it, wherever the plan came from - the repository here, the GUI's
        /// viewer for the local server's get-open-query-plan.  The plan XML is dropped whole, with a note, when it would
        /// take the result over <paramref name="maxOutputChars"/>.
        ///
        /// The plan must already be masked - parsed from <see cref="AiDataMasker.MaskXml"/> - so that the statement, the
        /// parameters and everything worked out from them are.  <paramref name="mask"/> is only said in the result, so the
        /// model reads ? and (value A) as masking rather than as the query.
        /// </summary>
        public static string Describe(
            ExecutionPlan plan,
            PlanStatement statement,
            string? instance,
            int? instanceId,
            string heading,
            bool includePlanXml,
            int maxOutputChars,
            AiDataMask mask)
        {
            var withPlans = plan.StatementsWithPlans.ToList();
            var payload = PlanAnalysisPayload.Build(plan, statement, instance, includePlanXml: includePlanXml);

            var header = new StringBuilder();
            header.AppendLine(heading);
            header.AppendLine(statement.IsActualPlan
                ? "An actual plan: its row counts and times are what happened."
                : "An estimated (cached) plan: there are no runtime measurements in it.");
            if (withPlans.Count > 1 && withPlans.Contains(statement))
            {
                header.AppendLine($"The plan has {withPlans.Count} statements with plans; this is statement {withPlans.IndexOf(statement) + 1}.");
            }

            if (MaskNote(mask) is { } note)
            {
                header.AppendLine(note);
            }

            if (includePlanXml && payload.PlanXmlOmittedReason is { } reason)
            {
                header.AppendLine(reason);
            }

            var text = header + AiPlanPromptBuilder.Data(ToRequest(payload, instanceId));

            // The XML is the part that can blow the budget, and the summary is most of the value - drop it
            // whole rather than let the cap cut it mid-tree, where it would read as a complete plan.
            if (text.Length > maxOutputChars && payload.PlanXmlIncluded)
            {
                payload = PlanAnalysisPayload.Build(plan, statement, instance, includePlanXml: false);
                header.AppendLine($"The plan XML was left out: with it the result would be {text.Length} characters, over the {maxOutputChars} limit.");
                text = header + AiPlanPromptBuilder.Data(ToRequest(payload, instanceId));
            }

            return text;
        }

        /// <summary>What the masking means for a model reading a plan or deadlock, or null when nothing is masked.</summary>
        public static string? MaskNote(AiDataMask mask) => mask switch
        {
            { MetadataOnly: true } => "DBA Dash's data setting (metadataOnly) withholds statement text, parameter values and literals, and replaces logins, hosts and application names with codes - login#..., host#..., app#... - the same code for the same name.",
            { Parameters: true, Literals: true } => "Masked by DBA Dash's data setting: literals are ?, and parameter values are placeholders such as (value A) - the same placeholder is the same value.  NULL is left as it is.",
            { Parameters: true } => "Masked by DBA Dash's data setting: parameter values are placeholders such as (value A) - the same placeholder is the same value.  NULL is left as it is.  Literals in the statement are not masked.",
            { Literals: true } => "Masked by DBA Dash's data setting: literals are ?.  Parameter values are not masked.",
            _ => null
        };

        /// <summary>The payload as the AI service receives it from the plan viewer - see PlanAnalysisClient.ToJson.</summary>
        internal static AiPlanAnalysisRequest ToRequest(PlanAnalysisPayload payload, int? instanceId) => new()
        {
            PlanXml = payload.PlanXml,
            StatementText = payload.StatementText,
            StatementTruncated = payload.StatementTruncated,
            Signature = payload.Signature,
            PlanHash = payload.PlanHash,
            PayloadVersion = payload.Version,
            InstanceId = instanceId,
            Instance = payload.Instance,
            FileName = payload.FileName,
            Context = payload.Context.ToList(),
            Statistics = payload.Statistics.ToList(),
            Insights = payload.Insights.ToList(),
            MissingIndexes = payload.MissingIndexes.ToList(),
            Operators = payload.Operators.ToList(),
            Waits = payload.Waits.ToList(),
            Parameters = payload.Parameters.ToList(),
            Objects = payload.Objects.ToList(),
            IsActualPlan = payload.IsActualPlan
        };
    }
}
