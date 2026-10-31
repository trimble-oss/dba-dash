using DBADashAI.Models;

namespace DBADashAI.Services
{
    public interface IAiTool
    {
        string Name { get; }

        string Description { get; }

        string InputHint { get; }

        string[] Keywords { get; }

        /// <summary>
        /// True when the tool passes <see cref="AiAskRequest.ToUtc"/> on, so its window can end in the past - its
        /// procedure takes @ToDate.  The MCP adapter only offers a "to" argument for these.
        /// </summary>
        bool SupportsEndTime => false;

        Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken);
    }
}
