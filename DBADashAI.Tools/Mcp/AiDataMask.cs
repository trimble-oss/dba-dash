using Microsoft.Extensions.Configuration;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// What is held back from an AI model: the values parameters were compiled for and ran with, and the values passed
    /// to a procedure or sp_executesql in a slow or running query's text; the literals in
    /// statement text, or - <see cref="MetadataOnly"/> - statement text, plans and input buffers altogether, with logins
    /// and hosts - and application names - replaced by stable pseudonyms.  <see cref="AiDataMasker"/> applies it.
    ///
    /// The two are separate because they protect different things.  Parameter values are real values from a real
    /// workload.  Literals in statement text are often just the application's constants when it uses parameters, and
    /// worth keeping for the analysis - but where it doesn't, they are the data itself.
    /// </summary>
    public readonly record struct AiDataMask(bool Parameters, bool Literals, bool MetadataOnly = false)
    {
        public const string SettingName = "Mcp:DataDetail";

        /// <summary>Parameter values and literals masked: what is used when nothing is configured.</summary>
        public static readonly AiDataMask Default = new(true, true);

        /// <summary>Nothing masked.</summary>
        public static readonly AiDataMask None = new(false, false);

        public static readonly AiDataMask Metadata = new(true, true, true);

        /// <summary>The words accepted for the setting, in the form they are written on the command line, in config and in the repository.</summary>
        public static readonly string[] Words = ["full", "parameters", "literals", "parametersAndLiterals", "metadataOnly"];

        public bool IsNone => !Parameters && !Literals && !MetadataOnly;

        /// <summary>Whether parameter values are masked or withheld.</summary>
        public bool MasksParameters => Parameters || MetadataOnly;

        /// <summary>Whether literals in statement text are masked or withheld.</summary>
        public bool MasksLiterals => Literals || MetadataOnly;

        /// <summary>The setting's word, one of <see cref="Words"/>.</summary>
        public string Word => MetadataOnly ? "metadataOnly"
            : Parameters && Literals ? "parametersAndLiterals"
            : Parameters ? "parameters"
            : Literals ? "literals"
            : "full";

        public override string ToString() => Word;

        /// <summary>
        /// Masks everything either does: a request can ask for more masking than the setting, never less.  Used where
        /// the setting is an administrator's floor, as the built-in AI's is.
        /// </summary>
        public AiDataMask Union(AiDataMask other) =>
            new(Parameters || other.Parameters, Literals || other.Literals, MetadataOnly || other.MetadataOnly);

        /// <summary>
        /// One of <see cref="Words"/>, without regard to case.
        /// </summary>
        public static bool TryParse(string? word, out AiDataMask mask)
        {
            switch (word?.Trim().ToLowerInvariant())
            {
                case "full":
                case "none":
                    mask = None;
                    return true;
                case "parameters":
                    mask = new AiDataMask(true, false);
                    return true;
                case "literals":
                    mask = new AiDataMask(false, true);
                    return true;
                case "parametersandliterals":
                    mask = Default;
                    return true;
                case "metadataonly":
                    mask = Metadata;
                    return true;
                default:
                    mask = Default;
                    return false;
            }
        }

        /// <summary>
        /// The setting from configuration: <see cref="Default"/> when it is absent, and an error when it is set to a word
        /// that isn't one of <see cref="Words"/>, so a typo stops the service rather than unmasking it.
        /// </summary>
        public static AiDataMask FromConfiguration(IConfiguration? configuration, string key = SettingName)
        {
            var word = configuration?[key];
            if (string.IsNullOrWhiteSpace(word))
            {
                return Default;
            }

            return TryParse(word, out var mask)
                ? mask
                : throw new InvalidOperationException($"{key} must be one of: {string.Join(", ", Words)}.  It is '{word}'.");
        }
    }
}
