using DBADashAI.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DBADashAI.Services
{
    /// <summary>
    /// Serializes tool result data into a token-budget-aware string for LLM prompts.
    /// Prevents context-window overflow by:
    ///   1. Capping rows per result-set array (highest-priority rows, returned first by the SP, are kept).
    ///   2. Truncating long string values within each row (e.g. query text, alert messages).
    ///   3. Enforcing a total character budget across all tool sections; if a tool's data
    ///      still exceeds the remaining budget after trimming, only the column schema is emitted
    ///      so the LLM at least knows the shape of the data.
    ///
    /// All three limits are configurable via appsettings.json under the "AI" section:
    ///   AI:MaxPromptDataChars      — total chars for all tool data (default 80,000 ≈ ~20K tokens)
    ///   AI:MaxRowsPerResultSet     — rows per array before truncation (default 50)
    ///   AI:MaxStringValueLength    — chars per string field before truncation (default 300)
    /// </summary>
    public class AiPromptDataSerializer
    {
        private const int DefaultMaxTotalChars = 80_000;
        private const int DefaultMaxRowsPerResultSet = 50;
        private const int DefaultMaxStringValueLength = 300;

        private readonly int _maxTotalChars;
        private readonly int _maxRowsPerResultSet;
        private readonly int _maxStringValueLength;

        private static readonly JsonSerializerOptions CompactOptions = new()
        {
            WriteIndented = false
        };

        public AiPromptDataSerializer(IConfiguration configuration)
        {
            _maxTotalChars = configuration.GetValue<int?>("AI:MaxPromptDataChars") ?? DefaultMaxTotalChars;
            _maxRowsPerResultSet = configuration.GetValue<int?>("AI:MaxRowsPerResultSet") ?? DefaultMaxRowsPerResultSet;
            _maxStringValueLength = configuration.GetValue<int?>("AI:MaxStringValueLength") ?? DefaultMaxStringValueLength;
        }

        /// <summary>
        /// Serializes all tool results into a compact, budget-bounded multi-line string.
        /// Each tool section is prefixed with its name, row count, execution time, and any
        /// truncation notices so the LLM has full context about what data was omitted.
        /// </summary>
        public string Serialize(IReadOnlyCollection<AiToolExecutionResult> toolResults)
        {
            var sb = new StringBuilder();
            var remaining = _maxTotalChars;

            foreach (var tool in toolResults)
            {
                if (remaining <= 0)
                {
                    sb.AppendLine($"- Tool: {tool.Tool} [OMITTED: prompt budget exhausted]");
                    continue;
                }

                var section = SerializeTool(tool, remaining);
                sb.Append(section);
                remaining -= section.Length;
            }

            return sb.ToString();
        }

        private string SerializeTool(AiToolExecutionResult tool, int budgetRemaining)
        {
            // Data is already a serialized JsonElement — parse to a mutable JsonNode for in-place trimming.
            var node = JsonNode.Parse(tool.Data.GetRawText());

            var header = new StringBuilder();
            header.AppendLine($"- Tool: {tool.Tool}");
            header.AppendLine($"  RowCount: {tool.RowCount}");
            header.AppendLine($"  ExecutionMs: {tool.ExecutionMs}");

            if (node is null)
            {
                header.AppendLine($"  Data: {{}}");
                header.AppendLine();
                return header.ToString();
            }

            var truncationNotes = new List<string>();
            TrimNode(node, truncationNotes);

            var serialized = node.ToJsonString(CompactOptions);

            // Reserve chars for the header we've already built plus a newline.
            var dataAllowance = budgetRemaining - header.Length - 10;

            if (dataAllowance <= 0 || serialized.Length > dataAllowance)
            {
                serialized = BuildSchemaOnlyFallback(node, tool, Math.Max(dataAllowance, 200));
                truncationNotes.Add(
                    $"data ({tool.RowCount} rows) exceeded remaining prompt budget; schema-only emitted. " +
                    "Use InstanceFilter or reduce MaxRows for full detail.");
            }

            if (truncationNotes.Count > 0)
                header.AppendLine($"  Note: {string.Join("; ", truncationNotes)}");

            header.AppendLine($"  Data: {serialized}");
            header.AppendLine();

            return header.ToString();
        }

        /// <summary>
        /// Walks the JSON node tree in-place, trimming arrays and long string values.
        /// </summary>
        private void TrimNode(JsonNode? node, List<string> notes)
        {
            switch (node)
            {
                case JsonArray arr:
                    TrimArray(arr, notes);
                    break;

                case JsonObject obj:
                    TrimObject(obj, notes);
                    break;
            }
            // JsonValue primitives are handled by the parent object (TrimObject) so that
            // we can replace a string value on the parent without needing ReplaceWith.
        }

        private void TrimArray(JsonArray arr, List<string> notes)
        {
            if (arr.Count > _maxRowsPerResultSet)
            {
                var originalCount = arr.Count;
                // Remove from the tail — SPs return highest-signal rows first.
                while (arr.Count > _maxRowsPerResultSet)
                    arr.RemoveAt(arr.Count - 1);

                notes.Add($"array trimmed to {_maxRowsPerResultSet}/{originalCount} rows (highest-signal rows kept)");
            }

            // Snapshot keys before iteration so mutation is safe.
            foreach (var item in arr.ToList())
                TrimNode(item, notes);
        }

        private void TrimObject(JsonObject obj, List<string> notes)
        {
            // Snapshot keys before iteration so we can safely replace values.
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                var value = obj[key];

                if (value is JsonValue val && val.TryGetValue<string>(out var str) && str.Length > _maxStringValueLength)
                {
                    obj[key] = JsonValue.Create(str[.._maxStringValueLength] + "...[truncated]");
                }
                else
                {
                    TrimNode(value, notes);
                }
            }
        }

        /// <summary>
        /// When a tool's data still exceeds the remaining budget after trimming, emits only
        /// the column names from the first result-set array so the LLM knows the data shape.
        /// </summary>
        private static string BuildSchemaOnlyFallback(JsonNode node, AiToolExecutionResult tool, int budgetAllowance)
        {
            JsonArray? firstArray = null;
            string? arrayPropertyName = null;

            if (node is JsonObject obj)
            {
                foreach (var prop in obj)
                {
                    if (prop.Value is JsonArray arr && arr.Count > 0)
                    {
                        firstArray = arr;
                        arrayPropertyName = prop.Key;
                        break;
                    }
                }
            }
            else if (node is JsonArray rootArr && rootArr.Count > 0)
            {
                firstArray = rootArr;
            }

            if (firstArray is null || firstArray.Count == 0 || firstArray[0] is not JsonObject firstRow)
            {
                var minimal = $"{{\"_budgetExceeded\":true,\"rowCount\":{tool.RowCount}}}";
                return minimal.Length <= budgetAllowance ? minimal : $"{{\"_budgetExceeded\":true}}";
            }

            var columns = firstRow.Select(p => p.Key).ToList();
            var fallback = new JsonObject
            {
                ["_budgetExceeded"] = JsonValue.Create(true),
                ["rowCount"] = JsonValue.Create(tool.RowCount),
                ["resultSet"] = JsonValue.Create(arrayPropertyName ?? "rows"),
                ["columns"] = JsonValue.Create(string.Join(", ", columns)),
                ["note"] = JsonValue.Create(
                    "Full data omitted to stay within LLM context window. " +
                    "Reduce MaxRows or apply InstanceFilter to get full data for this tool.")
            };

            var result = fallback.ToJsonString(CompactOptions);
            return result.Length <= budgetAllowance ? result : $"{{\"_budgetExceeded\":true,\"rowCount\":{tool.RowCount},\"columns\":\"{string.Join(",", columns.Take(10))}\"}}";
        }
    }
}
