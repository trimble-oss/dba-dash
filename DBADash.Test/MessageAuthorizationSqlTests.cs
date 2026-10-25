using DBADash.Messaging;
using Microsoft.Data.SqlClient;
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
    /// Messaging.SendMessageFromGUIToService against SQL Server: users without the role a message type needs can't send it,
    /// and can't get round the proc.  Deploys the repository DACPAC to a new database the way DBA Dash does, so every
    /// permission the repository grants is in place, and audits what it grants.  The database is dropped afterwards.
    ///
    /// <para>Runs against the server in the <see cref="ServerEnvironmentVariable"/> connection string (CI sets it to each
    /// SQL version it tests), failing if it can't connect.  Without it, LocalDB is used and the tests are inconclusive if
    /// LocalDB isn't available.</para>
    /// </summary>
    [TestClass]
    [TestCategory("SqlServer")]
    public class MessageAuthorizationSqlTests
    {
        /// <summary>Connection string for the SQL instance to test against.  Needs permission to create databases.</summary>
        public const string ServerEnvironmentVariable = "DBADASH_TEST_SQL";

        private static readonly string? ServerConnectionString = Environment.GetEnvironmentVariable(ServerEnvironmentVariable) is { Length: > 0 } cs ? cs : null;
        private static readonly string Database = "DBADashMessagingTest_" + Guid.NewGuid().ToString("N");
        private static string MasterConnectionString => ConnectionStringFor("master");
        private static string ConnectionString => ConnectionStringFor(Database);
        private static int AgentID;

        private static string ConnectionStringFor(string database)
        {
            var builder = ServerConnectionString == null
                // Long timeout: LocalDB stops when idle and the first connection starts it
                ? new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60 }
                : new SqlConnectionStringBuilder(ServerConnectionString);
            builder.InitialCatalog = database;
            return builder.ConnectionString;
        }

        private static readonly string[] Roles =
            { "AllowKillSession", "AllowPlanForcing", "AllowJobExecution", "AllowScheduleChanges", "ManageConnections", "ManageXE", "WatchXE", "AdhocXE", "CommunityScripts", "CustomTools" };

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            try
            {
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
            }
            catch (SqlException ex) when (ServerConnectionString == null)
            {
                // A server given in the environment variable must be available - the tests fail rather than being skipped
                Assert.Inconclusive($"LocalDB isn't available: {ex.Message}");
            }

            // The repository database as DBA Dash deploys it, so the tests see every permission it grants - not only the
            // ones in the messaging objects.
            new DacpacUtility.DacpacService().ProcessDacPac(MasterConnectionString, Database, FindDacpac(), DBValidations.DBVersionStatusEnum.CreateDB);
            using var cn = Open();

            using (var cmd = new SqlCommand("INSERT INTO dbo.DBADashAgent(AgentHostName,AgentServiceName,AgentVersion,AgentPath) VALUES('Host','Service','1','Path'); SELECT CAST(SCOPE_IDENTITY() AS INT)", cn))
            {
                AgentID = (int)cmd.ExecuteScalar();
            }
            // Granted by older versions of Messaging.Setup - check Setup revokes it
            Execute(cn, $"EXEC Messaging.Setup @DBADashAgentID = {AgentID}; GRANT RECEIVE ON dbo.DBADashServiceInitiatorQueue TO Messaging; EXEC Messaging.Setup @DBADashAgentID = {AgentID};");
            SqlConnection.ClearAllPools(); // Enabling the broker closes other connections

            // A user in each role, plus Messaging alone, App alone and both together.  All but NoMessaging have Messaging
            Execute(cn, "CREATE USER MessagingOnly WITHOUT LOGIN; ALTER ROLE Messaging ADD MEMBER MessagingOnly;");
            Execute(cn, "CREATE USER AppOnly WITHOUT LOGIN; ALTER ROLE App ADD MEMBER AppOnly;");
            Execute(cn, "CREATE USER MessagingApp WITHOUT LOGIN; ALTER ROLE Messaging ADD MEMBER MessagingApp; ALTER ROLE App ADD MEMBER MessagingApp;");
            foreach (var role in Roles)
            {
                Execute(cn, $"CREATE USER [{role}User] WITHOUT LOGIN; ALTER ROLE Messaging ADD MEMBER [{role}User]; ALTER ROLE [{role}] ADD MEMBER [{role}User];");
            }

            // Every role the repository creates other than the ones that allow privileged messages - including roles added
            // in future, so a role granted something that gets round the checks is caught.
            Execute(cn, $@"CREATE USER AllOtherRoles WITHOUT LOGIN;
DECLARE @SQL NVARCHAR(MAX) = N'';
SELECT @SQL += N'ALTER ROLE ' + QUOTENAME(name) + N' ADD MEMBER AllOtherRoles;'
FROM sys.database_principals
WHERE type = 'R'
AND is_fixed_role = 0
AND name NOT IN(N'public',{string.Join(",", Roles.Select(r => $"N'{r}'"))});
EXEC sp_executesql @SQL;");
        }

        /// <summary>
        /// The DACPAC built from DBADashDB - the newest in DBADashBuild (built to DBADashBuild\{Configuration}).  It must be
        /// newer than the SQL files, so the tests don't run against a stale build.
        /// </summary>
        internal static string FindDacpac()
        {
            var root = MessageAuthorizationTests.FindRepositoryRoot();
            var buildFolder = Path.Combine(root, "DBADashBuild");
            var dacpac = Directory.Exists(buildFolder)
                ? Directory.GetDirectories(buildFolder).Select(d => new FileInfo(Path.Combine(d, DBValidations.DACPackFile))).Where(f => f.Exists).MaxBy(f => f.LastWriteTimeUtc)
                : null;
            var newestSql = Directory.EnumerateFiles(Path.Combine(root, "DBADashDB"), "*.sql", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Max(File.GetLastWriteTimeUtc);

            var problem = dacpac == null ? "DBADashDB.dacpac wasn't found in DBADashBuild"
                : dacpac.LastWriteTimeUtc < newestSql ? $"{dacpac.FullName} is older than the DBADashDB SQL files"
                : null;
            if (problem != null)
            {
                var message = $"{problem}.  Build the DBADashDB project first.";
                if (ServerConnectionString == null) Assert.Inconclusive(message);
                Assert.Fail(message);
            }
            return dacpac!.FullName;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                Execute(master, $"IF DB_ID('{Database}') IS NOT NULL BEGIN ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Database}]; END");
            }
            catch (SqlException)
            {
                // The server wasn't available - nothing to clean up
            }
        }

        /// <summary>Every message type, sent by a user in each role (and by db_owner) - allowed only with a role the type lists.</summary>
        [TestMethod]
        public void EachMessageTypeNeedsItsRole()
        {
            var permissions = MessageAuthorizationTests.ReadPermissions();
            var failures = new List<string>();
            foreach (var type in MessageBase.MessageTypes.Values.Where(t => t != typeof(ProcedureExecutionMessage)))
            {
                var payload = ((MessageBase)Activator.CreateInstance(type)!).Serialize();
                var needs = permissions[type.Name];
                foreach (var user in Roles.Select(r => r + "User").Append("MessagingOnly").Append(null))
                {
                    var role = user?[..^"User".Length];
                    var expected = user == null || needs.Count == 0 || (role != null && needs.Contains(role));
                    var error = Send(payload, user);
                    if ((error == null) != expected)
                    {
                        failures.Add($"{type.Name} sent by {user ?? "dbo"}: expected {(expected ? "allowed" : "denied")} but {error ?? "allowed"}");
                    }
                }
            }
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        [TestMethod]
        public void DeniedMessagesSayWhichRoleIsNeeded()
        {
            var error = Send(new KillSessionMessage().Serialize(), "MessagingOnly");

            Assert.AreEqual("You don't have permission to send KillSessionMessage.  It requires the AllowKillSession role.", error);
            Assert.AreEqual("You don't have permission to send XESessionListMessage.  It requires the ManageXE or WatchXE role.", Send(new XESessionListMessage().Serialize(), "MessagingOnly"));
        }

        [TestMethod]
        public void AllowedMessagesAreSent()
        {
            var before = QueueCount();

            Assert.IsNull(Send(new KillSessionMessage().Serialize(), "AllowKillSessionUser"));
            Assert.IsNull(Send(new CollectionMessage().Serialize(), "MessagingOnly"));

            Assert.AreEqual(before + 2, QueueCount());
        }

        /// <summary>Embedded scripts need Messaging only, community procs need CommunityScripts and other procs need CustomTools.</summary>
        [TestMethod]
        [DataRow("dbo", "sp_Blitz", false, "CommunityScriptsUser", true)]
        [DataRow("dbo", "sp_Blitz", false, "CustomToolsUser", false)]
        [DataRow("dbo", "sp_Blitz", false, "MessagingOnly", false)]
        [DataRow("dbo", "MyProc", false, "CustomToolsUser", true)]
        [DataRow("dbo", "MyProc", false, "CommunityScriptsUser", false)]
        [DataRow("dbo", "sp_blitz", false, "CommunityScriptsUser", false)] // Not exactly a community proc name, so a custom proc
        [DataRow(null, "sp_Blitz", false, "CommunityScriptsUser", true)] // No schema is dbo
        [DataRow("other", "sp_Blitz", false, "CommunityScriptsUser", false)] // A community proc must be dbo
        [DataRow("other", "sp_Blitz", false, "CustomToolsUser", true)]
        [DataRow("dbo", "MyProc", true, "MessagingOnly", true)]
        public void ProcedureExecutionNeedsTheRoleForTheProc(string? schema, string proc, bool embedded, string user, bool expected)
        {
            var msg = new ProcedureExecutionMessage
            {
                EmbeddedScript = embedded ? ProcedureExecutionMessage.EmbeddedScripts.TuningRecommendations : null,
                ProcedureName = proc,
                SchemaName = schema ?? "dbo"
            };
            var json = MessageBase.ReadMessageJson(msg.Serialize());
            if (schema == null)
            {
                json = Regex.Replace(json, "\"SchemaName\":\"[^\"]*\",?", "");
                StringAssert.DoesNotMatch(json, new Regex("SchemaName"));
            }
            var payload = MessageAuthorizationTests.Payload(json);
            Assert.AreEqual(msg.GetType(), MessageBase.Deserialize(payload).GetType()); // The service reads the message too

            var error = Send(payload, user);

            Assert.AreEqual(expected, error == null, error);
        }

        /// <summary>The same checks as the service makes - anything that could be read differently there is rejected, whoever sends it.</summary>
        [TestMethod]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\",\"__type\":\"DBADash.Messaging.KillSessionMessage, DBADash\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\",\"ProcedureName\":\"sp_Blitz\",\"procedurename\":\"MyProc\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.UnknownMessage, DBADash\"}")]
        [DataRow("{\"__type\":\"System.IO.FileInfo, System.IO.FileSystem\"}")]
        [DataRow("{\"__type\":1}")]
        [DataRow("{\"ConnectionID\":\"SQL1\"}")]
        [DataRow("[{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}]")]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}{\"__type\":\"DBADash.Messaging.KillSessionMessage, DBADash\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\" /* comment */}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\",\"EmbeddedScript \":0,\"ProcedureName\":\"MyProc\"}")] // = ignores trailing spaces
        [DataRow("{\"__type \":\"DBADash.Messaging.CollectionMessage, DBADash\"}")]
        [DataRow("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\",\"\":1}")]
        public void InvalidMessagesAreRejected(string json)
        {
            var payload = MessageAuthorizationTests.Payload(json);

            Assert.IsNotNull(Send(payload, "MessagingOnly"));
            Assert.IsNotNull(Send(payload, null));
        }

        [TestMethod]
        public void NonAsciiAndNonGzipPayloadsAreRejected()
        {
            var nonAscii = Encoding.UTF8.GetBytes("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\",\"ConnectionID\":\"Sé\"}").Compress();

            Assert.IsNotNull(Send(nonAscii, null));
            Assert.IsNotNull(Send(Encoding.ASCII.GetBytes("{\"__type\":\"DBADash.Messaging.CollectionMessage, DBADash\"}"), null));
            Assert.IsNotNull(Send(Array.Empty<byte>(), null));
        }

        /// <summary>
        /// A member of every role other than the privileged message roles can't send a privileged message - whatever those
        /// roles are granted elsewhere in the repository.
        /// </summary>
        [TestMethod]
        public void OtherRolesCantSendPrivilegedMessages()
        {
            var permissions = MessageAuthorizationTests.ReadPermissions();
            var privileged = MessageBase.MessageTypes.Values.Where(t => t != typeof(ProcedureExecutionMessage) && permissions[t.Name].Count > 0).ToList();
            Assert.IsTrue(privileged.Count > 10);

            var sent = privileged.Where(t => Send(((MessageBase)Activator.CreateInstance(t)!).Serialize(), "AllOtherRoles") == null).Select(t => t.Name).ToList();
            var customProc = new ProcedureExecutionMessage { ProcedureName = "MyProc", SchemaName = "dbo" }.Serialize();
            if (Send(customProc, "AllOtherRoles") == null) sent.Add("ProcedureExecutionMessage (custom proc)");

            Assert.AreEqual(0, sent.Count, "Sent by a member of the other roles: " + string.Join(", ", sent));
        }

        /// <summary>Members of the app roles can't send to the service other than through the proc.</summary>
        [TestMethod]
        [DataRow("MessagingApp")]
        [DataRow("MessagingOnly")]
        [DataRow("AppOnly")]
        [DataRow("AllOtherRoles")]
        public void ProcCantBeBypassed(string user)
        {
            var payload = new KillSessionMessage().Serialize();
            using var cn = Open();

            var internalError = Run(cn, user, "EXEC MessagingInternal.SendMessage @DBADashAgentID=@AgentID,@Payload=@Payload,@ConversationGroup=@Group,@InitDlgHandle=@Handle,@Lifetime=60", payload);
            StringAssert.Contains(internalError, "permission was denied");

            var dialogError = Run(cn, user, $@"DECLARE @h UNIQUEIDENTIFIER;
BEGIN DIALOG @h FROM SERVICE [//dbadash.com/DBADashService/ComsInit] TO SERVICE '//dbadash.com/DBADashService/ComsTarget{AgentID}' ON CONTRACT [//dbadash.com/DBADashService/ComsContract] WITH ENCRYPTION = OFF;
SEND ON CONVERSATION @h MESSAGE TYPE [//dbadash.com/DBADashService/Send](@Payload);", payload);
            Assert.IsNotNull(dialogError, "Sent to the service directly");

            var insertError = Run(cn, user, $"INSERT INTO dbo.DBADashServiceTargetQueue{AgentID}(message_body) VALUES(@Payload)", payload);
            Assert.IsNotNull(insertError, "Inserted into the queue");

            var receiveError = Run(cn, user, "RECEIVE TOP(1) conversation_handle FROM dbo.DBADashServiceInitiatorQueue", payload);
            Assert.IsNotNull(receiveError, "Received from the initiator queue - RECEIVE allows a dialog to be begun from its service");
        }

        /// <summary>Messaging members receive replies through the proc, without RECEIVE on the queue.</summary>
        [TestMethod]
        public void RepliesAreReceivedThroughTheProc()
        {
            var payload = new CollectionMessage { ConnectionID = Guid.NewGuid().ToString() }.Serialize();
            var group = Guid.NewGuid();
            Assert.IsNull(Send(payload, "MessagingOnly", group));

            using var cn = Open();
            using (var reply = new SqlCommand($@"DECLARE @Handle UNIQUEIDENTIFIER = (SELECT TOP(1) conversation_handle FROM dbo.DBADashServiceTargetQueue{AgentID} WHERE message_body = @Payload);
EXEC Messaging.SendReplyFromServiceToGUI @RecvReqDlgHandle = @Handle, @Payload = 0x0102;", cn))
            {
                reply.Parameters.Add("@Payload", System.Data.SqlDbType.VarBinary, -1).Value = payload;
                reply.ExecuteNonQuery();
            }

            using var receive = new SqlCommand("EXECUTE AS USER = 'MessagingOnly'; EXEC Messaging.ReceiveReplyFromServiceToGUI @ConversationGroupID = @Group, @Timeout = 5000; REVERT;", cn);
            receive.Parameters.AddWithValue("@Group", group);
            using var rdr = receive.ExecuteReader();
            Assert.IsTrue(rdr.Read(), "No reply received");
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, (byte[])rdr["message_body"]);
        }

        /// <summary>
        /// The repository only grants its roles read and execute permissions (plus SQL Server's own grants to public).
        /// Anything else - ALTER, CONTROL, IMPERSONATE, SEND, RECEIVE... - could get round the messaging checks, so a new
        /// grant fails here until it's been reviewed and allowed.
        /// </summary>
        [TestMethod]
        public void RepositoryOnlyGrantsReadAndExecute()
        {
            var unexpected = Query(@"
SELECT CONCAT(pr.name,N': ',p.permission_name COLLATE DATABASE_DEFAULT,N' on ',p.class_desc COLLATE DATABASE_DEFAULT,N' ',
		CASE p.class WHEN 1 THEN OBJECT_SCHEMA_NAME(p.major_id) + N'.' + OBJECT_NAME(p.major_id) WHEN 3 THEN SCHEMA_NAME(p.major_id) WHEN 6 THEN TYPE_NAME(p.major_id) ELSE CAST(p.major_id AS NVARCHAR(20)) END)
FROM sys.database_permissions p
JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
WHERE p.state IN('G','W')
AND pr.name <> N'dbo'
AND NOT (p.class = 1 AND OBJECT_SCHEMA_NAME(p.major_id) = N'sys') /* System views granted to public by SQL Server */
AND NOT (p.class = 0 AND p.permission_name IN(N'CONNECT',N'UNMASK',N'VIEW ANY COLUMN ENCRYPTION KEY DEFINITION',N'VIEW ANY COLUMN MASTER KEY DEFINITION'))
AND NOT (p.class IN(1,3,6) AND p.permission_name IN(N'SELECT',N'EXECUTE',N'UNMASK'))");

            Assert.AreEqual(0, unexpected.Count, "Review these grants for a way round the messaging checks: " + string.Join(", ", unexpected));
        }

        /// <summary>
        /// Nothing is granted on MessagingInternal or the Service Broker objects.  EXECUTE on MessagingInternal would allow
        /// any message to be sent, as would RECEIVE on the initiator queue - see Messaging.Setup.
        /// </summary>
        [TestMethod]
        public void NothingIsGrantedOnMessagingInternalOrServiceBroker()
        {
            var granted = Query(@"
SELECT CONCAT(pr.name,N': ',p.permission_name COLLATE DATABASE_DEFAULT,N' on ',p.class_desc COLLATE DATABASE_DEFAULT,N' ',p.major_id)
FROM sys.database_permissions p
JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
LEFT JOIN sys.objects o ON p.class = 1 AND o.object_id = p.major_id
WHERE p.state IN('G','W')
AND pr.name <> N'dbo'
AND (	(p.class = 3 AND p.major_id = SCHEMA_ID(N'MessagingInternal'))
		OR o.schema_id = SCHEMA_ID(N'MessagingInternal')
		OR o.type = 'SQ'
		OR p.class_desc IN(N'MESSAGE_TYPE',N'SERVICE_CONTRACT',N'SERVICE',N'REMOTE_SERVICE_BINDING',N'ROUTE')
	)");

            Assert.AreEqual(0, granted.Count, string.Join(", ", granted));
        }

        /// <summary>
        /// The modules that run as another user.  A module that runs as owner can do anything dbo can, so each must be
        /// checked so it can't be used to send a message.  Report.ProcedureInsights only reads report data (its dynamic SQL
        /// is parameterised).  Signed modules are elevated the same way - there aren't any.
        /// </summary>
        [TestMethod]
        public void OnlyReviewedModulesRunAsAnotherUser()
        {
            var modules = Query(@"
SELECT CONCAT(OBJECT_SCHEMA_NAME(object_id),N'.',OBJECT_NAME(object_id))
FROM sys.sql_modules
WHERE execute_as_principal_id IS NOT NULL
UNION ALL
SELECT CONCAT(N'Signed: ',class_desc COLLATE DATABASE_DEFAULT,N' ',major_id)
FROM sys.crypt_properties");

            CollectionAssert.AreEquivalent(new[] { "Messaging.ReceiveReplyFromServiceToGUI", "MessagingInternal.SendMessage", "Report.ProcedureInsights" }, modules, string.Join(", ", modules));
        }

        /// <summary>No role or user is made a member of a fixed database role (db_owner, db_ddladmin...) - only dbo.</summary>
        [TestMethod]
        public void FixedRolesHaveNoMembers()
        {
            var members = Query(@"
SELECT CONCAT(r.name,N': ',m.name)
FROM sys.database_role_members rm
JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
WHERE r.is_fixed_role = 1
AND m.name <> N'dbo'");

            Assert.AreEqual(0, members.Count, string.Join(", ", members));
        }

        private static List<string> Query(string sql)
        {
            using var cn = Open();
            using var cmd = new SqlCommand(sql, cn);
            using var rdr = cmd.ExecuteReader();
            var results = new List<string>();
            while (rdr.Read()) results.Add(rdr.GetString(0));
            return results;
        }

        /// <summary>
        /// Procedure execution payloads the GUI doesn't send, read the same way as the service: an empty string isn't an
        /// embedded script (the service reads it as none), and names are compared exactly - = ignores trailing spaces.
        /// </summary>
        [TestMethod]
        [DataRow("\"EmbeddedScript\":\"\",\"ProcedureName\":\"MyProc\",\"SchemaName\":\"dbo\"", "MessagingOnly", false)]
        [DataRow("\"EmbeddedScript\":\"\",\"ProcedureName\":\"MyProc\",\"SchemaName\":\"dbo\"", "CustomToolsUser", true)]
        [DataRow("\"EmbeddedScript\":null,\"ProcedureName\":\"MyProc\",\"SchemaName\":\"dbo\"", "MessagingOnly", false)]
        [DataRow("\"EmbeddedScript\":\"TuningRecommendations\",\"ProcedureName\":\"MyProc\"", "MessagingOnly", false)] // Only a number, as the GUI sends
        [DataRow("\"ProcedureName\":\"sp_Blitz \",\"SchemaName\":\"dbo\"", "CommunityScriptsUser", false)]
        [DataRow("\"ProcedureName\":\"sp_Blitz \",\"SchemaName\":\"dbo\"", "CustomToolsUser", true)]
        [DataRow("\"ProcedureName\":\"sp_Blitz\",\"SchemaName\":\"dbo \"", "CommunityScriptsUser", false)]
        [DataRow("\"ProcedureName\":\"sp_Blitz\",\"SchemaName\":\"dbo\"", "CommunityScriptsUser", true)]
        public void ProcedureExecutionPayloadsAreReadAsTheServiceReadsThem(string properties, string user, bool expected)
        {
            var payload = MessageAuthorizationTests.Payload("{\"__type\":\"DBADash.Messaging.ProcedureExecutionMessage, DBADash\"," + properties + "}");

            var error = Send(payload, user);

            Assert.AreEqual(expected, error == null, error);
        }

        /// <summary>
        /// Only db_owner (the service) can change an agent's registration - it decides where messages are relayed and
        /// results written.  App can execute dbo procs, including DBADashAgent_Upd and Instance_Upd, which calls it.
        /// </summary>
        [TestMethod]
        [DataRow("MessagingApp")]
        [DataRow("AppOnly")]
        [DataRow("AllOtherRoles")]
        public void AgentRegistrationCantBeChanged(string user)
        {
            using var cn = Open();
            Execute(cn, $"UPDATE dbo.DBADashAgent SET ServiceSQSQueueUrl = N'https://sqs.registered', S3Path = N's3://registered' WHERE DBADashAgentID = {AgentID}");

            Assert.IsNotNull(Run(cn, user, "DECLARE @ID INT; EXEC dbo.DBADashAgent_Upd @AgentHostName = N'Host', @AgentServiceName = N'Service', @AgentVersion = '1', @AgentPath = N'Path', @ServiceSQSQueueUrl = N'https://sqs.other', @S3Path = N's3://other', @DBADashAgentID = @ID OUT;", Array.Empty<byte>()));
            Run(cn, user, "DECLARE @ID INT; EXEC dbo.Instance_Upd @ConnectionID = N'AgentTest', @Instance = N'AgentTest', @SnapshotDate = '2026-01-01', @AgentHostName = N'Host', @AgentServiceName = N'Service', @InstanceID = @ID OUT;", Array.Empty<byte>());
            Assert.IsNotNull(Run(cn, user, $"UPDATE dbo.DBADashAgent SET ServiceSQSQueueUrl = N'https://sqs.other' WHERE DBADashAgentID = @AgentID", Array.Empty<byte>()));
            Assert.IsNotNull(Run(cn, user, "EXEC Messaging.Setup @DBADashAgentID = @AgentID, @Enable = 0", Array.Empty<byte>()));

            using var cmd = new SqlCommand($"SELECT CONCAT(ServiceSQSQueueUrl, N'|', S3Path, N'|', MessagingEnabled) FROM dbo.DBADashAgent WHERE DBADashAgentID = {AgentID}", cn);
            Assert.AreEqual("https://sqs.registered|s3://registered|1", (string)cmd.ExecuteScalar());
        }

        [TestMethod]
        public void ServiceCanRegisterAgents()
        {
            using var cn = Open();

            Assert.IsNull(Run(cn, null, "DECLARE @ID INT; EXEC dbo.DBADashAgent_Upd @AgentHostName = N'Host2', @AgentServiceName = N'Service2', @AgentVersion = '1', @AgentPath = N'Path', @ServiceSQSQueueUrl = N'https://sqs.host2', @DBADashAgentID = @ID OUT;", Array.Empty<byte>()));
        }

        [TestMethod]
        public void UsersWithoutMessagingCantSend()
        {
            using var cn = Open();
            Execute(cn, "IF USER_ID('NoMessaging') IS NULL BEGIN CREATE USER NoMessaging WITHOUT LOGIN; ALTER ROLE AllowKillSession ADD MEMBER NoMessaging; END");

            StringAssert.Contains(Send(new KillSessionMessage().Serialize(), "NoMessaging"), "permission was denied");
        }

        /// <summary>Sends a message as <paramref name="user"/> (dbo if null), returning the error or null if it was sent.</summary>
        private static string? Send(byte[] payload, string? user, Guid? group = null)
        {
            using var cn = Open();
            return Run(cn, user, "EXEC Messaging.SendMessageFromGUIToService @DBADashAgentID=@AgentID,@Payload=@Payload,@ConversationGroup=@Group,@InitDlgHandle=@Handle,@Lifetime=60", payload, group);
        }

        private static string? Run(SqlConnection cn, string? user, string sql, byte[] payload, Guid? group = null)
        {
            var batch = user == null ? sql : $"EXECUTE AS USER = '{user}';\nBEGIN TRY\n{sql}\nEND TRY\nBEGIN CATCH\nREVERT;\nTHROW;\nEND CATCH\nREVERT;";
            using var cmd = new SqlCommand(batch, cn);
            cmd.Parameters.AddWithValue("@AgentID", AgentID);
            cmd.Parameters.Add("@Payload", System.Data.SqlDbType.VarBinary, -1).Value = payload;
            cmd.Parameters.AddWithValue("@Group", group ?? Guid.NewGuid());
            cmd.Parameters.AddWithValue("@Handle", Guid.NewGuid());
            try
            {
                cmd.ExecuteNonQuery();
                return null;
            }
            catch (SqlException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// The key the GUI encrypts connection strings with is only published by a running service - a registration
        /// without one (e.g. the collect agent read from an older service's data) keeps the key the agent has.
        /// </summary>
        [TestMethod]
        public void AgentRegistration_KeepsPublicKeyWhenNoneSupplied()
        {
            var agent = new DBADashAgent
            {
                AgentHostName = "KeyHost",
                AgentServiceName = "KeyService",
                AgentPath = "C:\\DBADash",
                AgentVersion = "1.0",
                MessagingEnabled = true,
                ConnectionChangesEnabled = true,
                PublicKey = "Key1"
            };
            var id = agent.GetDBADashAgentID(ConnectionString);
            Assert.AreEqual("Key1", DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKey);

            agent.PublicKey = null;
            agent.AgentVersion = "1.1"; // Not cached
            Assert.AreEqual(id, agent.GetDBADashAgentID(ConnectionString));
            var registered = DBADashAgent.GetDBADashAgent(ConnectionString, id);
            Assert.AreEqual("Key1", registered.PublicKey);
            Assert.AreEqual("1.1", registered.AgentVersion);
            Assert.IsTrue(registered.ConnectionChangesEnabled);

            agent.PublicKey = "Key2";
            agent.GetDBADashAgentID(ConnectionString);
            Assert.AreEqual("Key2", DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKey);

            // Only a newer key replaces one with a creation time - a dataset collected before a restart can be imported after one collected since
            var created = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
            agent.PublicKey = "Key3";
            agent.PublicKeyCreatedUtc = created;
            agent.GetDBADashAgentID(ConnectionString);
            Assert.AreEqual("Key3", DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKey);
            Assert.AreEqual(created, DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKeyCreatedUtc);

            agent.PublicKey = "OlderKey";
            agent.PublicKeyCreatedUtc = created.AddMinutes(-5);
            agent.GetDBADashAgentID(ConnectionString);
            Assert.AreEqual("Key3", DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKey);

            agent.PublicKey = "NoTimeKey";
            agent.PublicKeyCreatedUtc = null;
            agent.GetDBADashAgentID(ConnectionString);
            Assert.AreEqual("Key3", DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKey);

            agent.PublicKey = "Key4";
            agent.PublicKeyCreatedUtc = created.AddMinutes(5);
            agent.GetDBADashAgentID(ConnectionString);
            Assert.AreEqual("Key4", DBADashAgent.GetDBADashAgent(ConnectionString, id).PublicKey);

            // Listed for the GUI to add connections to, routed through itself as it has no instances
            using var cn = Open();
            using var cmd = new SqlCommand("dbo.ConnectionManagementAgents_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rdr = cmd.ExecuteReader();
            var listed = false;
            while (rdr.Read())
            {
                if ((int)rdr["CollectAgentID"] != id) continue;
                listed = true;
                Assert.AreEqual(id, (int)rdr["ImportAgentID"]);
                Assert.IsTrue((bool)rdr["HasPublicKey"]);
            }
            Assert.IsTrue(listed, "Agent with connection changes enabled wasn't listed");
        }

        /// <summary>
        /// A remote service that writes to S3 is reached through the service that imports its data.  Once its instances
        /// have all been removed (and marked deleted) the route still comes from them, preferring active instances.
        /// </summary>
        [TestMethod]
        public void ConnectionManagementAgents_RouteFromInactiveInstances()
        {
            using var cn = Open();
            int Agent(string host, bool connectionChanges)
            {
                using var cmd = new SqlCommand("INSERT INTO dbo.DBADashAgent(AgentHostName,AgentServiceName,AgentVersion,AgentPath,MessagingEnabled,ConnectionChangesEnabled) VALUES(@Host,'RouteService','1','Path',1,@CC); SELECT CAST(SCOPE_IDENTITY() AS INT)", cn);
                cmd.Parameters.AddWithValue("@Host", host);
                cmd.Parameters.AddWithValue("@CC", connectionChanges);
                return (int)cmd.ExecuteScalar();
            }
            var remote = Agent("Remote" + Guid.NewGuid().ToString("N")[..8], true);
            var importer = Agent("Import" + Guid.NewGuid().ToString("N")[..8], false);
            using (var cmd = new SqlCommand("INSERT INTO dbo.Instances(ConnectionID,Instance,IsActive,CollectAgentID,ImportAgentID) VALUES(@ID,@ID,0,@Remote,@Importer)", cn))
            {
                cmd.Parameters.AddWithValue("@ID", "RouteTest" + Guid.NewGuid().ToString("N")[..8]);
                cmd.Parameters.AddWithValue("@Remote", remote);
                cmd.Parameters.AddWithValue("@Importer", importer);
                cmd.ExecuteNonQuery();
            }

            using var get = new SqlCommand("dbo.ConnectionManagementAgents_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rdr = get.ExecuteReader();
            int? route = null;
            while (rdr.Read())
            {
                if ((int)rdr["CollectAgentID"] == remote) route = (int)rdr["ImportAgentID"];
            }
            Assert.AreEqual(importer, route);
        }

        /// <summary>
        /// A ManageConnections member with read-only app access can mark an instance deleted or restore it - after
        /// removing or adding its connection - but can't run Instance_Del, which can also hard delete.
        /// </summary>
        [TestMethod]
        public void ManageConnections_CanSoftDeleteOnly()
        {
            using var cn = Open();
            Execute(cn, "IF USER_ID('ReadOnlyConnections') IS NULL BEGIN CREATE USER ReadOnlyConnections WITHOUT LOGIN; ALTER ROLE AppReadOnly ADD MEMBER ReadOnlyConnections; ALTER ROLE ManageConnections ADD MEMBER ReadOnlyConnections; END");
            string? Run(string sql)
            {
                try
                {
                    Execute(cn, $"EXECUTE AS USER = 'ReadOnlyConnections'; BEGIN TRY {sql} END TRY BEGIN CATCH REVERT; THROW; END CATCH; REVERT;");
                    return null;
                }
                catch (SqlException ex)
                {
                    return ex.Message;
                }
            }

            // Permitted - fails only because there's no such instance
            Assert.AreEqual("Instance not found", Run("EXEC dbo.Instance_SetActive @ConnectionID = N'NoSuchInstance', @IsActive = 0;"));
            StringAssert.Contains(Run("EXEC dbo.Instance_Del @ConnectionID = N'NoSuchInstance';"), "permission");
        }

        private static int QueueCount()
        {
            using var cn = Open();
            using var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.DBADashServiceTargetQueue{AgentID}", cn);
            return (int)cmd.ExecuteScalar();
        }

        private static SqlConnection Open()
        {
            var cn = new SqlConnection(ConnectionString);
            cn.Open();
            return cn;
        }

        private static void Execute(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }
    }
}
