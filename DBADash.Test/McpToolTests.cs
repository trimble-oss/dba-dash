using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DBADashAI.Mcp;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Server;

namespace DBADash.Test
{
    /// <summary>
    /// The repository tools offered over MCP.  The client's model fills in the arguments, so they are
    /// checked here rather than trusted, and a bad one comes back as an error the model can correct.
    /// </summary>
    [TestClass]
    public class McpToolTests
    {
        private static Dictionary<string, JsonElement> Args(object value) =>
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(value))!;

        [TestMethod]
        public void NoArguments_UseTheDefaults()
        {
            var (request, error) = AiToolMcpAdapter.ParseArguments("waits-summary", null);

            Assert.IsNull(error);
            Assert.IsNotNull(request);
            Assert.AreEqual(AiToolMcpAdapter.DefaultMaxRows, request.MaxRows);
            Assert.IsNull(request.InstanceFilter);
            Assert.IsNull(request.HoursBack);
            Assert.IsFalse(request.IncludeAiSummary);
            Assert.AreEqual("waits-summary", request.ToolName);
        }

        [TestMethod]
        public void Arguments_MapOntoTheRequest()
        {
            var (request, error) = AiToolMcpAdapter.ParseArguments("waits-summary",
                Args(new { instance = " SQL01 ", hoursBack = 48, maxRows = 10 }));

            Assert.IsNull(error);
            Assert.AreEqual("SQL01", request!.InstanceFilter);
            Assert.AreEqual(48, request.HoursBack);
            Assert.AreEqual(10, request.MaxRows);
        }

        [TestMethod]
        public void NullAndBlankArguments_AreLeftOut()
        {
            var (request, error) = AiToolMcpAdapter.ParseArguments("waits-summary",
                Args(new { instance = "  ", hoursBack = (int?)null }));

            Assert.IsNull(error);
            Assert.IsNull(request!.InstanceFilter);
            Assert.IsNull(request.HoursBack);
        }

        [TestMethod]
        [DataRow("{\"hoursBack\":0}")]
        [DataRow("{\"hoursBack\":1.5}")]
        [DataRow("{\"hoursBack\":\"24\"}")]
        [DataRow("{\"maxRows\":201}")]
        [DataRow("{\"maxRows\":0}")]
        [DataRow("{\"instance\":5}")]
        [DataRow("{\"question\":\"why\"}")]
        public void BadArguments_AreRefusedWithAReason(string json)
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            var (request, error) = AiToolMcpAdapter.ParseArguments("waits-summary", args);

            Assert.IsNull(request);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }

        [TestMethod]
        public void ToolTypes_CoverEveryTool()
        {
            var implemented = typeof(IAiTool).Assembly.GetTypes()
                .Where(t => typeof(IAiTool).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
                .ToHashSet();

            CollectionAssert.AreEquivalent(implemented.ToList(), AiToolRegistration.ToolTypes.Concat(McpToolRegistration.McpOnlyToolTypes).ToList());
            Assert.IsFalse(AiToolRegistration.ToolTypes.Intersect(McpToolRegistration.McpOnlyToolTypes).Any());
        }

        [TestMethod]
        public void Adapters_DescribeEveryToolWithoutTouchingTheRepository()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Repository"] = "Server=nowhere.invalid;Database=none;Connect Timeout=1"
                })
                .Build();

            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddAiTools()
                .AddMcpTools()
                .BuildServiceProvider();

            var tools = services.GetServices<McpServerTool>().Select(t => t.ProtocolTool).ToList();

            // The summary tools, the MCP-only ones, get-deadlock, get-query-plan, get-performance-summary, top-queries,
            // describe-deadlock-graph, describe-query-plan and the four performance lookups.
            Assert.HasCount(AiToolRegistration.ToolTypes.Count + McpToolRegistration.McpOnlyToolTypes.Count + 10, tools);
            Assert.HasCount(3, services.GetServices<McpServerPrompt>());
            Assert.AreEqual(tools.Count, tools.Select(t => t.Name).Distinct().Count(), "Tool names must be unique.");

            foreach (var tool in tools)
            {
                Assert.MatchesRegex(new Regex("^[a-zA-Z0-9_-]{1,64}$"), tool.Name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description), tool.Name);
                Assert.IsTrue(tool.Annotations?.ReadOnlyHint, tool.Name);
                Assert.AreEqual("object", tool.InputSchema.GetProperty("type").GetString());
            }
        }

        /// <summary>
        /// DBADashMCP.exe runs as the user, who needs McpUser to run what the tools call.  A new tool whose
        /// procedure isn't granted would fail for everyone but sysadmins.
        /// </summary>
        [TestMethod]
        public void McpUser_IsGrantedEveryToolProcedure()
        {
            var root = MessageAuthorizationTests.FindRepositoryRoot();
            var sources = Directory.GetFiles(Path.Combine(root, "DBADashAI.Tools", "Services", "Tools"), "*.cs")
                .Concat(Directory.GetFiles(Path.Combine(root, "DBADashAI.Tools", "Mcp"), "*.cs"))
                .Append(Path.Combine(root, "DBADash", "PerformanceAnalysis", "PerformanceAnalysisLoader.cs"))
                .Append(Path.Combine(root, "DBADashAI.Tools", "Services", "PerformanceLookups.cs"));
            var called = sources
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"""((?:AI|dbo)\.\w+_Get)""").Select(m => m.Groups[1].Value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var postDeploy = File.ReadAllText(Path.Combine(root, "DBADashDB", "Script.PostDeployment1.sql"));
            var granted = Regex.Matches(postDeploy, @"GRANT EXECUTE ON OBJECT::((?:AI|dbo)\.\w+) TO McpUser;")
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.IsNotEmpty(called);
            CollectionAssert.AreEquivalent(called.Order().ToList(), granted.Order().ToList());
        }
    }
}
