using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DBADash.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Registering DBA Dash's MCP server with AI tools by editing their config files.  These are other applications'
    /// files: what else is in them has to survive, a backup has to be left, and a registration from another copy of
    /// DBA Dash has to be recognised as one.  Everything here works in a scratch folder.
    /// </summary>
    [TestClass]
    public class McpClientRegistrationTests
    {
        private string _folder = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "dbadash-mcp-reg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }

        private string ConfigPath => Path.Combine(_folder, "cursor", "mcp.json");

        private JsonFileMcpClient Cursor(bool installed = true) =>
            new("Cursor", ConfigPath, "mcpServers", includeType: false, installed, "Restart it.");

        private static McpServerEntry Entry(string connection = @"SQL01\DBADashDB") =>
            McpServerEntry.ForConnection(@"C:\Program Files\DBA Dash\DBADashMCP.exe", connection);

        [TestMethod]
        public async Task Register_CreatesTheFile_WhenThereIsNone()
        {
            await Cursor().RegisterAsync(Entry(), CancellationToken.None);

            var server = JsonNode.Parse(File.ReadAllText(ConfigPath))!["mcpServers"]!["dbadash"]!;
            Assert.AreEqual(@"C:\Program Files\DBA Dash\DBADashMCP.exe", server["command"]!.GetValue<string>());
            CollectionAssert.AreEqual(new[] { "--connection", @"SQL01\DBADashDB" }, server["args"]!.AsArray().Select(a => a!.GetValue<string>()).ToArray());
            Assert.IsNull(server["type"], "Cursor's format has no type.");
            Assert.AreEqual(McpClientState.Registered, Cursor().GetStatus(Entry()).State);
        }

        [TestMethod]
        public async Task Register_KeepsOtherServersAndSettings_AndBacksUp()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            const string original = """{ "mcpServers": { "github": { "command": "gh-mcp" } }, "theme": "dark" }""";
            File.WriteAllText(ConfigPath, original);

            var message = await Cursor().RegisterAsync(Entry(), CancellationToken.None);

            var root = JsonNode.Parse(File.ReadAllText(ConfigPath))!;
            Assert.AreEqual("gh-mcp", root["mcpServers"]!["github"]!["command"]!.GetValue<string>());
            Assert.AreEqual("dark", root["theme"]!.GetValue<string>());
            Assert.IsNotNull(root["mcpServers"]!["dbadash"]);
            Assert.AreEqual(original, File.ReadAllText(ConfigPath + ".dbadash-backup"));
            Assert.DoesNotContain("comments", message);
        }

        [TestMethod]
        public async Task Register_InAFileWithComments_SaysTheyWerentKept()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, "{\n  // my servers\n  \"servers\": {},\n}");
            var vsCode = new JsonFileMcpClient("VS Code", ConfigPath, "servers", includeType: true, installed: true, "Reload.");

            var message = await vsCode.RegisterAsync(Entry(), CancellationToken.None);

            StringAssert.Contains(message, "comments couldn't be kept");
            Assert.AreEqual("stdio", JsonNode.Parse(File.ReadAllText(ConfigPath))!["servers"]!["dbadash"]!["type"]!.GetValue<string>());
            StringAssert.Contains(File.ReadAllText(ConfigPath + ".dbadash-backup"), "// my servers");
        }

        [TestMethod]
        public async Task Status_RecognisesAnotherCopyOrConnection()
        {
            await Cursor().RegisterAsync(Entry(), CancellationToken.None);

            Assert.AreEqual(McpClientState.Registered, Cursor().GetStatus(Entry()).State);
            Assert.AreEqual(McpClientState.RegisteredDifferently, Cursor().GetStatus(Entry("Other")).State);
            Assert.AreEqual(McpClientState.RegisteredDifferently,
                Cursor().GetStatus(McpServerEntry.ForConnection(@"D:\Old\DBADashMCP.exe", @"SQL01\DBADashDB")).State);
        }

        [TestMethod]
        public async Task Unregister_RemovesOnlyDbaDash()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, """{ "mcpServers": { "github": { "command": "gh-mcp" } } }""");
            await Cursor().RegisterAsync(Entry(), CancellationToken.None);

            await Cursor().UnregisterAsync(CancellationToken.None);

            var servers = JsonNode.Parse(File.ReadAllText(ConfigPath))!["mcpServers"]!.AsObject();
            Assert.IsFalse(servers.ContainsKey("dbadash"));
            Assert.IsTrue(servers.ContainsKey("github"));
            Assert.AreEqual(McpClientState.NotRegistered, Cursor().GetStatus(Entry()).State);
        }

        [TestMethod]
        public void Status_OfAMissingClient_IsNotInstalled()
        {
            Assert.AreEqual(McpClientState.NotInstalled, Cursor(installed: false).GetStatus(Entry()).State);
        }

        [TestMethod]
        public void ClaudeCode_WithoutTheCli_IsNotInstalled_AndReadsItsConfigOtherwise()
        {
            var config = Path.Combine(_folder, ".claude.json");
            File.WriteAllText(config, """{ "mcpServers": { "dbadash": { "type": "stdio", "command": "C:\\Program Files\\DBA Dash\\DBADashMCP.exe", "args": ["--connection", "SQL01\\DBADashDB"] } } }""");
            var paths = new McpClientPaths(config, "", "", "", "", ClaudeCli: null);

            Assert.AreEqual(McpClientState.NotInstalled, new ClaudeCodeMcpClient(paths).GetStatus(Entry()).State);
            Assert.AreEqual(McpClientState.Registered, new ClaudeCodeMcpClient(paths with { ClaudeCli = "claude.exe" }).GetStatus(Entry()).State);
        }

        [TestMethod]
        public void ConfigJson_IsTheUsualMcpServersShape()
        {
            var json = JsonNode.Parse(Entry().ToConfigJson())!;

            Assert.AreEqual(@"C:\Program Files\DBA Dash\DBADashMCP.exe", json["mcpServers"]!["dbadash"]!["command"]!.GetValue<string>());
        }

        [TestMethod]
        public void NoConnection_MeansNoArguments()
        {
            Assert.IsEmpty(McpServerEntry.ForConnection(@"C:\x\DBADashMCP.exe", null).Args);
        }
    }
}
