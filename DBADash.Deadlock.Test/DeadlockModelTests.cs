using System;
using System.Linq;
using DBADash.Deadlock.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// The derived members on the model that the layout and rendering layers will read.
    /// </summary>
    [TestClass]
    public class DeadlockModelTests
    {
        private static DeadlockProcess FirstProcess(string xml) =>
            DeadlockParser.Parse(xml).Single().Processes.First();

        [TestMethod]
        public void ProcessDisplayName_UsesSpid()
        {
            var process = FirstProcess(
                """
                <deadlock><process-list><process id="p1" spid="61" ecid="0" /></process-list></deadlock>
                """);

            Assert.AreEqual("SPID 61", process.DisplayName);
        }

        [TestMethod]
        public void ProcessDisplayName_IncludesEcidForParallelWorker()
        {
            var process = FirstProcess(
                """
                <deadlock><process-list><process id="p1" spid="88" ecid="3" /></process-list></deadlock>
                """);

            Assert.AreEqual("SPID 88 (ecid 3)", process.DisplayName);
        }

        [TestMethod]
        public void ProcessDisplayName_FallsBackToIdWhenSpidMissing()
        {
            var process = FirstProcess(
                """
                <deadlock><process-list><process id="processabc123" /></process-list></deadlock>
                """);

            Assert.AreEqual("processabc123", process.DisplayName);
        }

        [TestMethod]
        public void PrimaryStatement_PrefersInnermostExecutionStackFrame()
        {
            var process = FirstProcess(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <executionStack>
                        <frame procname="dbo.usp_Inner" line="4">UPDATE dbo.T SET C = 1;</frame>
                        <frame procname="adhoc" line="1">EXEC dbo.usp_Inner;</frame>
                      </executionStack>
                      <inputbuf>EXEC dbo.usp_Inner;</inputbuf>
                    </process>
                  </process-list>
                </deadlock>
                """);

            Assert.AreEqual("UPDATE dbo.T SET C = 1;", process.PrimaryStatement);
        }

        [TestMethod]
        public void PrimaryStatement_SkipsEmptyFrames()
        {
            var process = FirstProcess(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <executionStack>
                        <frame procname="unknown" line="1" />
                        <frame procname="adhoc" line="1">SELECT 1;</frame>
                      </executionStack>
                    </process>
                  </process-list>
                </deadlock>
                """);

            Assert.AreEqual("SELECT 1;", process.PrimaryStatement);
        }

        [TestMethod]
        public void PrimaryStatement_IgnoresUnknownFrameTextAndFallsBackToInputBuffer()
        {
            // SQL Server writes the literal placeholder "unknown" as the frame text when it cannot
            // resolve the statement - common for adhoc batches whose plan has aged out of cache.
            var process = FirstProcess(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <executionStack>
                        <frame procname="adhoc" line="1" stmtstart="38" stmtend="180">
                unknown    </frame>
                        <frame procname="adhoc" line="1" stmtend="132">
                unknown    </frame>
                      </executionStack>
                      <inputbuf>
                UPDATE dbo.[DeadlockResource2]
                SET Value = Value + 1
                WHERE Id = 1;   </inputbuf>
                    </process>
                  </process-list>
                </deadlock>
                """);

            Assert.AreEqual(
                "UPDATE dbo.[DeadlockResource2]\nSET Value = Value + 1\nWHERE Id = 1;",
                process.PrimaryStatement);
        }

        [TestMethod]
        public void PrimaryStatement_FallsBackToInputBuffer()
        {
            var process = FirstProcess(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <inputbuf>EXEC dbo.usp_Something;</inputbuf>
                    </process>
                  </process-list>
                </deadlock>
                """);

            Assert.AreEqual("EXEC dbo.usp_Something;", process.PrimaryStatement);
        }

        [TestMethod]
        public void PrimaryStatement_IsNullWhenNoTextAvailable()
        {
            var process = FirstProcess(
                """
                <deadlock><process-list><process id="p1" spid="61" /></process-list></deadlock>
                """);

            Assert.IsNull(process.PrimaryStatement);
        }

        [TestMethod]
        public void ResourceDisplayName_CombinesObjectAndIndex()
        {
            var resource = DeadlockParser.Parse(
                    """
                    <deadlock>
                      <resource-list>
                        <keylock objectname="Sales.dbo.Orders" indexname="PK_Orders" mode="X" />
                      </resource-list>
                    </deadlock>
                    """)
                .Single().Resources.Single();

            // The database is left off the name that identifies the resource on screen, and kept on
            // the two that are read away from it.
            Assert.AreEqual("dbo.Orders (PK_Orders)", resource.DisplayName);
            Assert.AreEqual("Sales.dbo.Orders (PK_Orders)", resource.ObjectDisplayName);
            Assert.AreEqual("Sales.dbo.Orders (PK_Orders)", resource.QualifiedDisplayName);
            Assert.AreEqual("Sales", resource.DatabaseName);
            Assert.AreEqual("dbo.Orders", resource.SchemaQualifiedName);
        }

        /// <summary>
        /// The graph writes object names unquoted, so a database with a dot in it is ambiguous.
        /// Splitting at the last two dots rather than the first resolves it the way that is right far
        /// more often: schema and object names very rarely contain one.
        /// </summary>
        [TestMethod]
        [DataRow("Sales.dbo.Orders", "Sales", "dbo.Orders")]
        [DataRow("{ffd5880a-42ec}_Tenant.dbo.UploadedFiles", "{ffd5880a-42ec}_Tenant", "dbo.UploadedFiles")]
        [DataRow("my.db.dbo.Orders", "my.db", "dbo.Orders")]
        public void ResourceObjectName_SplitsAtTheLastTwoDots(string objectName, string database, string obj)
        {
            var resource = DeadlockParser.Parse(
                    $"""
                    <deadlock>
                      <resource-list>
                        <keylock objectname="{objectName}" mode="X" />
                      </resource-list>
                    </deadlock>
                    """)
                .Single().Resources.Single();

            Assert.AreEqual(database, resource.DatabaseName);
            Assert.AreEqual(obj, resource.SchemaQualifiedName);
        }

        /// <summary>A name with nothing to strip is left whole rather than mangled.</summary>
        [TestMethod]
        public void ResourceObjectName_WithoutADatabasePart_IsLeftAlone()
        {
            var resource = DeadlockParser.Parse(
                    """
                    <deadlock>
                      <resource-list>
                        <keylock objectname="dbo.Orders" mode="X" />
                      </resource-list>
                    </deadlock>
                    """)
                .Single().Resources.Single();

            Assert.IsNull(resource.DatabaseName);
            Assert.AreEqual("dbo.Orders", resource.SchemaQualifiedName);
            Assert.AreEqual("dbo.Orders", resource.DisplayName);
        }

        [TestMethod]
        [DataRow("keylock", DeadlockResourceType.KeyLock)]
        [DataRow("pagelock", DeadlockResourceType.PageLock)]
        [DataRow("objectlock", DeadlockResourceType.ObjectLock)]
        [DataRow("ridlock", DeadlockResourceType.RidLock)]
        [DataRow("hobtlock", DeadlockResourceType.HobtLock)]
        [DataRow("allocunitlock", DeadlockResourceType.AllocUnitLock)]
        [DataRow("databaselock", DeadlockResourceType.DatabaseLock)]
        [DataRow("filelock", DeadlockResourceType.FileLock)]
        [DataRow("extentlock", DeadlockResourceType.ExtentLock)]
        [DataRow("applicationlock", DeadlockResourceType.ApplicationLock)]
        [DataRow("metadatalock", DeadlockResourceType.MetadataLock)]
        [DataRow("xactlock", DeadlockResourceType.TransactionLock)]
        [DataRow("exchangeEvent", DeadlockResourceType.ExchangeEvent)]
        [DataRow("threadPoolWait", DeadlockResourceType.ThreadPoolWait)]
        [DataRow("waitfor", DeadlockResourceType.WaitFor)]
        [DataRow("syncPoint", DeadlockResourceType.SyncPoint)]
        [DataRow("brandNewLockType", DeadlockResourceType.Unknown)]
        public void ResourceType_IsMappedFromElementName(string elementName, DeadlockResourceType expected)
        {
            var xml = $"<deadlock><resource-list><{elementName} /></resource-list></deadlock>";

            var resource = DeadlockParser.Parse(xml).Single().Resources.Single();

            Assert.AreEqual(expected, resource.Type);
            Assert.AreEqual(elementName, resource.TypeName, "The raw element name must be preserved.");
        }

        [TestMethod]
        [DataRow("exchangeEvent")]
        [DataRow("threadPoolWait")]
        [DataRow("syncPoint")]
        public void IsParallelismResource_IsTrueForIntraQueryResources(string elementName)
        {
            var xml = $"<deadlock><resource-list><{elementName} /></resource-list></deadlock>";

            var graph = DeadlockParser.Parse(xml).Single();

            Assert.IsTrue(graph.Resources.Single().IsParallelismResource);
            Assert.IsTrue(graph.IsParallel);
        }

        [TestMethod]
        public void IsParallel_IsTrueWhenProcessesShareSpid()
        {
            // A parallel deadlock without a parallelism resource still shows several process entries
            // sharing one spid.
            var graph = DeadlockParser.Parse(
                    """
                    <deadlock>
                      <process-list>
                        <process id="p1" spid="88" ecid="0" />
                        <process id="p2" spid="88" ecid="1" />
                      </process-list>
                    </deadlock>
                    """)
                .Single();

            Assert.IsTrue(graph.IsParallel);
        }

        [TestMethod]
        public void IsParallel_IsFalseForDistinctSpids()
        {
            var graph = DeadlockParser.Parse(
                    """
                    <deadlock>
                      <process-list>
                        <process id="p1" spid="61" ecid="0" />
                        <process id="p2" spid="74" ecid="0" />
                      </process-list>
                    </deadlock>
                    """)
                .Single();

            Assert.IsFalse(graph.IsParallel);
        }

        [TestMethod]
        public void IsParallel_IgnoresProcessesWithoutSpid()
        {
            var graph = DeadlockParser.Parse(
                    """
                    <deadlock>
                      <process-list>
                        <process id="p1" />
                        <process id="p2" />
                      </process-list>
                    </deadlock>
                    """)
                .Single();

            Assert.IsFalse(graph.IsParallel);
        }

        [TestMethod]
        public void OccurredAt_IsTheLatestActivityInTheGraph()
        {
            // The graph has no timestamp of its own - the deadlock is detected some time after the
            // last participant started - so the latest batch or transaction start is the closest it
            // can say.  Used to ask for the schema as it was then.
            var graph = DeadlockParser.Parse(
                    """
                    <deadlock>
                      <process-list>
                        <process id="p1" spid="61" lastbatchstarted="2024-03-14T09:21:44.523"
                                 lasttranstarted="2024-03-14T09:21:40.000" />
                        <process id="p2" spid="74" lastbatchstarted="2024-03-14T09:21:46.100" />
                      </process-list>
                    </deadlock>
                    """)
                .Single();

            Assert.AreEqual(new DateTime(2024, 3, 14, 9, 21, 46, 100), graph.OccurredAt);
        }

        [TestMethod]
        public void OccurredAt_IsNullWhenTheGraphCarriesNoTimes()
        {
            var graph = DeadlockParser.Parse(
                    """
                    <deadlock><process-list><process id="p1" spid="61" /></process-list></deadlock>
                    """)
                .Single();

            Assert.IsNull(graph.OccurredAt);
        }

        [TestMethod]
        public void PrimaryFrame_IsTheFrameCarryingTheStatement()
        {
            var process = FirstProcess(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <executionStack>
                        <frame procname="Sales.dbo.usp_Inner" line="4" stmtstart="418" sqlhandle="0x03000700a1">UPDATE dbo.T SET C = 1;</frame>
                        <frame procname="adhoc" line="1" sqlhandle="0x020000004a">EXEC dbo.usp_Inner;</frame>
                      </executionStack>
                    </process>
                  </process-list>
                </deadlock>
                """);

            Assert.AreEqual("0x03000700a1", process.PrimaryFrame?.SqlHandle);
            Assert.AreEqual(418, process.PrimaryFrame?.StatementStart);
        }

        [TestMethod]
        public void PrimaryFrame_FallsBackToAFrameThatOnlyIdentifiesTheStatement()
        {
            // SQL Server writes "unknown" as the frame text when it can no longer resolve the statement.
            // The sql handle is still there, and is enough to look the plan up on the instance.
            var process = FirstProcess(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <executionStack>
                        <frame procname="adhoc" line="1" sqlhandle="0x020000004a">unknown</frame>
                      </executionStack>
                    </process>
                  </process-list>
                </deadlock>
                """);

            Assert.AreEqual("0x020000004a", process.PrimaryFrame?.SqlHandle);
            Assert.IsNull(process.PrimaryStatement);
        }

        [TestMethod]
        public void PrimaryFrame_IsNullWithoutAnExecutionStack()
        {
            var process = FirstProcess(
                """
                <deadlock><process-list><process id="p1" spid="61" /></process-list></deadlock>
                """);

            Assert.IsNull(process.PrimaryFrame);
        }

        [TestMethod]
        public void ModuleName_IsSplitFromAThreePartProcedureName()
        {
            var frame = FirstProcess(
                    """
                    <deadlock>
                      <process-list>
                        <process id="p1" spid="61">
                          <executionStack>
                            <frame procname="Sales.dbo.usp_UpdateOrder" line="12">UPDATE dbo.Orders SET Status = 1;</frame>
                          </executionStack>
                        </process>
                      </process-list>
                    </deadlock>
                    """)
                .PrimaryFrame;

            Assert.IsTrue(frame!.IsModule);
            Assert.AreEqual("Sales", frame.ModuleDatabaseName);
            Assert.AreEqual("dbo", frame.ModuleSchemaName);
            Assert.AreEqual("usp_UpdateOrder", frame.ModuleObjectName);
        }

        [TestMethod]
        public void ModuleName_IgnoresThePlaceholdersUsedForStatementsOutsideAModule()
        {
            foreach (var procname in new[] { "adhoc", "unknown", "Unknown" })
            {
                var frame = FirstProcess(
                        $"""
                         <deadlock>
                           <process-list>
                             <process id="p1" spid="61">
                               <executionStack>
                                 <frame procname="{procname}" line="1">SELECT 1;</frame>
                               </executionStack>
                             </process>
                           </process-list>
                         </deadlock>
                         """)
                    .PrimaryFrame;

                Assert.IsFalse(frame!.IsModule, procname);
                Assert.IsNull(frame.ModuleObjectName, procname);
                Assert.IsNull(frame.ModuleDatabaseName, procname);
            }
        }

        [TestMethod]
        public void ModuleName_HandlesANameThatIsNotFullyQualified()
        {
            var frame = FirstProcess(
                    """
                    <deadlock>
                      <process-list>
                        <process id="p1" spid="61">
                          <executionStack>
                            <frame procname="usp_UpdateOrder" line="12">UPDATE dbo.Orders SET Status = 1;</frame>
                          </executionStack>
                        </process>
                      </process-list>
                    </deadlock>
                    """)
                .PrimaryFrame;

            Assert.AreEqual("usp_UpdateOrder", frame!.ModuleObjectName);
            Assert.IsNull(frame.ModuleSchemaName);
            Assert.IsNull(frame.ModuleDatabaseName);
        }
    }
}
