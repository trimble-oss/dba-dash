using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADash.CommunityTools;
using DBADash.Messaging;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class CommunityToolsTests
    {
        private static CommunityToolsManifest Manifest => CommunityToolsManifest.Default;

        private static ConnectionInfo Instance(int majorVersion, DatabaseEngineEdition edition = DatabaseEngineEdition.Enterprise, string database = "master") => new()
        {
            ProductVersion = majorVersion + ".0.1000.0",
            EngineEdition = edition,
            DatabaseName = database,
            ComputerNetBIOSName = ""
        };

        [TestMethod]
        public void Manifest_CoversEveryCommunityProcTheGuiCanRun()
        {
            var manifestNames = Manifest.Tools.Select(t => t.Name).OrderBy(n => n).ToList();
            var procNames = Enum.GetNames<ProcedureExecutionMessage.CommunityProcs>().OrderBy(n => n).ToList();
            CollectionAssert.AreEqual(procNames, manifestNames);
        }

        [TestMethod]
        public void Manifest_EntriesAreComplete()
        {
            foreach (var family in Manifest.Families)
            {
                Assert.IsFalse(string.IsNullOrEmpty(family.Repository), family.Name);
                Assert.IsFalse(string.IsNullOrEmpty(family.Ref), family.Name);
                Assert.IsFalse(string.IsNullOrEmpty(family.LicenseUrl), family.Name);
                Assert.IsNotNull(family.VersionRegex, family.Name);
                foreach (var tool in family.Tools)
                {
                    Assert.AreSame(family, tool.Family);
                    Assert.IsTrue(tool.Files.Count > 0, tool.Name);
                    foreach (var file in tool.Files)
                    {
                        StringAssert.Matches(file.Sha256, new System.Text.RegularExpressions.Regex("^[0-9a-f]{64}$"), tool.Name);
                        Assert.IsFalse(string.IsNullOrEmpty(file.Version), tool.Name);
                    }
                }
            }
        }

        [TestMethod]
        public void ResolveTools_AcceptsFamiliesToolsAndStar()
        {
            Assert.AreEqual(Manifest.Tools.Count(), Manifest.ResolveTools("*").Count);

            var tools = Manifest.ResolveTools("sp_whoisactive, FirstResponderKit, sp_Blitz");
            Assert.AreEqual("sp_WhoIsActive", tools[0].Name);
            Assert.AreEqual(1 + Manifest.Families.Single(f => f.Name == "FirstResponderKit").Tools.Count, tools.Count, "sp_Blitz is already in the family, so isn't added twice");
        }

        [TestMethod]
        public void ResolveTools_UnknownNameThrows()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Manifest.ResolveTools("sp_WhoIsActive,sp_Typo"));
        }

        [TestMethod]
        public void GetFile_PicksWhoIsActiveBuildForVersion()
        {
            var tool = Manifest.GetTool("sp_WhoIsActive");
            Assert.AreEqual("2008/sp_WhoIsActive.sql", tool.GetFile(Instance(10), out _).Path);
            Assert.AreEqual("2019/sp_WhoIsActive.sql", tool.GetFile(Instance(11), out _).Path);
            Assert.AreEqual("2019/sp_WhoIsActive.sql", tool.GetFile(Instance(15), out _).Path);
            Assert.AreEqual("sp_WhoIsActive.sql", tool.GetFile(Instance(16), out _).Path);
            Assert.AreEqual("sp_WhoIsActive.sql", tool.GetFile(Instance(17), out _).Path);
        }

        [TestMethod]
        public void GetFile_AzureGetsLatestBuildDespiteReportingVersion12()
        {
            var tool = Manifest.GetTool("sp_WhoIsActive");
            Assert.AreEqual("sp_WhoIsActive.sql", tool.GetFile(Instance(12, DatabaseEngineEdition.SqlDatabase, "UserDB"), out _).Path);
            Assert.AreEqual("sp_WhoIsActive.sql", tool.GetFile(Instance(12, DatabaseEngineEdition.SqlManagedInstance), out _).Path);
            Assert.IsNotNull(Manifest.GetTool("sp_Blitz").GetFile(Instance(12, DatabaseEngineEdition.SqlManagedInstance), out _), "MinMajorVersion doesn't apply to MI");
        }

        [TestMethod]
        public void GetFile_NotSupported()
        {
            Assert.IsNull(Manifest.GetTool("sp_Blitz").GetFile(Instance(12), out var reason));
            StringAssert.Contains(reason, "13");

            Assert.IsNull(Manifest.GetTool("sp_LogHunter").GetFile(Instance(12, DatabaseEngineEdition.SqlDatabase, "UserDB"), out reason));
            StringAssert.Contains(reason, "Azure");
        }

        [TestMethod]
        public void ParseVersion_FirstResponderKit()
        {
            var family = Manifest.Families.Single(f => f.Name == "FirstResponderKit");
            var (version, date) = CommunityToolVersionChecker.ParseVersion(family, "SET NOCOUNT ON;\r\n\tSELECT @Version = '8.34', @VersionDate = '20260702';\r\n");
            Assert.AreEqual("8.34", version);
            Assert.AreEqual("20260702", date);
        }

        [TestMethod]
        public void ParseVersion_DarlingDataAcrossLines()
        {
            var family = Manifest.Families.Single(f => f.Name == "DarlingData");
            var (version, date) = CommunityToolVersionChecker.ParseVersion(family, "SELECT\r\n        @version = '3.10',\r\n        @version_date = '20261001';");
            Assert.AreEqual("3.10", version);
            Assert.AreEqual("20261001", date);
        }

        [TestMethod]
        public void ParseVersion_WhoIsActiveOldAndNewFormats()
        {
            var family = Manifest.Families.Single(f => f.Name == "WhoIsActive");
            Assert.AreEqual("2200.20260409", CommunityToolVersionChecker.ParseVersion(family, "/*\r\nWho Is Active? v2200.20260409\r\n").version);
            Assert.AreEqual("12.00", CommunityToolVersionChecker.ParseVersion(family, "/*********\r\nWho Is Active? v12.00 (2021-11-10)\r\n").version);
        }

        [TestMethod]
        public void ParseVersion_NoMatch()
        {
            var family = Manifest.Families.Single(f => f.Name == "SQLStudies");
            Assert.AreEqual((null, null), CommunityToolVersionChecker.ParseVersion(family, "CREATE PROCEDURE dbo.sp_SrvPermissions AS PRINT 'Stub';"));
        }

        [TestMethod]
        public void CompareToPin_PrefersDates()
        {
            var pin = new CommunityToolFile { Version = "8.34", VersionDate = "20260702" };
            Assert.AreEqual(-1, CommunityToolVersionChecker.CompareToPin("8.40", "20250101", pin), "Date wins over version");
            Assert.AreEqual(0, CommunityToolVersionChecker.CompareToPin("8.34", "20260702", pin));
            Assert.AreEqual(1, CommunityToolVersionChecker.CompareToPin("8.35", "20260801", pin));
        }

        [TestMethod]
        public void CompareToPin_FallsBackToVersions()
        {
            var pin = new CommunityToolFile { Version = "1219.20260409" };
            Assert.AreEqual(-1, CommunityToolVersionChecker.CompareToPin("12.00", null, pin));
            Assert.AreEqual(0, CommunityToolVersionChecker.CompareToPin("1219.20260409", null, pin));
            Assert.IsNull(CommunityToolVersionChecker.CompareToPin(null, null, pin));
            Assert.IsNull(CommunityToolVersionChecker.CompareToPin("beta", null, pin));
        }

        [TestMethod]
        public void GetVersionInfo_Statuses()
        {
            var tool = Manifest.GetTool("sp_Blitz");
            Assert.AreEqual(CommunityToolStatus.NotInstalled, CommunityToolVersionChecker.GetVersionInfo(tool, Instance(16), "master", null, null).Status);
            Assert.AreEqual(CommunityToolStatus.NotSupported, CommunityToolVersionChecker.GetVersionInfo(tool, Instance(12), "master", null, null).Status);
            Assert.AreEqual(CommunityToolStatus.Unknown, CommunityToolVersionChecker.GetVersionInfo(tool, Instance(16), "master", "", null).Status, "Encrypted - definition not visible");
            var outdated = CommunityToolVersionChecker.GetVersionInfo(tool, Instance(16), "master", "SELECT @Version = '8.12', @VersionDate = '20230101';", "A note");
            Assert.AreEqual(CommunityToolStatus.Outdated, outdated.Status);
            Assert.AreEqual("8.12", outdated.InstalledVersion);
            Assert.AreEqual("8.34", outdated.ExpectedVersion);
            Assert.AreEqual("A note", outdated.Note);
        }

        [TestMethod]
        public void GetTargetDatabase()
        {
            Assert.AreEqual("UserDB", CommunityToolDeployer.GetTargetDatabase(Instance(16, database: "UserDB"), null), "Connection's database by default");
            Assert.AreEqual("DBA", CommunityToolDeployer.GetTargetDatabase(Instance(16), "DBA"));
            Assert.AreEqual("UserDB", CommunityToolDeployer.GetTargetDatabase(Instance(12, DatabaseEngineEdition.SqlDatabase, "UserDB"), null));
            Assert.ThrowsExactly<InvalidOperationException>(() => CommunityToolDeployer.GetTargetDatabase(Instance(12, DatabaseEngineEdition.SqlDatabase), null));
            Assert.ThrowsExactly<InvalidOperationException>(() => CommunityToolDeployer.GetTargetDatabase(Instance(12, DatabaseEngineEdition.SqlDatabase, "UserDB"), "OtherDB"));
        }

        [TestMethod]
        public void WithCredentials_ReplacesOnlyCredentials()
        {
            const string sqlAuth = "Data Source=SQL1;Initial Catalog=DBA;User ID=monitor;Password=secret;Encrypt=True;Application Name=DBADash";

            var windows = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(CommunityToolDeployer.WithCredentials(sqlAuth, "Data Source=Other;Integrated Security=True"));
            Assert.IsTrue(windows.IntegratedSecurity);
            Assert.AreEqual("", windows.UserID);
            Assert.AreEqual("", windows.Password);
            Assert.AreEqual("SQL1", windows.DataSource, "Server comes from the instance, not the credentials");
            Assert.AreEqual("DBA", windows.InitialCatalog);
            Assert.IsTrue(windows.Encrypt == Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Mandatory);

            var admin = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(CommunityToolDeployer.WithCredentials("Data Source=SQL2;Integrated Security=True", "User ID=admin;Password=pw"));
            Assert.IsFalse(admin.IntegratedSecurity);
            Assert.AreEqual("admin", admin.UserID);
            Assert.AreEqual("pw", admin.Password);
            Assert.AreEqual("SQL2", admin.DataSource);
        }

        [TestMethod]
        public void Split_OnGoLines()
        {
            var batches = SqlBatchSplitter.Split("SET ANSI_NULLS ON;\r\nGO\r\nCREATE PROC a AS SELECT 1\r\ngo -- trailing comment\r\n  GO  \r\nSELECT 2");
            Assert.AreEqual(3, batches.Count, "Empty batch between consecutive GOs is dropped");
            StringAssert.StartsWith(batches[1], "CREATE PROC a");
            StringAssert.StartsWith(batches[2], "SELECT 2");
        }

        [TestMethod]
        public void Split_IgnoresGoInCommentsAndStrings()
        {
            const string script = "/* header\nGO\n /* nested\nGO\n*/\nGO\n*/\nSELECT 'it''s\nGO\nstill a string'\n-- GO\nGOTO Done\nGO\nSELECT 2\n";
            var batches = SqlBatchSplitter.Split(script);
            Assert.AreEqual(2, batches.Count);
            StringAssert.Contains(batches[0], "still a string");
            StringAssert.Contains(batches[0], "GOTO Done");
        }

        [TestMethod]
        public void Split_GoCountRepeatsBatch()
        {
            var batches = SqlBatchSplitter.Split("INSERT INTO T DEFAULT VALUES\nGO 3\n");
            Assert.AreEqual(3, batches.Count);
        }

        [TestMethod]
        public void IsUseOnlyBatch()
        {
            Assert.IsTrue(SqlBatchSplitter.IsUseOnlyBatch("USE master\r\n"));
            Assert.IsTrue(SqlBatchSplitter.IsUseOnlyBatch("/* comment */ USE [master];\r\n-- comment"));
            Assert.IsFalse(SqlBatchSplitter.IsUseOnlyBatch("USE master\r\nSELECT 1"));
            Assert.IsFalse(SqlBatchSplitter.IsUseOnlyBatch("EXEC('USE [?];')"));
        }

        [TestMethod]
        public async Task FolderSource_FindsRepositoryLayoutAndFlatFiles()
        {
            var folder = Path.Combine(Path.GetTempPath(), "DBADashCommunityTools_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(folder, "sp_HumanEvents"));
                var bytes = Encoding.UTF8.GetBytes("SELECT 1");
                await File.WriteAllBytesAsync(Path.Combine(folder, "sp_HumanEvents", "sp_HumanEvents.sql"), bytes);
                await File.WriteAllTextAsync(Path.Combine(folder, "sp_Blitz.sql"), "SELECT 2");
                await File.WriteAllTextAsync(Path.Combine(folder, "sp_WhoIsActive.sql"), "SELECT 3");

                var source = new FolderScriptSource(folder);
                var humanEvents = Manifest.GetTool("sp_HumanEvents");
                var script = await source.GetScriptAsync(humanEvents, humanEvents.Files[0]);
                Assert.AreEqual("SELECT 1", script.Text);
                Assert.AreEqual(CommunityToolScriptSource.ComputeSha256(bytes), script.Sha256);
                Assert.IsFalse(script.MatchesPin);

                var blitz = Manifest.GetTool("sp_Blitz");
                Assert.AreEqual("SELECT 2", (await source.GetScriptAsync(blitz, blitz.Files[0])).Text);

                // Root build of sp_WhoIsActive is at the repository root, so a flat folder works for it...
                var whoIsActive = Manifest.GetTool("sp_WhoIsActive");
                Assert.AreEqual("SELECT 3", (await source.GetScriptAsync(whoIsActive, whoIsActive.Files[0])).Text);
                // ...but the 2019 build shares its file name, so it needs the repository layout.
                await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => source.GetScriptAsync(whoIsActive, whoIsActive.Files[1]));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [TestMethod]
        public void ManifestUpdater_ChangesOnlyTheUpdatedValues()
        {
            var json = Utility.GetResourceString("DBADash.CommunityTools.CommunityToolsManifest.json");
            var manifest = CommunityToolsManifest.Parse(json);
            var frk = manifest.Families.Single(f => f.Name == "FirstResponderKit");
            var whoIsActive = manifest.Families.Single(f => f.Name == "WhoIsActive");
            var blitz = frk.Tools.Single(t => t.Name == "sp_Blitz");
            var who = whoIsActive.Tools.Single();
            var newHash = new string('a', 64);
            var updates = new[]
            {
                new CommunityToolFamilyUpdate
                {
                    Family = frk, NewRef = "20270101",
                    Files = { new CommunityToolFileUpdate { Tool = blitz, File = blitz.Files[0], NewVersion = "9.0", NewVersionDate = "20270101", NewSha256 = newHash } }
                },
                new CommunityToolFamilyUpdate
                {
                    // WhoIsActive has no VersionDate, and its file paths overlap ("sp_WhoIsActive.sql" vs "2019/sp_WhoIsActive.sql")
                    Family = whoIsActive, NewRef = "20270202",
                    Files = { new CommunityToolFileUpdate { Tool = who, File = who.Files[1], NewVersion = "1220.20270202", NewSha256 = newHash.Replace('a', 'b') } }
                },
                new CommunityToolFamilyUpdate { Family = manifest.Families.Single(f => f.Name == "DarlingData"), Error = "failed" }
            };

            var updated = CommunityToolsManifest.Parse(CommunityToolsManifestUpdater.Apply(json, updates));

            var newFrk = updated.Families.Single(f => f.Name == "FirstResponderKit");
            Assert.AreEqual("20270101", newFrk.Ref);
            var newBlitz = newFrk.Tools.Single(t => t.Name == "sp_Blitz").Files[0];
            Assert.AreEqual(("9.0", "20270101", newHash), (newBlitz.Version, newBlitz.VersionDate, newBlitz.Sha256));
            Assert.AreEqual(frk.Tools[1].Files[0].Sha256, newFrk.Tools[1].Files[0].Sha256);

            var newWho = updated.Families.Single(f => f.Name == "WhoIsActive");
            Assert.AreEqual("20270202", newWho.Ref);
            Assert.AreEqual("1220.20270202", newWho.Tools[0].Files[1].Version);
            Assert.IsNull(newWho.Tools[0].Files[1].VersionDate);
            Assert.AreEqual(who.Files[0].Sha256, newWho.Tools[0].Files[0].Sha256);

            Assert.AreEqual(manifest.Families.Single(f => f.Name == "DarlingData").Ref, updated.Families.Single(f => f.Name == "DarlingData").Ref);
        }

        [TestMethod]
        public void CompareParameters_FlagsRemovedAndRetypedParametersTheGuiUses()
        {
            var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllText(Path.Combine(folder, "sp_Test.cs"), "new Param { ParamName = \"@sort_order\", ParamType = \"VARCHAR\" }");
                var before = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>
                {
                    ["sp_Test"] = new() { ["@sort_order"] = "varchar", ["@top"] = "int", ["@debug"] = "bit" },
                    ["sp_NotDeployed"] = new()
                };
                var after = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>
                {
                    ["sp_Test"] = new() { ["@top"] = "bigint", ["@debug"] = "bit", ["@new_option"] = "bit" },
                    ["sp_NotDeployed"] = new()
                };

                var summary = DBADashConfig.CommunityToolsHelper.CompareParameters(before, after, folder, new[] { "- sp_Failed: not compared" });

                StringAssert.Contains(summary, "`@sort_order` removed **- used by the GUI**");
                StringAssert.Contains(summary, "`@top` changed from int to bigint\r\n".Replace("\r\n", Environment.NewLine));
                StringAssert.Contains(summary, "new parameters `@new_option`");
                Assert.IsFalse(summary.Contains("@debug"));
                Assert.IsFalse(summary.Contains("sp_NotDeployed"));
                StringAssert.Contains(summary, "- sp_Failed: not compared");
                Assert.IsFalse(summary.Contains("None"));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [TestMethod]
        public void ManifestUpdater_UpdatesTheRightFamilyWhenRefsAreShared()
        {
            // First Responder Kit and sp_WhoIsActive both tag releases yyyyMMdd, so they can be pinned to the same Ref.
            var json = Utility.GetResourceString("DBADash.CommunityTools.CommunityToolsManifest.json")
                .Replace("\"Ref\": \"20260409\"", "\"Ref\": \"20260708\"");
            var manifest = CommunityToolsManifest.Parse(json);
            var whoIsActive = manifest.Families.Single(f => f.Name == "WhoIsActive");
            var who = whoIsActive.Tools.Single();
            var update = new CommunityToolFamilyUpdate
            {
                Family = whoIsActive, NewRef = "20270202",
                Files = { new CommunityToolFileUpdate { Tool = who, File = who.Files[0], NewVersion = "2201.20270202", NewSha256 = new string('c', 64) } }
            };

            var updated = CommunityToolsManifest.Parse(CommunityToolsManifestUpdater.Apply(json, new[] { update }));

            Assert.AreEqual("20260708", updated.Families.Single(f => f.Name == "FirstResponderKit").Ref);
            Assert.AreEqual("20270202", updated.Families.Single(f => f.Name == "WhoIsActive").Ref);
        }

        [TestMethod]
        public void ManifestUpdater_EscapesUpstreamMarkdown()
        {
            Assert.AreEqual(@"1\|2 \[x\]\(http://y\) \`z\`", CommunityToolsManifestUpdater.Escape("1|2 [x](http://y)\n`z`"));
        }

        [TestMethod]
        public void Locate_FollowsSpLookupOrder()
        {
            var lookup = new[] { "UserDB", "master" };

            // Default: the copy DBA Dash runs - the connection's database, then master
            Assert.AreEqual(new CommunityToolLocation("UserDB", null), CommunityToolLocator.Locate(null, lookup, Array.Empty<string>()));
            Assert.AreEqual(new CommunityToolLocation("master", null), CommunityToolLocator.Locate(null, lookup, new[] { "master" }));
            Assert.AreEqual(new CommunityToolLocation("UserDB", "Also in master, which isn't used"), CommunityToolLocator.Locate(null, lookup, new[] { "MASTER", "userdb" }));

            // Requested database
            Assert.AreEqual(new CommunityToolLocation("DBA", null), CommunityToolLocator.Locate("DBA", lookup, Array.Empty<string>()));
            Assert.AreEqual(new CommunityToolLocation("DBA", "DBA Dash uses the copy in master"), CommunityToolLocator.Locate("DBA", lookup, new[] { "master" }));
            Assert.AreEqual(new CommunityToolLocation("master", "The copy in UserDB is used instead"), CommunityToolLocator.Locate("master", lookup, new[] { "master", "UserDB" }));
            Assert.AreEqual(new CommunityToolLocation("DBA", "DBA Dash won't find this copy - it connects to UserDB"), CommunityToolLocator.Locate("DBA", lookup, new[] { "DBA" }));
            Assert.AreEqual(new CommunityToolLocation("UserDB", null), CommunityToolLocator.Locate("UserDB", lookup, new[] { "UserDB", "master" }));
        }

        [TestMethod]
        public void LookupOrder_MasterOnlyWhereSqlServerSearchesIt()
        {
            CollectionAssert.AreEqual(new[] { "UserDB", "master" }, CommunityToolLocator.LookupOrder(Instance(16), "UserDB"));
            CollectionAssert.AreEqual(new[] { "master" }, CommunityToolLocator.LookupOrder(Instance(16), "master"));
            CollectionAssert.AreEqual(new[] { "UserDB" }, CommunityToolLocator.LookupOrder(Instance(12, DatabaseEngineEdition.SqlDatabase, "UserDB"), "UserDB"));
        }
    }
}
