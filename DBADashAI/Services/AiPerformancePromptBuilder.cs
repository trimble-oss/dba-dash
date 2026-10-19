using DBADash.PerformanceAnalysis;
using DBADashAI.Models;

namespace DBADashAI.Services
{
    /// <summary>
    /// Turns an instance's performance over a window into the prompt the model answers.  The performance
    /// counterpart of <see cref="AiPlanPromptBuilder"/>.  The prompt itself is <see cref="PerformanceAnalysisPrompt"/>,
    /// shared with the GUI so that a reader who copies it to use elsewhere gets the same question.
    /// </summary>
    public class AiPerformancePromptBuilder
    {
        public string Build(AiPerformanceAnalysisRequest request) =>
            PerformanceAnalysisPrompt.Build(
                request.IsLatchesAndSpinlocks,
                request.Instance,
                request.Window,
                request.Baseline,
                request.Notes,
                request.Sections.Select(s => (s.Title, s.Content ?? string.Empty)));
    }
}
