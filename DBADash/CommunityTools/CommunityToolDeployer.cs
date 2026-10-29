using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    public enum CommunityToolDeployStatus
    {
        Deployed,
        Skipped,
        Failed
    }

    public sealed class CommunityToolDeployResult
    {
        public CommunityTool Tool { get; init; }

        /// <summary>Where the tool went.  By default the copy DBA Dash runs, so it can differ between tools.</summary>
        public string Database { get; init; }

        public CommunityToolDeployStatus Status { get; init; }
        public string Message { get; init; }

        /// <summary>Version of the script that was deployed, when known.</summary>
        public string Version { get; init; }

        public string Origin { get; init; }
    }

    public sealed class CommunityToolInstanceDeployResult
    {
        public string Instance { get; init; }
        public string Database { get; init; }

        /// <summary>Set when the instance couldn't be deployed to at all, e.g. connection failure.</summary>
        public string Error { get; init; }

        public List<CommunityToolDeployResult> Tools { get; init; } = new();
    }

    /// <summary>
    /// Deploys community tools to an instance.
    ///
    /// <para>Runs as whoever the connection string says - the person running the config tool or CLI for integrated
    /// security, or credentials they supply.  Never the DBA Dash service: creating procedures needs far more than the
    /// service account should have.</para>
    /// </summary>
    public sealed class CommunityToolDeployer
    {
        private readonly CommunityToolScriptSource source;

        public CommunityToolDeployer(CommunityToolScriptSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>Database to deploy to.  Null to update each tool where DBA Dash runs it from (the connection's database, then master), or the connection's database if it isn't installed.</summary>
        public string Database { get; init; }

        public int CommandTimeout { get; init; } = 300;

        /// <summary>
        /// The database the connection lands in by default - its Initial Catalog, or the login's default database.  That's
        /// where the service runs the tools, so it's where they belong.  (Every community tool is an sp_ procedure, so a
        /// copy in master is also found from any database, but a copy in the connection's database takes precedence.)
        /// Azure SQL Database has no cross-database lookup, so the tools can only go in the connected database.
        /// </summary>
        public static string GetTargetDatabase(ConnectionInfo info, string requested)
        {
            if (!info.IsAzureDB)
            {
                return string.IsNullOrWhiteSpace(requested) ? info.DatabaseName : requested;
            }
            if (info.IsAzureMasterDB)
            {
                throw new InvalidOperationException("Community tools can't be deployed to master on Azure SQL Database.  Connect to the user database instead.");
            }
            if (!string.IsNullOrWhiteSpace(requested) && !string.Equals(requested, info.DatabaseName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"On Azure SQL Database community tools can only be deployed to the connected database ({info.DatabaseName}).");
            }
            return info.DatabaseName;
        }

        /// <summary>
        /// The connection string with its credentials replaced by those in <paramref name="credentials"/>.  Lets one
        /// admin login be used across every instance - the stored credentials for SQL auth connections are usually the
        /// low privilege login DBA Dash monitors with.
        /// </summary>
        public static string WithCredentials(string connectionString, string credentials)
        {
            var source = new SqlConnectionStringBuilder(credentials);
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                IntegratedSecurity = source.IntegratedSecurity,
                Authentication = source.Authentication
            };
            builder.Remove("User ID");
            builder.Remove("Password");
            if (!string.IsNullOrEmpty(source.UserID))
            {
                builder.UserID = source.UserID;
            }
            if (!string.IsNullOrEmpty(source.Password))
            {
                builder.Password = source.Password;
            }
            return builder.ConnectionString;
        }

        public async Task<CommunityToolInstanceDeployResult> DeployAsync(string connectionString, IReadOnlyCollection<CommunityTool> tools, CancellationToken cancellationToken = default)
        {
            var instance = new SqlConnectionStringBuilder(connectionString).DataSource;
            ConnectionInfo info;
            string database;
            try
            {
                info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);
                database = GetTargetDatabase(info, Database);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to deploy community tools to {instance}", instance);
                return new CommunityToolInstanceDeployResult { Instance = instance, Error = ex.Message };
            }

            var result = new CommunityToolInstanceDeployResult { Instance = info.ServerName is { Length: > 0 } serverName ? serverName : instance, Database = database };
            await using var cn = new SqlConnection(connectionString);
            try
            {
                await cn.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to connect to {instance}", instance);
                return new CommunityToolInstanceDeployResult { Instance = result.Instance, Database = database, Error = ex.Message };
            }
            cn.InfoMessage += (_, e) => Log.Debug("{instance}: {message}", result.Instance, e.Message);

            // By default each tool updates the copy DBA Dash runs (the connection's database, then master), so an
            // existing install in master isn't joined by a second copy in the user database that takes over from it.
            var lookupOrder = CommunityToolLocator.LookupOrder(info, database);
            Dictionary<string, List<string>> installed;
            try
            {
                installed = string.IsNullOrWhiteSpace(Database)
                    ? await CommunityToolLocator.GetInstalledAsync(cn, tools, lookupOrder, cancellationToken)
                    : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Unable to check where community tools are installed on {instance}", instance);
                return new CommunityToolInstanceDeployResult { Instance = result.Instance, Database = database, Error = ex.Message };
            }

            foreach (var tool in tools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var toolDatabase = installed == null ? database : CommunityToolLocator.Locate(null, lookupOrder, installed[tool.Name]).Database;
                result.Tools.Add(await DeployToolAsync(cn, info, toolDatabase, tool, result.Instance, cancellationToken));
            }
            return result;
        }

        private async Task<CommunityToolDeployResult> DeployToolAsync(SqlConnection cn, ConnectionInfo info, string database, CommunityTool tool, string instance, CancellationToken cancellationToken)
        {
            var file = tool.GetFile(info, out var notSupportedReason);
            if (file == null)
            {
                Log.Information("Skipping {tool} on {instance}: {reason}", tool.Name, instance, notSupportedReason);
                return new CommunityToolDeployResult { Tool = tool, Status = CommunityToolDeployStatus.Skipped, Message = notSupportedReason, Database = database };
            }

            CommunityToolScript script = null;
            try
            {
                script = await source.GetScriptAsync(tool, file, cancellationToken);
                // Reset each time - a script could leave the connection in another database.
                cn.ChangeDatabase(database);
                foreach (var batch in SqlBatchSplitter.Split(script.Text))
                {
                    if (SqlBatchSplitter.IsUseOnlyBatch(batch))
                    {
                        Log.Debug("Skipping USE batch in {tool}: {batch}", tool.Name, batch.Trim());
                        continue;
                    }
                    await using var cmd = new SqlCommand(batch, cn) { CommandTimeout = CommandTimeout };
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
                var version = script.MatchesPin ? file.Version : CommunityToolVersionChecker.ParseVersion(tool.Family, script.Text).version;
                Log.Information("Deployed {tool} {version} to {instance}.{database}", tool.Name, version, instance, database);
                return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Deployed, Version = version, Origin = script.Origin, Message = script.MatchesPin ? null : "Not the version tested with DBA Dash" };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Error deploying {tool} to {instance}.{database}", tool.Name, instance, database);
                return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed, Message = ex.Message, Origin = script?.Origin };
            }
        }
    }
}
