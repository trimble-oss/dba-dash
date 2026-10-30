using DBADash;
using DBADash.CommunityTools;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DBADashConfig
{
    public static class CommunityToolsHelper
    {
        private const int MaxParallelInstances = 4;

        public static void ListTools()
        {
            var rows = CommunityToolsManifest.Default.Families.SelectMany(f => f.Tools.Select(t => new[]
            {
                f.Name,
                t.Name,
                string.Join(", ", t.Files.Select(file => file.Version).Distinct()),
                f.Ref,
                f.License,
                t.AzureSqlDb ? "Yes" : "No",
                f.RepositoryUrl
            }));
            WriteTable(new[] { "Family", "Tool", "Version", "Release", "License", "Azure DB", "Repository" }, rows);
        }

        public static async Task CheckAsync(CollectionConfig config, Options o)
        {
            var tools = CommunityToolsManifest.Default.ResolveTools(o.Tools);
            var connections = await GetConnectionsAsync(config, o);
            var results = await RunAsync(connections, async cn => await CommunityToolVersionChecker.CheckAsync(cn.ConnectionString, tools, o.Database));

            var rows = new List<string[]>();
            var failed = false;
            foreach (var (connection, versions, error) in results)
            {
                if (error != null)
                {
                    failed = true;
                    rows.Add(new[] { connection.ConnectionForPrint, "", "Error", "", "", "", error });
                    continue;
                }
                rows.AddRange(versions!.Select(v => new[]
                {
                    connection.ConnectionForPrint,
                    v.Tool.Name,
                    v.Status.ToString(),
                    v.InstalledVersion ?? "",
                    v.ExpectedVersion ?? "",
                    v.Database,
                    string.Join("  ", new[]
                    {
                        v.Message,
                        v.Note
                    }.Where(s => !string.IsNullOrEmpty(s)))
                }));
            }
            WriteTable(new[] { "Instance", "Tool", "Status", "Installed", "Expected", "Database", "Note" }, rows);
            if (failed)
            {
                Environment.Exit(1);
            }
        }

        public static async Task DeployAsync(CollectionConfig config, Options o)
        {
            if (!o.AcceptDisclaimer)
            {
                Log.Error("{Disclaimer}", CommunityToolsManifest.Disclaimer);
                Log.Error("Use --AcceptDisclaimer to confirm and deploy");
                Environment.Exit(1);
            }
            var tools = CommunityToolsManifest.Default.ResolveTools(o.Tools);
            var connections = await GetConnectionsAsync(config, o);
            var source = CommunityToolScriptSource.Create(o.Source, o.SourceFolder);
            if (o.Source == CommunityToolScriptSource.SourceTypes.Latest)
            {
                Log.Warning("Deploying the latest release of each tool.  These versions haven't been tested with DBA Dash.");
            }
            if (o.OverrideSafetyChecks)
            {
                Log.Warning("Safety checks are overridden: customized procedures and procedures whose update would break Agent jobs will be replaced");
            }
            var deployer = new CommunityToolDeployer(source) { Database = o.Database, OverrideSafetyChecks = o.OverrideSafetyChecks, IncludeAzureSqlDb = o.IncludeAzureSqlDb, RollbackFolder = o.RollbackFolder };
            var results = await RunAsync(connections, async cn => await deployer.DeployAsync(cn.ConnectionString, tools));

            var rows = new List<string[]>();
            foreach (var (connection, result, error) in results)
            {
                var instanceError = error ?? result!.Error;
                if (instanceError != null)
                {
                    rows.Add(new[] { connection.ConnectionForPrint, "", "", "Failed", "", instanceError });
                    continue;
                }
                rows.AddRange(result!.Tools.Select(t => new[]
                {
                    result.Instance,
                    t.Database ?? result.Database,
                    t.Tool.Name,
                    t.Status.ToString(),
                    t.Version ?? "",
                    t.Message ?? ""
                }));
            }
            WriteTable(new[] { "Instance", "Database", "Tool", "Status", "Version", "Message" }, rows);
            foreach (var rollback in results.Select(r => r.Result?.RollbackScript).Where(p => p != null))
            {
                Log.Information("Rollback script: {Path}", rollback);
            }

            var toolResults = results.Where(r => r.Result?.Error == null && r.Error == null).SelectMany(r => r.Result!.Tools).ToList();
            var deployedCount = toolResults.Count(t => t.Status == CommunityToolDeployStatus.Deployed);
            var failedCount = toolResults.Count(t => t.Status == CommunityToolDeployStatus.Failed) + results.Count(r => r.Error != null || r.Result?.Error != null);
            var upToDateCount = toolResults.Count(t => t.Status == CommunityToolDeployStatus.UpToDate);
            var skippedCount = toolResults.Count(t => t.Status == CommunityToolDeployStatus.Skipped);
            Log.Information("{Deployed} deployed, {UpToDate} already up to date, {Failed} failed, {Skipped} skipped", deployedCount, upToDateCount, failedCount, skippedCount);

            if (o.AddToAllowedScripts)
            {
                AddToAllowedScripts(config, o, toolResults.Where(t => t.Status is CommunityToolDeployStatus.Deployed or CommunityToolDeployStatus.UpToDate && t.Tool.RunFromGui).Select(t => t.Tool.Name));
            }
            if (failedCount > 0)
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Moves the manifest's pins to the newest upstream releases (for the scheduled workflow).  With -c, the updated
        /// tools are deployed to that instance from the new pins and their versions checked, so a release that doesn't
        /// install or whose version can't be read fails here instead of in a PR.
        /// </summary>
        public static async Task UpdateManifestAsync(Options o)
        {
            if (string.IsNullOrEmpty(o.ManifestPath))
            {
                throw new ArgumentException("--ManifestPath is required");
            }
            var json = await File.ReadAllTextAsync(o.ManifestPath);
            var updates = await CommunityToolsManifestUpdater.GetUpdatesAsync(CommunityToolsManifest.Parse(json));
            var failed = updates.Any(u => u.Error != null);
            var summary = CommunityToolsManifestUpdater.ToMarkdown(updates);

            var newJson = CommunityToolsManifestUpdater.Apply(json, updates);
            if (newJson == json)
            {
                Log.Information("No community tool updates");
            }
            else
            {
                await File.WriteAllTextAsync(o.ManifestPath, newJson);
                Log.Information("Updated {Path}", o.ManifestPath);
                await AddPinnedKnownVersionsAsync(o.ManifestPath, CommunityToolsManifest.Parse(newJson), updates);
                if (!string.IsNullOrEmpty(o.ConnectionString))
                {
                    var (passed, testSummary) = await TestUpdatesAsync(o.ConnectionString, CommunityToolsManifest.Parse(json), CommunityToolsManifest.Parse(newJson), updates, o.GuiSourcePath);
                    failed |= !passed;
                    summary += testSummary;
                }
            }
            if (!string.IsNullOrEmpty(o.SummaryPath))
            {
                await File.WriteAllTextAsync(o.SummaryPath, summary);
            }
            if (failed)
            {
                Environment.Exit(1);
            }
        }

        private static string KnownVersionsPath(string manifestPath) =>
            Path.Combine(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!, CommunityToolKnownVersions.FileName);

        /// <summary>The new pins of guarded families become known releases, so installs of them can be updated later.</summary>
        private static async Task AddPinnedKnownVersionsAsync(string manifestPath, CommunityToolsManifest manifest, List<CommunityToolFamilyUpdate> updates)
        {
            var families = updates.Where(u => u.Error == null && u.Family.GuardCustomizations).Select(u => u.Family.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (families.Count == 0)
            {
                return;
            }
            var path = KnownVersionsPath(manifestPath);
            var known = CommunityToolKnownVersions.Parse(await File.ReadAllTextAsync(path));
            var source = new PinnedGitHubScriptSource();
            foreach (var tool in manifest.Tools.Where(t => families.Contains(t.Family.Name)))
            {
                foreach (var file in tool.Files)
                {
                    var script = await source.GetScriptAsync(tool, file);
                    if (!known.Add(tool, script.Text))
                    {
                        Log.Information("{Tool} {Version} is already a known release", tool.Name, file.Version);
                    }
                }
            }
            await File.WriteAllTextAsync(path, known.ToJson());
            // This build's embedded catalog doesn't have the new releases yet, and they're tested next
            CommunityToolKnownVersions.UseInstead(known);
        }

        /// <summary>Adds each guarded tool release found in the .sql files under --SourceFolder to the known versions.</summary>
        public static async Task AddKnownVersionsAsync(Options o)
        {
            if (string.IsNullOrEmpty(o.ManifestPath) || string.IsNullOrEmpty(o.SourceFolder))
            {
                throw new ArgumentException("--ManifestPath and --SourceFolder are required");
            }
            var manifest = CommunityToolsManifest.Parse(await File.ReadAllTextAsync(o.ManifestPath));
            var tools = manifest.Tools.Where(t => t.Family.GuardCustomizations).ToList();
            var path = KnownVersionsPath(o.ManifestPath);
            var known = CommunityToolKnownVersions.Parse(await File.ReadAllTextAsync(path));
            var added = 0;
            foreach (var file in Directory.EnumerateFiles(o.SourceFolder, "*.sql", SearchOption.AllDirectories))
            {
                var text = await File.ReadAllTextAsync(file);
                added += tools.Count(tool => known.Add(tool, text));
            }
            await File.WriteAllTextAsync(path, known.ToJson());
            Log.Information("Added {Count} known releases to {Path}", added, path);
        }

        private static async Task<(bool Passed, string Summary)> TestUpdatesAsync(string connectionString, CommunityToolsManifest oldManifest, CommunityToolsManifest manifest, List<CommunityToolFamilyUpdate> updates, string? guiSourcePath)
        {
            var familyNames = updates.Where(u => u.Error == null).Select(u => u.Family.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var oldTools = oldManifest.Tools.Where(t => familyNames.Contains(t.Family.Name)).ToList();
            var tools = manifest.Tools.Where(t => familyNames.Contains(t.Family.Name)).ToList();

            // The old pins go on first so their parameters can be compared with the new release's.
            var oldDeployed = await new CommunityToolDeployer(new PinnedGitHubScriptSource()) { IncludeAzureSqlDb = true }.DeployAsync(connectionString, oldTools);
            var oldParameters = oldDeployed.Error == null ? await GetParametersAsync(connectionString, oldDeployed.Database, oldTools) : null;

            var deployed = await new CommunityToolDeployer(new PinnedGitHubScriptSource()) { IncludeAzureSqlDb = true }.DeployAsync(connectionString, tools);
            if (deployed.Error != null)
            {
                Log.Error("Test deployment failed: {Error}", deployed.Error);
                return (false, $"Test deployment failed: {deployed.Error}{Environment.NewLine}");
            }
            var versions = await CommunityToolVersionChecker.CheckAsync(connectionString, tools);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"### Test deployment to {deployed.Instance}");
            sb.AppendLine();
            sb.AppendLine("| Tool | Deploy | Version check |");
            sb.AppendLine("|---|---|---|");
            var passed = true;
            foreach (var result in deployed.Tools)
            {
                var version = versions.First(v => v.Tool == result.Tool);
                var ok = result.Status is CommunityToolDeployStatus.Skipped or CommunityToolDeployStatus.UpToDate
                    || (result.Status == CommunityToolDeployStatus.Deployed && version.Status == CommunityToolStatus.UpToDate);
                passed &= ok;
                sb.AppendLine($"| {result.Tool.Name} | {result.Status} {result.Message} | {version.Status} {version.InstalledVersion} |");
                if (!ok)
                {
                    Log.Error("{Tool} failed testing: deploy {DeployStatus} {Message}, version check {VersionStatus}", result.Tool.Name, result.Status, result.Message, version.Status);
                }
            }
            sb.AppendLine();

            if (oldParameters == null)
            {
                sb.AppendLine($"Parameters weren't compared - the previous versions didn't deploy: {oldDeployed.Error}");
            }
            else
            {
                // CompareParameters skips tools with no old parameters, which would otherwise look like "no changes".
                var notCompared = oldDeployed.Tools.Where(t => t.Status == CommunityToolDeployStatus.Failed)
                    .Select(t => $"- ⚠️ {t.Tool.Name}: parameters not compared - the previous version didn't deploy: {t.Message?.ReplaceLineEndings(" ")}");
                sb.Append(CompareParameters(oldParameters, await GetParametersAsync(connectionString, deployed.Database, tools), guiSourcePath, notCompared));
            }
            return (passed, sb.ToString());
        }

        /// <summary>Parameter name → type for each tool, from sys.parameters.</summary>
        private static async Task<Dictionary<string, Dictionary<string, string>>> GetParametersAsync(string connectionString, string database, List<CommunityTool> tools)
        {
            const string sql = @"
SELECT OBJECT_NAME(p.object_id) AS ProcName, p.name, TYPE_NAME(p.user_type_id) AS TypeName
FROM sys.parameters p
WHERE p.object_id IN (SELECT object_id FROM sys.procedures WHERE SCHEMA_NAME(schema_id) = 'dbo')
AND p.name <> ''";
            var result = tools.ToDictionary(t => t.Name, _ => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            await using var cn = new SqlConnection(new SqlConnectionStringBuilder(connectionString) { InitialCatalog = database }.ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                if (result.TryGetValue(rdr.GetString(0), out var parameters))
                {
                    parameters[rdr.GetString(1)] = rdr.GetString(2);
                }
            }
            return result;
        }

        /// <summary>
        /// Removed parameters and type changes, as warnings for the PR.  The GUI lists each tool's parameters in
        /// DBADashGUI\CommunityTools\{tool}.cs, so a removed parameter named there will break the GUI's call.
        /// </summary>
        public static string CompareParameters(Dictionary<string, Dictionary<string, string>> oldParameters, Dictionary<string, Dictionary<string, string>> newParameters, string? guiSourcePath, IEnumerable<string>? notCompared = null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var line in notCompared ?? Enumerable.Empty<string>())
            {
                sb.AppendLine(line);
            }
            foreach (var (tool, before) in oldParameters)
            {
                var after = newParameters[tool];
                if (before.Count == 0)
                {
                    continue; // The old version wasn't deployed (unsupported, or reported as not compared), so there's nothing to compare.
                }
                var guiFile = guiSourcePath == null ? null : Path.Combine(guiSourcePath, tool + ".cs");
                var guiSource = guiFile != null && File.Exists(guiFile) ? File.ReadAllText(guiFile) : null;
                string UsedByGui(string name) =>
                    guiSource?.Contains($"\"{name}\"", StringComparison.OrdinalIgnoreCase) == true ? " **- used by the GUI**" : "";

                foreach (var (name, type) in before)
                {
                    if (!after.TryGetValue(name, out var newType))
                    {
                        sb.AppendLine($"- ⚠️ {tool}: `{name}` removed{UsedByGui(name)}");
                    }
                    else if (!string.Equals(type, newType, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"- ⚠️ {tool}: `{name}` changed from {type} to {newType}{UsedByGui(name)}");
                    }
                }
                var added = after.Keys.Where(name => !before.ContainsKey(name)).ToList();
                if (added.Count > 0)
                {
                    sb.AppendLine($"- {tool}: new parameters {string.Join(", ", added.Select(name => $"`{name}`"))}");
                }
            }
            var changes = sb.ToString();
            if (changes.Length > 0)
            {
                Log.Warning("Parameter changes:{NewLine}{Changes}", Environment.NewLine, changes);
            }
            return $"### Parameter changes{Environment.NewLine}{Environment.NewLine}{(changes.Length == 0 ? "None" + Environment.NewLine : changes)}";
        }

        private static void AddToAllowedScripts(CollectionConfig config, Options o, IEnumerable<string> toolNames)
        {
            if (config.AllowedScripts == "*")
            {
                Log.Information("AllowedScripts already allows all community tools");
                return;
            }
            var allowed = (config.AllowedScripts ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var added = toolNames.Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => !allowed.Contains(name, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (added.Count == 0)
            {
                Log.Information("Deployed tools are already in AllowedScripts");
                return;
            }
            Log.Information("Adding {Tools} to AllowedScripts", string.Join(", ", added));
            config.AllowedScripts = string.Join(",", allowed.Concat(added));
            Helper.SaveConfig(config, o);
        }

        /// <summary>
        /// The instances to work on: -c for a single connection string (which needn't be in the config), --ConnectionID
        /// for one instance from the config, or --ConnectionIDs for several ("*" for all).
        /// </summary>
        private static async Task<List<DBADashConnection>> GetConnectionsAsync(CollectionConfig config, Options o)
        {
            List<DBADashConnection> connections;
            if (!string.IsNullOrEmpty(o.ConnectionString))
            {
                connections = new List<DBADashConnection> { new(o.ConnectionString) };
            }
            else if (o.ConnectionIDs == "*")
            {
                connections = config.SourceConnections.Select(s => s.SourceConnection).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(o.ConnectionIDs) || !string.IsNullOrEmpty(o.ConnectionID))
            {
                var ids = (o.ConnectionIDs ?? o.ConnectionID).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                connections = new List<DBADashConnection>();
                foreach (var id in ids)
                {
                    var src = await config.GetSourceConnectionAsync(id) ?? throw new ArgumentException($"ConnectionID not found: {id}");
                    connections.Add(src.SourceConnection);
                }
            }
            else
            {
                throw new ArgumentException("Specify instances with -c, --ConnectionID or --ConnectionIDs");
            }

            var nonSql = connections.Where(c => c.Type != DBADashConnection.ConnectionType.SQL).ToList();
            foreach (var c in nonSql)
            {
                Log.Warning("Skipping {Connection} - not a SQL Server connection", c.ConnectionForPrint);
            }
            connections = connections.Except(nonSql).ToList();
            if (connections.Count == 0)
            {
                throw new ArgumentException("No SQL Server connections to work on");
            }
            if (string.IsNullOrEmpty(o.SqlUser))
            {
                return connections;
            }
            var password = Helper.ReadSecret(o.SqlPassword, Helper.PasswordEnvironmentVariable, $"Password for {o.SqlUser}: ", confirm: false);
            return connections.Select(c => WithSqlCredentials(c, o.SqlUser, password)).ToList();
        }

        private static DBADashConnection WithSqlCredentials(DBADashConnection connection, string user, string? password)
        {
            var credentials = new SqlConnectionStringBuilder { UserID = user, Password = password ?? "" };
            return new DBADashConnection(CommunityToolDeployer.WithCredentials(connection.ConnectionString, credentials.ConnectionString));
        }

        private static async Task<List<(DBADashConnection Connection, T? Result, string? Error)>> RunAsync<T>(List<DBADashConnection> connections, Func<DBADashConnection, Task<T>> action) where T : class
        {
            using var throttle = new SemaphoreSlim(MaxParallelInstances);
            var tasks = connections.Select(async connection =>
            {
                await throttle.WaitAsync();
                try
                {
                    return (connection, await action(connection), (string?)null);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing {Connection}", connection.ConnectionForPrint);
                    return (connection, (T?)null, ex.Message);
                }
                finally
                {
                    throttle.Release();
                }
            });
            return (await Task.WhenAll(tasks)).ToList();
        }

        private static void WriteTable(string[] headers, IEnumerable<string[]> rows)
        {
            var allRows = rows.ToList();
            var widths = headers.Select((h, i) => Math.Max(h.Length, allRows.Select(r => r[i].Length).DefaultIfEmpty(0).Max())).ToArray();
            string Format(string[] cells) => string.Join("  ", cells.Select((c, i) => i == cells.Length - 1 ? c : c.PadRight(widths[i]))).TrimEnd();
            Console.WriteLine(Format(headers));
            Console.WriteLine(Format(widths.Select(w => new string('-', w)).ToArray()));
            allRows.ForEach(r => Console.WriteLine(Format(r)));
        }
    }
}
