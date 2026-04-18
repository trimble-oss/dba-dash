using DBADashAI.Models;

namespace DBADashAI.Services;

public interface IAiTool
{
    string Name { get; }

    string Description { get; }

    string InputHint { get; }

    string[] Keywords { get; }

    Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken);
}
