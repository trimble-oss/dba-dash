using System.Collections.Generic;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class XETraceDefinitionTests
    {
        private static XETraceDefinition NewDef() => new()
        {
            Events = XETraceEventType.RpcCompleted | XETraceEventType.SqlBatchCompleted,
            TargetType = XETraceTargetType.RingBuffer
        };

        // Filter builders for the name-based model.
        private static XEFilter Duration(XEFilterOp op, string v) =>
            new() { Field = "duration", IsAction = false, IsNumeric = true, Op = op, Value = v };

        private static XEFilter AppName(XEFilterOp op, string v) =>
            new() { Field = "client_app_name", FieldPackage = "sqlserver", IsAction = true, IsNumeric = false, Op = op, Value = v };

        private static XEFilter UserName(XEFilterOp op, string v) =>
            new() { Field = "username", FieldPackage = "sqlserver", IsAction = true, IsNumeric = false, Op = op, Value = v };

        private static XEFilter SessionId(XEFilterOp op, string v) =>
            new() { Field = "session_id", FieldPackage = "sqlserver", IsAction = true, IsNumeric = true, Op = op, Value = v };

        // ---- Extra (arbitrary) events -----------------------------------------------------------

        [TestMethod]
        public void TargetNone_EmitsNoTargetClause()
        {
            var def = NewDef();
            def.TargetType = XETraceTargetType.None;

            var sql = def.BuildCreateSessionSql();

            StringAssert.Contains(sql, "ADD EVENT sqlserver.rpc_completed"); // events still emitted
            Assert.IsFalse(sql.Contains("ADD TARGET"), "a target-less (live) session must not emit an ADD TARGET clause");
        }

        [TestMethod]
        public void ExtraEvents_AreEmitted()
        {
            var def = NewDef();
            def.Events = 0;
            def.ExtraEvents.Add(new XETraceEventDef("sqlserver", "module_end", new[] { "duration", "cpu_time" }));

            StringAssert.Contains(def.BuildCreateSessionSql(), "ADD EVENT sqlserver.module_end");
        }

        [TestMethod]
        public void ExtraEvent_DataColumnFilter_AppliedOnlyWhenEventHasTheColumn()
        {
            var def = NewDef();
            def.Events = 0;
            def.Filters.Add(Duration(XEFilterOp.GreaterThan, "500"));
            // has duration -> filter applies; lacks duration -> filter skipped
            def.ExtraEvents.Add(new XETraceEventDef("sqlserver", "module_end", new[] { "duration" }));
            def.ExtraEvents.Add(new XETraceEventDef("sqlserver", "login", new[] { "is_cached" }));

            var sql = def.BuildCreateSessionSql();
            var moduleIdx = sql.IndexOf("module_end", System.StringComparison.Ordinal);
            var loginIdx = sql.IndexOf("sqlserver.login", System.StringComparison.Ordinal);
            Assert.IsTrue(sql.Substring(moduleIdx, loginIdx - moduleIdx).Contains("[duration]>(500)"),
                "duration filter should apply to module_end");
            Assert.IsFalse(sql.Substring(loginIdx).Contains("[duration]"),
                "duration filter must be skipped for an event without a duration column");
        }

        [TestMethod]
        public void ExtraEvent_InvalidName_Throws()
        {
            var def = NewDef();
            def.Events = 0;
            def.ExtraEvents.Add(new XETraceEventDef("sqlserver", "evil; DROP", null));
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Build_NoEventsAndNoExtras_Throws()
        {
            var def = NewDef();
            def.Events = 0;
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        // ---- Global actions ("global fields") ---------------------------------------------------

        [TestMethod]
        public void GlobalActions_DefaultSet_IsEmitted()
        {
            var sql = NewDef().BuildCreateSessionSql();
            StringAssert.Contains(sql, "ACTION(sqlserver.client_app_name");
            StringAssert.Contains(sql, "sqlserver.session_id");
            StringAssert.Contains(sql, "sqlserver.context_info");
        }

        [TestMethod]
        public void GlobalActions_Custom_ReplacesDefaults()
        {
            var def = NewDef();
            def.GlobalActions = new List<XEActionDef> { new("sqlserver", "sql_text") };

            var sql = def.BuildCreateSessionSql();

            StringAssert.Contains(sql, "ACTION(sqlserver.sql_text)");
            Assert.IsFalse(sql.Contains("context_info"), "non-selected default actions must not be emitted");
        }

        [TestMethod]
        public void GlobalActions_Empty_EmitsNoActionClause()
        {
            var def = NewDef();
            def.GlobalActions = new List<XEActionDef>();

            var sql = def.BuildCreateSessionSql();

            Assert.IsFalse(sql.Contains("ACTION("), "an empty action list must emit no ACTION(...) clause");
            StringAssert.Contains(sql, "WHERE ("); // event still has its predicate
        }

        [TestMethod]
        public void GlobalActions_Duplicates_AreDeDuplicated()
        {
            var def = NewDef();
            def.GlobalActions = new List<XEActionDef> { new("sqlserver", "session_id"), new("sqlserver", "session_id") };

            var sql = def.BuildCreateSessionSql();

            var first = sql.IndexOf("sqlserver.session_id", System.StringComparison.Ordinal);
            var second = sql.IndexOf("sqlserver.session_id", first + 1, System.StringComparison.Ordinal);
            // The same action appears once per event block, but not twice within one ACTION(...) clause.
            StringAssert.Contains(sql, "ACTION(sqlserver.session_id)");
            Assert.IsTrue(second == -1 || sql.IndexOf("ADD EVENT", first, System.StringComparison.Ordinal) < second,
                "a duplicate action must not appear twice in the same ACTION clause");
        }

        [TestMethod]
        public void GlobalActions_InvalidName_Throws()
        {
            var def = NewDef();
            def.GlobalActions = new List<XEActionDef> { new("sqlserver", "evil; DROP") };
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        // ---- Customizable columns (SET toggles) -------------------------------------------------

        [TestMethod]
        public void Customizations_BuiltInEvent_EmitsSetBeforeAction()
        {
            var def = NewDef();
            def.Events = XETraceEventType.RpcCompleted;
            def.EventCustomizations["rpc_completed"] = new List<XECustomization> { new("collect_statement", "0") };

            var sql = def.BuildCreateSessionSql();

            StringAssert.Contains(sql, "SET collect_statement=(0)");
            // SET must precede ACTION within the rpc_completed block.
            var rpc = sql.IndexOf("rpc_completed", System.StringComparison.Ordinal);
            var set = sql.IndexOf("SET collect_statement", rpc, System.StringComparison.Ordinal);
            var action = sql.IndexOf("ACTION(", rpc, System.StringComparison.Ordinal);
            Assert.IsTrue(set > 0 && set < action, "SET must appear before ACTION in the event block");
        }

        [TestMethod]
        public void Customizations_ExtraEvent_IsEmitted()
        {
            var def = NewDef();
            def.Events = 0;
            def.ExtraEvents.Add(new XETraceEventDef("sqlserver", "module_end", new[] { "duration" }));
            def.EventCustomizations["module_end"] = new List<XECustomization> { new("collect_statement", "1") };

            StringAssert.Contains(def.BuildCreateSessionSql(), "SET collect_statement=(1)");
        }

        [TestMethod]
        public void Customizations_None_EmitsNoSet()
        {
            var sql = NewDef().BuildCreateSessionSql();
            // The target clause legitimately uses SET (e.g. ring_buffer's SET max_memory), so only assert the event
            // blocks (everything before ADD TARGET) carry no SET.
            var events = sql.Substring(0, sql.IndexOf("ADD TARGET", System.StringComparison.Ordinal));
            Assert.IsFalse(events.Contains("SET "), "an event with no customizations must not emit a SET clause");
        }

        [TestMethod]
        public void Customizations_InvalidName_Throws()
        {
            var def = NewDef();
            def.Events = XETraceEventType.RpcCompleted;
            def.EventCustomizations["rpc_completed"] = new List<XECustomization> { new("evil; DROP", "1") };
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Customizations_NonNumericValue_Throws()
        {
            var def = NewDef();
            def.Events = XETraceEventType.RpcCompleted;
            def.EventCustomizations["rpc_completed"] = new List<XECustomization> { new("collect_statement", "yes'; DROP") };
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        // ---- Structure / happy path -------------------------------------------------------------

        [TestMethod]
        public void Build_SelectedEvents_AreEmitted()
        {
            var def = NewDef();
            def.Events = XETraceEventType.RpcCompleted | XETraceEventType.SqlBatchCompleted |
                         XETraceEventType.ErrorReported;

            var sql = def.BuildCreateSessionSql();

            StringAssert.Contains(sql, "ADD EVENT sqlserver.rpc_completed");
            StringAssert.Contains(sql, "ADD EVENT sqlserver.sql_batch_completed");
            StringAssert.Contains(sql, "ADD EVENT sqlserver.error_reported");
        }

        [TestMethod]
        public void Build_UnselectedEvent_IsNotEmitted()
        {
            var def = NewDef();
            def.Events = XETraceEventType.RpcCompleted;

            var sql = def.BuildCreateSessionSql();

            StringAssert.Contains(sql, "sqlserver.rpc_completed");
            Assert.IsFalse(sql.Contains("sql_batch_completed"));
            Assert.IsFalse(sql.Contains("error_reported"));
        }

        [TestMethod]
        public void Build_ServerScope_EmitsOnServer()
        {
            var sql = NewDef().BuildCreateSessionSql();
            StringAssert.Contains(sql, "CREATE EVENT SESSION [DBADash_AdHoc] ON SERVER");
        }

        [TestMethod]
        public void Build_DatabaseScope_EmitsOnDatabase()
        {
            var def = NewDef();
            def.Scope = XESessionScope.Database;
            StringAssert.Contains(def.BuildCreateSessionSql(), "ON DATABASE");
        }

        [TestMethod]
        public void Build_RingBufferTarget_EmitsRingBuffer()
        {
            StringAssert.Contains(NewDef().BuildCreateSessionSql(), "ADD TARGET package0.ring_buffer");
        }

        [TestMethod]
        public void Build_EventFileTarget_EmitsFilenameAndRollover()
        {
            var def = NewDef();
            def.TargetType = XETraceTargetType.EventFile;
            // Realistic default: the resolved SQL Server LOG directory (always exists, engine-writable).
            def.FileName = @"C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Log\DBADash_AdHoc_42.xel";
            def.MaxFileSizeMB = 250;
            def.MaxRolloverFiles = 3;

            var sql = def.BuildCreateSessionSql();

            StringAssert.Contains(sql, @"package0.event_file(SET filename=N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Log\DBADash_AdHoc_42.xel'");
            StringAssert.Contains(sql, "max_file_size=(250)");
            StringAssert.Contains(sql, "max_rollover_files=(3)");
        }

        [TestMethod]
        public void Build_AlwaysExcludesReaderAppName()
        {
            var sql = NewDef().BuildCreateSessionSql();
            StringAssert.Contains(sql, "[sqlserver].[client_app_name]<>N'DBADashXE'");
        }

        [TestMethod]
        public void Build_DispatchLatency_IsHonoured()
        {
            var def = NewDef();
            def.MaxDispatchLatencySeconds = 2;
            StringAssert.Contains(def.BuildCreateSessionSql(), "MAX_DISPATCH_LATENCY=2 SECONDS");
        }

        // ---- Filters ----------------------------------------------------------------------------

        [TestMethod]
        public void Filter_NumericDuration_ProducesTypedTerm()
        {
            var def = NewDef();
            def.Filters.Add(Duration(XEFilterOp.GreaterThan, "1000000"));

            StringAssert.Contains(def.BuildCreateSessionSql(), "[duration]>(1000000)");
        }

        [TestMethod]
        public void Filter_StringEquality_ProducesEscapedLiteral()
        {
            var def = NewDef();
            def.Filters.Add(AppName(XEFilterOp.Equal, "SQLCMD"));

            StringAssert.Contains(def.BuildCreateSessionSql(), "[sqlserver].[client_app_name]=N'SQLCMD'");
        }

        [TestMethod]
        public void Filter_Like_ProducesLikeTerm()
        {
            var def = NewDef();
            def.Filters.Add(UserName(XEFilterOp.Like, "app_%"));

            StringAssert.Contains(def.BuildCreateSessionSql(), "[sqlserver].[username] LIKE N'app_%'");
        }

        [TestMethod]
        public void Filter_DataColumn_SkippedForEventWithoutTheColumn()
        {
            var def = NewDef();
            def.Events = XETraceEventType.SqlBatchCompleted | XETraceEventType.ErrorReported;
            def.Filters.Add(Duration(XEFilterOp.GreaterThan, "500"));

            var sql = def.BuildCreateSessionSql();

            // The duration term belongs to the completed event only.
            var batchIdx = sql.IndexOf("sql_batch_completed", System.StringComparison.Ordinal);
            var errorIdx = sql.IndexOf("error_reported", System.StringComparison.Ordinal);
            var errorBlock = sql.Substring(errorIdx);
            Assert.IsTrue(sql.Substring(batchIdx, errorIdx - batchIdx).Contains("[duration]>(500)"),
                "duration filter should be on the batch_completed event");
            Assert.IsFalse(errorBlock.Contains("[duration]"),
                "duration is not a column on error_reported and must be skipped");
        }

        [TestMethod]
        public void Filter_ScopedToSpecificEvent_OnlyAppliesToThatEvent()
        {
            var def = NewDef();
            def.Events = XETraceEventType.RpcCompleted | XETraceEventType.SqlBatchCompleted;
            var f = AppName(XEFilterOp.Equal, "SQLCMD");
            f.EventName = "sql_batch_completed"; // scope to just batch
            def.Filters.Add(f);

            var sql = def.BuildCreateSessionSql();
            var rpcIdx = sql.IndexOf("rpc_completed", System.StringComparison.Ordinal);
            var batchIdx = sql.IndexOf("sql_batch_completed", System.StringComparison.Ordinal);
            var rpcBlock = rpcIdx < batchIdx ? sql.Substring(rpcIdx, batchIdx - rpcIdx) : sql.Substring(rpcIdx);
            Assert.IsFalse(rpcBlock.Contains("client_app_name]=N'SQLCMD'"),
                "a batch-scoped filter must not appear on the rpc event");
            StringAssert.Contains(sql.Substring(batchIdx), "client_app_name]=N'SQLCMD'");
        }

        [TestMethod]
        public void ErrorReported_AppliesSeverityFloor()
        {
            var def = NewDef();
            def.Events = XETraceEventType.ErrorReported;
            def.ErrorSeverityFloor = 16;

            StringAssert.Contains(def.BuildCreateSessionSql(), "[severity]>=(16)");
        }

        // ---- Injection / validation guards ------------------------------------------------------

        [TestMethod]
        public void Injection_SingleQuoteInStringValue_IsEscaped()
        {
            var def = NewDef();
            def.Filters.Add(AppName(XEFilterOp.Equal, "x'); DROP EVENT SESSION [DBADash_1] ON SERVER;--"));

            var sql = def.BuildCreateSessionSql();

            // The quote is doubled, so the payload stays inside a single string literal - it cannot break out.
            StringAssert.Contains(sql, "N'x''); DROP EVENT SESSION [DBADash_1] ON SERVER;--'");
            Assert.IsFalse(sql.Contains("N'x');"), "unescaped quote would allow statement break-out");
        }

        [TestMethod]
        public void Injection_MaliciousFieldName_Throws()
        {
            var def = NewDef();
            def.Filters.Add(new XEFilter
            { Field = "duration]=(1) OR [1", IsAction = false, IsNumeric = true, Op = XEFilterOp.Equal, Value = "1" });

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Injection_TrailingNewlineInEventName_Throws()
        {
            // .NET's $ anchor also matches before a trailing \n, so an "^[A-Za-z0-9_]+$" allow-list would leak a
            // trailing newline into the identifier.  The builder must reject it (uses \A..\z).
            var def = NewDef();
            def.Events = 0;
            def.ExtraEvents.Add(new XETraceEventDef("sqlserver", "rpc_completed\n", null));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Injection_TrailingNewlineInActionName_Throws()
        {
            var def = NewDef();
            def.GlobalActions = new List<XEActionDef> { new("sqlserver", "sql_text\n") };

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Injection_TrailingNewlineInFilterField_Throws()
        {
            var def = NewDef();
            def.Filters.Add(new XEFilter
            { Field = "duration\n", IsAction = false, IsNumeric = true, Op = XEFilterOp.Equal, Value = "1" });

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Injection_ControlCharInStringValue_Throws()
        {
            var def = NewDef();
            def.Filters.Add(AppName(XEFilterOp.Equal, "bad\x00value"));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Injection_NonNumericNumericFilter_Throws()
        {
            var def = NewDef();
            def.Filters.Add(Duration(XEFilterOp.GreaterThan, "1000) OR (1=1"));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Validation_OverLongStringValue_Throws()
        {
            var def = NewDef();
            def.Filters.Add(AppName(XEFilterOp.Equal, new string('a', 257)));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Validation_LikeOnNumericField_Throws()
        {
            var def = NewDef();
            def.Filters.Add(Duration(XEFilterOp.Like, "5"));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Validation_ComparisonOnStringField_Throws()
        {
            var def = NewDef();
            def.Filters.Add(AppName(XEFilterOp.GreaterThan, "x"));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Validation_NegativeNumericValue_Throws()
        {
            var def = NewDef();
            def.Filters.Add(SessionId(XEFilterOp.Equal, "-1"));

            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Validation_NoEventsSelected_Throws()
        {
            var def = NewDef();
            def.Events = 0;
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(26)]
        public void Validation_SeverityFloorOutOfRange_Throws(int floor)
        {
            var def = NewDef();
            def.Events = XETraceEventType.ErrorReported;
            def.ErrorSeverityFloor = floor;
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void Validation_EventFileWithoutFileName_Throws()
        {
            var def = NewDef();
            def.TargetType = XETraceTargetType.EventFile;
            def.FileName = null;
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        [DataRow("DBADash_AdHoc]; DROP")]
        [DataRow("has space")]
        [DataRow("DBADash_AdHoc\n")] // trailing newline must be rejected (the \A..\z anchor, not ^..$)
        [DataRow("")]
        public void Validation_BadSessionName_Throws(string name)
        {
            var def = NewDef();
            def.SessionName = name;
            Assert.ThrowsExactly<System.ArgumentException>(() => def.BuildCreateSessionSql());
        }

        [TestMethod]
        public void CustomExcludedAppNames_AreAllEmitted()
        {
            var def = NewDef();
            def.ExcludedAppNames = new List<string> { "DBADashXE", "DBADash" };

            var sql = def.BuildCreateSessionSql();
            StringAssert.Contains(sql, "[sqlserver].[client_app_name]<>N'DBADashXE'");
            StringAssert.Contains(sql, "[sqlserver].[client_app_name]<>N'DBADash'");
        }
    }
}
