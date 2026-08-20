using System.Data;
using System.Linq;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DBADash.Test
{
    [TestClass]
    public class XEObjectCatalogTests
    {
        // Builds a DataSet shaped like the five result sets returned by XEObjectCatalogMessage.
        private static DataSet BuildDataSet()
        {
            var ds = new DataSet();

            var events = new DataTable("Events");
            events.Columns.Add("package_name");
            events.Columns.Add("event_name");
            events.Columns.Add("description");
            // error_reported exists in two packages (the real-world duplicate-name case).
            events.Rows.Add("xesvlpkg", "error_reported", "svl");
            events.Rows.Add("sqlserver", "error_reported", "sql");
            events.Rows.Add("sqlserver", "rpc_completed", "rpc");
            ds.Tables.Add(events);

            var fields = new DataTable("EventFields");
            fields.Columns.Add("package_name");
            fields.Columns.Add("event_name");
            fields.Columns.Add("field_name");
            fields.Columns.Add("type_name");
            fields.Rows.Add("sqlserver", "error_reported", "error_number", "int32");
            fields.Rows.Add("sqlserver", "error_reported", "message", "unicode_string");
            fields.Rows.Add("xesvlpkg", "error_reported", "svl_only", "int32");
            fields.Rows.Add("sqlserver", "rpc_completed", "duration", "uint64");
            ds.Tables.Add(fields);

            var pred = new DataTable("PredSources");
            pred.Columns.Add("package_name");
            pred.Columns.Add("field_name");
            pred.Columns.Add("type_name");
            pred.Rows.Add("sqlserver", "session_id", "uint16");
            ds.Tables.Add(pred);

            var actions = new DataTable("Actions");
            actions.Columns.Add("package_name");
            actions.Columns.Add("field_name");
            actions.Columns.Add("type_name");
            actions.Rows.Add("sqlserver", "sql_text", "unicode_string");
            ds.Tables.Add(actions);

            var cust = new DataTable("Customizations");
            cust.Columns.Add("package_name");
            cust.Columns.Add("event_name");
            cust.Columns.Add("field_name");
            cust.Columns.Add("type_name");
            cust.Columns.Add("default_value");
            cust.Rows.Add("sqlserver", "rpc_completed", "collect_statement", "boolean", "true");
            ds.Tables.Add(cust);

            return ds;
        }

        [TestMethod]
        public void FromDataSet_DuplicateEventName_FieldsAttachedPerPackage()
        {
            var catalog = XEObjectCatalog.FromDataSet(BuildDataSet());

            // FindEvent prefers the sqlserver package, which must carry its own two fields (not zero).
            var error = catalog.FindEvent("error_reported");
            Assert.AreEqual("sqlserver", error.Package);
            CollectionAssert.AreEquivalent(new[] { "error_number", "message" },
                error.Fields.Select(f => f.Name).ToArray());
        }

        [TestMethod]
        public void FromDataSet_ParsesActionsPredSourcesAndCustomizations()
        {
            var catalog = XEObjectCatalog.FromDataSet(BuildDataSet());

            Assert.AreEqual("sql_text", catalog.Actions.Single().Name);
            Assert.AreEqual("session_id", catalog.PredSources.Single().Name);

            var rpc = catalog.FindEvent("rpc_completed");
            var collect = rpc.Customizations.Single();
            Assert.AreEqual("collect_statement", collect.Name);
            Assert.IsTrue(collect.IsBoolean);
            Assert.IsTrue(collect.DefaultOn, "default_value 'true' should map to DefaultOn");
        }

        [TestMethod]
        public void Catalog_JsonRoundTrips()
        {
            var original = XEObjectCatalog.FromDataSet(BuildDataSet());

            var restored = JsonConvert.DeserializeObject<XEObjectCatalog>(JsonConvert.SerializeObject(original))!;

            Assert.AreEqual(original.Events.Count, restored.Events.Count);
            Assert.AreEqual(original.Actions.Count, restored.Actions.Count);
            var rpc = restored.FindEvent("rpc_completed");
            Assert.AreEqual("duration", rpc.Fields.Single().Name);
            Assert.IsTrue(rpc.Customizations.Single().DefaultOn); // recomputed from persisted default_value
        }
    }
}
