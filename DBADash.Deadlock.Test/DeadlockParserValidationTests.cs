using System;
using System.Linq;
using DBADash.Deadlock.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// Input validation and tolerance: what the parser rejects, what it accepts despite being odd,
    /// and how it fails when it does.
    /// </summary>
    [TestClass]
    public class DeadlockParserValidationTests
    {
        private const string ExecutionPlanXml =
            """
            <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.539">
              <BatchSequence />
            </ShowPlanXML>
            """;

        private const string MinimalDeadlockXml =
            """
            <deadlock>
              <victim-list />
              <process-list />
              <resource-list />
            </deadlock>
            """;

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("\r\n\t")]
        public void Parse_EmptyInput_Throws(string? xml)
        {
            Assert.ThrowsExactly<DeadlockParseException>(() => DeadlockParser.Parse(xml));
        }

        [TestMethod]
        public void Parse_MalformedXml_ThrowsWithInnerXmlException()
        {
            var ex = Assert.ThrowsExactly<DeadlockParseException>(
                () => DeadlockParser.Parse("<deadlock><process-list>"));

            StringAssert.Contains(ex.Message, "not well formed");
            Assert.IsInstanceOfType<System.Xml.XmlException>(ex.InnerException);
        }

        [TestMethod]
        public void Parse_XmlWithoutDeadlockElement_ThrowsNamingTheRoot()
        {
            var ex = Assert.ThrowsExactly<DeadlockParseException>(() => DeadlockParser.Parse(ExecutionPlanXml));

            StringAssert.Contains(ex.Message, "ShowPlanXML");
        }

        [TestMethod]
        public void TryParse_InvalidInput_ReturnsFalseAndEmptyList()
        {
            Assert.IsFalse(DeadlockParser.TryParse(ExecutionPlanXml, out var graphs));
            Assert.AreEqual(0, graphs.Count);
        }

        [TestMethod]
        public void TryParse_ValidInput_ReturnsTrue()
        {
            Assert.IsTrue(DeadlockParser.TryParse(TestGraphs.Load(TestGraphs.KeyLock), out var graphs));
            Assert.AreEqual(1, graphs.Count);
        }

        [TestMethod]
        public void IsDeadlockXml_AcceptsDeadlockListWrapper()
        {
            // The behaviour that a root-element check for "deadlock" gets wrong: this is what SSMS
            // writes when saving a .xdl, and it is a genuine deadlock graph.
            Assert.IsTrue(DeadlockParser.IsDeadlockXml(TestGraphs.Load(TestGraphs.DeadlockListWrapper)));
        }

        [TestMethod]
        public void IsDeadlockXml_AcceptsBareDeadlockAndEventEnvelope()
        {
            Assert.IsTrue(DeadlockParser.IsDeadlockXml(TestGraphs.Load(TestGraphs.KeyLock)));
            Assert.IsTrue(DeadlockParser.IsDeadlockXml(TestGraphs.Load(TestGraphs.XeEventEnvelope)));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not xml at all")]
        [DataRow("<html><body>nope</body></html>")]
        public void IsDeadlockXml_RejectsNonDeadlockInput(string? xml)
        {
            Assert.IsFalse(DeadlockParser.IsDeadlockXml(xml));
        }

        [TestMethod]
        public void IsDeadlockXml_RejectsExecutionPlan()
        {
            Assert.IsFalse(DeadlockParser.IsDeadlockXml(ExecutionPlanXml));
        }

        [TestMethod]
        public void Parse_LeadingWhitespace_IsTolerated()
        {
            var graphs = DeadlockParser.Parse("\r\n   " + MinimalDeadlockXml);

            Assert.AreEqual(1, graphs.Count);
        }

        [TestMethod]
        public void Parse_LeadingByteOrderMark_IsTolerated()
        {
            // char.IsWhiteSpace does not treat U+FEFF as whitespace, so a BOM surviving a round trip
            // through a string column would otherwise fail the parse.
            var graphs = DeadlockParser.Parse("﻿" + MinimalDeadlockXml);

            Assert.AreEqual(1, graphs.Count);
        }

        [TestMethod]
        public void Parse_XmlDeclaration_IsTolerated()
        {
            var graphs = DeadlockParser.Parse(
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>" + Environment.NewLine + MinimalDeadlockXml);

            Assert.AreEqual(1, graphs.Count);
        }

        [TestMethod]
        public void Parse_EmptyDeadlock_ReturnsGraphWithNoParticipants()
        {
            var graph = DeadlockParser.Parse(MinimalDeadlockXml).Single();

            Assert.AreEqual(0, graph.Processes.Count);
            Assert.AreEqual(0, graph.Resources.Count);
            Assert.AreEqual(0, graph.Victims.Count);
            Assert.IsFalse(graph.IsParallel);
        }

        [TestMethod]
        public void Parse_ElementNamesAreMatchedCaseInsensitively()
        {
            // Casing of names such as exchangeEvent and lockMode has not been consistent across SQL
            // Server versions and tools, so matching is on local name, case insensitively.
            const string xml =
                """
                <DEADLOCK>
                  <Victim-List>
                    <VictimProcess id="p1" />
                  </Victim-List>
                  <Process-List>
                    <Process id="p1" spid="7" ecid="0" LockMode="X">
                      <InputBuf>SELECT 1;</InputBuf>
                    </Process>
                  </Process-List>
                  <Resource-List>
                    <EXCHANGEEVENT id="Pipe1" WaitType="e_waitPipeNewRow">
                      <Owner-List>
                        <Owner id="p1" />
                      </Owner-List>
                    </EXCHANGEEVENT>
                  </Resource-List>
                </DEADLOCK>
                """;

            var graph = DeadlockParser.Parse(xml).Single();

            Assert.AreEqual(1, graph.Processes.Count);
            Assert.AreEqual(7, graph.Processes[0].Spid);
            Assert.AreEqual("X", graph.Processes[0].LockMode);
            Assert.AreEqual("SELECT 1;", graph.Processes[0].InputBuffer);
            Assert.AreEqual(1, graph.Victims.Count);
            Assert.AreEqual(DeadlockResourceType.ExchangeEvent, graph.Resources[0].Type);
            Assert.AreEqual("p1", graph.Resources[0].Owners.Single().ProcessId);
        }

        [TestMethod]
        public void Parse_NonNumericAttribute_YieldsNullRatherThanThrowing()
        {
            const string xml =
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="not-a-number" waittime="" logused="huge" />
                  </process-list>
                </deadlock>
                """;

            var process = DeadlockParser.Parse(xml).Single().Processes.Single();

            Assert.IsNull(process.Spid);
            Assert.IsNull(process.WaitTime);
            Assert.IsNull(process.LogUsed);
        }

        [TestMethod]
        public void Parse_DuplicateProcessId_DoesNotThrow()
        {
            // A malformed graph could repeat an id.  First one wins rather than the lookup throwing.
            const string xml =
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="10" />
                    <process id="p1" spid="11" />
                  </process-list>
                </deadlock>
                """;

            var graph = DeadlockParser.Parse(xml).Single();

            Assert.AreEqual(2, graph.Processes.Count);
            Assert.AreEqual(10, graph.FindProcess("p1")!.Spid);
        }
    }
}
