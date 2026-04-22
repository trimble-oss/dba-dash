using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashAI.Services
{
    public class SqlToolExecutor
    {
        private readonly string _connectionString;

        public SqlToolExecutor(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Repository")
                ?? throw new InvalidOperationException("Connection string 'Repository' is required.");
        }

        public async Task<List<Dictionary<string, object?>>> QueryAsync(string storedProcedure, int maxRows, CancellationToken cancellationToken)
        {
            return await QueryAsync(storedProcedure, maxRows, null, null, cancellationToken);
        }

        public async Task<List<Dictionary<string, object?>>> QueryAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
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
            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
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
            await using var cn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

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
