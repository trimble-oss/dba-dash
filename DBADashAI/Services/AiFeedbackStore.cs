using DBADashAI.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DBADashAI.Services
{
    public class AiFeedbackStore
    {
        private readonly ConcurrentQueue<AiFeedbackRecord> _records = new();
        private readonly string? _filePath;
        private readonly ILogger<AiFeedbackStore> _logger;
        // Separate counter so the size check in Add() is O(1).
        // ConcurrentQueue.Count enumerates the entire queue on every call.
        private int _count;
        private const int MaxRecords = 10000;
        private readonly SemaphoreSlim _fileWriteSemaphore = new(1, 1);

        // Number of recent feedback records per tool considered when computing a penalty.
        private const int PenaltyWindowSize = 20;
        // Unhelpful rate above this threshold causes a routing penalty.
        private const double PenaltyThreshold = 0.33;

        public AiFeedbackStore(IConfiguration configuration, ILogger<AiFeedbackStore> logger)
        {
            _filePath = configuration["AI:FeedbackStorePath"];
            _logger = logger;
            ReplayFromFile();
        }

        /// <summary>
        /// Replays persisted feedback records from the JSONL file into the in-memory queue
        /// so routing penalties survive service restarts. Only the most recent
        /// <see cref="MaxRecords"/> lines are loaded to keep startup fast.
        /// </summary>
        private void ReplayFromFile()
        {
            if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath)) return;

            try
            {
                // Read only the tail of the file — we only need the recent window for penalties.
                var lines = File.ReadLines(_filePath)
                    .TakeLast(MaxRecords)
                    .ToList();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var record = JsonSerializer.Deserialize<AiFeedbackRecord>(line);
                    if (record is null) continue;
                    _records.Enqueue(record);
                    Interlocked.Increment(ref _count);
                }

                if (_count > 0)
                    _logger.LogInformation("Replayed {Count} feedback records from {FilePath}", _count, _filePath);
            }
            catch (Exception ex)
            {
                // Non-fatal — service starts cleanly with an empty queue if the file is corrupt.
                _logger.LogWarning(ex, "Failed to replay feedback from {FilePath}. Starting with empty feedback store.", _filePath);
            }
        }

        public void Add(AiFeedbackRequest request)
        {
            var record = new AiFeedbackRecord
            {
                RequestId = request.RequestId,
                IsHelpful = request.IsHelpful,
                Category = request.Category?.Trim(),
                Comment = request.Comment?.Trim(),
                ToolName = request.ToolName?.Trim(),
                QuestionExcerpt = request.QuestionExcerpt is { Length: > 120 }
                    ? request.QuestionExcerpt[..120].Trim()
                    : request.QuestionExcerpt?.Trim(),
                CreatedUtc = DateTime.UtcNow
            };

            // Emit a structured warning for every thumbs-down so operators can diagnose
            // routing problems without having to parse the persisted feedback file.
            if (!record.IsHelpful)
            {
                _logger.LogWarning(
                    "AI feedback negative. RequestId={RequestId}, Tool={ToolName}, Category={Category}, QuestionExcerpt={QuestionExcerpt}, Comment={Comment}",
                    record.RequestId,
                    record.ToolName ?? "(unknown)",
                    record.Category ?? "(none)",
                    record.QuestionExcerpt ?? "(none)",
                    record.Comment ?? "(none)");
            }

            _records.Enqueue(record);
            Interlocked.Increment(ref _count);

            // Trim to the maximum bound.  Each successful dequeue decrements the counter.
            while (_count > MaxRecords && _records.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _count);
            }

            Persist(record);
        }

        public IReadOnlyCollection<AiFeedbackRecord> GetRecent(int max = 200)
        {
            return _records.Reverse().Take(Math.Max(1, max)).ToList();
        }

        /// <summary>
        /// Returns an integer score penalty (0–5) to subtract from a tool's routing score
        /// based on its recent unhelpful feedback rate. Only penalises when there are at
        /// least <see cref="PenaltyWindowSize"/> feedback records for the tool and the
        /// unhelpful rate exceeds <see cref="PenaltyThreshold"/>.
        ///
        /// Scale:
        ///   unhelpful rate ≥ 0.33 and &lt; 0.50  → penalty 1
        ///   unhelpful rate ≥ 0.50 and &lt; 0.67  → penalty 2
        ///   unhelpful rate ≥ 0.67               → penalty 3
        ///
        /// Penalty is intentionally small so that a strong keyword match still wins;
        /// it only matters when two tools have similar keyword scores.
        /// </summary>
        public int GetToolPenalty(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName)) return 0;

            var toolRecords = _records
                .Where(r => string.Equals(r.ToolName, toolName, StringComparison.OrdinalIgnoreCase))
                .TakeLast(PenaltyWindowSize)
                .ToList();

            if (toolRecords.Count < PenaltyWindowSize) return 0;

            var unhelpfulRate = toolRecords.Count(r => !r.IsHelpful) / (double)toolRecords.Count;

            if (unhelpfulRate >= 0.67) return 3;
            if (unhelpfulRate >= 0.50) return 2;
            if (unhelpfulRate >= PenaltyThreshold) return 1;
            return 0;
        }

        private void Persist(AiFeedbackRecord record)
        {
            if (string.IsNullOrWhiteSpace(_filePath)) return;

            // Serialize on the calling thread (cheap, CPU-only), then write asynchronously
            // so the request thread is not blocked on disk I/O. The semaphore ensures
            // concurrent feedback submissions never interleave writes.
            var json = JsonSerializer.Serialize(record) + Environment.NewLine;
            var path = _filePath;

            _ = Task.Run(async () =>
            {
                await _fileWriteSemaphore.WaitAsync();
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    await File.AppendAllTextAsync(path, json);
                }
                catch (Exception ex)
                {
                    // Persistence is best-effort — never let a file I/O failure block the feedback flow.
                    _logger.LogWarning(ex, "Failed to persist feedback record to {FilePath}", path);
                }
                finally
                {
                    _fileWriteSemaphore.Release();
                }
            });
        }
    }

    public class AiFeedbackRecord
    {
        public string RequestId { get; set; } = string.Empty;

        public bool IsHelpful { get; set; }

        public string? Category { get; set; }

        public string? Comment { get; set; }

        public string? ToolName { get; set; }

        public string? QuestionExcerpt { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
