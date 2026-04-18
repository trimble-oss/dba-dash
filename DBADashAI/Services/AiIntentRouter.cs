using DBADashAI.Models;

namespace DBADashAI.Services;

public sealed class AiIntentRouter
{
    public IReadOnlyList<IAiTool> SelectTools(AiAskRequest request, IEnumerable<IAiTool> tools)
    {
        var toolList = tools.ToList();

        if (!string.IsNullOrWhiteSpace(request.ToolName))
        {
            var explicitTool = toolList.FirstOrDefault(t => string.Equals(t.Name, request.ToolName, StringComparison.OrdinalIgnoreCase));
            return explicitTool is null ? [] : [explicitTool];
        }

        var q = request.Question.ToLowerInvariant();

        var scored = toolList
            .Select(t => new
            {
                Tool = t,
                Score = ComputeScore(q, t)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Tool.Name)
            .ToList();

        var wantsBroadTriage = q.Contains("why", StringComparison.OrdinalIgnoreCase)
                               || q.Contains("root cause", StringComparison.OrdinalIgnoreCase)
                               || q.Contains("triage", StringComparison.OrdinalIgnoreCase)
                               || q.Contains("summary", StringComparison.OrdinalIgnoreCase)
                               || q.Contains("overall", StringComparison.OrdinalIgnoreCase)
                               || (q.Contains("blocking", StringComparison.OrdinalIgnoreCase) && q.Contains("wait", StringComparison.OrdinalIgnoreCase));

        if (scored.Count == 0)
        {
            var fallback = toolList.FirstOrDefault(t => t.Name == "active-alerts-summary") ?? toolList.FirstOrDefault();
            return fallback is null ? [] : [fallback];
        }

        if (wantsBroadTriage)
        {
            return scored.Take(3).Select(x => x.Tool).ToList();
        }

        return [scored[0].Tool];
    }

    private static int ComputeScore(string question, IAiTool tool)
    {
        var score = tool.Keywords.Sum(k => question.Contains(k, StringComparison.OrdinalIgnoreCase) ? 1 : 0);

        if (tool.Name == "slow-queries-summary"
            && (question.Contains("regress", StringComparison.OrdinalIgnoreCase)
                || question.Contains("query", StringComparison.OrdinalIgnoreCase)
                || question.Contains("slow", StringComparison.OrdinalIgnoreCase)))
        {
            score += 3;
        }

        if (tool.Name == "waits-summary"
            && (question.Contains("wait profile", StringComparison.OrdinalIgnoreCase)
                || question.Contains("lock contention", StringComparison.OrdinalIgnoreCase)
                || question.Contains("contention", StringComparison.OrdinalIgnoreCase)))
        {
            score += 3;
        }

        if (tool.Name == "blocking-summary"
            && question.Contains("blocking", StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (tool.Name == "instance-metadata-summary")
        {
            var asksCount = question.Contains("how many", StringComparison.OrdinalIgnoreCase)
                            || question.Contains("count", StringComparison.OrdinalIgnoreCase)
                            || question.Contains("number of", StringComparison.OrdinalIgnoreCase);
            var asksInventory = question.Contains("server", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("instance", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("version", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("edition", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("ram", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("memory", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("metadata", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("inventory", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("sql 2016", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("sql 2017", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("sql 2019", StringComparison.OrdinalIgnoreCase)
                                || question.Contains("sql 2022", StringComparison.OrdinalIgnoreCase);

            if (asksCount && asksInventory)
            {
                score += 5;
            }
            else if (asksInventory)
            {
                score += 3;
            }
        }

        if (tool.Name == "ag-dr-risk-summary")
        {
            var asksAgDr = question.Contains("availability group", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("ag ", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("replica", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("failover", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("dr", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("rpo", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("recoverability", StringComparison.OrdinalIgnoreCase)
                           || question.Contains("hadr", StringComparison.OrdinalIgnoreCase);
            if (asksAgDr)
            {
                score += 5;
            }
        }

        if (tool.Name == "workload-pressure-summary")
        {
            var asksPressure = question.Contains("pressure", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("contention", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("bottleneck", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("why are we slow", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("performance triage", StringComparison.OrdinalIgnoreCase)
                               || (question.Contains("blocking", StringComparison.OrdinalIgnoreCase)
                                   && question.Contains("wait", StringComparison.OrdinalIgnoreCase));
            if (asksPressure)
            {
                score += 5;
            }
        }

        if (tool.Name == "reliability-risk-summary")
        {
            var asksReliability = question.Contains("reliability", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("stable", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("unstable", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("restart", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("offline", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("job failure", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("recurring", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("incident", StringComparison.OrdinalIgnoreCase);
            if (asksReliability)
            {
                score += 5;
            }
        }

        if (tool.Name == "capacity-forecast-summary")
        {
            var asksCapacity = question.Contains("capacity", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("runway", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("fill up", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("free space", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("growth", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("disk", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("drive", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("memory pressure", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("forecast", StringComparison.OrdinalIgnoreCase);
            if (asksCapacity)
            {
                score += 5;
            }
        }

        if (tool.Name == "config-risk-drift-summary")
        {
            var asksConfigRisk = question.Contains("config risk", StringComparison.OrdinalIgnoreCase)
                                 || question.Contains("drift", StringComparison.OrdinalIgnoreCase)
                                 || question.Contains("configuration change", StringComparison.OrdinalIgnoreCase)
                                 || question.Contains("trace flag", StringComparison.OrdinalIgnoreCase)
                                 || question.Contains("patch", StringComparison.OrdinalIgnoreCase)
                                 || question.Contains("change review", StringComparison.OrdinalIgnoreCase)
                                 || question.Contains("what changed", StringComparison.OrdinalIgnoreCase);
            if (asksConfigRisk)
            {
                score += 5;
            }
        }

        if (tool.Name == "operational-hygiene-summary")
        {
            var asksHygiene = question.Contains("hygiene", StringComparison.OrdinalIgnoreCase)
                              || question.Contains("acknowledge", StringComparison.OrdinalIgnoreCase)
                              || question.Contains("unacknowledged", StringComparison.OrdinalIgnoreCase)
                              || question.Contains("stale alert", StringComparison.OrdinalIgnoreCase)
                              || question.Contains("backlog", StringComparison.OrdinalIgnoreCase)
                              || question.Contains("alert debt", StringComparison.OrdinalIgnoreCase)
                              || question.Contains("workflow", StringComparison.OrdinalIgnoreCase);
            if (asksHygiene)
            {
                score += 5;
            }
        }

        if (tool.Name == "cross-signal-correlation-summary")
        {
            var asksCorrelation = question.Contains("correlation", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("cross-signal", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("systemic", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("multi signal", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("overall risk", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("root cause cluster", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("top risks", StringComparison.OrdinalIgnoreCase)
                                  || (question.Contains("why", StringComparison.OrdinalIgnoreCase)
                                      && question.Contains("slow", StringComparison.OrdinalIgnoreCase));
            if (asksCorrelation)
            {
                score += 6;
            }
        }

        return score;
    }
}
