using DBADashAI.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DBADashAI.Services;

public sealed class AiFeedbackStore(IConfiguration configuration)
{
    private static readonly ConcurrentQueue<AiFeedbackRecord> Records = new();
    private readonly string? _filePath = configuration["AI:FeedbackStorePath"];

    public void Add(AiFeedbackRequest request)
    {
        var record = new AiFeedbackRecord
        {
            RequestId = request.RequestId,
            IsHelpful = request.IsHelpful,
            Category = request.Category?.Trim(),
            Comment = request.Comment?.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        Records.Enqueue(record);

        while (Records.Count > 10000 && Records.TryDequeue(out _))
        {
            // keep bounded in-memory store
        }

        Persist(record);
    }

    public IReadOnlyCollection<AiFeedbackRecord> GetRecent(int max = 200)
    {
        return Records.Reverse().Take(Math.Max(1, max)).ToList();
    }

    private void Persist(AiFeedbackRecord record)
    {
        if (string.IsNullOrWhiteSpace(_filePath)) return;

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(record);
            File.AppendAllText(_filePath, json + Environment.NewLine);
        }
        catch
        {
            // Keep feedback flow non-blocking even if persistence fails.
        }
    }
}

public sealed class AiFeedbackRecord
{
    public string RequestId { get; set; } = string.Empty;

    public bool IsHelpful { get; set; }

    public string? Category { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedUtc { get; set; }
}
