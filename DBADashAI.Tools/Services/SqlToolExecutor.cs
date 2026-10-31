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

        public Task<List<Dictionary<string, object?>>> QueryAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, CancellationToken cancellationToken) =>
            QueryAsync(storedProcedure, maxRows, instanceFilter, hoursBack, null, cancellationToken);

        /// <param name="toUtc">When the window ends, passed as @ToDate - only to a procedure that takes it.  Null leaves it out.</param>
        public async Task<List<Dictionary<string, object?>>> QueryAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, DateTime? toUtc, CancellationToken cancellationToken)
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
            if (toUtc.HasValue)
                cmd.Parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toUtc.Value;

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            return await ReadResultSetAsync(reader, cancellationToken);
        }

        public async Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(string storedProcedure, int maxRows, CancellationToken cancellationToken)
        {
            return await QueryMultiAsync(storedProcedure, maxRows, null, null, cancellationToken);
        }

        public Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, CancellationToken cancellationToken) =>
            QueryMultiAsync(storedProcedure, maxRows, instanceFilter, hoursBack, null, cancellationToken);

        /// <param name="toUtc">When the window ends, passed as @ToDate - only to a procedure that takes it.  Null leaves it out.</param>
        public async Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(string storedProcedure, int maxRows, string? instanceFilter, int? hoursBack, DateTime? toUtc, CancellationToken cancellationToken)
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
            if (toUtc.HasValue)
                cmd.Parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toUtc.Value;

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

        /// <summary>Whether a repository connection is configured - in local mode there is none.</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

        /// <summary>Every result set of a procedure that takes its own parameters rather than the tool convention.</summary>
        public async Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(string storedProcedure, IEnumerable<(string Name, object? Value)> parameters, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(RequiredConnectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

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

        /// <summary>The repository connection, for callers that read through code shared with the GUI.</summary>
        public string ConnectionString => RequiredConnectionString;

        public int CommandTimeoutSeconds => _sqlTimeoutSeconds;

        /// <summary>A procedure with parameters of its own, rather than the tools' MaxRows/InstanceFilter/HoursBack.</summary>
        public async Task<List<Dictionary<string, object?>>> QueryAsync(
            string storedProcedure,
            IEnumerable<SqlParameter> parameters,
            CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(RequiredConnectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };
            cmd.Parameters.AddRange(parameters.ToArray());

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            return await ReadResultSetAsync(reader, cancellationToken);
        }

        /// <summary>Every result set of a procedure with parameters of its own.</summary>
        public async Task<List<List<Dictionary<string, object?>>>> QueryMultiAsync(
            string storedProcedure,
            IEnumerable<SqlParameter> parameters,
            CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(RequiredConnectionString);
            await using var cmd = new SqlCommand(storedProcedure, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };
            cmd.Parameters.AddRange(parameters.ToArray());

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
