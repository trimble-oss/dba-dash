using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashAI.Services;

public sealed class SqlToolExecutor(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("Repository")
        ?? throw new InvalidOperationException("Connection string 'Repository' is required.");

    public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, int maxRows, CancellationToken cancellationToken)
    {
        await using var cn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 30
        };

        await cn.OpenAsync(cancellationToken);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken) && rows.Count < Math.Max(1, maxRows))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = await reader.IsDBNullAsync(i, cancellationToken)
                    ? null
                    : reader.GetValue(i);

                row[reader.GetName(i)] = value;
            }

            rows.Add(row);
        }

        return rows;
    }
}
