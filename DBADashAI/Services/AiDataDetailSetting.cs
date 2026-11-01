using DBADashAI.Mcp;

namespace DBADashAI.Services
{
    /// <summary>
    /// The least the built-in AI masks before anything goes to its provider.  The setting is kept in the repository
    /// (AI.DataDetail), where a db_owner changes it in the GUI, and this holds the last value read.
    ///
    /// It is a floor: a request can ask for more masking than this, never less, so turning masking off for a request
    /// needs the setting at full.  It starts at <see cref="AiDataMask.Default"/>, and a failed read keeps the last value,
    /// so an unreadable repository never unmasks the data.
    /// </summary>
    public sealed class AiDataDetailSetting
    {
        private AiDataMask _value = AiDataMask.Default;
        private readonly Lock _lock = new();

        public AiDataMask Value
        {
            get
            {
                lock (_lock) return _value;
            }
        }

        public void Set(AiDataMask value)
        {
            lock (_lock) _value = value;
        }

        /// <summary>
        /// What a request is masked with: what it asked for - the default when it asked for nothing or something unknown -
        /// and at least this setting.
        /// </summary>
        public AiDataMask For(string? requested) =>
            (AiDataMask.TryParse(requested, out var mask) ? mask : AiDataMask.Default).Union(Value);

        /// <summary>
        /// Why a payload the caller masked itself can't be sent, or null when it can: the caller says how it masked it, and
        /// that has to cover this setting.  The service can't mask a payload that has already been summarised into prose.
        /// </summary>
        public string? Refusal(string? applied)
        {
            var mask = AiDataMask.TryParse(applied, out var parsed) && !string.IsNullOrWhiteSpace(applied) ? parsed : AiDataMask.None;
            var floor = Value;
            return mask.Union(floor) == mask
                ? null
                : $"The AI service's data setting is '{floor.Word}', which this request doesn't meet ('{mask.Word}').  " +
                  $"Tick {(floor.MetadataOnly ? "Metadata only" : "the masking options")} and send it again, or ask a db_owner to change the built-in AI's setting in Options > Connect AI Tools.";
        }

        /// <summary>Reads a stored word, such as the one AI.DataDetail holds.  An unknown word keeps the current value.</summary>
        public bool TrySetFromWord(string? word)
        {
            if (!AiDataMask.TryParse(word, out var value))
            {
                return false;
            }

            Set(value);
            return true;
        }
    }
}
