using System.Reflection;

namespace DBADashAI.Services
{
    /// <summary>
    /// Loads the AI system prompt with the following resolution order:
    ///
    ///   1. Explicit path from <c>AI:SystemPromptPath</c> in configuration.
    ///   2. <c>system-prompt.txt</c> in the application base directory (next to the exe).
    ///      Create this file to customise the prompt without touching appsettings.json.
    ///      It will never be overwritten by a deployment because it is not part of the build output.
    ///   3. The embedded resource <c>DBADashAI.Prompts.system-prompt.txt</c> compiled into
    ///      the assembly. This is always present and is the safe fallback.
    ///
    /// The resolved prompt is cached for the lifetime of the singleton so the file is read
    /// once at startup, not on every request.
    /// </summary>
    public class SystemPromptLoader
    {
        private const string ConventionalFileName = "system-prompt.txt";
        private const string EmbeddedResourceName = "DBADashAI.Prompts.system-prompt.txt";

        private readonly string _prompt;
        private readonly string _source;

        public SystemPromptLoader(IConfiguration configuration, ILogger<SystemPromptLoader> logger)
        {
            (_prompt, _source) = Load(configuration, logger);
            logger.LogInformation("System prompt loaded from {Source} ({Length} chars)", _source, _prompt.Length);
        }

        /// <summary>The resolved system prompt text.</summary>
        public string Prompt => _prompt;

        /// <summary>Human-readable description of where the prompt was loaded from.</summary>
        public string Source => _source;

        private static (string prompt, string source) Load(IConfiguration configuration, ILogger logger)
        {
            // 1. Explicit path from configuration.
            var configPath = configuration["AI:SystemPromptPath"];
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                var fullPath = Path.IsPathRooted(configPath)
                    ? configPath
                    : Path.Combine(AppContext.BaseDirectory, configPath);

                if (File.Exists(fullPath))
                {
                    var text = File.ReadAllText(fullPath).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        return (text, $"config path: {fullPath}");
                }

                logger.LogWarning(
                    "AI:SystemPromptPath is set to '{ConfigPath}' but the file was not found or is empty. " +
                    "Falling back to conventional file or embedded resource.", configPath);
            }

            // 2. Conventional override file next to the executable.
            var conventionalPath = Path.Combine(AppContext.BaseDirectory, ConventionalFileName);
            if (File.Exists(conventionalPath))
            {
                var text = File.ReadAllText(conventionalPath).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return (text, $"override file: {conventionalPath}");
            }

            // 3. Embedded resource — always present, never missing.
            return (ReadEmbeddedResource(), $"embedded resource: {EmbeddedResourceName}");
        }

        private static string ReadEmbeddedResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{EmbeddedResourceName}' not found. " +
                    "Ensure Prompts/system-prompt.txt is marked as EmbeddedResource in the project file.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }
    }
}
