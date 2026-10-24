using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBADash.Test
{
    /// <summary>
    /// Messaging.SendMessageFromGUIToService checks a user can send a message - its type needs the role the GUI requires.
    /// These tests keep the proc in step with the message types, and check the service only accepts messages the proc
    /// reads the same way.  The proc itself is tested against SQL Server in <see cref="MessageAuthorizationSqlTests"/>.
    /// </summary>
    [TestClass]
    public class MessageAuthorizationTests
    {
        private static readonly Regex PermissionRow = new(@"\(N'DBADash\.Messaging\.(?<type>\w+)',\s*(?:N'(?<role>\w+)'|NULL|@ProcedureRole)\)");

        internal static string SendProcPath => Path.Combine(FindRepositoryRoot(), "DBADashDB", "Messaging", "SendMessageFromGUIToService.sql");

        /// <summary>Message type (name only) => roles that can send it, from the proc.  An empty set needs Messaging only.</summary>
        internal static Dictionary<string, HashSet<string>> ReadPermissions()
        {
            var permissions = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (Match m in PermissionRow.Matches(File.ReadAllText(SendProcPath)))
            {
                var roles = permissions.TryGetValue(m.Groups["type"].Value, out var r) ? r : permissions[m.Groups["type"].Value] = new HashSet<string>();
                if (m.Groups["role"].Success) roles.Add(m.Groups["role"].Value);
            }
            return permissions;
        }

        [TestMethod]
        public void EveryMessageTypeIsListedInTheSendProc()
        {
            var listed = ReadPermissions().Keys.OrderBy(k => k).ToList();
            var messageTypes = MessageBase.MessageTypes.Values.Select(t => t.Name).OrderBy(n => n).ToList();

            Assert.IsTrue(messageTypes.Count > 20, "Message types weren't found");
            Assert.IsTrue(MessageBase.MessageTypes.Values.All(t => t.Namespace == "DBADash.Messaging"), "The send proc lists types in the DBADash.Messaging namespace");
            CollectionAssert.AreEqual(messageTypes, listed, "Add new message types to Messaging.SendMessageFromGUIToService with the roles that can send them");
        }

        [TestMethod]
        public void PrivilegedMessageTypesNeedTheirRoles()
        {
            var permissions = ReadPermissions();
            void AssertRoles(string type, params string[] roles) =>
                CollectionAssert.AreEquivalent(roles, permissions[type].ToList(), type);

            AssertRoles(nameof(KillSessionMessage), "AllowKillSession");
            AssertRoles(nameof(QueryStorePlanForcingMessage), "AllowPlanForcing");
            AssertRoles(nameof(FlushPlanMessage), "AllowPlanForcing");
            AssertRoles(nameof(AgentJobExecutionMessage), "AllowJobExecution");
            // Connection management includes schedules - someone who can add a connection can set its schedule
            AssertRoles(nameof(CollectionPauseMessage), "AllowScheduleChanges", "ManageConnections");
            AssertRoles(nameof(CollectionScheduleUpdateMessage), "AllowScheduleChanges", "ManageConnections");
            AssertRoles(nameof(CollectionScheduleGetMessage), "AllowScheduleChanges", "ManageConnections");
            AssertRoles(nameof(ConnectionTestMessage), "ManageConnections");
            AssertRoles(nameof(ConnectionAddMessage), "ManageConnections");
            AssertRoles(nameof(ConnectionRemoveMessage), "ManageConnections");
            AssertRoles(nameof(ConnectionOptionsGetMessage), "ManageConnections");
            AssertRoles(nameof(ConnectionOptionsUpdateMessage), "ManageConnections");
            AssertRoles(nameof(XESessionControlMessage), "ManageXE");
            AssertRoles(nameof(XESessionListMessage), "ManageXE", "WatchXE");
            AssertRoles(nameof(XESessionScriptMessage), "ManageXE", "WatchXE");
            AssertRoles(nameof(XEWatchSessionMessage), "WatchXE");
            AssertRoles(nameof(XEViewTargetDataMessage), "WatchXE");
            AssertRoles(nameof(XETraceMessage), "AdhocXE");
            AssertRoles(nameof(XETraceStopMessage), "AdhocXE");
            AssertRoles(nameof(XEObjectCatalogMessage), "AdhocXE");
            AssertRoles(nameof(CollectionMessage));
        }

        [TestMethod]
        public void EveryRoleInTheSendProcExists()
        {
            var securityFolder = Path.Combine(FindRepositoryRoot(), "DBADashDB", "Security");
            var roles = Directory.GetFiles(securityFolder, "*.sql")
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"CREATE ROLE \[?(\w+)\]?", RegexOptions.IgnoreCase).Select(m => m.Groups[1].Value))
                .ToHashSet(StringComparer.Ordinal);

            var used = ReadPermissions().Values.SelectMany(r => r).Concat(new[] { "CommunityScripts", "CustomTools" }).Distinct();
            foreach (var role in used)
            {
                Assert.Contains(role, roles, $"Role {role} isn't created in DBADashDB\\Security");
            }
        }

        [TestMethod]
        public void SendProcListsTheCommunityProcs()
        {
            var sql = File.ReadAllText(SendProcPath);
            var procs = Regex.Matches(sql, @"\(N'(sp_\w+)'\)").Select(m => m.Groups[1].Value).OrderBy(p => p).ToList();

            CollectionAssert.AreEqual(Enum.GetNames<ProcedureExecutionMessage.CommunityProcs>().OrderBy(p => p).ToList(), procs);
        }

        [TestMethod]
        [DataRow("dbo", "sp_Blitz", true)]
        [DataRow(null, "sp_Blitz", true)]
        [DataRow("dbo", "sp_blitz", false)] // Case sensitive - not a community proc, so a custom proc
        [DataRow("DBO", "sp_Blitz", false)]
        [DataRow("other", "sp_Blitz", false)]
        [DataRow("dbo", "0", false)] // Enum value, not a name
        [DataRow("dbo", "sp_Blitz,sp_BlitzWho", false)]
        [DataRow("dbo", "sp_Blitz ", false)] // Trailing space
        [DataRow("dbo ", "sp_Blitz", false)]
        [DataRow("dbo", "MyProc", false)]
        public void IsCommunityProc(string? schema, string proc, bool expected)
        {
            Assert.AreEqual(expected, ProcedureExecutionMessage.IsCommunityProc(schema!, proc));
        }

        [TestMethod]
        public void CommunityProc_IsNotSerialized()
        {
            var msg = new ProcedureExecutionMessage { ProcedureName = "MyProc", SchemaName = "dbo" };
            var json = MessageBase.ReadMessageJson(msg.Serialize());

            Assert.DoesNotContain("CommunityProc", json);
            Assert.IsNull(((ProcedureExecutionMessage)MessageBase.Deserialize(Payload(json.Replace("\"ProcedureName\"", "\"CommunityProc\":0,\"ProcedureName\"")))).CommunityProc);
        }

        /// <summary>Every message type the GUI sends must pass the checks - e.g. no two properties named alike ignoring case.</summary>
        [TestMethod]
        public void EveryMessageTypeRoundTrips()
        {
            foreach (var type in MessageBase.MessageTypes.Values)
            {
                var msg = (MessageBase)Activator.CreateInstance(type)!;
                var payload = msg.Serialize();

                var roundTrip = MessageBase.Deserialize(payload);

                Assert.AreEqual(type, roundTrip.GetType());
            }
        }

        [TestMethod]
        public void Serialize_EscapesNonAscii()
        {
            var msg = new ProcedureExecutionMessage { ProcedureName = "Prøc_日本", SchemaName = "dbo" };
            var payload = msg.Serialize();

            Assert.IsTrue(payload.Decompress().All(b => b < 0x80));
            Assert.AreEqual("Prøc_日本", ((ProcedureExecutionMessage)MessageBase.Deserialize(payload)).ProcedureName);
        }

        [TestMethod]
        public void Deserialize_IgnoresTheAssembly()
        {
            var msg = MessageBase.Deserialize(Payload("{\"__type\":\"DBADash.Messaging.CollectionMessage, SomeOtherAssembly\"}"));

            Assert.IsInstanceOfType<CollectionMessage>(msg);
        }

        [TestMethod]
        [DataRow("System.IO.FileInfo, System.IO.FileSystem")]
        [DataRow("DBADash.Messaging.MessageBase, DBADash")] // Abstract
        [DataRow("DBADash.Messaging.ResponseMessage, DBADash")] // Not a message the service processes
        [DataRow("DBADash.CollectionConfig, DBADash")]
        [DataRow(" DBADash.Messaging.CollectionMessage\t, DBADash")] // Only spaces are trimmed, as in the repository
        public void Deserialize_RejectsTypesThatArentMessages(string typeName)
        {
            Assert.ThrowsExactly<ArgumentException>(() => MessageBase.Deserialize(Payload($"{{\"__type\":\"{typeName.Replace("\t", "\\t")}\"}}")));
        }

        [TestMethod]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\",\"__type\":\"DBADash.Messaging.KillSessionMessage, DBADash\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\",\"ProcedureName\":\"sp_Blitz\",\"procedurename\":\"MyProc\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\",\"\\u017FchemaName\":\"other\"}")] // Non-ASCII name - long s
        [DataRow("{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\",\"EmbeddedScript \":0,\"ProcedureName\":\"MyProc\"}")] // Trailing space - matched by SQL's =
        [DataRow("{\"__type \":\"DBADash.Messaging.KillSessionMessage, DBADash\",\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\",\"\":1}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}{\"__type\":\"DBADash.Messaging.KillSessionMessage, DBADash\"}")]
        [DataRow("[{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}]")]
        [DataRow("{\"ConnectionID\":\"SQL1\"}")]
        [DataRow("{\"__type\":1}")]
        public void Deserialize_RejectsMessagesThatCouldBeReadAnotherWay(string json)
        {
            Assert.Throws<Exception>(() => MessageBase.Deserialize(Payload(json)));
        }

        [TestMethod]
        public void Deserialize_RejectsNonAscii()
        {
            var json = "{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\",\"ProcedureName\":\"Prøc\"}";

            Assert.ThrowsExactly<ArgumentException>(() => MessageBase.Deserialize(Encoding.UTF8.GetBytes(json).Compress()));
        }

        [TestMethod]
        public void Deserialize_RejectsConcatenatedGzipMembers()
        {
            var first = Payload("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}");
            var second = Payload("{\"__type\":\"DBADash.Messaging.KillSessionMessage, DBADash\"}");

            Assert.Throws<Exception>(() => MessageBase.Deserialize(first.Concat(second).ToArray()));
        }

        internal static byte[] Payload(string json) => Encoding.ASCII.GetBytes(json).Compress();

        internal static string FindRepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "DBADash.sln"))) return dir.FullName;
                dir = dir.Parent;
            }

            Assert.Inconclusive("Repository root (the directory containing DBADash.sln) was not found.");
            return string.Empty;
        }
    }
}
