using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DBADash.Deadlock;
using DBADash.QueryPlan;
using DBADashAI.Mcp;
using DBADashAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>The masking of what goes to an AI model - see <see cref="AiDataMasker"/>.</summary>
    [TestClass]
    public class AiDataMaskTests
    {
        private static readonly AiDataMask ParametersOnly = new(true, false);
        private static readonly AiDataMask LiteralsOnly = new(false, true);

        // ---------------------------------------------------------------- the tokenizer

        [TestMethod]
        [DataRow("SELECT * FROM t WHERE id = 42 AND name = 'it''s' AND v > 1.5", "SELECT * FROM t WHERE id = ? AND name = ? AND v > ?")]
        [DataRow("SELECT col2 FROM table1 WHERE x = 7", "SELECT col2 FROM table1 WHERE x = ?")]
        [DataRow("WHERE n = N'x' AND b = 0x1A2B AND m = $12.50 AND e = 1.5E+10 AND f = .5", "WHERE n = ? AND b = ? AND m = ? AND e = ? AND f = ?")]
        [DataRow("SELECT [Order 2], \"col 3\", @p1, #t9, ##g2, a$1 FROM t", "SELECT [Order 2], \"col 3\", @p1, #t9, ##g2, a$1 FROM t")]
        [DataRow("SELECT CAST(x AS nvarchar(50)), CAST(y AS decimal(18, 2)), CONVERT(varchar(max), z, 120)", "SELECT CAST(x AS nvarchar(50)), CAST(y AS decimal(18, 2)), CONVERT(varchar(max), z, ?)")]
        [DataRow("SELECT CHAR(65) + NCHAR(8364), CAST(x AS char(10)) WHERE y = CHAR(9)", "SELECT CHAR(?) + NCHAR(?), CAST(x AS char(10)) WHERE y = CHAR(?)")]
        [DataRow("DECLARE @c nchar(5); CREATE TABLE t ([c] char(3), d char(2))", "DECLARE @c nchar(5); CREATE TABLE t ([c] char(3), d char(2))")]
        [DataRow("WAITFOR DELAY '00:10:00'", "WAITFOR DELAY ?")]
        [DataRow("SELECT * FROM t WHERE x = 'never closed", "SELECT * FROM t WHERE x = ?")]
        [DataRow("SELECT [a]]b] FROM t WHERE c = 'x]'", "SELECT [a]]b] FROM t WHERE c = ?")]
        [DataRow("SELECT 1 -- customer 12345 'bob'\nFROM t", "SELECT ? -- customer ? ?\nFROM t")]
        [DataRow("SELECT 1 /* outer /* inner 99 */ 'x' */ FROM t", "SELECT ? /* outer /* inner ? */ ? */ FROM t")]
        [DataRow("[Shop].[dbo].[Customer].[code]=(424242) AND [x]>=CONVERT_IMPLICIT(nvarchar(10),[y],0)", "[Shop].[dbo].[Customer].[code]=(?) AND [x]>=CONVERT_IMPLICIT(nvarchar(10),[y],?)")]
        [DataRow("WHERE d >= {ts '2024-01-01 00:00:00'} AND g = {guid'0E984725-C51C-4BF4-9960-E1C80E27ABA0'}", "WHERE d >= {ts ?} AND g = {guid?}")]
        [DataRow("SELECT TOP (10) x FROM t WHERE y IS NULL", "SELECT TOP (?) x FROM t WHERE y IS NULL")]
        public void MaskLiterals_MasksEveryLiteral_AndKeepsIdentifiersAndTypes(string sql, string expected)
        {
            Assert.AreEqual(expected, AiDataMasker.MaskLiterals(sql));
        }

        [TestMethod]
        public void MaskLiterals_IsIdempotent()
        {
            const string sql = "SELECT N'a', 0x1F, $3, 1e5, x FROM t WHERE y = 'b''c' -- 12 'd'";
            var once = AiDataMasker.MaskLiterals(sql);
            Assert.AreEqual(once, AiDataMasker.MaskLiterals(once));
        }

        [TestMethod]
        public void MaskSql_FollowsTheMask()
        {
            Assert.AreEqual("SELECT ?", AiDataMasker.MaskSql("SELECT 9", AiDataMask.Default));
            Assert.AreEqual("SELECT 9", AiDataMasker.MaskSql("SELECT 9", ParametersOnly), "Literals stay when only parameters are masked.");
            Assert.AreEqual("SELECT 9", AiDataMasker.MaskSql("SELECT 9", AiDataMask.None));
            Assert.AreEqual(AiDataMasker.Withheld, AiDataMasker.MaskSql("SELECT 9", AiDataMask.Metadata));
        }

        // An application's parameterised calls, as they show in slow queries, input buffers and running queries.
        [TestMethod]
        [DataRow("exec dbo.GetCustomer @Email=N'bob@example.com', @Id=42",
            "exec dbo.GetCustomer @Email=?, @Id=?",
            "exec dbo.GetCustomer @Email=?, @Id=?")]
        [DataRow("exec sp_executesql N'SELECT * FROM t WHERE x = 1 AND id = @id',N'@id int, @name nvarchar(50)',@id=987654,@name=N'Smith'",
            "exec sp_executesql N'SELECT * FROM t WHERE x = 1 AND id = @id',N'@id int, @name nvarchar(50)',@id=?,@name=?",
            "exec sp_executesql N'SELECT * FROM t WHERE x = ? AND id = @id',N'@id int, @name nvarchar(50)',@id=?,@name=?")]
        [DataRow("EXEC [sys].[sp_executesql] @stmt = N'UPDATE t SET a = ''it''''s'' WHERE id = @p', @params = N'@p int', @p = 7",
            "EXEC [sys].[sp_executesql] @stmt = N'UPDATE t SET a = ''it''''s'' WHERE id = @p', @params = N'@p int', @p = ?",
            "EXEC [sys].[sp_executesql] @stmt = N'UPDATE t SET a = ? WHERE id = @p', @params = N'@p int', @p = ?")]
        [DataRow("declare @p1 int\nset @p1=5\nexec sp_prepexec @p1 output,N'@P1 varchar(20)',N'SELECT name FROM t WHERE code = @P1 AND n > 3','SECRET'\nselect @p1",
            "declare @p1 int\nset @p1=?\nexec sp_prepexec @p1 output,N'@P1 varchar(20)',N'SELECT name FROM t WHERE code = @P1 AND n > 3',?\nselect @p1",
            "declare @p1 int\nset @p1=?\nexec sp_prepexec @p1 output,N'@P1 varchar(20)',N'SELECT name FROM t WHERE code = @P1 AND n > ?',?\nselect @p1")]
        [DataRow("declare @p1 datetime2(7)\nset @p1='2026-10-31 10:24:24.7036173'\ndeclare @p9 bit\nset @p9=NULL\nexec dbo.R_Get @From=@p1 output,@Flag=@p9 output\nselect @p1, @p9",
            "declare @p1 datetime2(7)\nset @p1=?\ndeclare @p9 bit\nset @p9=NULL\nexec dbo.R_Get @From=@p1 output,@Flag=@p9 output\nselect @p1, @p9",
            "declare @p1 datetime2(7)\nset @p1=?\ndeclare @p9 bit\nset @p9=NULL\nexec dbo.R_Get @From=@p1 output,@Flag=@p9 output\nselect @p1, @p9")]
        [DataRow("exec @rc = dbo.p 5, 'x' ; SELECT 'kept' FROM t",
            "exec @rc = dbo.p ?, ? ; SELECT 'kept' FROM t",
            "exec @rc = dbo.p ?, ? ; SELECT ? FROM t")]
        [DataRow("EXEC (N'SELECT * FROM t WHERE c = ''abc''')",
            "EXEC (N'SELECT * FROM t WHERE c = ''abc''')",
            "EXEC (N'SELECT * FROM t WHERE c = ?')")]
        [DataRow("exec sp_executesql N'SELECT * FROM t WHERE c = ''x'' AND id = @id',N'@id int',@id=9876",
            "exec sp_executesql N'SELECT * FROM t WHERE c = ''x'' AND id = @id',N'@id int',@id=?",
            "exec sp_executesql N'SELECT * FROM t WHERE c = ? AND id = @id',N'@id int',@id=?")]
        [DataRow("exec sp_executesql N'SELECT * FROM t WHERE c = ''abc",
            "exec sp_executesql N'SELECT * FROM t WHERE c = ''abc",
            "exec sp_executesql N'SELECT * FROM t WHERE c = ?")]
        public void Calls_MaskTheValuesPassed_AndKeepTheStatementToRead(string sql, string parametersOnly, string masked)
        {
            Assert.AreEqual(parametersOnly, AiDataMasker.MaskSql(sql, ParametersOnly));
            Assert.AreEqual(masked, AiDataMasker.MaskSql(sql, AiDataMask.Default));
            Assert.AreEqual(masked, AiDataMasker.MaskSql(masked, AiDataMask.Default), "Masking again changes nothing.");
            Assert.AreEqual(parametersOnly, AiDataMasker.MaskSql(parametersOnly, ParametersOnly), "Masking again changes nothing.");
        }

        [TestMethod]
        public void Calls_GrantExecute_AndAStatementAfterACall_AreNotArguments()
        {
            Assert.AreEqual("GRANT EXECUTE ON dbo.p TO bob", AiDataMasker.MaskSql("GRANT EXECUTE ON dbo.p TO bob", ParametersOnly));
            Assert.AreEqual("exec p ?\nSELECT 'kept'", AiDataMasker.MaskSql("exec p 1\nSELECT 'kept'", ParametersOnly));
            Assert.AreEqual("SET @Count = 5", AiDataMasker.MaskSql("SET @Count = 5", ParametersOnly), "A module's own variable is code, not a parameter value.");
        }

        [TestMethod]
        public void SlowAndRunningQueries_AreMasked_ForEachMask()
        {
            var data = JsonDocument.Parse("""
                {"rows":[{"SampleQueryText":"exec dbo.Find @Name=N'Smith', @Top=10","SampleUsername":"DOMAIN\\bob",
                          "StatementText":"exec sp_executesql N'SELECT 1 WHERE a = @a',N'@a int',@a=424242"}]}
                """).RootElement;

            var parameters = AiDataMasker.MaskJson(data, ParametersOnly).GetProperty("rows")[0];
            Assert.AreEqual("exec dbo.Find @Name=?, @Top=?", parameters.GetProperty("SampleQueryText").GetString());
            Assert.AreEqual("exec sp_executesql N'SELECT 1 WHERE a = @a',N'@a int',@a=?", parameters.GetProperty("StatementText").GetString());

            var both = AiDataMasker.MaskJson(data, AiDataMask.Default).GetProperty("rows")[0];
            Assert.AreEqual("exec sp_executesql N'SELECT ? WHERE a = @a',N'@a int',@a=?", both.GetProperty("StatementText").GetString());

            var none = AiDataMasker.MaskJson(data, AiDataMask.None).GetProperty("rows")[0];
            StringAssert.Contains(none.GetProperty("StatementText").GetString(), "424242");

            var metadata = AiDataMasker.MaskJson(data, AiDataMask.Metadata).GetProperty("rows")[0];
            StringAssert.StartsWith(metadata.GetProperty("SampleUsername").GetString(), "login#");
        }

        // ---------------------------------------------------------------- plans, end to end

        private const string Plan = """
            <?xml version="1.0" encoding="utf-16"?>
            <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.564" Build="16.0.4150.1">
              <BatchSequence><Batch><Statements>
                <StmtSimple StatementText="SELECT name FROM dbo.Customer WHERE id = @id AND region = N'SECRETREGION' AND code = 424242" StatementId="1" StatementCompId="1" StatementType="SELECT" StatementSubTreeCost="0.0032831" StatementEstRows="1" QueryHash="0x1111111111111111" QueryPlanHash="0x2222222222222222">
                  <QueryPlan CachedPlanSize="16" CompileTime="1" CompileCPU="1" CompileMemory="200">
                    <RelOp NodeId="0" PhysicalOp="Clustered Index Seek" LogicalOp="Clustered Index Seek" EstimateRows="1" EstimateIO="0.003125" EstimateCPU="0.0001581" AvgRowSize="20" EstimatedTotalSubtreeCost="0.0032831" Parallel="0" EstimateRebinds="0" EstimateRewinds="0">
                      <OutputList />
                      <IndexScan Ordered="1" ScanDirection="FORWARD">
                        <Object Database="[Shop]" Schema="[dbo]" Table="[Customer]" Index="[PK_Customer]" />
                        <Predicate>
                          <ScalarOperator ScalarString="[Shop].[dbo].[Customer].[region]=N'SECRETREGION' AND [Shop].[dbo].[Customer].[code]=(424242)">
                            <Const ConstValue="N'SECRETREGION'" />
                          </ScalarOperator>
                        </Predicate>
                      </IndexScan>
                    </RelOp>
                    <ParameterList>
                      <ColumnReference Column="@id" ParameterDataType="int" ParameterCompiledValue="(987654)" ParameterRuntimeValue="(123321)" />
                      <ColumnReference Column="@flag" ParameterDataType="bit" ParameterCompiledValue="NULL" ParameterRuntimeValue="NULL" />
                    </ParameterList>
                  </QueryPlan>
                </StmtSimple>
              </Statements></Batch></BatchSequence>
            </ShowPlanXML>
            """;

        private static readonly string[] ParameterValues = ["987654", "123321"];
        private static readonly string[] Literals = ["SECRETREGION", "424242"];

        /// <summary>What get-query-plan and describe-query-plan return for the plan, with its XML.</summary>
        private static string Describe(AiDataMask mask)
        {
            var plan = PlanParser.Parse(AiDataMasker.MaskXml(Plan, mask));
            return GetQueryPlanTool.Describe(plan, plan.Statements[0], null, null, "Heading.", includePlanXml: true, 1_000_000, mask);
        }

        [TestMethod]
        public void Plan_Default_MasksParameterValuesAndLiterals_InTheSummaryAndTheXml()
        {
            var text = Describe(AiDataMask.Default);

            foreach (var secret in ParameterValues.Concat(Literals))
            {
                Assert.IsFalse(text.Contains(secret, StringComparison.Ordinal), $"{secret} is in the result:{Environment.NewLine}{text}");
            }

            StringAssert.Contains(text, "(value A)");
            StringAssert.Contains(text, "(value B)");
            StringAssert.Contains(text, "different from the compiled value", "Parameter sniffing is still visible through the placeholders.");
            StringAssert.Contains(text, "NULL", "NULL is not data.");
            StringAssert.Contains(text, "Masked by DBA Dash's data setting", "The model is told what ? and (value A) are.");
        }

        [TestMethod]
        public void Plan_ParametersOnly_KeepsLiterals()
        {
            var text = Describe(ParametersOnly);
            foreach (var secret in ParameterValues)
            {
                Assert.IsFalse(text.Contains(secret, StringComparison.Ordinal), $"{secret} is in the result.");
            }

            StringAssert.Contains(text, "SECRETREGION");
            StringAssert.Contains(text, "424242");
        }

        [TestMethod]
        public void Plan_LiteralsOnly_KeepsParameterValues()
        {
            var text = Describe(LiteralsOnly);
            foreach (var secret in Literals)
            {
                Assert.IsFalse(text.Contains(secret, StringComparison.Ordinal), $"{secret} is in the result.");
            }

            StringAssert.Contains(text, "987654");
        }

        [TestMethod]
        public void Plan_None_IsUnchanged()
        {
            Assert.AreEqual(Plan, AiDataMasker.MaskXml(Plan, AiDataMask.None));
            StringAssert.Contains(Describe(AiDataMask.None), "987654");
        }

        [TestMethod]
        public void Plan_MetadataOnly_WithholdsTheStatement()
        {
            var text = Describe(AiDataMask.Metadata);
            foreach (var secret in ParameterValues.Concat(Literals))
            {
                Assert.IsFalse(text.Contains(secret, StringComparison.Ordinal), $"{secret} is in the result.");
            }

            StringAssert.Contains(text, AiDataMasker.Withheld);
        }

        [TestMethod]
        public void Plan_MaskedXml_KeepsTheFiguresAndIdentity()
        {
            var masked = AiDataMasker.MaskXml(Plan, AiDataMask.Default);
            StringAssert.Contains(masked, "EstimateRows=\"1\"");
            StringAssert.Contains(masked, "QueryPlanHash=\"0x2222222222222222\"");
            StringAssert.Contains(masked, "ParameterDataType=\"int\"");
            Assert.AreEqual(masked, AiDataMasker.MaskXml(masked, AiDataMask.Default), "Masking a masked plan changes nothing.");
        }

        [TestMethod]
        public void Plan_QuotedIdentifierOff_MasksDoubleQuotedText()
        {
            const string xml = """
                <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan"><BatchSequence><Batch><Statements>
                <StmtSimple StatementId="1" StatementText="SELECT &quot;customer-secret&quot; FROM t"><StatementSetOptions QUOTED_IDENTIFIER="false" /></StmtSimple>
                <StmtSimple StatementId="2" StatementText="SELECT &quot;col&quot; FROM t"><StatementSetOptions QUOTED_IDENTIFIER="true" /></StmtSimple>
                </Statements></Batch></BatchSequence></ShowPlanXML>
                """;

            var texts = System.Xml.Linq.XDocument.Parse(AiDataMasker.MaskXml(xml, AiDataMask.Default))
                .Descendants().Where(e => e.Name.LocalName == "StmtSimple").Select(e => (string?)e.Attribute("StatementText")).ToList();

            CollectionAssert.AreEqual(new[] { "SELECT ? FROM t", "SELECT \"col\" FROM t" }, texts);
        }

        // ---------------------------------------------------------------- deadlocks

        private const string Deadlock = """
            <deadlock>
              <victim-list><victimProcess id="process1" /></victim-list>
              <process-list>
                <process id="process1" taskpriority="0" logused="0" waitresource="KEY: 7:72057594043039744 (8194443284a0)" waittime="1000" ownerId="1" transactionname="user_transaction" lasttranstarted="2024-01-01T00:00:00" XDES="0x1" lockMode="U" schedulerid="1" kpid="1" status="suspended" spid="55" sbid="0" ecid="0" priority="0" trancount="2" lastbatchstarted="2024-01-01T00:00:00" lastbatchcompleted="2024-01-01T00:00:00" lastattention="1900-01-01T00:00:00" clientapp="PayrollApp" hostname="APP01" hostpid="1" loginname="DOMAIN\bob" isolationlevel="read committed (2)" xactid="1" currentdb="7" currentdbname="Shop" lockTimeout="4294967295" clientoption1="0" clientoption2="0">
                  <executionStack>
                    <frame procname="Shop.dbo.Proc2" line="5" stmtstart="100" stmtend="200" sqlhandle="0x03">UPDATE dbo.DeadlockTest SET Col1 = 'SECRETVALUE' WHERE ID = 555111</frame>
                  </executionStack>
                  <inputbuf>EXEC dbo.Proc2 @ID = 555111, @Name = N'SECRETVALUE'</inputbuf>
                </process>
              </process-list>
              <resource-list />
            </deadlock>
            """;

        [TestMethod]
        public void Deadlock_InputBuffersAndFrames_AreMasked_AndStillParse()
        {
            var masked = AiDataMasker.MaskXml(Deadlock, AiDataMask.Default);
            Assert.IsFalse(masked.Contains("SECRETVALUE", StringComparison.Ordinal));
            Assert.IsFalse(masked.Contains("555111", StringComparison.Ordinal));
            StringAssert.Contains(masked, "<inputbuf>EXEC dbo.Proc2 @ID = ?, @Name = ?</inputbuf>");
            StringAssert.Contains(masked, "loginname=\"DOMAIN\\bob\"", "Logins are only pseudonymised for metadataOnly.");
            Assert.AreEqual(1, DeadlockParser.Parse(masked).Count);
        }

        [TestMethod]
        public void Deadlock_MetadataOnly_PseudonymisesLoginsHostsAndApps()
        {
            var masked = AiDataMasker.MaskXml(Deadlock, AiDataMask.Metadata);
            Assert.IsFalse(masked.Contains("DOMAIN", StringComparison.Ordinal));
            Assert.IsFalse(masked.Contains("APP01", StringComparison.Ordinal));
            Assert.IsFalse(masked.Contains("PayrollApp", StringComparison.Ordinal));
            StringAssert.Contains(masked, "loginname=\"login#");
            StringAssert.Contains(masked, "hostname=\"host#");
            StringAssert.Contains(masked, "<inputbuf>[withheld]</inputbuf>");
        }

        // ---------------------------------------------------------------- results by name

        [TestMethod]
        public void Json_TextColumns_AreMaskedByName_WhateverTheTool()
        {
            var data = JsonDocument.Parse("""
                {"rows":[{"VictimStatement":"UPDATE t SET a = 'x' WHERE ID = 5","SampleQueryText":"SELECT 7","statement_text":"SELECT 8",
                          "InputBuffer":"EXEC p 9","QueryPlan":"<ShowPlanXML><Const ConstValue=\"(11)\" /></ShowPlanXML>",
                          "login_name":"DOMAIN\\bob","wait_type":"CXPACKET","QueryName":"dbo.Proc 2","StatementType":"SELECT",
                          "LastFailedLogin":"2024-01-01T00:00:00","Count":3}]}
                """).RootElement;

            var row = AiDataMasker.MaskJson(data, AiDataMask.Default).GetProperty("rows")[0];
            Assert.AreEqual("UPDATE t SET a = ? WHERE ID = ?", row.GetProperty("VictimStatement").GetString());
            Assert.AreEqual("SELECT ?", row.GetProperty("SampleQueryText").GetString());
            Assert.AreEqual("SELECT ?", row.GetProperty("statement_text").GetString());
            Assert.AreEqual("EXEC p ?", row.GetProperty("InputBuffer").GetString());
            StringAssert.Contains(row.GetProperty("QueryPlan").GetString(), "ConstValue=\"(?)\"");
            Assert.AreEqual("DOMAIN\\bob", row.GetProperty("login_name").GetString());
            Assert.AreEqual("CXPACKET", row.GetProperty("wait_type").GetString());
            Assert.AreEqual("dbo.Proc 2", row.GetProperty("QueryName").GetString());
            Assert.AreEqual("2024-01-01T00:00:00", row.GetProperty("LastFailedLogin").GetString());

            var metadata = AiDataMasker.MaskJson(data, AiDataMask.Metadata).GetProperty("rows")[0];
            Assert.AreEqual(AiDataMasker.Withheld, metadata.GetProperty("VictimStatement").GetString());
            StringAssert.StartsWith(metadata.GetProperty("login_name").GetString(), "login#");
            Assert.AreEqual("2024-01-01T00:00:00", metadata.GetProperty("LastFailedLogin").GetString(), "A date is not a login.");
        }

        [TestMethod]
        public void LookupTables_AreMaskedByColumn()
        {
            var sets = new List<List<Dictionary<string, object?>>>
            {
                new()
                {
                    new() { ["DatabaseName"] = "master", ["StatementText"] = "WAITFOR DELAY '00:10:00'", ["MaxElapsedMs"] = 597653 }
                }
            };

            var text = PerformanceLookups.Format("x", sets, PerformanceLookups.LongRunning, t => t.ToString("O"), "Times are UTC.", false, 10_000);
            StringAssert.Contains(text, "master | WAITFOR DELAY ? | 597653");

            var unmasked = PerformanceLookups.Format("x", sets, PerformanceLookups.LongRunning, t => t.ToString("O"), "Times are UTC.", false, 10_000, AiDataMask.None);
            StringAssert.Contains(unmasked, "'00:10:00'");
        }

        // ---------------------------------------------------------------- the backstop

        [TestMethod]
        public void FilterText_CatchesXmlAndJsonInFinishedText()
        {
            const string text = "Plan:\n<StmtSimple StatementText=\"SELECT 1 WHERE a = 'b'\"><ColumnReference ParameterCompiledValue=\"(5)\" ParameterRuntimeValue=\"(6)\" /><Const ConstValue=\"(77)\" /></StmtSimple>\n" +
                                "{\"rows\": [{\"VictimStatement\": \"DELETE t WHERE id = 88\", \"Other\": \"99\"}]}\n" +
                                "{\"QueryPlan\": \"<StmtSimple StatementText=\\\"SELECT 'x'\\\" />\"}";

            var filtered = AiDataMasker.FilterText(text, AiDataMask.Default);
            StringAssert.Contains(filtered, "StatementText=\"SELECT ? WHERE a = ?\"");
            StringAssert.Contains(filtered, "ParameterCompiledValue=\"(value A)\"");
            StringAssert.Contains(filtered, "ParameterRuntimeValue=\"(value B)\"");
            StringAssert.Contains(filtered, "ConstValue=\"(?)\"");
            StringAssert.Contains(filtered, "\"VictimStatement\": \"DELETE t WHERE id = ?\"");
            StringAssert.Contains(filtered, "\"Other\": \"99\"");
            Assert.IsFalse(filtered.Contains("'x'", StringComparison.Ordinal), filtered);
            Assert.AreEqual(filtered, AiDataMasker.FilterText(filtered, AiDataMask.Default), "Filtering filtered text changes nothing.");
        }

        // ---------------------------------------------------------------- the setting

        [TestMethod]
        public void Words_RoundTrip()
        {
            foreach (var word in AiDataMask.Words)
            {
                Assert.IsTrue(AiDataMask.TryParse(word, out var mask), word);
                Assert.AreEqual(word, mask.Word);
            }

            Assert.IsFalse(AiDataMask.TryParse("everything", out _));
            Assert.AreEqual("parametersAndLiterals", AiDataMask.Default.Word);
        }

        [TestMethod]
        public void FromConfiguration_DefaultsToMasked_AndRejectsATypo()
        {
            Assert.AreEqual(AiDataMask.Default, AiDataMask.FromConfiguration(new ConfigurationBuilder().Build()));

            var typo = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [AiDataMask.SettingName] = "maskliteral" })
                .Build();
            try
            {
                AiDataMask.FromConfiguration(typo);
                Assert.Fail("A misspelt level must stop the server, not fall back.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void BuiltInSetting_IsAFloor()
        {
            var setting = new AiDataDetailSetting();
            Assert.AreEqual(AiDataMask.Default, setting.Value, "Masked until the repository has been read.");

            // At the default, a request can't turn masking off...
            Assert.AreEqual(AiDataMask.Default, setting.For("full"));
            Assert.IsNotNull(setting.Refusal("full"));
            Assert.IsNotNull(setting.Refusal(null), "A caller that says nothing is taken to have masked nothing.");
            Assert.IsNull(setting.Refusal("parametersAndLiterals"));
            Assert.IsNull(setting.Refusal("metadataOnly"), "More masking is always allowed.");

            // ...but at full it can.
            setting.Set(AiDataMask.None);
            Assert.AreEqual(AiDataMask.None, setting.For("full"));
            Assert.AreEqual(AiDataMask.Default, setting.For(null), "A request that says nothing is masked.");
            Assert.IsNull(setting.Refusal("full"));

            setting.Set(new AiDataMask(true, false));
            Assert.AreEqual(new AiDataMask(true, true), setting.For("literals"));
            Assert.IsNotNull(setting.Refusal("literals"));
        }

        [TestMethod]
        public void Pseudonym_IsStableAcrossCallsAndCase()
        {
            Assert.AreEqual(AiDataMasker.Pseudonym("login", "DOMAIN\\Bob"), AiDataMasker.Pseudonym("login", "domain\\bob"));
            Assert.AreNotEqual(AiDataMasker.Pseudonym("login", "bob"), AiDataMasker.Pseudonym("login", "alice"));
            var once = AiDataMasker.Pseudonym("hostname", "APP01");
            Assert.AreEqual(once, AiDataMasker.Pseudonym("hostname", once), "A pseudonym is not hashed again.");
        }

        // ---------------------------------------------------------------- messages and other text

        private const string JobMessage =
            "Executed as user: CONTOSO\\svc_payroll. Conversion failed when converting the varchar value 'ACME-4411' to data type int. [SQLSTATE 22018] (Error 245).  The step failed.";

        [TestMethod]
        public void AJobMessage_LosesItsQuotedValues_AndKeepsItsErrorNumbers()
        {
            var masked = AiDataMasker.MaskNamed("message", JobMessage, AiDataMask.Default)!;

            Assert.DoesNotContain("ACME-4411", masked);
            StringAssert.Contains(masked, "converting the varchar value ? to data type int");
            StringAssert.Contains(masked, "(Error 245)");
            StringAssert.Contains(masked, "CONTOSO\\svc_payroll", "The login is only coded for metadata only.");
            Assert.AreEqual(JobMessage, AiDataMasker.MaskNamed("message", JobMessage, ParametersOnly));
        }

        [TestMethod]
        public void AJobMessage_UnderMetadataOnly_CodesWhoItRanAs()
        {
            var masked = AiDataMasker.MaskNamed("LastMessage", JobMessage, AiDataMask.Metadata)!;

            Assert.DoesNotContain("svc_payroll", masked);
            StringAssert.Contains(masked, "Executed as user: " + AiDataMasker.Pseudonym("login", "CONTOSO\\svc_payroll") + ".");
            Assert.DoesNotContain("ACME-4411", masked);
        }

        [TestMethod]
        [DataRow(@"Executed as user: NT SERVICE\SQLSERVERAGENT. The step failed.", @"NT SERVICE\SQLSERVERAGENT")]
        [DataRow("Executed as user: svc.app.prod. The step failed.", "svc.app.prod")]
        [DataRow(@"Job run by user: CONTOSO\Bob", @"CONTOSO\Bob")]
        public void ALoginWithSpacesOrDots_IsCodedWhole(string message, string login)
        {
            var masked = AiDataMasker.MaskNamed("message", message, AiDataMask.Metadata)!;

            StringAssert.Contains(masked, AiDataMasker.Pseudonym("login", login));
            Assert.DoesNotContain(login, masked);
        }

        [TestMethod]
        public void AFailedLoginsClient_IsCodedAsAHost()
        {
            var masked = AiDataMasker.MaskNamed("Client", "[CLIENT: 10.20.30.41]", AiDataMask.Metadata)!;

            StringAssert.StartsWith(masked, "host#");
            Assert.AreEqual("[CLIENT: 10.20.30.41]", AiDataMasker.MaskNamed("Client", "[CLIENT: 10.20.30.41]", AiDataMask.Default));
        }

        [TestMethod]
        [DataRow("Value 'secret' was rejected", "Value ? was rejected")]
        [DataRow("N'unicode secret' in a column nobody listed", "? in a column nobody listed")]
        [DataRow("Don't touch the users' apostrophes", "Don't touch the users' apostrophes")]
        [DataRow("SQL01\\Prod", "SQL01\\Prod")]
        public void AColumnNotOnTheLists_StillLosesQuotedLiterals(string value, string expected)
        {
            Assert.AreEqual(expected, AiDataMasker.MaskNamed("SomeNewColumn", value, AiDataMask.Default));
            Assert.AreEqual(expected, AiDataMasker.MaskNamed("SomeNewColumn", value, AiDataMask.Metadata));
            Assert.AreEqual(value, AiDataMasker.MaskNamed("SomeNewColumn", value, ParametersOnly));
        }
    }
}
