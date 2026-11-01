using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// One of the repository tools, offered over MCP.
    ///
    /// /ask has to work out which tools a question needs, and what instance and time window it is about,
    /// from the question's words.  Over MCP the client's model does that itself: it reads the descriptions,
    /// picks the tool and fills in the arguments, so none of the routing is needed here - only the tool.
    ///
    /// The tool is created for each call, from the call's own services, because the tools are scoped and
    /// hold a <see cref="SqlToolExecutor"/>.  Its name and description are read once, when the adapter is
    /// made, so listing the tools never touches the repository.
    /// </summary>
    public sealed class AiToolMcpAdapter : McpServerTool
    {
        public const int DefaultMaxRows = 50;
        public const int MaxHoursBack = 24 * 366;
        public const int DefaultMaxOutputChars = 60_000;
        public const int DefaultMaxStringValueLength = 2_000;

        /// <summary>The arguments every summary tool takes, and with <paramref name="supportsEndTime"/> the end of the window too.</summary>
        internal static JsonElement InputSchema(bool supportsEndTime) => supportsEndTime ? WindowedSchema : PlainSchema;

        private static readonly JsonElement PlainSchema = Schema(false);
        private static readonly JsonElement WindowedSchema = Schema(true);

        private static JsonElement Schema(bool supportsEndTime) => JsonDocument.Parse($$"""
            {
              "type": "object",
              "properties": {
                "instance": {
                  "type": "string",
                  "description": "Only instances whose display name starts with this text.  Leave it out for every instance.  instance-metadata-summary lists the instances."
                },
                "hoursBack": {
                  "type": "integer",
                  "minimum": 1,
                  "maximum": {{MaxHoursBack}},
                  "description": "How many hours of history to look at, for the tools that look back.  Leave it out for the tool's own default, usually 24."
                },
                "maxRows": {
                  "type": "integer",
                  "minimum": 1,
                  "maximum": {{AiAskRequest.MaxAllowedRows}},
                  "description": "The most rows to return from each result set.  Defaults to {{DefaultMaxRows}}."
                }{{(supportsEndTime ? ToProperty : "")}}
              },
              "additionalProperties": false
            }
            """).RootElement.Clone();

        private const string ToProperty = """
            ,
                "to": {
                  "type": "string",
                  "description": "When the window ends, in UTC, such as 2026-10-23T14:00:00.  The window is the hoursBack hours before it.  Leave it out for now.  Use it to look at a past incident."
                }
            """;

        private readonly Type _toolType;
        private readonly bool _supportsEndTime;
        private readonly AiPromptDataSerializer _serializer;
        private readonly AiDataMask _mask;

        public AiToolMcpAdapter(Type toolType, string name, string description, AiPromptDataSerializer serializer,
            bool supportsEndTime = false, AiDataMask? mask = null)
        {
            _toolType = toolType;
            _serializer = serializer;
            _supportsEndTime = supportsEndTime;
            _mask = mask ?? AiDataMask.Default;
            ProtocolTool = new Tool
            {
                Name = name,
                Description = description,
                InputSchema = InputSchema(supportsEndTime),
                Annotations = new ToolAnnotations
                {
                    Title = name,
                    ReadOnlyHint = true,
                    DestructiveHint = false,
                    IdempotentHint = true,
                    OpenWorldHint = false
                }
            };
        }

        public override Tool ProtocolTool { get; }

        public override IReadOnlyList<object> Metadata => [];

        /// <summary>
        /// The adapter for a tool type, described by an instance of the tool made in a throwaway scope.
        /// Making one doesn't open a connection - the tools only query when they are run.
        /// </summary>
        public static AiToolMcpAdapter Create(IServiceProvider services, Type toolType)
        {
            using var scope = services.CreateScope();
            var tool = (IAiTool)ActivatorUtilities.CreateInstance(scope.ServiceProvider, toolType);
            var config = services.GetService<IConfiguration>();

            var serializer = new AiPromptDataSerializer(
                config?.GetValue<int?>("Mcp:MaxOutputChars") ?? DefaultMaxOutputChars,
                AiAskRequest.MaxAllowedRows,
                config?.GetValue<int?>("Mcp:MaxStringValueLength") ?? DefaultMaxStringValueLength);

            return new AiToolMcpAdapter(toolType, tool.Name, Describe(tool), serializer, tool.SupportsEndTime, AiDataMask.FromConfiguration(config));
        }

        internal static string Describe(IAiTool tool) => $"{tool.Description} {tool.InputHint}";

        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default)
        {
            var (askRequest, error) = ParseArguments(ProtocolTool.Name, request.Params?.Arguments, _supportsEndTime);
            if (askRequest is null)
            {
                return Error(error!);
            }

            var services = request.Services ?? request.Server.Services
                ?? throw new InvalidOperationException("The MCP server has no service provider.");
            var tool = (IAiTool)ActivatorUtilities.CreateInstance(services, _toolType);

            AiToolResult result;
            var sw = Stopwatch.StartNew();
            try
            {
                result = await tool.RunAsync(askRequest, cancellationToken);
            }
            catch (SqlException ex)
            {
                // Said back to the model rather than thrown: a permission or timeout it can explain to the
                // user, or work round with a narrower instance or window.
                return Error(McpResultNotes.Describe(ex));
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }

            sw.Stop();

            var execution = new AiToolExecutionResult
            {
                Tool = ProtocolTool.Name,
                Data = McpResultNotes.RoundNumbers(AiDataMasker.MaskJson(JsonSerializer.SerializeToElement(result.Data), AiDataMaskSource.ForCall ?? _mask)),
                Evidence = result.Evidence,
                RowCount = result.RowCount,
                ExecutionMs = sw.ElapsedMilliseconds
            };

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = Format(execution, askRequest) }]
            };
        }

        /// <summary>
        /// The call's arguments as the request the tools already take.  Returns the error to send back
        /// instead when an argument is the wrong type or out of range - the model can correct and retry.
        /// </summary>
        internal static (AiAskRequest? Request, string? Error) ParseArguments(
            string toolName,
            IDictionary<string, JsonElement>? arguments,
            bool supportsEndTime = false)
        {
            var request = new AiAskRequest
            {
                Question = $"MCP call to {toolName}",
                ToolName = toolName,
                IncludeAiSummary = false,
                MaxRows = DefaultMaxRows
            };

            if (arguments is null)
            {
                return (request, null);
            }

            foreach (var (key, value) in arguments)
            {
                if (value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                switch (key)
                {
                    case "instance":
                        if (value.ValueKind != JsonValueKind.String)
                        {
                            return (null, "instance must be a string.");
                        }

                        var instance = value.GetString();
                        request.InstanceFilter = string.IsNullOrWhiteSpace(instance) ? null : instance.Trim();
                        break;

                    case "hoursBack":
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var hoursBack) || hoursBack < 1 || hoursBack > MaxHoursBack)
                        {
                            return (null, $"hoursBack must be a whole number from 1 to {MaxHoursBack}.");
                        }

                        request.HoursBack = hoursBack;
                        break;

                    case "maxRows":
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var maxRows) || maxRows < 1 || maxRows > AiAskRequest.MaxAllowedRows)
                        {
                            return (null, $"maxRows must be a whole number from 1 to {AiAskRequest.MaxAllowedRows}.");
                        }

                        request.MaxRows = maxRows;
                        break;

                    case "to" when supportsEndTime:
                        if (value.ValueKind != JsonValueKind.String || !DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var to))
                        {
                            return (null, "to must be a date and time in UTC, such as 2026-10-23T14:00:00.");
                        }

                        request.ToUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);
                        break;

                    default:
                        return (null, $"Unknown argument '{key}'.  The arguments are instance, hoursBack{(supportsEndTime ? ", maxRows and to" : " and maxRows")}.");
                }
            }

            return (request, null);
        }

        private string Format(AiToolExecutionResult execution, AiAskRequest request)
        {
            var text = new StringBuilder();

            text.Append($"Filters: instance={request.InstanceFilter ?? "(all)"}, ");
            text.Append($"hoursBack={request.HoursBack?.ToString() ?? "(tool default)"}, maxRows={request.MaxRows}");
            text.AppendLine(request.ToUtc is { } to ? $", to={to:yyyy-MM-ddTHH:mm:ss} UTC" : "");
            text.Append(_serializer.Serialize([execution]));

            foreach (var note in McpResultNotes.RowLimitNotes(execution.Data, request.MaxRows))
            {
                text.AppendLine($"Note: {note}");
            }

            if (execution.Evidence.Count > 0)
            {
                text.AppendLine("Sources:");
                foreach (var evidence in execution.Evidence)
                {
                    text.AppendLine($"- {evidence.Source}: {evidence.Detail}");
                }
            }

            return text.ToString();
        }

        private static CallToolResult Error(string message) => new()
        {
            IsError = true,
            Content = [new TextContentBlock { Text = message }]
        };
    }
}
