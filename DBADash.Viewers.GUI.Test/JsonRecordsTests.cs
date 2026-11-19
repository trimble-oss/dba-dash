using DBADashGUI.CustomReports;
using DBADashGUI.Grids;
using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using System.Text;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// JSON the grid viewer opens besides its own exports: sequences of records such as JSON lines and structured logs,
    /// told from a DBA Dash grid or DataSet export by parsing rather than by layout.
    /// </summary>
    [TestClass]
    public class JsonRecordsTests
    {
        private string _folder;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DBADashJsonRecordsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        private DataSet Load(string file, string content)
        {
            var path = Path.Combine(_folder, file);
            File.WriteAllText(path, content);
            return GridSerializer.LoadDataSet(path);
        }

        private static string[] ColumnNames(DataTable table) =>
            table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

        private static DataTable GridTable()
        {
            var table = new DataTable("Grid");
            table.Columns.Add("id", typeof(long));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("when", typeof(DateTime));
            table.Rows.Add(1L, "first", new DateTime(2026, 11, 19, 11, 23, 59));
            table.Rows.Add(2L, "second", new DateTime(2026, 11, 19, 11, 24, 0));
            return table;
        }

        /// <summary>
        /// DBA Dash's own exports still read as before however they're laid out - indented as it writes them today, or
        /// all on one line, as a more compact export might be.
        /// </summary>
        [TestMethod]
        [DataRow(Formatting.Indented)]
        [DataRow(Formatting.None)]
        public void GridExports_ReadAsBefore_WhateverTheLayout(Formatting formatting)
        {
            var table = Load("grid.json", JsonConvert.SerializeObject(GridTable(), formatting)).Tables.Cast<DataTable>().Single();
            CollectionAssert.AreEqual(new[] { "id", "name", "when" }, ColumnNames(table));
            Assert.AreEqual(typeof(DateTime), table.Columns["when"]!.DataType);
            Assert.AreEqual("second", table.Rows[1]["name"]);

            var ds = new DataSet();
            ds.Tables.Add(GridTable());
            var other = GridTable();
            other.TableName = "Other";
            ds.Tables.Add(other);

            var read = Load("dataset.json", JsonConvert.SerializeObject(ds, formatting));
            CollectionAssert.AreEqual(new[] { "Grid", "Other" }, read.Tables.Cast<DataTable>().Select(t => t.TableName).ToArray());
            Assert.AreEqual(2, read.Tables["Other"]!.Rows.Count);
        }

        /// <summary>A Serilog compact (CLEF) log, as Serilog.Formatting.Compact writes it, reads as a log.</summary>
        [TestMethod]
        public void CompactLog_ReadsAsLog()
        {
            const string log =
                """
                {"@t":"2026-11-19T11:23:59.2252255Z","@m":"Now listening on: \"http://localhost:5080\"","@i":"d826f4b8","address":"http://localhost:5080","EventId":{"Id":14,"Name":"ListeningOnAddress"},"SourceContext":"Microsoft.Hosting.Lifetime"}
                {"@t":"2026-11-19T11:24:03.0863608Z","@mt":"{Pass} found {Count,3} new message(s) on source {SourceId} {{literal}} {Missing}","@l":"Warning","Pass":"TrailingPoll","Count":8,"SourceId":"mailgun-dev","SourceContext":"InboundMail.Core.Polling.SourcePoller"}
                {"@t":"2026-11-19T11:24:04.0000000Z","@mt":"Failed {@Item}","@l":"Error","@x":"System.Exception: boom","Item":{"A":1},"@@odd":"escaped"}
                """;

            var table = Load("inboundmail-20261119.json", log).Tables.Cast<DataTable>().Single();

            Assert.AreEqual(JsonRecords.LogTableName, table.TableName);
            CollectionAssert.AreEqual(
                new[] { "Timestamp", "Level", "Message", "Exception", "address", "EventId", "SourceContext", "Pass", "Count", "SourceId", "Item", "@odd", "EventType" },
                ColumnNames(table));
            Assert.AreEqual(typeof(DateTime), table.Columns["Timestamp"]!.DataType);
            Assert.AreEqual(new DateTime(2026, 11, 19, 11, 23, 59, DateTimeKind.Utc).AddTicks(2252255), table.Rows[0]["Timestamp"]);
            CollectionAssert.AreEqual(new[] { "Information", "Warning", "Error" }, table.Rows.Cast<DataRow>().Select(r => r["Level"]).ToArray());
            Assert.AreEqual("Now listening on: \"http://localhost:5080\"", table.Rows[0]["Message"]);
            Assert.AreEqual("\"TrailingPoll\" found   8 new message(s) on source \"mailgun-dev\" {literal} {Missing}", table.Rows[1]["Message"]);
            Assert.AreEqual("Failed {\"A\":1}", table.Rows[2]["Message"]);
            Assert.AreEqual("System.Exception: boom", table.Rows[2]["Exception"]);
            Assert.AreEqual("{\"Id\":14,\"Name\":\"ListeningOnAddress\"}", table.Rows[0]["EventId"]);
            Assert.AreEqual(typeof(long), table.Columns["Count"]!.DataType);
            Assert.AreEqual("escaped", table.Rows[2]["@odd"]);
            Assert.AreEqual("d826f4b8", table.Rows[0]["EventType"]);
        }

        /// <summary>
        /// A template is rendered from its own event: the writer's "@r" text for holes with a format, a property only
        /// another event has doesn't fill a hole, and a format the value can't take leaves the hole rather than
        /// stopping the log opening.
        /// </summary>
        [TestMethod]
        public void CompactLog_RendersEachEventFromItsOwnProperties()
        {
            const string log =
                """
                {"@t":"2026-11-19T11:00:00Z","@mt":"Paid {Amount:C} to {Name} ({Count,3:N0} items, {Other:C})","Amount":12,"Name":"a","Count":1,"@r":["£12.00","  1"]}
                {"@t":"2026-11-19T11:00:01Z","@mt":"Hello {Name} {Nothing}","Nothing":null}
                {"@t":"2026-11-19T11:00:02Z","@mt":"Count {Count:Q}","Count":1}
                """;

            var table = Load("render.clef", log).Tables.Cast<DataTable>().Single();

            CollectionAssert.AreEqual(
                new object[]
                {
                    "Paid £12.00 to \"a\" (  1 items, {Other:C})",
                    "Hello {Name} null",
                    "Count {Count:Q}"
                },
                table.Rows.Cast<DataRow>().Select(r => r["Message"]).ToArray());
            Assert.IsFalse(table.Columns.Contains("@r"), "the renderings are part of the message, not a column");
            Assert.IsTrue(table.Rows[1].IsNull("Nothing"));
        }

        /// <summary>
        /// Any other sequence of records reads generically: a column per property in the order first seen, NULL where a
        /// record leaves one out, types from the values, nested values as JSON, and a value that isn't an object in a
        /// column of its own.  Records needn't be one per line.
        /// </summary>
        [TestMethod]
        public void JsonLines_ReadGenerically()
        {
            const string records =
                """
                {"id":1,"name":"a","ok":true,"ratio":1,"mixed":1,"nested":[1,2]}
                {"id":2,"Name":"case","ok":false,"ratio":0.5,"mixed":"x","when":"2026-11-19T10:00:00Z"}{"id":3,
                  "extra":null}
                "just a string"
                """;

            var table = Load("data.jsonl", records).Tables.Cast<DataTable>().Single();

            Assert.AreEqual(JsonRecords.RecordsTableName, table.TableName);
            CollectionAssert.AreEqual(
                new[] { "id", "name", "ok", "ratio", "mixed", "nested", "Name (2)", "when", "extra", "Value" },
                ColumnNames(table));
            Assert.AreEqual(4, table.Rows.Count);
            Assert.AreEqual(typeof(long), table.Columns["id"]!.DataType);
            Assert.AreEqual(typeof(bool), table.Columns["ok"]!.DataType);
            Assert.AreEqual(typeof(double), table.Columns["ratio"]!.DataType);
            Assert.AreEqual(1d, table.Rows[0]["ratio"]);
            Assert.AreEqual(typeof(string), table.Columns["mixed"]!.DataType);
            CollectionAssert.AreEqual(new object[] { "1", "x" }, new[] { table.Rows[0]["mixed"], table.Rows[1]["mixed"] });
            Assert.AreEqual("[1,2]", table.Rows[0]["nested"]);
            Assert.AreEqual(typeof(DateTime), table.Columns["when"]!.DataType);
            Assert.IsTrue(table.Rows[2].IsNull("name"));
            Assert.AreEqual("just a string", table.Rows[3]["Value"]);
            Assert.IsFalse(table.Rows.Cast<DataRow>().Any(r => r.HasErrors));
        }

        /// <summary>
        /// A record that isn't valid JSON - here the last line of a log still being written - doesn't stop the rest
        /// opening: it's shown as text, with the error on its row.  The file can be read while the logger has it open.
        /// </summary>
        [TestMethod]
        public void UnreadableLine_IsShownWithItsError_AndOpenFileCanBeRead()
        {
            var path = Path.Combine(_folder, "live.clef");
            using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            var bytes = Encoding.UTF8.GetBytes(
                "{\"@t\":\"2026-11-19T11:23:59Z\",\"@m\":\"one\"}\n" +
                "{\"@t\":\"2026-11-19T11:24:00Z\",\"@m\":\"two\"}\n" +
                "{\"@t\":\"2026-11-19T11:24:01Z\",\"@m\":\"thr");
            writer.Write(bytes);
            writer.Flush();

            var table = GridSerializer.LoadDataSet(path).Tables.Cast<DataTable>().Single();

            Assert.AreEqual(JsonRecords.LogTableName, table.TableName, "still read as a log");
            Assert.AreEqual(3, table.Rows.Count);
            CollectionAssert.AreEqual(new object[] { "one", "two", DBNull.Value }, table.Rows.Cast<DataRow>().Select(r => r["Message"]).ToArray());
            Assert.IsTrue(table.Rows[2].HasErrors);
            StringAssert.StartsWith(table.Rows[2].RowError, "Line 3:");
            StringAssert.Contains((string)table.Rows[2][JsonRecords.UnreadableColumnName], "\"thr");
        }

        /// <summary>
        /// The records read before one that isn't valid JSON are kept, however they're laid out - several on a line, or
        /// one over several lines - and the reading picks up again after it.  A broken record over several lines is one row.
        /// </summary>
        [TestMethod]
        [DataRow("{\"id\":1}{\"id\":2}\n{\"id\":3,\n  \"x\":\n", new long[] { 1, 2 }, "Lines 2-3:")]
        [DataRow("{\n  \"id\": 1\n}\n{\n  \"id\": 2\n}\n{\n  \"id\":", new long[] { 1, 2 }, "Lines 7-8:")]
        [DataRow("{\"id\":1}\n{\"id\":2 oops}\n{\"id\":3}\n", new long[] { 1, 3 }, "Line 2:")]
        [DataRow("{\"id\":1} {\"id\":2} {\"id\":", new long[] { 1, 2 }, "Line 1:")]
        public void UnreadableRecord_KeepsTheOthers(string content, long[] ids, string error)
        {
            var table = Load("partial.jsonl", content).Tables.Cast<DataTable>().Single();

            var readable = table.Rows.Cast<DataRow>().Where(r => !r.HasErrors).ToList();
            CollectionAssert.AreEqual(ids, readable.Select(r => (long)r["id"]).ToArray());
            var bad = table.Rows.Cast<DataRow>().Single(r => r.HasErrors);
            StringAssert.StartsWith(bad.RowError, error);
            Assert.IsFalse(bad.RowError.Contains(", line "), "the line is the file's, not the fragment's");
            StringAssert.StartsWith((string)table.ExtendedProperties[JsonRecords.LoadWarningProperty], "1 part of the file couldn't be read");
        }

        /// <summary>
        /// After a bad record, reading picks up the records that follow however they're laid out - a pretty-printed one
        /// over several lines too - while the pieces of a broken pretty-printed record aren't taken for records.
        /// </summary>
        [TestMethod]
        public void UnreadableRecord_RecordsAfterItAreFound()
        {
            const string content =
                """
                {"id":1}
                {"id":2 oops}
                {
                  "id": 3
                }
                {
                  "id": 4,
                  "name": broken
                }
                {"id":5}
                """;

            var table = Load("resync.jsonl", content).Tables.Cast<DataTable>().Single();

            CollectionAssert.AreEqual(new long[] { 1, 3, 5 },
                table.Rows.Cast<DataRow>().Where(r => !r.HasErrors).Select(r => (long)r["id"]).ToArray());
            CollectionAssert.AreEqual(new[] { "Line 2:", "Lines 6-9:" },
                table.Rows.Cast<DataRow>().Where(r => r.HasErrors).Select(r => r.RowError[..r.RowError.IndexOf(':')] + ":").ToArray());
            Assert.IsFalse(table.Columns.Contains(JsonRecords.ValueColumnName), "no piece of the broken record became a record");
            CollectionAssert.AreEqual(new object[] { 1L, DBNull.Value, 3L, DBNull.Value, 5L },
                table.Rows.Cast<DataRow>().Select(r => r["id"]).ToArray(), "in file order");
        }

        /// <summary>The grid says, above it, when part of the file couldn't be read - and says nothing when all of it could.</summary>
        [TestMethod]
        public void GridViewer_ShowsLoadWarning()
        {
            var partial = Load("partial.jsonl", "{\"id\":1}\n{\"id\":\n").Tables[0];
            using (var control = new GridViewerControl(partial, "partial"))
            {
                StringAssert.StartsWith(control.Notice, "1 part of the file couldn't be read as JSON");
            }

            var whole = Load("whole.jsonl", "{\"id\":1}\n{\"id\":2}\n").Tables[0];
            Assert.IsFalse(whole.ExtendedProperties.ContainsKey(JsonRecords.LoadWarningProperty));
            using (var control = new GridViewerControl(whole, "whole"))
            {
                Assert.IsNull(control.Notice);
            }
        }

        /// <summary>
        /// Strings are kept as written: a message that looks like a date doesn't stop a file being read as a log, or
        /// get rendered as a date - while a column of dates is still typed as one.
        /// </summary>
        [TestMethod]
        public void DateLikeStrings_StayAsWritten()
        {
            const string log =
                """
                {"@t":"2026-11-19T11:00:00Z","@m":"2026-11-19T10:00:00Z","When":"2026-11-19T09:00:00+01:00"}
                {"@t":"2026-11-19T11:00:01Z","@mt":"Due {When}","When":"2026-11-19T09:00:00+01:00"}
                """;

            var table = Load("dates.clef", log).Tables.Cast<DataTable>().Single();

            Assert.AreEqual(JsonRecords.LogTableName, table.TableName);
            CollectionAssert.AreEqual(new object[] { "2026-11-19T10:00:00Z", "Due \"2026-11-19T09:00:00+01:00\"" },
                table.Rows.Cast<DataRow>().Select(r => r["Message"]).ToArray());
            Assert.AreEqual(typeof(DateTime), table.Columns["Timestamp"]!.DataType);
            Assert.AreEqual(typeof(DateTime), table.Columns["When"]!.DataType);
        }

        /// <summary>A single object that isn't a DataSet is one record; an array Newtonsoft can't read as a table is records.</summary>
        [TestMethod]
        public void SingleValues_ThatArentExports_ReadAsRecords()
        {
            var one = Load("one.json", "{\"a\":1,\"b\":{\"c\":2}}").Tables.Cast<DataTable>().Single();
            Assert.AreEqual(1, one.Rows.Count);
            Assert.AreEqual("{\"c\":2}", one.Rows[0]["b"]);

            var nested = Load("nested.json", "[{\"a\":1,\"b\":{\"c\":2}},{\"a\":\"x\"}]").Tables.Cast<DataTable>().Single();
            CollectionAssert.AreEqual(new[] { "a", "b" }, ColumnNames(nested));
            Assert.AreEqual("x", nested.Rows[1]["a"]);
        }

        [TestMethod]
        public void NotJson_IsReported()
        {
            Assert.ThrowsExactly<JsonReaderException>(() => Load("bad.json", "this isn't json\n{\"a\":1}"));
        }

        [TestMethod]
        [DataRow("log.jsonl", true)]
        [DataRow("log.ndjson", true)]
        [DataRow("log.clef", true)]
        [DataRow("log.txt", false)]
        public void CanOpen_JsonRecordsExtensions(string file, bool expected)
        {
            Assert.AreEqual(expected, ViewerApp.CanOpen(file));
        }
    }
}
