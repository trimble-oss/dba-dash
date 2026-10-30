using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    public enum CommunityToolDeployStatus
    {
        Deployed,

        /// <summary>The installed procedure is already exactly what would be deployed, so it was left as it is.</summary>
        UpToDate,
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

        /// <summary>An object with the tool's name exists but isn't the tool, so it was left alone.</summary>
        public bool IsConflict { get; init; }
    }

    public sealed class CommunityToolInstanceDeployResult
    {
        public string Instance { get; init; }
        public string Database { get; init; }

        /// <summary>Set when the instance couldn't be deployed to at all, e.g. connection failure.</summary>
        public string Error { get; init; }

        public List<CommunityToolDeployResult> Tools { get; init; } = new();

        /// <summary>Script that puts back what the deployment replaced, if anything was deployed.</summary>
        public string RollbackScript { get; set; }
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
        /// Replace a GuardCustomizations tool even if it doesn't match a known release, or the update would break an Agent
        /// job.  Never replaces an object that isn't the tool (a conflict).
        /// </summary>
        public bool OverrideSafetyChecks { get; init; }

        /// <summary>Where rollback scripts are saved.  Null for <see cref="CommunityToolRollbackScript.DefaultFolder"/>.</summary>
        public string RollbackFolder { get; init; }

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
            tools = CommunityTool.WithRequired(tools);
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
                    ? await CommunityToolLocator.GetInstalledAsync(cn, tools.Where(t => !t.Family.ExactDatabase).ToList(), lookupOrder, cancellationToken)
                    : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Unable to check where community tools are installed on {instance}", instance);
                return new CommunityToolInstanceDeployResult { Instance = result.Instance, Database = database, Error = ex.Message };
            }

            // Guarded families are updated as a set: the procedures call each other, so updating one while another installed
            // alongside it stays old can break it (e.g. a new CommandExecute parameter the old DatabaseBackup doesn't pass).
            // Every installed member is included, and if any can't be updated safely, none are.
            var guardedFailures = new Dictionary<CommunityTool, CommunityToolDeployResult>();
            var blockedFamilies = new Dictionary<CommunityToolFamily, List<string>>();
            var setupErrors = new Dictionary<(CommunityToolFamily, string), string>();
            var checks = new Dictionary<CommunityTool, PreCheck>();
            var rollback = new Lazy<CommunityToolRollbackScript>(() => new CommunityToolRollbackScript(RollbackFolder ?? CommunityToolRollbackScript.DefaultFolder(), result.Instance));
            try
            {
                var guardedFamilies = tools.Select(t => t.Family).Where(f => f.GuardCustomizations && f.ExactDatabase).Distinct().ToList();
                if (guardedFamilies.Count > 0)
                {
                    var familyTools = guardedFamilies.SelectMany(f => f.Tools).ToList();
                    var installedHere = await CommunityToolLocator.GetInstalledAsync(cn, familyTools, new[] { database }, cancellationToken);
                    tools = CommunityTool.WithRequired(tools.Concat(familyTools.Where(t => installedHere[t.Name].Count > 0)).Distinct());
                    foreach (var tool in tools.Where(t => guardedFamilies.Contains(t.Family)))
                    {
                        var failure = await DeployToolAsync(cn, info, database, tool, result.Instance, setupErrors, rollback, checks, cancellationToken, checkOnly: true);
                        // A conflict is left alone without stopping the rest - unless another tool being deployed calls it.
                        if (failure != null && (!failure.IsConflict || tools.Any(t => t.Requires.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))))
                        {
                            guardedFailures[tool] = failure;
                            BlockFamily(blockedFamilies, tool);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Unable to check the installed community tools on {instance}", instance);
                return new CommunityToolInstanceDeployResult { Instance = result.Instance, Database = database, Error = ex.Message };
            }

            foreach (var tool in tools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (guardedFailures.TryGetValue(tool, out var failure))
                {
                    result.Tools.Add(failure);
                    continue;
                }
                if (blockedFamilies.TryGetValue(tool.Family, out var blockedNames))
                {
                    result.Tools.Add(new CommunityToolDeployResult
                    {
                        Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed,
                        Message = $"Not deployed: {string.Join(", ", blockedNames)} couldn't be updated, and {tool.Family.DisplayName} is updated as a set."
                    });
                    continue;
                }
                var toolDatabase = installed == null || tool.Family.ExactDatabase ? database : CommunityToolLocator.Locate(null, lookupOrder, installed[tool.Name]).Database;
                var toolResult = await DeployToolAsync(cn, info, toolDatabase, tool, result.Instance, setupErrors, rollback, checks, cancellationToken);
                result.Tools.Add(toolResult);
                if (toolResult.Status == CommunityToolDeployStatus.Failed && tool.Family.GuardCustomizations && !toolResult.IsConflict)
                {
                    // Stop the rest of the set.  Anything already replaced can be put back with the rollback script.
                    BlockFamily(blockedFamilies, tool);
                }
            }
            if (rollback.IsValueCreated && System.IO.File.Exists(rollback.Value.Path))
            {
                result.RollbackScript = rollback.Value.Path;
                Log.Information("Rollback script for {instance}: {path}", result.Instance, result.RollbackScript);
            }
            return result;
        }

        private async Task<CommunityToolDeployResult> DeployToolAsync(SqlConnection cn, ConnectionInfo info, string database, CommunityTool tool, string instance,
            Dictionary<(CommunityToolFamily, string), string> setupErrors, Lazy<CommunityToolRollbackScript> rollback, Dictionary<CommunityTool, PreCheck> checks,
            CancellationToken cancellationToken, bool checkOnly = false)
        {
            var file = tool.GetFile(info, out var notSupportedReason);
            if (file == null)
            {
                if (checkOnly) return null;
                Log.Information("Skipping {tool} on {instance}: {reason}", tool.Name, instance, notSupportedReason);
                return new CommunityToolDeployResult { Tool = tool, Status = CommunityToolDeployStatus.Skipped, Message = notSupportedReason, Database = database };
            }

            CommunityToolScript script = null;
            try
            {
                // Reset each time - a script could leave the connection in another database.
                cn.ChangeDatabase(database);
                // The guarded set's pre-check already read the object, fetched the script and checked it - reuse that.
                var check = checks.GetValueOrDefault(tool);
                var existing = check != null ? check.Existing : await GetExistingAsync(cn, tool, cancellationToken);
                if (GetConflict(tool, existing) is { } conflict)
                {
                    Log.Warning("Not deploying {tool} to {instance}.{database}: {reason}", tool.Name, instance, database, conflict);
                    return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed, Message = conflict, IsConflict = true };
                }

                script = check?.Script ?? await source.GetScriptAsync(tool, file, cancellationToken);
                if (IsAlreadyDeployed(tool, existing, script))
                {
                    if (checkOnly)
                    {
                        checks.TryAdd(tool, new PreCheck(existing, script, null, null));
                        return null;
                    }
                    // The procedure is fine, but the tables it logs to could still be missing
                    if (await GetSetupErrorAsync(cn, tool, instance, database, setupErrors, cancellationToken) is { } upToDateSetupError)
                    {
                        return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed, Message = upToDateSetupError };
                    }
                    var installedVersion = script.MatchesPin ? file.Version : CommunityToolVersionChecker.ParseVersion(tool.Family, script.Text).version;
                    Log.Information("{tool} {version} is already deployed to {instance}.{database}", tool.Name, installedVersion, instance, database);
                    return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.UpToDate, Version = installedVersion, Origin = script.Origin };
                }
                var warnings = new List<string>();
                if (!script.MatchesPin)
                {
                    warnings.Add("Not the version tested with DBA Dash.");
                }
                if (existing != null && tool.Family.GuardCustomizations)
                {
                    string block;
                    List<string> guardWarnings;
                    if (check?.Warnings != null)
                    {
                        (block, guardWarnings) = (check.Block, check.Warnings);
                    }
                    else
                    {
                        cn.ChangeDatabase(database);
                        (block, guardWarnings) = await CheckCustomizationsAsync(cn, tool, existing, script, database, cancellationToken);
                    }
                    if (checkOnly)
                    {
                        checks[tool] = new PreCheck(existing, script, block, guardWarnings);
                    }
                    warnings.AddRange(guardWarnings);
                    if (block != null)
                    {
                        if (!OverrideSafetyChecks)
                        {
                            Log.Warning("Not deploying {tool} to {instance}.{database}: {reason}", tool.Name, instance, database, block);
                            return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed, Message = block + "  Override the safety checks to replace it anyway.", Origin = script.Origin };
                        }
                        Log.Warning("Deploying {tool} to {instance}.{database} with safety checks overridden: {reason}", tool.Name, instance, database, block);
                        warnings.Add("Safety checks overridden: " + block);
                    }
                }
                if (checkOnly)
                {
                    checks.TryAdd(tool, new PreCheck(existing, script, null, null));
                    return null;
                }

                // Resolved before setup creates anything, so a missing rollback folder fails before any change is made.
                _ = rollback.Value;
                if (await GetSetupErrorAsync(cn, tool, instance, database, setupErrors, cancellationToken) is { } setupError)
                {
                    return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed, Message = setupError };
                }

                // Saved before anything is replaced - if it can't be written, the tool isn't deployed.
                rollback.Value.Add(instance, database, tool, existing is { Type: "P" }
                    ? new CommunityToolRollbackScript.ExistingProcedure(existing.Definition, existing.UsesAnsiNulls, existing.UsesQuotedIdentifier)
                    : null);

                cn.ChangeDatabase(database);
                await ExecuteScriptAsync(cn, script, tool.Name, cancellationToken);
                var version = script.MatchesPin ? file.Version : CommunityToolVersionChecker.ParseVersion(tool.Family, script.Text).version;
                Log.Information("Deployed {tool} {version} to {instance}.{database}", tool.Name, version, instance, database);
                return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Deployed, Version = version, Origin = script.Origin, Message = warnings.Count == 0 ? null : string.Join("  ", warnings) };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Error deploying {tool} to {instance}.{database}", tool.Name, instance, database);
                return new CommunityToolDeployResult { Tool = tool, Database = database, Status = CommunityToolDeployStatus.Failed, Message = ex.Message, Origin = script?.Origin };
            }
        }

        /// <summary>What the guarded set's pre-check found for a tool.  Warnings is null when the customization check didn't run.</summary>
        private sealed record PreCheck(ExistingObject Existing, CommunityToolScript Script, string Block, List<string> Warnings);

        /// <summary>Runs the family's setup once per database.  The error, or null.</summary>
        private async Task<string> GetSetupErrorAsync(SqlConnection cn, CommunityTool tool, string instance, string database,
            Dictionary<(CommunityToolFamily, string), string> setupErrors, CancellationToken cancellationToken)
        {
            var setupKey = (tool.Family, database.ToLowerInvariant());
            if (!setupErrors.TryGetValue(setupKey, out var setupError))
            {
                setupError = await RunSetupAsync(cn, tool, instance, database, cancellationToken);
                setupErrors[setupKey] = setupError;
            }
            return setupError;
        }

        private static void BlockFamily(Dictionary<CommunityToolFamily, List<string>> blockedFamilies, CommunityTool tool)
        {
            if (!blockedFamilies.TryGetValue(tool.Family, out var names))
            {
                blockedFamilies[tool.Family] = names = new List<string>();
            }
            names.Add(tool.Name);
        }

        private async Task ExecuteScriptAsync(SqlConnection cn, CommunityToolScript script, string name, CancellationToken cancellationToken)
        {
            foreach (var batch in SqlBatchSplitter.Split(script.Text))
            {
                if (SqlBatchSplitter.IsUseOnlyBatch(batch))
                {
                    Log.Debug("Skipping USE batch in {name}: {batch}", name, batch.Trim());
                    continue;
                }
                await using var cmd = new SqlCommand(batch, cn) { CommandTimeout = CommandTimeout };
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Whether the installed procedure is exactly what the script would create, so there's nothing to do.  Compares the
        /// normalized definitions rather than versions, so a customized copy with the same version is still treated as different.
        /// </summary>
        private static bool IsAlreadyDeployed(CommunityTool tool, ExistingObject existing, CommunityToolScript script)
        {
            if (existing is not { Type: "P" } || string.IsNullOrEmpty(existing.Definition))
            {
                return false;
            }
            var batch = CommunityToolSafety.FindProcedureBatch(script.Text, tool.Name);
            if (batch == null || CommunityToolSafety.HashDefinition(batch) != CommunityToolSafety.HashDefinition(existing.Definition))
            {
                return false;
            }
            // Same text but created with different SET options behaves differently, so it's redeployed.
            var (ansiNulls, quotedIdentifier) = CommunityToolSafety.GetSetOptions(script.Text, batch);
            return ansiNulls == existing.UsesAnsiNulls && quotedIdentifier == existing.UsesQuotedIdentifier;
        }

        private sealed record ExistingObject(string Type, string Definition, bool UsesAnsiNulls, bool UsesQuotedIdentifier, List<string> Parameters);

        /// <summary>The object that has the tool's name in the current database, or null.</summary>
        private static async Task<ExistingObject> GetExistingAsync(SqlConnection cn, CommunityTool tool, CancellationToken cancellationToken)
        {
            await using var cmd = new SqlCommand(@"SELECT O.type, M.definition, M.uses_ansi_nulls, M.uses_quoted_identifier
FROM sys.objects O
LEFT JOIN sys.sql_modules M ON M.object_id = O.object_id
WHERE O.schema_id = SCHEMA_ID(@Schema)
AND O.name = @Name;
SELECT P.name
FROM sys.parameters P
JOIN sys.objects O ON O.object_id = P.object_id
WHERE O.schema_id = SCHEMA_ID(@Schema)
AND O.name = @Name
AND P.name <> ''
ORDER BY P.parameter_id", cn);
            cmd.Parameters.AddWithValue("@Schema", CommunityTool.Schema);
            cmd.Parameters.AddWithValue("@Name", tool.Name);
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await rdr.ReadAsync(cancellationToken))
            {
                return null;
            }
            var type = rdr.GetString(0).Trim();
            var definition = rdr.IsDBNull(1) ? null : rdr.GetString(1);
            var ansiNulls = !rdr.IsDBNull(2) && rdr.GetBoolean(2);
            var quotedIdentifier = !rdr.IsDBNull(3) && rdr.GetBoolean(3);
            await rdr.NextResultAsync(cancellationToken);
            var parameters = new List<string>();
            while (await rdr.ReadAsync(cancellationToken))
            {
                parameters.Add(rdr.GetString(0));
            }
            return new ExistingObject(type, definition, ansiNulls, quotedIdentifier, parameters);
        }

        /// <summary>
        /// Why an existing object with the tool's name mustn't be replaced, or null if it's fine to deploy.  Only for families
        /// with an OwnershipPattern: their tools have generic names, so anything that can't be confirmed as the tool is left
        /// alone.  Never overridden.
        /// </summary>
        private static string GetConflict(CommunityTool tool, ExistingObject existing)
        {
            if (tool.Family.OwnershipRegex == null || existing == null)
            {
                return null;
            }
            if (existing.Type != "P")
            {
                return $"dbo.{tool.Name} exists but isn't a procedure (type {existing.Type}).  It won't be replaced.";
            }
            return CommunityToolVersionChecker.IsOwned(tool, existing.Definition) ? null : CommunityToolVersionChecker.ConflictMessage(tool);
        }

        /// <summary>
        /// For GuardCustomizations families: a reason not to replace the installed copy (customized, or a parameter an Agent
        /// job passes is removed), and warnings to report either way.  Agent jobs are only read when the parameters change.
        /// </summary>
        private static async Task<(string Block, List<string> Warnings)> CheckCustomizationsAsync(SqlConnection cn, CommunityTool tool, ExistingObject existing, CommunityToolScript script, string database, CancellationToken cancellationToken)
        {
            var warnings = new List<string>();
            var blocks = new List<string>();
            if (CommunityToolKnownVersions.Default.Find(tool.Name, existing.Definition) == null)
            {
                var installedVersion = CommunityToolVersionChecker.ParseVersion(tool.Family, existing.Definition ?? "").version;
                blocks.Add($"dbo.{tool.Name} doesn't match any known release of {tool.Family.DisplayName}{(installedVersion == null ? "" : $" (it says it's {installedVersion})")}.  It may have been customized.");
            }

            var batch = CommunityToolSafety.FindProcedureBatch(script.Text, tool.Name);
            if (batch == null)
            {
                blocks.Add($"Couldn't find dbo.{tool.Name} in {script.Origin} to compare its parameters.");
                return (string.Join("  ", blocks), warnings);
            }
            var newParameters = CommunityToolSafety.ParseParameters(batch);
            var removed = existing.Parameters.Where(p => !newParameters.Any(n => string.Equals(n.Name, p, StringComparison.OrdinalIgnoreCase))).ToList();
            var addedRequired = newParameters.Where(n => !n.HasDefault && !existing.Parameters.Contains(n.Name, StringComparer.OrdinalIgnoreCase)).Select(n => n.Name).ToList();
            if (removed.Count > 0 || addedRequired.Count > 0)
            {
                await CheckJobStepsAsync(cn, tool, database, removed, addedRequired, blocks, warnings, cancellationToken);
            }
            return (blocks.Count == 0 ? null : string.Join("  ", blocks), warnings);
        }

        /// <summary>Only called when the parameters change, so Agent jobs aren't read for most updates.</summary>
        private static async Task CheckJobStepsAsync(SqlConnection cn, CommunityTool tool, string database, List<string> removed, List<string> addedRequired, List<string> blocks, List<string> warnings, CancellationToken cancellationToken)
        {
            List<CommunityToolSafety.JobStep> steps;
            try
            {
                steps = (await CommunityToolSafety.GetJobStepsAsync(cn, tool.Name, cancellationToken))
                    .Where(step => CommunityToolSafety.CallsProcedureIn(step, tool.Name, database)).ToList();
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "Unable to read Agent job steps to check {tool}'s parameters", tool.Name);
                // Not knowing is treated as unsafe - overriding the safety checks is the way past it
                blocks.Add($"The new version {DescribeChanges(removed, addedRequired)}, and Agent jobs couldn't be checked for calls that would break: {ex.Message}");
                return;
            }

            var broken = steps.SelectMany(step =>
                    removed.Where(p => CommunityToolSafety.UsesParameter(step.Command, p)).Select(p => $"job '{step.Job}' step {step.StepId} passes {p}, which the new version removes")
                        .Concat(addedRequired.Where(p => !CommunityToolSafety.UsesParameter(step.Command, p)).Select(p => $"job '{step.Job}' step {step.StepId} doesn't pass {p}, which the new version requires"))
                        .Concat(CommunityToolSafety.PassesPositionalParameters(step.Command, tool.Name)
                            ? new[] { $"job '{step.Job}' step {step.StepId} passes parameters by position, so it can't be checked" }
                            : Array.Empty<string>()))
                .ToList();
            if (broken.Count > 0)
            {
                blocks.Add($"The update would break Agent jobs: {string.Join("; ", broken)}.");
            }
            else
            {
                warnings.Add($"The new version {DescribeChanges(removed, addedRequired)}.  No Agent job is affected - check anything else that calls dbo.{tool.Name}.");
            }
        }

        private static string DescribeChanges(List<string> removed, List<string> addedRequired) =>
            string.Join(" and ", new[]
            {
                removed.Count == 0 ? null : $"removes {string.Join(", ", removed)}",
                addedRequired.Count == 0 ? null : $"adds required {string.Join(", ", addedRequired)}"
            }.Where(s => s != null));

        /// <summary>
        /// Runs the family's setup scripts in the current database.  Each creates a table only if it doesn't exist, so an
        /// existing table is first checked for the columns the tools need - a different table of the same name is left alone
        /// and the family isn't deployed.  The error, or null on success.
        /// </summary>
        private async Task<string> RunSetupAsync(SqlConnection cn, CommunityTool tool, string instance, string database, CancellationToken cancellationToken)
        {
            foreach (var file in tool.Family.SetupFiles)
            {
                try
                {
                    if (!string.IsNullOrEmpty(file.Table) && await GetTableConflictAsync(cn, file, cancellationToken) is { } conflict)
                    {
                        Log.Warning("Not deploying {family} to {instance}.{database}: {reason}", tool.Family.Name, instance, database, conflict);
                        return conflict;
                    }
                    var script = await source.GetScriptAsync(tool, file, cancellationToken);
                    cn.ChangeDatabase(database);
                    await ExecuteScriptAsync(cn, script, file.Path, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Error(ex, "Error running {file} on {instance}.{database}", file.Path, instance, database);
                    return $"{file.Path}: {ex.Message}";
                }
            }
            return null;
        }

        private static async Task<string> GetTableConflictAsync(SqlConnection cn, CommunityToolFile file, CancellationToken cancellationToken)
        {
            await using var cmd = new SqlCommand(@"SELECT O.type, C.name
FROM sys.objects O
LEFT JOIN sys.columns C ON C.object_id = O.object_id
WHERE O.schema_id = SCHEMA_ID(@Schema)
AND O.name = @Name", cn);
            cmd.Parameters.AddWithValue("@Schema", CommunityTool.Schema);
            cmd.Parameters.AddWithValue("@Name", file.Table);
            string type = null;
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var rdr = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await rdr.ReadAsync(cancellationToken))
                {
                    type = rdr.GetString(0).Trim();
                    if (!rdr.IsDBNull(1)) columns.Add(rdr.GetString(1));
                }
            }
            if (type == null)
            {
                return null;
            }
            if (type != "U")
            {
                return $"dbo.{file.Table} exists but isn't a table (type {type}).  The tools that use it weren't deployed.";
            }
            var missing = file.Columns.Where(c => !columns.Contains(c)).ToList();
            return missing.Count == 0 ? null : $"dbo.{file.Table} exists but doesn't have the expected columns ({string.Join(", ", missing)} missing).  The tools that use it weren't deployed.";
        }
    }
}
