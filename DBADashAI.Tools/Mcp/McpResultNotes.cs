using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// What a tool's result can't say for itself, said to the model in words: that a list stopped at the row limit
    /// rather than at the end of the data, and what a failed repository query means for whoever has to fix it.
    /// </summary>
    public static class McpResultNotes
    {
        /// <summary>
        /// One note per list in <paramref name="data"/> that came back exactly <paramref name="maxRows"/> long - the
        /// procedures stop there, so the list is probably the top of a longer one, and a model that reads it as the
        /// whole would say "only 50 waits" of a server with hundreds.
        /// </summary>
        public static IEnumerable<string> RowLimitNotes(JsonElement data, int maxRows)
        {
            if (data.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }

            foreach (var property in data.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array && property.Value.GetArrayLength() == maxRows)
                {
                    yield return $"{property.Name} has {maxRows} rows, which is the maxRows limit, so there are probably more.  " +
                                 $"Raise maxRows (up to {Models.AiAskRequest.MaxAllowedRows}) or narrow instance or hoursBack to see the rest.";
                }
            }
        }

        /// <summary>
        /// <paramref name="data"/> with every fractional number rounded to <paramref name="decimals"/> places.  SQL Server's
        /// decimal arithmetic hands back figures like 3708.017949558527843342 ms, whose last eighteen digits mean nothing
        /// and cost the client's context the same as the first four.  Whole numbers are left as they are.
        /// </summary>
        public static JsonElement RoundNumbers(JsonElement data, int decimals = 3)
        {
            var node = JsonNode.Parse(data.GetRawText());
            Round(node, decimals);
            return JsonSerializer.SerializeToElement(node);
        }

        private static void Round(JsonNode? node, int decimals)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var key in obj.Select(p => p.Key).ToList())
                    {
                        if (obj[key] is JsonValue value && Rounded(value, decimals) is { } rounded) obj[key] = rounded;
                        else Round(obj[key], decimals);
                    }

                    break;
                case JsonArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        if (array[i] is JsonValue value && Rounded(value, decimals) is { } rounded) array[i] = rounded;
                        else Round(array[i], decimals);
                    }

                    break;
            }
        }

        private static JsonValue? Rounded(JsonValue value, int decimals) =>
            value.GetValueKind() == JsonValueKind.Number && value.TryGetValue<decimal>(out var number) && number != decimal.Truncate(number)
                ? JsonValue.Create(Math.Round(number, decimals, MidpointRounding.AwayFromZero))
                : null;

        /// <summary>
        /// A failed query, explained.  Permission errors are the common one on a first run, and the fix is specific
        /// to DBA Dash, so it is spelt out rather than left as SQL Server's message.
        /// </summary>
        public static string Describe(SqlException ex) => ex.Number switch
        {
            // EXECUTE or SELECT denied on an object.
            229 or 230 =>
                $"The repository refused the query: {ex.Message}  The database user this MCP server connects as needs the " +
                "McpUser role in the DBA Dash repository - a DBA can run ALTER ROLE McpUser ADD MEMBER [<user>] there.  " +
                "Tell the user this; retrying won't help.",
            // Cannot open database / login failed.
            4060 or 18456 =>
                $"Couldn't sign in to the DBA Dash repository: {ex.Message}  Check the repository connection the MCP server " +
                "is configured with.  Tell the user this; retrying won't help.",
            // A procedure that isn't there, or doesn't take what this version passes: the repository predates this server.
            2812 or 8144 or 201 =>
                $"The repository's DBA Dash database is older than this MCP server: {ex.Message}  Upgrade the repository " +
                "database to the same DBA Dash version to use this tool.  Tell the user this; retrying won't help.",
            // Timeout.
            -2 =>
                $"The repository query timed out: {ex.Message}  Try a single instance, a shorter window or fewer rows.",
            _ => $"The repository query failed: {ex.Message}"
        };
    }
}
