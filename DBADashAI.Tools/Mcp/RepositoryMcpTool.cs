using System.Data;
using System.Globalization;
using System.Text.Json;
using DBADashAI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// What the client's model did wrong, or what it asked for that isn't there - said back to it as the tool's
    /// result so it can correct the call, rather than thrown as a protocol error.
    /// </summary>
    public sealed class McpToolException(string message) : Exception(message);

    /// <summary>
    /// An MCP tool over the repository that takes arguments of its own, unlike the summary tools that
    /// <see cref="AiToolMcpAdapter"/> offers - one deadlock, one plan, one instance's performance.  Checks the
    /// arguments against the schema's property names, reports a bad call or a failed query back to the model,
    /// and caps what it returns at Mcp:MaxOutputChars.
    /// </summary>
    public abstract class RepositoryMcpTool : McpServerTool
    {
        private readonly HashSet<string> _argumentNames;

        protected RepositoryMcpTool(string name, string description, string inputSchema, IConfiguration? config)
        {
            var schema = JsonDocument.Parse(inputSchema).RootElement.Clone();
            _argumentNames = schema.TryGetProperty("properties", out var properties)
                ? properties.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal)
                : [];
            MaxOutputChars = config?.GetValue<int?>("Mcp:MaxOutputChars") ?? AiToolMcpAdapter.DefaultMaxOutputChars;
            _configuredMask = AiDataMask.FromConfiguration(config);

            ProtocolTool = new Tool
            {
                Name = name,
                Description = description,
                InputSchema = schema,
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

        protected int MaxOutputChars { get; }

        private readonly AiDataMask _configuredMask;

        /// <summary>
        /// What this call holds back: the mask the server's call filter read when the call started, or Mcp:DataDetail for
        /// a tool run without one.  Each tool applies it to its data before describing it.
        /// </summary>
        protected AiDataMask Mask => AiDataMaskSource.ForCall ?? _configuredMask;

        private static readonly AsyncLocal<McpServer?> CallClient = new();

        /// <summary>The connection to the client making this call, to ask it things such as its roots; null for a tool run without one.</summary>
        protected static McpServer? Client => CallClient.Value;

        protected abstract Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken);

        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default)
        {
            var services = request.Services ?? request.Server.Services
                ?? throw new InvalidOperationException("The MCP server has no service provider.");
            CallClient.Value = request.Server;

            try
            {
                var args = new Arguments(request.Params?.Arguments, _argumentNames);
                var text = await RunAsync(args, services.GetRequiredService<SqlToolExecutor>(), cancellationToken);
                return Text(Cap(text));
            }
            catch (McpToolException ex)
            {
                return Error(ex.Message);
            }
            catch (SqlException ex)
            {
                return Error(McpResultNotes.Describe(ex));
            }
        }

        /// <summary>Cuts a result that would overrun the client's context, and says it was cut.</summary>
        protected string Cap(string text) => text.Length <= MaxOutputChars
            ? text
            : text[..MaxOutputChars] + $"{Environment.NewLine}...[cut at {MaxOutputChars} of {text.Length} characters]";

        /// <summary>
        /// The one active instance a name means: an exact display name, or the start of only one.  A name that
        /// matches several is refused with the candidates, so the model can pick.
        /// </summary>
        protected static async Task<(int InstanceId, string Instance)> ResolveInstanceAsync(
            SqlToolExecutor sql,
            string name,
            CancellationToken cancellationToken)
        {
            var rows = await sql.QueryAsync("AI.Instance_Get",
                [new SqlParameter("@InstanceFilter", SqlDbType.NVarChar, 256) { Value = name }], cancellationToken);

            if (rows.Count == 0)
            {
                throw new McpToolException(
                    $"No active instance's name starts with '{name}'.  instance-metadata-summary lists the instances.");
            }

            var first = rows[0];
            var firstName = Convert.ToString(first["InstanceDisplayName"]) ?? string.Empty;
            if (rows.Count > 1 && !string.Equals(firstName, name, StringComparison.OrdinalIgnoreCase))
            {
                throw new McpToolException(
                    $"'{name}' matches several instances - give one of them in full: " +
                    string.Join(", ", rows.Select(r => r["InstanceDisplayName"])));
            }

            return (Convert.ToInt32(first["InstanceID"]), firstName);
        }

        private static CallToolResult Text(string text) => new()
        {
            Content = [new TextContentBlock { Text = text }]
        };

        private static CallToolResult Error(string message) => new()
        {
            IsError = true,
            Content = [new TextContentBlock { Text = message }]
        };

        /// <summary>The call's arguments, read and checked one by one.  A bad one throws <see cref="McpToolException"/>.</summary>
        public sealed class Arguments
        {
            private readonly IDictionary<string, JsonElement> _values;

            public Arguments(IDictionary<string, JsonElement>? values, IReadOnlySet<string> known)
            {
                _values = values ?? new Dictionary<string, JsonElement>();

                var unknown = _values.Keys.Where(k => !known.Contains(k)).ToList();
                if (unknown.Count > 0)
                {
                    throw new McpToolException(
                        $"Unknown argument '{unknown[0]}'.  The arguments are {string.Join(", ", known)}.");
                }
            }

            private JsonElement? Value(string name) =>
                _values.TryGetValue(name, out var value) && value.ValueKind != JsonValueKind.Null ? value : null;

            public string? String(string name, bool required = false)
            {
                var value = Value(name);
                if (value is null)
                {
                    return required ? throw new McpToolException($"{name} is required.") : null;
                }

                if (value.Value.ValueKind != JsonValueKind.String)
                {
                    throw new McpToolException($"{name} must be a string.");
                }

                var text = value.Value.GetString()?.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    return required ? throw new McpToolException($"{name} is required.") : null;
                }

                return text;
            }

            public int? Int(string name, int min, int max, bool required = false)
            {
                var value = Value(name);
                if (value is null)
                {
                    return required ? throw new McpToolException($"{name} is required.") : null;
                }

                if (value.Value.ValueKind != JsonValueKind.Number || !value.Value.TryGetInt32(out var number) ||
                    number < min || number > max)
                {
                    throw new McpToolException($"{name} must be a whole number from {min} to {max}.");
                }

                return number;
            }

            public bool? Bool(string name)
            {
                var value = Value(name);
                return value?.ValueKind switch
                {
                    null => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => throw new McpToolException($"{name} must be true or false.")
                };
            }

            /// <summary>A UTC date and time.  One with an offset is converted; one without is taken as UTC already.</summary>
            public DateTime? UtcDateTime(string name, bool required = false)
            {
                var text = String(name, required);
                if (text is null)
                {
                    return null;
                }

                if (!DateTime.TryParse(text, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value))
                {
                    throw new McpToolException($"{name} must be a date and time in UTC, such as 2026-10-23T14:07:12.345.");
                }

                return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            /// <summary>A hex binary value in its "0x..." form, as the repository's tools return them.</summary>
            public string? Hex(string name, int bytes, bool required = false)
            {
                var text = String(name, required);
                if (text is null)
                {
                    return null;
                }

                var digits = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text;
                if (digits.Length != bytes * 2 || !digits.All(Uri.IsHexDigit))
                {
                    throw new McpToolException($"{name} must be {bytes} bytes in hex, written 0x followed by {bytes * 2} digits.");
                }

                return "0x" + digits.ToUpperInvariant();
            }
        }
    }
}
