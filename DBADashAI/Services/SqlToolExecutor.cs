using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashAI.Services
{
    public class SqlToolExecutor
    {
        private readonly string? _connectionString;
        private readonly int _sqlTimeoutSeconds;

        public SqlToolExecutor(IConfiguration configuration)
        {
            // Store null if not configured; individual query methods will throw with a clear
            // message at call time. This allows the service to start in local mode (no repository
            // connection required) without failing DI registration for every scoped request.
            _connectionString = configuration.GetConnectionString("Repository");
            _sqlTimeoutSeconds = configuration.GetValue<int?>("AI:SqlTimeoutSeconds") ?? 30;
        }

        private string RequiredConnectionString => _connectionString
            ?? throw new InvalidOperationException(
                "Repository connection string is not configured. " +
                "Set ConnectionStrings:Repository in appsettings.json or via an environment variable.");

        public async Task<List<Dictionary<string, object?>>> QueryAsync(string storedProcedure, int maxRows, CancellationToken cancellationToken)
        {
            return await QueryAsync(storedProcedure, maxRows, null, null, cancellationToken);
        }

        public async Task<List<Dictionary<string, object?>>> QueryAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(RequiredConnectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };
            cmd.Parameters.AddWithValue("@MaxRows", maxRows);
            if (!string.IsNullOrWhiteSpace(instanceFilter))
                cmd.Parameters.AddWithValue("@InstanceFilter", instanceFilter);
            if (hoursBack.HasValue)
                cmd.Parameters.AddWithValue("@HoursBack", hoursBack.Value);

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            return await ReadResultSetAsync(reader, cancellationToken);
        }

        public async Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(string storedProcedure, int maxRows, CancellationToken cancellationToken)
        {
            return await QueryMultiAsync(storedProcedure, maxRows, null, null, cancellationToken);
        }

        public async Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(RequiredConnectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };
            cmd.Parameters.AddWithValue("@MaxRows", maxRows);
            if (!string.IsNullOrWhiteSpace(instanceFilter))
                cmd.Parameters.AddWithValue("@InstanceFilter", instanceFilter);
            if (hoursBack.HasValue)
                cmd.Parameters.AddWithValue("@HoursBack", hoursBack.Value);

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var resultSets = new List<List<Dictionary<string, object?>>>();

            do
            {
                resultSets.Add(await ReadResultSetAsync(reader, cancellationToken));
            }
            while (await reader.NextResultAsync(cancellationToken));

            return resultSets;
        }

        public async Task<List<Dictionary<string, object?>>> QueryNoParamsAsync(string storedProcedure, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(RequiredConnectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            return await ReadResultSetAsync(reader, cancellationToken);
        }

        private static async Task<List<Dictionary<string, object?>>> ReadResultSetAsync(SqlDataReader reader, CancellationToken cancellationToken)
        {
            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
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
}
