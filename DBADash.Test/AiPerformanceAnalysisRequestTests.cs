using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// What the service accepts as a performance analysis, and the prompt it makes of one.  The sections
    /// arrive as text the reader approved, so the prompt has to carry them as they are.
    /// </summary>
    [TestClass]
    public class AiPerformanceAnalysisRequestTests
    {
        private static AiPerformanceAnalysisRequest Request(string focus = AiPerformanceAnalysisRequest.GeneralFocus) => new()
        {
            Instance = "SQL01",
            InstanceId = 1,
            Focus = focus,
            Window = "2026-10-17 12:00 to 2026-10-18 12:00 (UTC), 24 hours.",
            Baseline = "2026-10-10 12:00 to 2026-10-11 12:00 - the same window one week earlier.",
            Sections =
            [
                new() { Title = "Waits", Content = "wait_type | ms_per_sec\nWRITELOG | 40\n" },
                new() { Title = "CPU", Content = "period | sql_avg\nwindow | 25\n" }
            ]
        };

        [TestMethod]
        public void ValidRequest_IsAccepted()
        {
            Assert.IsNull(Request().Validate());
            Assert.IsNull(Request(AiPerformanceAnalysisRequest.LatchesAndSpinlocksFocus).Validate());
        }

        [TestMethod]
        public void NoSections_IsRejected()
        {
            var request = Request();
            request.Sections.Clear();

            StringAssert.Contains(request.Validate(), "at least one section");
        }

        [TestMethod]
        public void UnknownFocus_IsRejected()
        {
            StringAssert.Contains(Request("Everything").Validate(), "Focus");
        }

        [TestMethod]
        public void OversizedSections_AreRejectedRatherThanTruncated()
        {
            var request = Request();
            request.Sections.Add(new() { Title = "Metrics", Content = new string('x', AiPerformanceAnalysisRequest.MaxSectionsLength) });

            StringAssert.Contains(request.Validate(), "Leave out a section");
        }

        [TestMethod]
        public void FollowUp_FollowsTheConversationRules()
        {
            var request = Request();
            request.History = [new() { Role = AiConversationTurn.User, Content = "Why?" }];

            Assert.IsNotNull(request.Validate(), "History has to start with the answer to the figures");
        }

        [TestMethod]
        public void Prompt_CarriesEverySectionAsSent()
        {
            var request = Request();
            var prompt = new AiPerformancePromptBuilder().Build(request);

            foreach (var section in request.Sections)
            {
                StringAssert.Contains(prompt, $"## {section.Title}");
                StringAssert.Contains(prompt, section.Content.TrimEnd());
            }

            StringAssert.Contains(prompt, request.Window);
            StringAssert.Contains(prompt, request.Baseline);
        }

        [TestMethod]
        public void Prompt_WithoutBaseline_SaysThereIsNone()
        {
            var request = Request();
            request.Baseline = null;

            StringAssert.Contains(new AiPerformancePromptBuilder().Build(request), "Baseline: none");
        }

        [TestMethod]
        public void Prompt_LatchFocus_AsksAboutLatchesAndSpinlocks()
        {
            var general = new AiPerformancePromptBuilder().Build(Request());
            var latches = new AiPerformancePromptBuilder().Build(Request(AiPerformanceAnalysisRequest.LatchesAndSpinlocksFocus));

            StringAssert.Contains(latches, "2. Latches");
            StringAssert.Contains(latches, "3. Spinlocks");
            Assert.IsFalse(general.Contains("3. Spinlocks"));
        }

        [TestMethod]
        public void Request_ReadsTheShapeTheViewerSends()
        {
            // The viewer serialises camelCase with the focus as the enum's name; the service binds with the web defaults.
            const string json = """
                {
                  "instance": "SQL01",
                  "instanceId": 1,
                  "focus": "LatchesAndSpinlocks",
                  "window": "w",
                  "baseline": null,
                  "notes": ["n"],
                  "sections": [ { "title": "Latches", "content": "latch_class | wait_ms_per_sec" } ],
                  "payloadVersion": "1",
                  "modelOverride": null,
                  "conversationId": null,
                  "history": [],
                  "question": null
                }
                """;

            var request = JsonSerializer.Deserialize<AiPerformanceAnalysisRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            Assert.IsNull(request.Validate());
            Assert.IsTrue(request.IsLatchesAndSpinlocks);
            Assert.AreEqual("Latches", request.Sections.Single().Title);
            CollectionAssert.AreEqual(new List<string> { "n" }, request.Notes);
        }
    }
}
