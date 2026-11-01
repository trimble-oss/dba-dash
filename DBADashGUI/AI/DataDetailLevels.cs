#nullable enable
using System;
using System.Threading.Tasks;
using DBADashAI.Mcp;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.AI
{
    /// <summary>
    /// What the AI tools mask before data reaches a model, as the GUI offers it.  The words are <see cref="AiDataMask"/>'s,
    /// which DBADashMCP.exe and the AI service accept.
    /// </summary>
    internal static class DataDetailLevels
    {
        public const string Full = "full";
        public const string ParametersAndLiterals = "parametersAndLiterals";
        public const string MetadataOnly = "metadataOnly";

        /// <summary>The levels in the order the dialog lists them, with what each one means for the user.</summary>
        public static readonly (string Word, string Label, string Description)[] All =
        [
            (ParametersAndLiterals, "Mask parameter values and literals (recommended)", "Statement text and plans keep their shape.  Literals become ? and parameter values become placeholders such as (value A).  Logins, hosts and application names are shown."),
            ("parameters", "Mask parameter values", "Parameter values in plans become placeholders such as (value A), and the values passed to procedures and sp_executesql in query text become ?.  Literals in statement text are kept - for applications that pass their data as parameters, where the literals are the application's own constants."),
            ("literals", "Mask literals", "Literals in statement text and plans become ?.  Parameter values are kept."),
            (MetadataOnly, "Metadata only", "Statement text, plans and input buffers are withheld.  Logins, hosts and application names become stable codes such as login#3fa9c1d2, host#... and app#... - the same code for the same name."),
            (Full, "Full", "Nothing is masked: statement text, parameter values, logins, hosts and application names are sent as they are.")
        ];

        public static string Describe(string? word) => Array.Find(All, l => l.Word == Normalise(word)).Description ?? string.Empty;

        /// <summary>The level's label as a short phrase, without the list's "(recommended)".</summary>
        public static string Label(string? word) =>
            (Array.Find(All, l => l.Word == Normalise(word)).Label ?? string.Empty).Replace(" (recommended)", string.Empty);

        public static bool IsKnown(string? word) => Normalise(word) is not null;

        /// <summary>The word as the list has it - in its case, none for full - or null for none of them.</summary>
        public static string? Normalise(string? word) =>
            !string.IsNullOrWhiteSpace(word) && AiDataMask.TryParse(word, out var mask) ? mask.Word : null;

        /// <summary>
        /// The MCP setting the user chose for their own tools, from the file their registered servers read on each call.
        /// No file, or an unknown or unreadable word, reads as the default, which is what the servers use for it too.
        /// </summary>
        public static string McpCurrent()
        {
            var path = AiDataMaskSource.UserSetting.DefaultPath;
            try
            {
                if (AiDataMaskSource.UserSetting.ReadWord(path) is { } word) return Normalise(word) ?? ParametersAndLiterals;
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException)
            {
                Serilog.Log.Warning(ex, "Couldn't read the MCP masking setting in {Path}", path);
            }

            return ParametersAndLiterals;
        }

        /// <summary>Saves the MCP setting.  Registered servers mask their next call with it, in sessions already open too.</summary>
        public static void SetMcp(string word) => AiDataMaskSource.UserSetting.Write(AiDataMaskSource.UserSetting.DefaultPath, word);

        /// <summary>The MCP setting as a mask: what the Ask button's options start at.</summary>
        public static AiDataMask McpCurrentMask() => AiDataMask.TryParse(McpCurrent(), out var mask) ? mask : AiDataMask.Default;

        /// <summary>
        /// The built-in AI's setting in the repository, read with AI.DataDetail_Get.  Returns null when the repository has
        /// no such procedure yet (it predates the setting), so the dialog can say so instead of showing a made-up level.
        /// </summary>
        public static async Task<string?> ReadBuiltInAsync(string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("AI.DataDetail_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            return (await cmd.ExecuteScalarAsync())?.ToString();
        }

        /// <summary>
        /// Saves the built-in AI's setting with AI.DataDetail_Set.  The procedure refuses unless the caller is db_owner,
        /// and that refusal comes back as the exception's message for the dialog to show.
        /// </summary>
        public static async Task WriteBuiltInAsync(string connectionString, string word)
        {
            if (!IsKnown(word))
            {
                throw new ArgumentException($"Unknown data detail '{word}'.", nameof(word));
            }

            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("AI.DataDetail_Set", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DataDetail", word);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
