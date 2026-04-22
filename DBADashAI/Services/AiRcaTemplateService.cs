namespace DBADashAI.Services
{
    public class AiRcaTemplateService
    {
        public string GetTemplate(string question, IReadOnlyCollection<string> selectedTools)
        {
            var q = question.ToLowerInvariant();

            var incidentLike = q.Contains("why")
                               || q.Contains("root cause")
                               || q.Contains("incident")
                               || q.Contains("outage")
                               || q.Contains("slow");

            if (!incidentLike)
            {
                return "Use standard operational summary format.";
            }

            if (selectedTools.Contains("blocking-summary") || selectedTools.Contains("waits-summary"))
            {
                return "RCA template: Symptom timeline -> pressure source (wait/blocking) -> likely workload/query trigger -> immediate mitigation -> follow-up validation.";
            }

            if (selectedTools.Contains("agent-job-alerts") || selectedTools.Contains("backups-risk-summary"))
            {
                return "RCA template: Failure scope -> failed component/job -> dependency impact -> recovery actions -> prevention controls.";
            }

            if (selectedTools.Contains("config-drift-summary"))
            {
                return "RCA template: Change timeline -> drifted setting -> expected vs actual behavior -> rollback/adjustment plan -> verification checks.";
            }

            return "RCA template: Timeline -> strongest evidence -> primary hypothesis -> alternative hypotheses -> recommended actions and verification.";
        }
    }
}