using System;
using System.Collections.Generic;
using DBADash.XE;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Tests for <see cref="XELiteShredder"/> (IXEvent -> DataTable) and <see cref="XELiteEventFileReader.SplitPath"/>.
    /// The shredder must match <see cref="XETraceShredder"/>'s column conventions so the fast (binary) and fallback
    /// (TVF/XML) read paths produce interchangeable grids.
    /// </summary>
    [TestClass]
    public class XELiteShredderTests
    {
        /// <summary>Minimal IXEvent test double.</summary>
        private sealed class FakeXEvent : IXEvent
        {
            public string Name { get; init; } = string.Empty;
            public Guid UUID { get; init; }
            public DateTimeOffset Timestamp { get; init; }
            public IReadOnlyDictionary<string, object> Fields { get; init; } = new Dictionary<string, object>();
            public IReadOnlyDictionary<string, object> Actions { get; init; } = new Dictionary<string, object>();
            public long XEventStartOffsetInBytes => 0;
            public long XEventEndOffsetInBytes => 0;
            public long XEventSizeInBytes => 0;
        }

        [TestMethod]
        public void Build_BaseColumns_AlwaysPresent()
        {
            var ts = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var dt = XELiteShredder.Build(new[]
            {
                new FakeXEvent { Name = "rpc_completed", Timestamp = ts }
            });

            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual("rpc_completed", dt.Rows[0]["event_type"]);
            Assert.AreEqual(ts.UtcDateTime, dt.Rows[0]["timestamp"]);
        }

        [TestMethod]
        public void Build_NumericField_TypedLong()
        {
            var dt = XELiteShredder.Build(new[]
            {
                new FakeXEvent
                {
                    Name = "rpc_completed",
                    Timestamp = DateTimeOffset.UtcNow,
                    Fields = new Dictionary<string, object> { ["duration"] = 1234, ["cpu_time"] = 56L }
                }
            });

            Assert.AreEqual(typeof(long), dt.Columns["duration"]!.DataType);
            Assert.AreEqual(1234L, dt.Rows[0]["duration"]);
            Assert.AreEqual(56L, dt.Rows[0]["cpu_time"]);
        }

        [TestMethod]
        public void Build_StringAndActionFields_Merged()
        {
            var dt = XELiteShredder.Build(new[]
            {
                new FakeXEvent
                {
                    Name = "sql_batch_completed",
                    Timestamp = DateTimeOffset.UtcNow,
                    Fields = new Dictionary<string, object> { ["batch_text"] = "SELECT 1" },
                    Actions = new Dictionary<string, object> { ["username"] = "sa" }
                }
            });

            Assert.AreEqual(typeof(string), dt.Columns["batch_text"]!.DataType);
            Assert.AreEqual("SELECT 1", dt.Rows[0]["batch_text"]);
            Assert.AreEqual("sa", dt.Rows[0]["username"]);
        }

        [TestMethod]
        public void Build_ByteArrayField_RenderedAsHex()
        {
            var dt = XELiteShredder.Build(new[]
            {
                new FakeXEvent
                {
                    Name = "rpc_completed",
                    Timestamp = DateTimeOffset.UtcNow,
                    Actions = new Dictionary<string, object> { ["context_info"] = new byte[] { 0x00, 0xAB, 0xFF } }
                }
            });

            Assert.AreEqual("0x00ABFF", dt.Rows[0]["context_info"]);
        }

        [TestMethod]
        public void Build_VaryingSchema_UnionsColumns()
        {
            var dt = XELiteShredder.Build(new[]
            {
                new FakeXEvent { Name = "a", Timestamp = DateTimeOffset.UtcNow,
                    Fields = new Dictionary<string, object> { ["x"] = "1" } },
                new FakeXEvent { Name = "b", Timestamp = DateTimeOffset.UtcNow,
                    Fields = new Dictionary<string, object> { ["y"] = "2" } }
            });

            Assert.IsTrue(dt.Columns.Contains("x"));
            Assert.IsTrue(dt.Columns.Contains("y"));
            Assert.AreEqual(2, dt.Rows.Count);
            Assert.AreEqual(DBNull.Value, dt.Rows[0]["y"]);
        }

        [TestMethod]
        public void Build_NullOrEmpty_ReturnsSchemaOnly()
        {
            Assert.AreEqual(0, XELiteShredder.Build(null).Rows.Count);
            Assert.AreEqual(0, XELiteShredder.Build(Array.Empty<IXEvent>()).Rows.Count);
        }

        [TestMethod]
        public void SplitPath_RolloverSuffix_BecomesWildcardPattern()
        {
            var (dir, pattern) = XELiteEventFileReader.SplitPath(@"C:\Logs\MySession_0_133456789012345678.xel");

            Assert.AreEqual(@"C:\Logs", dir);
            Assert.AreEqual("MySession*.xel", pattern);
        }

        [TestMethod]
        public void SplitPath_NoRolloverSuffix_WidensStem()
        {
            var (dir, pattern) = XELiteEventFileReader.SplitPath(@"D:\XE\health.xel");

            Assert.AreEqual(@"D:\XE", dir);
            Assert.AreEqual("health*.xel", pattern);
        }

        [TestMethod]
        public void SplitPath_ForwardSlashes_Handled()
        {
            var (dir, pattern) = XELiteEventFileReader.SplitPath("/var/opt/mssql/log/sess_0_1337.xel");

            Assert.AreEqual("/var/opt/mssql/log", dir);
            Assert.AreEqual("sess*.xel", pattern);
        }

        [TestMethod]
        public void BuildEventStreamSource_FilenameOnly_SsmsStyle()
        {
            // SSMS passes "system_health_*.xel" (filename only) - a full directory path made fn_MSxe return an older subset.
            Assert.AreEqual("system_health_*.xel",
                XELiteEventFileReader.BuildEventStreamSource(
                    @"C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Log\system_health_0_134315117283840000.xel"));
        }

        [TestMethod]
        public void BuildEventStreamSource_NoRolloverSuffix_WidensStem()
        {
            Assert.AreEqual("mytrace*.xel", XELiteEventFileReader.BuildEventStreamSource(@"D:\XE\mytrace.xel"));
        }

        [TestMethod]
        public void LiveStreamShredder_InternalsResolve()
        {
            // The fn_MSxe fast path drives XELite's internal live-buffer parser by reflection.  This guards against a
            // XELite version bump silently breaking it: if it fails, the reflection targets moved - update
            // XELiveStreamShredder (the runtime path falls back to the TVF hybrid in the meantime).
            Assert.IsTrue(XELiveStreamShredder.TryInitialise(),
                "XELite internal live-stream parser members could not be resolved for the referenced XELite version.");
        }
    }
}
