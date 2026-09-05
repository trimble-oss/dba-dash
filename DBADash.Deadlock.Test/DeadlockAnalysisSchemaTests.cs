using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// The schema enrichment: object definitions as at the deadlock, carried in the payload and shown
    /// in the preview before anything is sent.
    /// </summary>
    [TestClass]
    public class DeadlockAnalysisSchemaTests
    {
        private static readonly DeadlockObjectDefinition Procedure = new()
        {
            Database = "Ops",
            Name = "dbo.usp_PostInvoice",
            ObjectType = "Stored Procedure",
            AsAt = new DateTime(2024, 7, 1, 3, 0, 0),
            Ddl = "CREATE PROC dbo.usp_PostInvoice AS SELECT 1"
        };

        private static readonly DeadlockObjectDefinition Table = new()
        {
            Database = "Ops",
            Name = "dbo.Customer",
            ObjectType = "Table",
            AsAt = new DateTime(2024, 7, 1, 3, 0, 0),
            Ddl = "CREATE TABLE dbo.Customer(CustomerID INT NOT NULL)",
            Truncated = true
        };

        private static DeadlockAnalysisPayload Build(IReadOnlyList<DeadlockObjectDefinition> schema)
        {
            var graph = DeadlockParser.Parse(TestGraphs.Load(TestGraphs.SharedLock)).First();
            return DeadlockAnalysisPayload.Build(graph, DeadlockAnalyser.Analyse(graph), "SQLPROD01", schema);
        }

        [TestMethod]
        public void Payload_CarriesTheObjectDefinitionsItWasGiven()
        {
            var payload = Build(new[] { Procedure, Table });

            Assert.AreEqual(2, payload.Schema.Count);
            CollectionAssert.AreEquivalent(
                new[] { "dbo.usp_PostInvoice", "dbo.Customer" },
                payload.Schema.Select(s => s.Name).ToArray());
        }

        [TestMethod]
        public void Payload_WithoutSchema_IsUnchanged()
        {
            // Schema snapshots are optional, and a graph opened from a file has no repository at all.
            var payload = Build(Array.Empty<DeadlockObjectDefinition>());

            Assert.AreEqual(0, payload.Schema.Count);
            Assert.IsFalse(payload.ToPreview().Contains("Object definitions"));
        }

        [TestMethod]
        public void Preview_ShowsTheDefinitionsThatWouldBeSent()
        {
            // The DDL leaves the estate with everything else, so it has to be in front of the user
            // before they press send - not summarised as "3 objects".
            var preview = Build(new[] { Procedure, Table }).ToPreview();

            StringAssert.Contains(preview, "Ops.dbo.usp_PostInvoice (Stored Procedure");
            StringAssert.Contains(preview, "CREATE PROC dbo.usp_PostInvoice");
            StringAssert.Contains(preview, "CREATE TABLE dbo.Customer");
        }

        [TestMethod]
        public void Preview_SaysWhenADefinitionIsDatedOrCutShort()
        {
            // Both change how much weight the answer deserves: the definition may predate a change,
            // and a truncated one may be missing the part that explains the deadlock.
            var preview = Build(new[] { Table }).ToPreview();

            StringAssert.Contains(preview, "snapshot 2024-07-01 03:00");
            StringAssert.Contains(preview, "truncated");
        }

        [TestMethod]
        public void Schema_DoesNotChangeTheSignature()
        {
            // The signature identifies the deadlock, not what we happened to know about the schema
            // when we looked at it - otherwise enrichment would fragment the grouping.
            Assert.AreEqual(
                Build(Array.Empty<DeadlockObjectDefinition>()).Signature,
                Build(new[] { Procedure, Table }).Signature);
        }
    }
}
