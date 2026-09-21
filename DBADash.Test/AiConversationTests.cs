using System;
using System.Collections.Generic;
using System.Linq;
using DBADashAI.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The rules a follow-up conversation has to obey before it is sent to a provider.
    ///
    /// These matter more than most validation.  The service holds no conversation state, so the
    /// transcript arrives from whoever posted the request; a malformed one is either rejected by the
    /// provider outright or - worse - accepted, and answered as though the reader had said something
    /// they did not.
    /// </summary>
    [TestClass]
    public class AiConversationTests
    {
        private static AiConversationTurn Assistant(string text = "Because of the key lookup.") =>
            new() { Role = AiConversationTurn.Assistant, Content = text };

        private static AiConversationTurn User(string text = "Why?") =>
            new() { Role = AiConversationTurn.User, Content = text };

        [TestMethod]
        public void FirstAnalysis_HasNoHistoryAndNeedsNoQuestion()
        {
            Assert.IsNull(AiConversation.Validate(new List<AiConversationTurn>(), null));
        }

        [TestMethod]
        public void History_MustStartWithTheAnswerToTheArtifact()
        {
            // The opening user message is the artifact, which the service builds rather than receives,
            // so what the caller sends starts with the answer to it.
            var history = new List<AiConversationTurn> { User(), Assistant() };

            StringAssert.Contains(AiConversation.Validate(history, "And then?"), "turn 1");
        }

        [TestMethod]
        public void History_MustAlternate()
        {
            var history = new List<AiConversationTurn> { Assistant(), Assistant() };

            StringAssert.Contains(AiConversation.Validate(history, "And then?"), "turn 2");
        }

        [TestMethod]
        public void History_MustEndWithAnAnswer()
        {
            var history = new List<AiConversationTurn> { Assistant(), User() };

            StringAssert.Contains(AiConversation.Validate(history, "And then?"), "never answered");
        }

        [TestMethod]
        public void FollowUp_NeedsAQuestion()
        {
            var history = new List<AiConversationTurn> { Assistant() };

            StringAssert.Contains(AiConversation.Validate(history, "   "), "follow-up question is required");
        }

        [TestMethod]
        public void Conversation_IsCappedByTurnsAndBySize()
        {
            var tooMany = Enumerable.Range(0, AiConversation.MaxTurns + 1)
                .Select(i => i % 2 == 0 ? Assistant() : User())
                .ToList();

            StringAssert.Contains(AiConversation.Validate(tooMany, "And then?"), "turns");

            var tooBig = new List<AiConversationTurn> { Assistant(new string('x', AiConversation.MaxHistoryLength + 1)) };
            StringAssert.Contains(AiConversation.Validate(tooBig, "And then?"), "characters");

            var longQuestion = new string('x', AiConversation.MaxQuestionLength + 1);
            StringAssert.Contains(AiConversation.Validate(new List<AiConversationTurn>(), longQuestion), "question exceeds");
        }

        [TestMethod]
        public void Build_PutsTheArtifactFirstAndTheQuestionLast()
        {
            var history = new List<AiConversationTurn> { Assistant("First answer."), User("Why?"), Assistant("Because.") };

            var messages = AiConversation.Build("THE PLAN", history, "What would fix it?");

            Assert.AreEqual(5, messages.Count);
            Assert.AreEqual("THE PLAN", messages[0].Content);
            Assert.IsTrue(messages[0].IsUser);
            Assert.AreEqual("What would fix it?", messages[^1].Content);
            Assert.IsTrue(messages[^1].IsUser);

            // Providers reject anything that does not alternate, and so does the validation above.
            for (var i = 1; i < messages.Count; i++)
            {
                Assert.AreNotEqual(messages[i - 1].IsUser, messages[i].IsUser, $"Turn {i} repeats the previous speaker.");
            }
        }

        [TestMethod]
        public void Build_WithNothingSaidYetIsAPlainFirstAnalysis()
        {
            var messages = AiConversation.Build("THE DEADLOCK", new List<AiConversationTurn>(), null);

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("THE DEADLOCK", messages[0].Content);
        }

        [TestMethod]
        public void DeadlockRequest_ValidatesItsConversationAsWellAsItsGraph()
        {
            var request = new AiDeadlockAnalysisRequest
            {
                GraphXml = "<deadlock/>",
                History = new List<AiConversationTurn> { Assistant(), User() },
                Question = "And then?"
            };

            StringAssert.Contains(request.Validate(), "never answered");

            request.History = new List<AiConversationTurn> { Assistant() };
            Assert.IsNull(request.Validate());
        }

        [TestMethod]
        public void PlanRequest_RequiresAStatementAndChecksBothIdentities()
        {
            var request = new AiPlanAnalysisRequest();
            StringAssert.Contains(request.Validate(), "StatementText is required");

            request.StatementText = "SELECT 1";
            request.Signature = "0x0123456789abcdef";
            Assert.IsNull(request.Validate());

            request.PlanHash = "not a hash";
            StringAssert.Contains(request.Validate(), "PlanHash");

            // A trailing newline must not get through the check and into the log.
            request.PlanHash = "0x0123456789abcdef\n";
            StringAssert.Contains(request.Validate(), "PlanHash");
        }

        [TestMethod]
        public void PlanRequest_AcceptsAPlanWithNoXmlAtAll()
        {
            // The viewer sends the summary alone for a plan too large to send, or when the reader
            // switched the XML off - neither is an error.
            var request = new AiPlanAnalysisRequest
            {
                StatementText = "SELECT 1",
                PlanXml = null,
                Operators = new List<string> { "node 1 Clustered Index Scan, 98% of cost" }
            };

            Assert.IsNull(request.Validate());
        }

        [TestMethod]
        public void PlanRequest_TakesAPlanLargerThanTheViewerWarnsAbout()
        {
            // The viewer warns at half a megabyte and sends anyway when the reader says so, because
            // whether a plan that size fits is the configured model's business rather than this check's.
            // A limit here at the size the warning appears would take that choice away.
            var request = new AiPlanAnalysisRequest
            {
                StatementText = "SELECT 1",
                PlanXml = new string('x', 1024 * 1024)
            };

            Assert.IsNull(request.Validate());

            // Still a ceiling, because a request arrives over the network and into memory.
            request.PlanXml = new string('x', AiPlanAnalysisRequest.MaxPlanXmlLength + 1);
            StringAssert.Contains(request.Validate(), "PlanXml");
        }
    }
}
