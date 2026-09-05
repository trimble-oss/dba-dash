using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// The payload version, which is recorded with an analysis alongside the signature and the model
    /// so a stored answer can be read knowing what it was produced from.  It changes when the
    /// request gains something the model could reason from.
    /// </summary>
    [TestClass]
    public class DeadlockPayloadVersionTests
    {
        private static readonly DeadlockObjectDefinition Definition = new()
        {
            Database = "Ops",
            Name = "dbo.Customer",
            ObjectType = "Table",
            Ddl = "CREATE TABLE dbo.Customer(CustomerID INT NOT NULL)"
        };

        private static DeadlockAnalysisPayload Build(IReadOnlyList<DeadlockObjectDefinition> schema)
        {
            var graph = DeadlockParser.Parse(TestGraphs.Load(TestGraphs.KeyLock)).First();
            return DeadlockAnalysisPayload.Build(graph, DeadlockAnalyser.Analyse(graph), "SQLPROD01", schema);
        }

        [TestMethod]
        public void Version_ChangesWhenTheRequestGainsSchema()
        {
            // Otherwise a stored answer produced before enrichment was available would be indistinguishable
            // from one produced with object definitions.
            Assert.AreNotEqual(
                Build(Array.Empty<DeadlockObjectDefinition>()).Version,
                Build(new[] { Definition }).Version);
        }

        [TestMethod]
        public void Version_IsStableForTheSameKindOfRequest()
        {
            Assert.AreEqual(
                Build(new[] { Definition }).Version,
                Build(new[] { Definition }).Version);

            Assert.AreEqual(
                Build(Array.Empty<DeadlockObjectDefinition>()).Version,
                Build(Array.Empty<DeadlockObjectDefinition>()).Version);
        }

        [TestMethod]
        public void Version_SaysWhetherSchemaWasIncluded()
        {
            // It ends up in a column people read when working out what an older answer was produced
            // from, so it is worth being legible rather than a bare number.
            StringAssert.Contains(Build(new[] { Definition }).Version, "schema");
            Assert.IsFalse(Build(Array.Empty<DeadlockObjectDefinition>()).Version.Contains("schema"));
        }

        [TestMethod]
        public void Version_IsShortEnoughForTheColumn()
        {
            // AI.DeadlockAnalysis.PayloadVersion is VARCHAR(30).
            Assert.IsTrue(Build(new[] { Definition }).Version.Length <= 30);
        }
    }
}
