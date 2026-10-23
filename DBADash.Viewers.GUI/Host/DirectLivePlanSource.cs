using DBADash;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Follows a running request over a connection straight to the instance, for a live plan opened from SSMS
    /// (<see cref="LivePlanRequestFile"/>) - the viewers have no DBA Dash service to ask.  Runs the same SQL the service
    /// runs for the DBA Dash GUI (<see cref="LiveQuerySql"/>), so it needs VIEW SERVER STATE (or VIEW SERVER PERFORMANCE
    /// STATE) and lightweight query profiling the same way.
    /// </summary>
    public sealed class DirectLivePlanSource : LivePlanFollower
    {
        private const string ApplicationName = "DBA Dash Live Plan";

        /// <summary>
        /// Finds the session's request: the one that started at the time SSMS showed, when it showed one, or the busiest
        /// one the session is running.  Its start time is returned in UTC, with the offset used to get there, which is
        /// what LiveQuerySql matches the request on.  The grid's value can have lost precision on the way, so the start time
        /// is matched to within a few ms - a session runs one request at a time apart from MARS.
        /// </summary>
        private const string ResolveRequestSql = """
            SET NOCOUNT ON;
            DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);
            SELECT TOP(1) DATEADD(mi,@UTCOffset,r.start_time) AS start_time_utc,
                    @UTCOffset AS utc_offset
            FROM sys.dm_exec_requests r
            WHERE r.session_id = @SessionID
            AND (@StartTime IS NULL OR r.start_time BETWEEN DATEADD(ms,-3,@StartTime) AND DATEADD(ms,3,@StartTime))
            ORDER BY r.cpu_time DESC, r.request_id;
            """;

        private readonly Connection _connection;
        private readonly DateTime _startTimeUtc;
        private readonly int _utcOffset;

        private DirectLivePlanSource(Connection connection, int sessionId, DateTime startTimeUtc, int utcOffset, DataRow initial)
            : base(sessionId, initial)
        {
            _connection = connection;
            _startTimeUtc = startTimeUtc;
            _utcOffset = utcOffset;
        }

        public override string Description => $"Session {SessionId} on {_connection.Server}";

        public override string RequestKey => RequestKeyOf("direct:" + _connection.Server, _startTimeUtc);

        protected override DateTime ToDisplayTime(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

        protected override Task<DataSet> ReadProfileAsync(CancellationToken cancellationToken) =>
            ReadAsync(_connection, LiveQuerySql.LiveQueryProfile, SessionId, _startTimeUtc, _utcOffset, ["Request", "Profiles"], 30, cancellationToken);

        protected override Task<DataSet> ReadPlanAsync(CancellationToken cancellationToken) =>
            ReadAsync(_connection, LiveQuerySql.LivePlan, SessionId, _startTimeUtc, _utcOffset, ["LivePlan"], 60, cancellationToken);

        /// <summary>
        /// The live plan <paramref name="request"/> asks for, and a source to follow it with.  Throws, with a message for
        /// the user, when the connection isn't one this supports, the session isn't running the request, or it has no plan
        /// to show.
        /// </summary>
        public static async Task<(string PlanXml, DirectLivePlanSource Source, DateTime Captured)> OpenAsync(LivePlanRequestFile request, CancellationToken cancellationToken)
        {
            var connection = Connection.From(request);

            DateTime startTimeUtc;
            int utcOffset;
            await using (var cn = connection.Open())
            await using (var cmd = new SqlCommand(ResolveRequestSql, cn) { CommandTimeout = 30 })
            {
                cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = request.SessionId;
                cmd.Parameters.Add("@StartTime", SqlDbType.DateTime2).Value = (object)request.StartTime ?? DBNull.Value;
                await cn.OpenAsync(cancellationToken);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(request.StartTime is { } started
                        ? $"Session {request.SessionId} is no longer running the request that started at {started:yyyy-MM-dd HH:mm:ss.fff}.  The query may have completed."
                        : $"Session {request.SessionId} isn't running a request.");
                }

                startTimeUtc = reader.GetDateTime(0);
                utcOffset = reader.GetInt32(1);
            }

            var data = await ReadAsync(connection, LiveQuerySql.LivePlan, request.SessionId, startTimeUtc, utcOffset, ["LivePlan"], 60, cancellationToken);
            var row = data.Tables["LivePlan"]?.Rows.Cast<DataRow>().FirstOrDefault()
                      ?? throw new InvalidOperationException("The live plan was not found.  The query may have completed.");
            if (row["query_plan"] == DBNull.Value)
            {
                throw new InvalidOperationException(row["unavailable_reason"] as string ?? "A live plan is not available for this request.");
            }

            var source = new DirectLivePlanSource(connection, request.SessionId, startTimeUtc, utcOffset, row);
            return (PlanOf(row), source, source.ToDisplayTime(Convert.ToDateTime(row["captured_utc"])));
        }

        private static async Task<DataSet> ReadAsync(Connection connection, string sql, int sessionId, DateTime startTimeUtc, int utcOffset,
            string[] tableNames, int timeout, CancellationToken cancellationToken)
        {
            await using var cn = connection.Open();
            await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = timeout };
            cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = sessionId;
            cmd.Parameters.Add("@ExpectedStartUtc", SqlDbType.DateTime).Value = startTimeUtc;
            cmd.Parameters.Add("@SnapshotUtcOffset", SqlDbType.Int).Value = utcOffset;

            await cn.OpenAsync(cancellationToken);
            var ds = new DataSet();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            foreach (var name in tableNames)
            {
                if (reader.IsClosed) break;
                var table = new DataTable(name);
                table.Load(reader);
                ds.Tables.Add(table);
            }

            return ds;
        }

        /// <summary>
        /// The connection to make, from the one the SSMS query window was using.  Only the settings that say where to
        /// connect and how to secure it are taken from the request - not whatever else a connection string can carry, such
        /// as a file to attach - and only Windows or SQL Server authentication.  A SQL login's password is kept as a
        /// <see cref="SqlCredential"/>, never in the connection string, for as long as the plan is followed.
        /// </summary>
        public sealed class Connection
        {
            private readonly string _connectionString;
            private readonly SqlCredential _credential;

            private Connection(string connectionString, SqlCredential credential, string server)
            {
                _connectionString = connectionString;
                _credential = credential;
                Server = server;
            }

            public string Server { get; }

            /// <summary>The connection string to connect with: no password, and nothing from the request but the settings it may set.</summary>
            public string ConnectionString => _connectionString;

            public bool HasCredential => _credential != null;

            public SqlConnection Open() => new(_connectionString, _credential);

            public static Connection From(LivePlanRequestFile request)
            {
                SqlConnectionStringBuilder given;
                try
                {
                    given = new SqlConnectionStringBuilder(request.ConnectionString);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException("The live plan request's connection isn't valid: " + ex.Message, ex);
                }

                if (string.IsNullOrWhiteSpace(given.DataSource))
                {
                    throw new InvalidOperationException("The live plan request doesn't say which instance to connect to.");
                }

                if (given.Authentication is not (SqlAuthenticationMethod.NotSpecified or SqlAuthenticationMethod.SqlPassword))
                {
                    throw new InvalidOperationException("Live plans from SSMS support Windows and SQL Server authentication only.");
                }

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = given.DataSource,
                    InitialCatalog = given.InitialCatalog,
                    Encrypt = given.Encrypt,
                    TrustServerCertificate = given.TrustServerCertificate,
                    HostNameInCertificate = given.HostNameInCertificate,
                    ServerCertificate = given.ServerCertificate,
                    ServerSPN = given.ServerSPN,
                    MultiSubnetFailover = given.MultiSubnetFailover,
                    // A read-only intent routes an availability group listener to the readable secondary the grid came from.
                    ApplicationIntent = given.ApplicationIntent,
                    ConnectTimeout = given.ConnectTimeout,
                    ApplicationName = ApplicationName
                };

                SqlCredential credential = null;
                if (request.Password == null)
                {
                    if (!given.IntegratedSecurity)
                    {
                        throw new InvalidOperationException("The live plan request has neither Windows authentication nor a SQL login's password.");
                    }

                    builder.IntegratedSecurity = true;
                }
                else
                {
                    if (given.IntegratedSecurity || string.IsNullOrEmpty(given.UserID))
                    {
                        throw new InvalidOperationException("The live plan request's password isn't for a SQL login.");
                    }

                    credential = new SqlCredential(given.UserID, request.Password);
                }

                return new Connection(builder.ConnectionString, credential, given.DataSource);
            }
        }
    }
}
