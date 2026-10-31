using System.Text;
using System.Text.Json;
using DBADash;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DBADashMCP
{
    /// <summary>
    /// What the user has open in the DBA Dash GUI, read from <see cref="GuiContextPipe"/>.  Local only: it reads the
    /// GUI running in the same Windows session, which only the stdio server shares with the user.
    /// </summary>
    internal sealed class GuiContextTool : McpServerTool
    {
        private readonly GuiContextReader _gui;

        public GuiContextTool(GuiContextReader gui)
        {
            _gui = gui;
            ProtocolTool = new Tool
            {
                Name = "get-gui-context",
                Description =
                    "What the user is looking at in the DBA Dash GUI right now: the selected tree node, the instance, " +
                    "database or object it is for, the open tab, the time window the charts show, and whether a deadlock or " +
                    "query plan is open in its viewers (get-open-deadlock and get-open-query-plan fetch those).  Use it when the " +
                    "user refers to \"this instance\", \"this database\", \"what I'm looking at\" or \"the selected time " +
                    "range\", then pass what it returns to the other tools.",
                InputSchema = JsonDocument.Parse("""{ "type": "object", "properties": {}, "additionalProperties": false }""").RootElement.Clone(),
                Annotations = new ToolAnnotations
                {
                    Title = "get-gui-context",
                    ReadOnlyHint = true,
                    DestructiveHint = false,
                    IdempotentHint = false,
                    OpenWorldHint = false
                }
            };
        }

        public override Tool ProtocolTool { get; }

        public override IReadOnlyList<object> Metadata => [];

        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default)
        {
            var (snapshot, same) = await _gui.ReadAsync(cancellationToken);
            return snapshot is null
                ? Result(GuiContextReader.NotOpen + "  Ask the user which instance and time window they mean.", isError: true)
                : Result(Describe(snapshot, same), isError: false);
        }

        internal static string Describe(GuiContextSnapshot snapshot, bool? sameRepository)
        {
            var text = new StringBuilder();
            text.AppendLine("The DBA Dash GUI is showing:");
            if (snapshot.Tab is not null) text.AppendLine($"- Tab: {snapshot.Tab}");
            if (snapshot.NodePath is not null) text.AppendLine($"- Tree node: {snapshot.NodePath} ({snapshot.NodeType})");

            if (snapshot.Instance is not null)
            {
                text.AppendLine($"- Instance: {snapshot.Instance}{(snapshot.InstanceId is { } id ? $" (InstanceID {id})" : "")}");
            }
            else if (snapshot.InstanceIds.Count > 0)
            {
                text.AppendLine(snapshot.InstanceIds.Count <= 20
                    ? $"- Instances in scope: {snapshot.InstanceIds.Count} (InstanceIDs {string.Join(", ", snapshot.InstanceIds)})"
                    : $"- Instances in scope: {snapshot.InstanceIds.Count}");
            }

            if (snapshot.Database is not null) text.AppendLine($"- Database: {snapshot.Database}");
            if (snapshot.Object is not null) text.AppendLine($"- Object: {snapshot.Object}");

            var length = snapshot.ToUtc - snapshot.FromUtc;
            var endsNow = (snapshot.CapturedUtc - snapshot.ToUtc).Duration() < TimeSpan.FromMinutes(2);
            // A window ending now is captured with its end a moment after its start was set, so it runs a little over its
            // whole hours: a minute's slack keeps "the last hour" from becoming hoursBack 2.
            var hours = Math.Max(1, (int)Math.Ceiling((length - TimeSpan.FromMinutes(1)).TotalHours));
            text.AppendLine($"- Time window: {snapshot.FromUtc:yyyy-MM-dd HH:mm} to {snapshot.ToUtc:yyyy-MM-dd HH:mm} UTC" +
                            (endsNow ? " (ending now)" : ""));

            text.AppendLine();
            var instanceArgument = snapshot.Instance is null ? "" : $"instance \"{snapshot.Instance}\", ";
            text.AppendLine(endsNow
                ? $"To match it, pass {instanceArgument}hoursBack {hours} to the other tools."
                : $"To match it, pass {instanceArgument}hoursBack {hours} and to \"{snapshot.ToUtc:yyyy-MM-ddTHH:mm:ss}\" to every tool that has a to argument.  " +
                  "A tool without one always ends its window now.");

            if (snapshot.Deadlock is { } deadlock)
            {
                text.AppendLine($"A deadlock is open in the GUI's deadlock viewer (\"{deadlock.Title}\", {GuiContextReader.Source(deadlock)}): get-open-deadlock describes it.");
            }

            if (snapshot.Plan is { } plan)
            {
                text.AppendLine($"A query plan is open in the GUI's plan viewer (\"{plan.Title}\", {GuiContextReader.Source(plan)}): get-open-query-plan describes it.");
            }

            if (sameRepository is null)
            {
                text.AppendLine("Couldn't check that the GUI and this server use the same repository.");
            }
            else if (sameRepository == false)
            {
                text.AppendLine("Warning: the GUI is connected to a different repository from this MCP server, so its " +
                                "instances and IDs may not match what the tools see.");
            }

            return text.ToString();
        }

        private static CallToolResult Result(string text, bool isError) => new()
        {
            IsError = isError ? true : null,
            Content = [new TextContentBlock { Text = text }]
        };
    }
}
