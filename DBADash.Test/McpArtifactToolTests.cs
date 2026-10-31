using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DBADash.PerformanceAnalysis;
using DBADashAI.Mcp;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The MCP tools that fetch one deadlock, plan or instance's performance, and the prompts that drive them.
    /// What they return is the data half of the AI service's own prompts, so those are checked to still be
    /// the same prompt with the data half split out.
    /// </summary>
    [TestClass]
    public class McpArtifactToolTests
    {
        private static readonly IReadOnlySet<string> Known = new HashSet<string> { "id", "when", "hash", "flag", "name" };

        private static RepositoryMcpTool.Arguments Args(string json) =>
            new(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json), Known);

        [TestMethod]
        public void Arguments_ReadEachType()
        {
            var args = Args("""{"id":5,"when":"2026-10-23T14:07:12.345","hash":"0xab12","flag":true,"name":" SQL01 "}""");

            Assert.AreEqual(5, args.Int("id", 1, 10));
            Assert.AreEqual(new DateTime(2026, 10, 23, 14, 7, 12, 345, DateTimeKind.Utc), args.UtcDateTime("when"));
            Assert.AreEqual(DateTimeKind.Utc, args.UtcDateTime("when")!.Value.Kind);
            Assert.AreEqual("0xAB12", args.Hex("hash", 2));
            Assert.IsTrue(args.Bool("flag"));
            Assert.AreEqual("SQL01", args.String("name"));
        }

        [TestMethod]
        public void Arguments_ConvertAnOffsetToUtc()
        {
            Assert.AreEqual(new DateTime(2026, 10, 23, 13, 0, 0, DateTimeKind.Utc),
                Args("""{"when":"2026-10-23T14:00:00+01:00"}""").UtcDateTime("when"));
        }

        [TestMethod]
        public void Arguments_MissingOptionalAreNull()
        {
            var args = Args("{}");

            Assert.IsNull(args.Int("id", 1, 10));
            Assert.IsNull(args.UtcDateTime("when"));
            Assert.IsNull(args.Hex("hash", 2));
            Assert.IsNull(args.Bool("flag"));
            Assert.IsNull(args.String("name"));
        }

        [TestMethod]
        [DataRow("""{"other":1}""", "id")]
        [DataRow("{}", "id")]
        [DataRow("""{"id":0}""", "id")]
        [DataRow("""{"hash":"0xab1"}""", "hash")]
        [DataRow("""{"hash":"0xzz12"}""", "hash")]
        [DataRow("""{"when":"yesterday"}""", "when")]
        [DataRow("""{"flag":"yes"}""", "flag")]
        public void Arguments_RefuseBadValuesWithAReason(string json, string name)
        {
            var ex = Assert.Throws<McpToolException>(() =>
            {
                var args = Args(json);
                switch (name)
                {
                    case "id": args.Int("id", 1, 10, required: true); break;
                    case "hash": args.Hex("hash", 2, required: true); break;
                    case "when": args.UtcDateTime("when", required: true); break;
                    case "flag": args.Bool("flag"); break;
                }
            });

            Assert.IsFalse(string.IsNullOrWhiteSpace(ex.Message));
        }

        [TestMethod]
        public void DeadlockPrompt_IsInstructionsThenData()
        {
            var request = new AiDeadlockAnalysisRequest
            {
                GraphXml = "<deadlock/>",
                Signature = "0x0102030405060708",
                Instance = "SQL01",
                Participants = ["spid 51 (victim)"],
                Findings = ["Warning: something"]
            };

            var prompt = new AiDeadlockPromptBuilder().Build(request);
            var data = AiDeadlockPromptBuilder.Data(request);

            Assert.EndsWith(data, prompt);
            foreach (var section in AiDeadlockPromptBuilder.AnswerSections)
            {
                Assert.Contains(section, prompt);
            }

            Assert.Contains("Findings already established locally", data);
            Assert.Contains("<deadlock/>", data);
            Assert.DoesNotContain("follow-up", data);
        }

        [TestMethod]
        public void PlanPrompt_IsInstructionsThenData()
        {
            var request = new AiPlanAnalysisRequest
            {
                StatementText = "SELECT 1",
                Signature = "0x01",
                PlanHash = "0x02",
                Operators = ["Clustered Index Scan 90%"]
            };

            var prompt = new AiPlanPromptBuilder().Build(request);
            var data = AiPlanPromptBuilder.Data(request);

            Assert.EndsWith(data, prompt);
            Assert.Contains("SELECT 1", data);
            Assert.Contains("The plan XML itself was not supplied", data);
        }

        [TestMethod]
        public void PerformancePrompt_IsInstructionsThenData()
        {
            var sections = new[] { ("CPU", "avg 10%") };
            var prompt = PerformanceAnalysisPrompt.Build(false, "SQL01", "last 24h", null, ["a note"], sections);

            Assert.StartsWith(PerformanceAnalysisPrompt.Instructions(false), prompt);
            Assert.EndsWith(PerformanceAnalysisPrompt.Data("SQL01", "last 24h", null, ["a note"], sections), prompt);
        }

        private static string Render(string name, bool guiContext, object? args = null)
        {
            var prompt = AnalysisMcpPrompt.All(guiContext).Single(p => p.ProtocolPrompt.Name == name);
            var values = args is null
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(args));
            return prompt.Render(values);
        }

        private static void AssertInOrder(string text, params string[] parts)
        {
            var at = -1;
            foreach (var part in parts)
            {
                var next = text.IndexOf(part, at + 1, StringComparison.Ordinal);
                Assert.IsGreaterThan(at, next, $"'{part}' should come after what precedes it in:\n{text}");
                at = next;
            }
        }

        [TestMethod]
        public void LocalDeadlockPrompt_WithoutArguments_StartsFromTheGui()
        {
            AssertInOrder(Render("analyse-deadlock", guiContext: true),
                "get-open-deadlock", "get-gui-context", "list-deadlocks", "list-deadlocks without instance");
        }

        [TestMethod]
        public void LocalPlanPrompt_WithoutArguments_StartsFromTheGui()
        {
            AssertInOrder(Render("analyse-query-plan", guiContext: true),
                "get-open-query-plan", "get-gui-context", "running-queries-summary", "Across every instance");
        }

        [TestMethod]
        public void LocalPerformancePrompt_WithoutAnInstance_UsesTheGui()
        {
            StringAssert.Contains(Render("analyse-performance", guiContext: true), "get-gui-context");
            Assert.IsFalse(AnalysisMcpPrompt.All(true).Single(p => p.ProtocolPrompt.Name == "analyse-performance")
                .ProtocolPrompt.Arguments!.Single(a => a.Name == "instance").Required);
        }

        [TestMethod]
        [DataRow("analyse-deadlock")]
        [DataRow("analyse-query-plan")]
        [DataRow("analyse-performance")]
        public void Prompts_GivenAnInstance_DoNotLookAtTheGui(string name)
        {
            var text = Render(name, guiContext: true, new { instance = "SQL01" });

            Assert.DoesNotContain("get-open-", text);
            Assert.DoesNotContain("get-gui-context", text);
            StringAssert.Contains(text, "SQL01");
        }

        [TestMethod]
        [DataRow("analyse-deadlock")]
        [DataRow("analyse-query-plan")]
        [DataRow("analyse-performance")]
        public void SharedPrompts_NeverNameTheGuiTools(string name)
        {
            var text = Render(name, guiContext: false);

            Assert.DoesNotContain("get-open-", text);
            Assert.DoesNotContain("get-gui-context", text);
        }

        [TestMethod]
        public void DeadlockPrompt_WithAKey_FetchesThatDeadlock()
        {
            var text = Render("analyse-deadlock", guiContext: true,
                new { instanceId = "3", eventTime = "2026-10-23T14:07:12.345", deadlockHash = "0xAB" });

            StringAssert.Contains(text, "get-deadlock tool: instanceId 3, eventTime 2026-10-23T14:07:12.345, deadlockHash 0xAB");
            Assert.DoesNotContain("get-open-deadlock", text);
        }

        [TestMethod]
        public void LocalPrompts_GivenAFile_DescribeIt()
        {
            var deadlock = Render("analyse-deadlock", guiContext: true, new { path = @"C:\ws\artifacts\deadlock-0xab.xdl", instance = "SQL01" });
            var plan = Render("analyse-query-plan", guiContext: true, new { path = @"C:\ws\artifacts\plan.sqlplan", statement = "2" });

            StringAssert.Contains(deadlock, @"describe-deadlock-graph tool: path C:\ws\artifacts\deadlock-0xab.xdl, instance SQL01 (for the object definitions)");
            Assert.DoesNotContain("get-open-deadlock", deadlock);
            StringAssert.Contains(plan, @"describe-query-plan tool: path C:\ws\artifacts\plan.sqlplan, statement 2");
            Assert.DoesNotContain("get-open-query-plan", plan);
        }

        [TestMethod]
        public void SharedPrompts_DontOfferAPath()
        {
            foreach (var prompt in AnalysisMcpPrompt.All(guiContext: false))
            {
                Assert.IsFalse(prompt.ProtocolPrompt.Arguments!.Any(a => a.Name == "path"), prompt.ProtocolPrompt.Name);
            }
        }

        [TestMethod]
        public void PerformancePrompt_TakesAnEndTime()
        {
            var text = Render("analyse-performance", guiContext: true, new { instance = "SQL01", hoursBack = "6", to = "2026-10-31T12:00:00" });

            StringAssert.Contains(text, "get-performance-summary tool: instance SQL01, hoursBack 6, to 2026-10-31T12:00:00");
        }

        [TestMethod]
        public void Prompts_NameTheToolsTheyNeed()
        {
            var prompts = AnalysisMcpPrompt.All().ToDictionary(p => p.ProtocolPrompt.Name);

            CollectionAssert.AreEquivalent(
                new[] { "analyse-deadlock", "analyse-query-plan", "analyse-performance" },
                prompts.Keys.ToList());

            foreach (var prompt in prompts.Values)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.ProtocolPrompt.Description));
                Assert.IsNotNull(prompt.ProtocolPrompt.Arguments);
            }
        }
    }
}
