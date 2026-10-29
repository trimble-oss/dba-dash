using DBADash.CommunityTools;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DBADash.Test
{
    /// <summary>
    /// Community tools against SQL Server: every pinned tool deploys and passes its version check (or is skipped as
    /// unsupported), and checking and deploying follow the sp_ lookup order - the connection's database, then master.
    /// Downloads the pinned scripts from GitHub.
    ///
    /// <para>Runs against the server in the <see cref="MessageAuthorizationSqlTests.ServerEnvironmentVariable"/>
    /// connection string (CI sets it to each SQL version and collation it tests), failing if it can't connect.  Without
    /// it, LocalDB is used and the tests are inconclusive if LocalDB isn't available.  Works in a new database that's
    /// dropped afterwards, and only touches master for a tool that isn't already there, removing it again.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class CommunityToolsSqlTests
    {
        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(MessageAuthorizationSqlTests.ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Database = "DBADashCommunityToolsTest_" + Guid.NewGuid().ToString("N");

        /// <summary>A connection that lands in the test database, as a source connection with an Initial Catalog would.</summary>
        private static string ConnectionString => ConnectionStringFor(Database);

        /// <summary>A separate database for the lookup test, so the tools the other test installs don't get in the way.</summary>
        private static readonly string LookupDatabase = Database + "_Lookup";

        private static string LookupConnectionString => ConnectionStringFor(LookupDatabase);

        private static string ConnectionStringFor(string database)
        {
            var builder = ServerConnectionString == null
                // Long timeout: LocalDB stops when idle and the first connection starts it
                ? new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }
                : new SqlConnectionStringBuilder(ServerConnectionString);
            builder.InitialCatalog = database;
            return builder.ConnectionString;
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            try
            {
                Execute("master", $"CREATE DATABASE {Quote(Database)}; CREATE DATABASE {Quote(LookupDatabase)}");
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                // A server given in the environment variable must be available - the tests fail rather than being skipped
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            SqlConnection.ClearAllPools();
            try
            {
                foreach (var db in new[] { Database, LookupDatabase })
                {
                    Execute("master", $"IF DB_ID({Literal(db)}) IS NOT NULL BEGIN ALTER DATABASE {Quote(db)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Quote(db)}; END");
                }
            }
            catch (SqlException) when (ServerConnectionString == null)
            {
                // LocalDB wasn't available, so there's nothing to drop
            }
        }

        [TestMethod]
        public async Task EveryPinnedToolDeploysAndPassesItsVersionCheck()
        {
            var tools = CommunityToolsManifest.Default.Tools.ToList();
            var deployed = await new CommunityToolDeployer(new PinnedGitHubScriptSource()) { Database = Database }.DeployAsync(ConnectionString, tools);
            Assert.IsNull(deployed.Error);

            var versions = await CommunityToolVersionChecker.CheckAsync(ConnectionString, tools, Database);
            foreach (var result in deployed.Tools)
            {
                var version = versions.Single(v => v.Tool == result.Tool);
                if (result.Status == CommunityToolDeployStatus.Skipped)
                {
                    // Only when the tool says it doesn't support this version - never a silent failure
                    Assert.AreEqual(CommunityToolStatus.NotSupported, version.Status, $"{result.Tool.Name}: {result.Message}");
                    continue;
                }
                Assert.AreEqual(CommunityToolDeployStatus.Deployed, result.Status, $"{result.Tool.Name}: {result.Message}");
                Assert.AreEqual(Database, result.Database, result.Tool.Name);
                Assert.AreEqual(CommunityToolStatus.UpToDate, version.Status, $"{result.Tool.Name}: installed {version.InstalledVersion}, expected {version.ExpectedVersion}");
            }
        }

        [TestMethod]
        public async Task CheckAndDeployFollowTheSpLookupOrder()
        {
            var info = await ConnectionInfo.GetConnectionInfoAsync(LookupConnectionString);
            // A supported tool that isn't in master already, so nothing installed there is changed
            var installed = await GetInstalledInMasterAsync();
            var tool = CommunityToolsManifest.Default.Tools.FirstOrDefault(t => t.GetFile(info, out _) != null && !installed.Contains(t.Name));
            if (tool == null)
            {
                Assert.Inconclusive("Every supported community tool is already in master on this instance");
            }
            var tools = new[] { tool };
            var source = new PinnedGitHubScriptSource();
            try
            {
                // Only in master: found from the user database
                await DeployAsync(source, tool, "master");
                var inMaster = (await CommunityToolVersionChecker.CheckAsync(LookupConnectionString, tools)).Single();
                Assert.AreEqual("master", inMaster.Database);
                Assert.AreEqual(CommunityToolStatus.UpToDate, inMaster.Status);
                Assert.IsNull(inMaster.Note);

                // Default deploy updates the copy in master rather than adding one to the user database
                var defaultDeploy = await DeployAsync(source, tool, null);
                Assert.AreEqual("master", defaultDeploy.Database);
                Assert.IsFalse(await ExistsAsync(LookupDatabase, tool), "Default deploy created a copy in the user database");

                // A copy in the user database takes over from master
                await DeployAsync(source, tool, LookupDatabase);
                var inBoth = (await CommunityToolVersionChecker.CheckAsync(LookupConnectionString, tools)).Single();
                Assert.AreEqual(LookupDatabase, inBoth.Database);
                Assert.AreEqual("Also in master, which isn't used", inBoth.Note);

                var masterRequested = (await CommunityToolVersionChecker.CheckAsync(LookupConnectionString, tools, "master")).Single();
                Assert.AreEqual($"The copy in {LookupDatabase} is used instead", masterRequested.Note);

                // ...and default deploys now update it there
                Assert.AreEqual(LookupDatabase, (await DeployAsync(source, tool, null)).Database);
            }
            finally
            {
                Execute("master", $"DROP PROCEDURE IF EXISTS dbo.{Quote(tool.Name)}");
            }
        }

        private static async Task<CommunityToolDeployResult> DeployAsync(CommunityToolScriptSource source, CommunityTool tool, string? database)
        {
            var result = await new CommunityToolDeployer(source) { Database = database }.DeployAsync(LookupConnectionString, new[] { tool });
            Assert.IsNull(result.Error);
            var toolResult = result.Tools.Single();
            Assert.AreEqual(CommunityToolDeployStatus.Deployed, toolResult.Status, toolResult.Message);
            return toolResult;
        }

        private static async Task<System.Collections.Generic.HashSet<string>> GetInstalledInMasterAsync()
        {
            var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var cn = new SqlConnection(ConnectionStringFor("master"));
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT name FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo')", cn);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                names.Add(rdr.GetString(0));
            }
            return names;
        }

        private static async Task<bool> ExistsAsync(string database, CommunityTool tool)
        {
            await using var cn = new SqlConnection(ConnectionStringFor(database));
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT OBJECT_ID(@name, 'P')", cn);
            cmd.Parameters.AddWithValue("@name", "dbo." + tool.Name);
            return await cmd.ExecuteScalarAsync() is not DBNull;
        }

        private static void Execute(string database, string sql)
        {
            using var cn = new SqlConnection(ConnectionStringFor(database));
            cn.Open();
            using var cmd = new SqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }

        private static string Quote(string name) => "[" + name.Replace("]", "]]") + "]";

        private static string Literal(string value) => "N'" + value.Replace("'", "''") + "'";
    }
}
