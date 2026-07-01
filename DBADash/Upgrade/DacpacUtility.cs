using Microsoft.SqlServer.Dac;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using static DBADash.DBValidations;

namespace DacpacUtility
{
    public class DacpacService
    {
        public List<string> MessageList { get; set; }

        public DacpacService()
        {
            MessageList = new List<string>();
            _dacDeployOptions = new DacDeployOptions
            {
                IgnorePermissions = true,
                IgnoreTableOptions = true,
                IgnorePartitionSchemes = true,
                IgnoreIndexOptions = true,
                IgnoreUserSettingsObjects = true,
                IgnoreRoleMembership = true,
                DropObjectsNotInSource = false,
                BlockOnPossibleDataLoss = false,
                RegisterDataTierApplication = false,
                BlockWhenDriftDetected = false,
                ExcludeObjectTypes = new[]
                {
                    ObjectType.Users,
                    ObjectType.Logins,
                    ObjectType.RoleMembership,
                    ObjectType.Permissions
                }
            };
        }

        // Sets the extended property 'IsDBUpgradeInProgress' to indicate if a deployment is in progress
        private void SetDeployInProgress(string connectionString, bool inProgress)
        {
            try
            {
                const string sql = @"IF NOT EXISTS(
		SELECT *
		FROM sys.extended_properties
		WHERE name = 'IsDBUpgradeInProgress'
		AND class = 0 /* class 0 is database */
		)
BEGIN
	EXECUTE sp_addextendedproperty
		@name = N'IsDBUpgradeInProgress',
		@value = @InProgress;
END
EXECUTE sp_updateextendedproperty
		@name = N'IsDBUpgradeInProgress',
		@value = @InProgress;";
                using var cn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@InProgress", inProgress ? "Y" : "N");
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting deploy in progress flag");
                MessageList.Add("Error setting deploy in progress flag: " + ex.Message);
            }
        }

        public void ProcessDacPac(string connectionString,
                                    string databaseName,
                                    string dacpacName,
                                    DBVersionStatusEnum status,
                                    int retryCount = 0)
        {

            MessageList.Add("*** Start of processing for " +
                             databaseName);
            // For an existing DB we want to skip this option.  Some users might prefer to use SIMPLE recovery model and this option would set it back to FULL each time the dacpac is deployed
            _dacDeployOptions.ScriptDatabaseOptions = status == DBVersionStatusEnum.CreateDB;
            if (!_dacDeployOptions.ScriptDatabaseOptions)
            {
                MessageList.Add("Skipping deployment of Database Options for existing database");
            }

            var dacServiceInstance = new DacServices(connectionString);
            dacServiceInstance.ProgressChanged +=
              (s, e) =>
              {
                  MessageList.Add(e.Message);
                  Log.Information(e.Message);
              };
            dacServiceInstance.Message +=
              (s, e) =>
              {
                  MessageList.Add(e.Message.Message);
                  Log.Information(e.Message.Message);
              };

            try
            {
                if(status != DBVersionStatusEnum.CreateDB) // We can't set the flag if the DB doesn't exist
                {
                    SetDeployInProgress(connectionString, true);
                }
                using (var dacpac = DacPackage.Load(dacpacName))
                {
                    if (_dacDeployOptions.SqlCommandVariableValues.ContainsKey("VersionNumber"))
                    {
                        _dacDeployOptions.SqlCommandVariableValues.Remove("VersionNumber");
                    }

                    _dacDeployOptions.SqlCommandVariableValues.Add("VersionNumber", dacpac.Version.ToString());
                    dacServiceInstance.Deploy(dacpac, databaseName,
                        upgradeExisting: true,
                        options: _dacDeployOptions);
                }
            }
            catch (DacServicesException ex) when (ex.Message.Contains("IX_Instances_InstanceDisplayName") && retryCount == 0)
            {
                var msg = "Encountered a known issue processing dacpac.Applying fix & re - running dacpac. ";
                MessageList.Add(msg);
                Log.Warning(ex, msg);
                DropIndexFix_1040(connectionString, databaseName);
                retryCount++;
                ProcessDacPac(connectionString, databaseName, dacpacName, status, retryCount);
            }
            // Views/functions the user created outside of the DBA Dash schema (e.g. a view in UserData
            // referencing a DBA Dash table) can end up needing to be dropped as a side effect of an unrelated
            // schema change elsewhere (e.g. reordering columns on a table it references). Since these objects
            // aren't part of the dacpac, DacFx can't recreate them itself and blocks the deployment (SQL72032).
            // Any object this error reports is - by definition - not part of the DBA Dash model (objects that
            // are recreated automatically and never trigger this), so it's safe to handle generically here.
            // We capture each object's definition, drop it, let the deployment proceed, then recreate it from
            // the captured definition - which works because the deployment's whole purpose was to preserve the
            // columns/tables the object depends on, just with a different physical column order. #1959
            catch (DacServicesException ex) when (retryCount == 0 && ex.Messages.Any(m => m.Number == 72032))
            {
                var msg = "Encountered view(s)/function(s) outside of the DBA Dash schema that must be dropped to apply this update. Attempting to remove and recreate them automatically.";
                MessageList.Add(msg);
                Log.Warning(ex, msg);
                var removedObjects = RemoveObjectsBlockingDeployment(ex, connectionString, databaseName);
                if (removedObjects != null)
                {
                    // Recreate in a finally so the objects go back even if the retried deployment below throws -
                    // they were only ever removed to work around this deployment, so they shouldn't stay missing
                    // just because the retry itself failed for some other reason.
                    try
                    {
                        retryCount++;
                        ProcessDacPac(connectionString, databaseName, dacpacName, status, retryCount);
                    }
                    finally
                    {
                        RecreateRemovedObjects(connectionString, databaseName, removedObjects);
                    }
                }
                else
                {
                    MessageList.Add("Unable to safely remove the blocking object(s) automatically.  Please review the error below, drop the object(s) manually and retry the upgrade.");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing dacpac");
                MessageList.Add(ex.Message);
                throw;
            }
            finally
            {
                SetDeployInProgress(connectionString, false);
            }
        }

        // Fix upgrade issue that occurs with versions 3.3 and older. #1040
        private static void DropIndexFix_1040(string connectionString, string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = databaseName
            };
            const string sql = "DROP INDEX IX_Instances_InstanceDisplayName ON dbo.Instances;";
            using var cn = new SqlConnection(builder.ConnectionString);
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        private static readonly Regex TwoPartNameRegex = new(@"\[(?<schema>[^\]]+)\]\.\[(?<name>[^\]]+)\]", RegexOptions.Compiled);

        // Only views and functions are known to be affected by this (confirmed empirically against #1959 - scalar,
        // inline table-valued and multi-statement table-valued functions all reproduce it identically to views).
        // Stored procedures are not affected - they're refreshed in place (EXECUTE sp_refreshsqlmodule) rather than
        // dropped, so they're deliberately excluded here rather than handled defensively for a case that doesn't occur.
        private static readonly Dictionary<string, string> DropKeywordByTypeDesc = new()
        {
            ["VIEW"] = "VIEW",
            ["SQL_SCALAR_FUNCTION"] = "FUNCTION",
            ["SQL_TABLE_VALUED_FUNCTION"] = "FUNCTION",
            ["SQL_INLINE_TABLE_VALUED_FUNCTION"] = "FUNCTION"
        };

        private static readonly string LogsFolder = Path.Combine(AppContext.BaseDirectory, "Logs");

        // Saves a dropped object's definition to its own file in the Logs folder so it survives even if the
        // MessageList/console output isn't kept.  Returns the saved path, or null if the write itself failed -
        // this is a convenience, not something that should ever block the deployment recovery from proceeding. #1959
        private string SaveDefinitionToLogFile(string schema, string name, string definition)
        {
            try
            {
                Directory.CreateDirectory(LogsFolder);
                var invalidChars = Path.GetInvalidFileNameChars();
                var safeSchema = new string(schema.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
                var safeName = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
                var filePath = Path.Combine(LogsFolder, $"{safeSchema}.{safeName}.sql");
                File.WriteAllText(filePath, definition);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to save definition of {Schema}.{Name} to the Logs folder", schema, name);
                return null;
            }
        }

        // Removes objects reported by SQL72032 ("will be dropped and not re-created") so the deployment can
        // proceed, after capturing each object's definition so it can be recreated afterwards.  Only drops
        // objects it can both resolve and capture a definition for - if any object can't be safely handled,
        // nothing is dropped and null is returned so the original error is preserved. #1959
        private List<(string Schema, string Name, string Definition)> RemoveObjectsBlockingDeployment(DacServicesException ex, string connectionString, string databaseName)
        {
            var names = ex.Messages
                .Where(m => m.Number == 72032)
                .Select(m => TwoPartNameRegex.Match(m.Message))
                .Where(m => m.Success)
                .Select(m => (Schema: m.Groups["schema"].Value, Name: m.Groups["name"].Value))
                .Distinct()
                .ToList();

            if (names.Count == 0)
            {
                return null;
            }

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = databaseName
            };
            using var cn = new SqlConnection(builder.ConnectionString);
            cn.Open();

            var toDrop = new List<(string Schema, string Name, string DropKeyword, string Definition)>();
            foreach (var (schema, name) in names)
            {
                const string lookupSql = @"SELECT o.type_desc, OBJECT_DEFINITION(o.object_id)
FROM sys.objects o
WHERE o.schema_id = SCHEMA_ID(@Schema) AND o.name = @Name";
                using var cmd = new SqlCommand(lookupSql, cn);
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Name", name);
                using var rdr = cmd.ExecuteReader();
                if (!rdr.Read())
                {
                    return null; // Object no longer resolvable - bail out without dropping anything.
                }
                var typeDesc = rdr.GetString(0);
                var definition = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                if (string.IsNullOrEmpty(definition) || !DropKeywordByTypeDesc.TryGetValue(typeDesc, out var dropKeyword))
                {
                    return null; // Couldn't capture a definition, or don't recognise the object type - too risky to drop.
                }
                toDrop.Add((schema, name, dropKeyword, definition));
            }

            foreach (var (schema, name, dropKeyword, definition) in toDrop)
            {
                var savedPath = SaveDefinitionToLogFile(schema, name, definition);
                var msg = savedPath != null
                    ? $"Dropping [{schema}].[{name}] to allow deployment to proceed.  Will attempt to recreate it once the deployment completes.  Definition saved to {savedPath}"
                    : $"Dropping [{schema}].[{name}] to allow deployment to proceed.  Will attempt to recreate it once the deployment completes.";
                MessageList.Add(msg);
                Log.Warning("Dropping {Schema}.{Name} to allow dacpac deployment to proceed. Definition saved to {SavedPath}", schema, name, savedPath);
                using var dropCmd = new SqlCommand($"DROP {dropKeyword} [{schema}].[{name}]", cn);
                dropCmd.ExecuteNonQuery();
            }
            return toDrop.Select(o => (o.Schema, o.Name, o.Definition)).ToList();
        }

        // Best-effort recreation of objects removed by RemoveObjectsBlockingDeployment, run once the deployment
        // they were blocking has completed successfully.  This works because the underlying reason the object
        // was dropped is that a table it depends on needed its columns temporarily removed and re-added (e.g. to
        // reorder them) - by the time deployment finishes those columns exist again, so replaying the object's
        // original CREATE statement against the now-deployed schema succeeds.  If it doesn't - e.g. because the
        // object also depended on something that genuinely changed - this only logs the failure; the deployment
        // itself has already succeeded by this point. #1959
        private void RecreateRemovedObjects(string connectionString, string databaseName, List<(string Schema, string Name, string Definition)> removedObjects)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = databaseName
            };
            using var cn = new SqlConnection(builder.ConnectionString);
            cn.Open();
            foreach (var (schema, name, definition) in removedObjects)
            {
                try
                {
                    using var cmd = new SqlCommand(definition, cn);
                    cmd.ExecuteNonQuery();
                    var msg = $"Recreated [{schema}].[{name}] after the upgrade completed.";
                    MessageList.Add(msg);
                    Log.Information(msg);
                }
                catch (Exception ex)
                {
                    var msg = $"Could not automatically recreate [{schema}].[{name}] after the upgrade - please recreate it manually using the definition saved to the Logs folder. Error: {ex.Message}";
                    MessageList.Add(msg);
                    Log.Warning(ex, msg);
                }
            }
        }

        private DacDeployOptions _dacDeployOptions;

        public DacDeployOptions DacDeployOpts
        {
            get => _dacDeployOptions;
            set => _dacDeployOptions = value;
        }

        public static Version GetVersion(string dacpacName)
        {
            using (var dacpac = DacPackage.Load(dacpacName))
            {
                return dacpac.Version;
            }
        }

        public string GenerateDeployScript(string connectionString,
                                string databaseName,
                                string dacpacName)
        {
            MessageList.Add("*** Start of processing for " +
                             databaseName);

            var dacServiceInstance = new DacServices(connectionString);
            dacServiceInstance.ProgressChanged +=
              (s, e) =>
                  MessageList.Add(e.Message);
            dacServiceInstance.Message +=
              (s, e) =>
                  MessageList.Add(e.Message.Message);

            try
            {
                using (var dacpac = DacPackage.Load(dacpacName))
                {
                    if (_dacDeployOptions.SqlCommandVariableValues.ContainsKey("VersionNumber"))
                    {
                        _dacDeployOptions.SqlCommandVariableValues.Remove("VersionNumber");
                    }
                    _dacDeployOptions.SqlCommandVariableValues.Add("VersionNumber", dacpac.Version.ToString());
                    return dacServiceInstance.GenerateDeployScript(dacpac, databaseName,
                                            options: _dacDeployOptions);
                }
            }
            catch (Exception ex)
            {
                MessageList.Add(ex.Message);
                return null;
            }
        }
    }
}