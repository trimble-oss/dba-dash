using System.Text.RegularExpressions;

namespace DBADashAI.Services
{
    /// <summary>
    /// Scores how well a user question matches a tool's purpose using TF-IDF weighted
    /// cosine similarity over each tool's name, description, input hint, and keywords.
    ///
    /// Built as a singleton at startup: corpus vectors are pre-computed once and cached.
    /// Query-time scoring is sub-millisecond (pure arithmetic, no I/O, no allocations beyond
    /// the query token list).
    ///
    /// Used by <see cref="AiIntentRouter"/> for two purposes:
    ///   1. Fallback ranking when keyword scoring yields zero matches for all tools.
    ///   2. Detecting ambiguous broad-intent questions (top tools score closely together).
    /// </summary>
    public class ToolSimilarityScorer
    {
        // Threshold below which a similarity score is considered negligible.
        public const double MinMeaningfulScore = 0.05;

        // If the similarity gap between rank-1 and rank-3 is below this value the
        // question is considered broadly-intentioned and multi-tool routing is triggered.
        public const double BroadTriageGap = 0.12;

        // Minimum similarity for each of the top 3 tools to qualify for broad-triage.
        public const double BroadTriageMinScore = 0.10;

        private readonly Dictionary<string, int> _termIndex;
        private readonly double[] _idfWeights;
        private readonly Dictionary<string, double[]> _toolVectors;

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "shall", "should",
            "may", "might", "must", "can", "could", "of", "in", "on", "at", "by", "for",
            "with", "about", "to", "from", "and", "or", "not", "no", "any", "all",
            "use", "used", "using", "get", "show", "list", "give", "tell", "find",
            "questions", "question", "information", "data", "summary", "summarize",
            "i", "me", "my", "we", "our", "you", "your", "it", "its", "this", "that",
            "what", "which", "who", "how", "when", "where", "why"
        };

        public ToolSimilarityScorer(IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var tools = scope.ServiceProvider.GetRequiredService<IEnumerable<IAiTool>>();
            var toolList = tools.ToList();

            // Build per-tool token lists (with duplicates for TF computation).
            var tokenizedCorpora = toolList.ToDictionary(
                t => t.Name,
                t => Tokenize(BuildCorpus(t)));

            // Global vocabulary.
            var vocabulary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tokens in tokenizedCorpora.Values)
                foreach (var token in tokens)
                    vocabulary.Add(token);

            _termIndex = vocabulary
                .Select((term, idx) => (term, idx))
                .ToDictionary(x => x.term, x => x.idx, StringComparer.OrdinalIgnoreCase);

            // Smoothed IDF: log((N+1) / (df+1)) + 1 so rare terms score higher
            // and terms that appear in every tool are down-weighted.
            var n = toolList.Count;
            _idfWeights = new double[_termIndex.Count];
            foreach (var (term, idx) in _termIndex)
            {
                var df = tokenizedCorpora.Values.Count(tokens => tokens.Contains(term));
                _idfWeights[idx] = Math.Log((n + 1.0) / (df + 1.0)) + 1.0;
            }

            // Build and normalise a unit-length TF-IDF vector for each tool.
            _toolVectors = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var (toolName, tokens) in tokenizedCorpora)
            {
                var vec = BuildTfIdfVector(tokens);
                Normalize(vec);
                _toolVectors[toolName] = vec;
            }
        }

        /// <summary>
        /// Returns a cosine similarity score in [0, 1] between the question and the tool's corpus.
        /// </summary>
        public double Score(string question, IAiTool tool)
        {
            if (!_toolVectors.TryGetValue(tool.Name, out var toolVec)) return 0;
            var queryTokens = Tokenize(question);
            if (queryTokens.Count == 0) return 0;
            var queryVec = BuildTfIdfVector(queryTokens);
            Normalize(queryVec);
            return CosineSimilarity(queryVec, toolVec);
        }

        /// <summary>
        /// Returns the best-matching tools sorted by similarity score descending,
        /// filtered to those with a score above <see cref="MinMeaningfulScore"/>.
        /// </summary>
        public IReadOnlyList<(IAiTool Tool, double Similarity)> GetTopMatches(
            string question, IEnumerable<IAiTool> tools, int topN = 3)
        {
            return tools
                .Select(t => (Tool: t, Similarity: Score(question, t)))
                .Where(x => x.Similarity >= MinMeaningfulScore)
                .OrderByDescending(x => x.Similarity)
                .ThenBy(x => x.Tool.Name)
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// Returns true when the top similarity matches are so close together that no single
        /// tool clearly dominates — indicating the question has broad or ambiguous intent and
        /// multi-tool routing would produce a better answer.
        /// Requires at least 3 tools above <see cref="BroadTriageMinScore"/> with a
        /// score gap between rank-1 and rank-3 of less than <see cref="BroadTriageGap"/>.
        /// </summary>
        public bool HasBroadIntent(IReadOnlyList<(IAiTool Tool, double Similarity)> topMatches)
        {
            if (topMatches.Count < 3) return false;
            if (topMatches[0].Similarity < BroadTriageMinScore) return false;
            if (topMatches[2].Similarity < BroadTriageMinScore) return false;
            return (topMatches[0].Similarity - topMatches[2].Similarity) < BroadTriageGap;
        }

        // ── Internals ────────────────────────────────────────────────────────────

        private double[] BuildTfIdfVector(List<string> tokens)
        {
            var vec = new double[_termIndex.Count];
            var termFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
                termFreq[token] = termFreq.GetValueOrDefault(token) + 1;

            foreach (var (term, freq) in termFreq)
            {
                if (!_termIndex.TryGetValue(term, out var idx)) continue;
                var tf = (double)freq / tokens.Count;
                vec[idx] = tf * _idfWeights[idx];
            }
            return vec;
        }

        private static void Normalize(double[] vec)
        {
            var magnitude = Math.Sqrt(vec.Sum(v => v * v));
            if (magnitude == 0) return;
            for (var i = 0; i < vec.Length; i++)
                vec[i] /= magnitude;
        }

        private static double CosineSimilarity(double[] a, double[] b)
        {
            var dot = 0.0;
            for (var i = 0; i < a.Length; i++)
                dot += a[i] * b[i];
            return Math.Clamp(dot, 0.0, 1.0);
        }

        /// <summary>
        /// Builds the TF-IDF corpus document for a tool. Keywords are repeated twice
        /// to give them higher term-frequency weight relative to prose description words.
        /// </summary>
        private static string BuildCorpus(IAiTool tool)
        {
            var keywords = string.Join(" ", tool.Keywords);
            return $"{tool.Name} {tool.Description} {tool.InputHint} {keywords} {keywords}";
        }

        private static readonly Regex NonAlphanumericRegex = new(@"[^a-zA-Z0-9]+", RegexOptions.Compiled);

        /// <summary>
        /// Splits text into lower-cased tokens, removing stop words and short tokens.
        /// Returns a list (with duplicates) so TF can be computed from term frequency.
        /// </summary>
        private static List<string> Tokenize(string text) =>
            NonAlphanumericRegex.Split(text.ToLowerInvariant())
                .Where(t => t.Length >= 3 && !StopWords.Contains(t))
                .ToList();
    }
}
