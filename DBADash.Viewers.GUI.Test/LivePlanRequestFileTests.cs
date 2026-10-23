using DBADashGUI.Viewers;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// The live plan request the SSMS extension hands over, and the connection the viewer makes from it.  Written here as
    /// VisualizerLauncher.OpenLivePlan writes it - the extension is net472 and can't be referenced - so a change to either
    /// side's format fails here.
    /// </summary>
    [TestClass]
    public class LivePlanRequestFileTests
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DBADash.LivePlanRequest");
        private static readonly byte[] FileEntropy = Encoding.UTF8.GetBytes("DBADash.LivePlanRequest.File");

        private const string SqlLogin = "Data Source=sql01;Integrated Security=False;User ID=app;Encrypt=True;TrustServerCertificate=False";

        private static string Write(string directory, string password = "s3cret!", string startTime = "2026-10-22T09:15:30.1230000",
            string connectionString = SqlLogin, int version = 2, byte[] fileEntropy = null, bool protectFile = true)
        {
            var root = new XElement("DBADashLivePlan",
                new XAttribute("Version", version),
                new XElement("ConnectionString", connectionString),
                new XElement("SessionId", 57));
            if (password != null)
            {
                root.Add(new XElement("Password",
                    Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(password), Entropy, DataProtectionScope.CurrentUser))));
            }
            if (startTime != null) root.Add(new XElement("StartTime", startTime));
            root.Add(new XElement("Title", "SQLQuery1.sql"));

            var path = Path.Combine(directory, "ssms_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + LivePlanRequestFile.Extension);
            using var xml = new MemoryStream();
            new XDocument(root).Save(xml);
            File.WriteAllBytes(path, protectFile
                ? ProtectedData.Protect(xml.ToArray(), fileEntropy ?? FileEntropy, DataProtectionScope.CurrentUser)
                : xml.ToArray());
            return path;
        }

        private static LivePlanRequestFile Read(string password = "s3cret!", string connectionString = SqlLogin) =>
            LivePlanRequestFile.ReadAndDelete(Write(Path.GetTempPath(), password: password, connectionString: connectionString));

        [TestMethod]
        public void ReadAndDelete_ReadsTheRequestAndDeletesTheFile()
        {
            var path = Write(Path.GetTempPath());

            var request = LivePlanRequestFile.ReadAndDelete(path);

            Assert.AreEqual(SqlLogin, request.ConnectionString);
            Assert.AreEqual("s3cret!", new NetworkCredential(string.Empty, request.Password).Password);
            Assert.IsTrue(request.Password.IsReadOnly(), "SqlCredential needs it read only.");
            Assert.AreEqual(57, request.SessionId);
            Assert.AreEqual(new DateTime(2026, 10, 22, 9, 15, 30, 123), request.StartTime);
            Assert.AreEqual(DateTimeKind.Unspecified, request.StartTime!.Value.Kind, "The instance's local time - not to be converted.");
            Assert.AreEqual("SQLQuery1.sql", request.Title);
            Assert.IsFalse(File.Exists(path), "The file has a protected password in it, so it is deleted once read.");
        }

        [TestMethod]
        public void ReadAndDelete_WindowsAuthenticationAndNoStartTime()
        {
            var request = LivePlanRequestFile.ReadAndDelete(Write(Path.GetTempPath(), password: null, startTime: null));

            Assert.IsNull(request.Password);
            Assert.IsNull(request.StartTime);
        }

        [TestMethod]
        public void ReadAndDelete_RefusesAFileTheExtensionDidNotWrite()
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            try
            {
                var path = Write(directory);
                Assert.ThrowsExactly<InvalidOperationException>(() => LivePlanRequestFile.ReadAndDelete(path));
                Assert.IsTrue(File.Exists(path), "Not the extension's file, so not its to delete.");
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public void ReadAndDelete_RefusesAnOldRequest()
        {
            var path = Write(Path.GetTempPath());
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-1));

            Assert.ThrowsExactly<InvalidOperationException>(() => LivePlanRequestFile.ReadAndDelete(path));
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void ReadAndDelete_TheFileIsNotPlainText()
        {
            var path = Write(Path.GetTempPath(), connectionString: "Data Source=sql01;Integrated Security=False;User ID=app;Some Secret Keyword=hush");
            try
            {
                var bytes = File.ReadAllBytes(path);
                foreach (var text in new[] { "sql01", "hush", "DBADashLivePlan" })
                {
                    Assert.IsFalse(Encoding.UTF8.GetString(bytes).Contains(text) || Encoding.Unicode.GetString(bytes).Contains(text), text);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void ReadAndDelete_RefusesAFileThatIsNotProtected()
        {
            // As written by the first version of the extension, or by anything other than this user's extension.
            var path = Write(Path.GetTempPath(), protectFile: false, version: 1);

            Assert.ThrowsExactly<InvalidDataException>(() => LivePlanRequestFile.ReadAndDelete(path));
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void ReadAndDelete_RefusesAFileProtectedForAnotherUse()
        {
            var path = Write(Path.GetTempPath(), fileEntropy: Encoding.UTF8.GetBytes("Something else"));

            Assert.ThrowsExactly<InvalidDataException>(() => LivePlanRequestFile.ReadAndDelete(path));
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void ReadAndDelete_RefusesAFileTooBigToBeARequest()
        {
            var path = Path.Combine(Path.GetTempPath(), "ssms_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + LivePlanRequestFile.Extension);
            File.WriteAllBytes(path, new byte[1024 * 1024]);

            Assert.ThrowsExactly<InvalidDataException>(() => LivePlanRequestFile.ReadAndDelete(path));
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void ReadAndDelete_RefusesAnotherVersion()
        {
            var path = Write(Path.GetTempPath(), version: 3);

            Assert.ThrowsExactly<InvalidDataException>(() => LivePlanRequestFile.ReadAndDelete(path));
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void IsRequestFile_ByExtension()
        {
            Assert.IsTrue(LivePlanRequestFile.IsRequestFile(@"C:\Temp\ssms_abc.dbadashlive"));
            Assert.IsFalse(LivePlanRequestFile.IsRequestFile(@"C:\Temp\ssms_abc.sqlplan"));
            Assert.IsFalse(ViewerApp.CanOpen(@"C:\Temp\ssms_abc.dbadashlive"), "Not offered for drag and drop or the Open dialog.");
        }

        [TestMethod]
        public void Connection_SqlLogin_PasswordIsACredentialNotInTheConnectionString()
        {
            var connection = DirectLivePlanSource.Connection.From(Read());
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);

            Assert.IsTrue(connection.HasCredential);
            Assert.AreEqual(string.Empty, builder.Password);
            Assert.AreEqual(string.Empty, builder.UserID, "The login goes in the SqlCredential with its password.");
            Assert.IsFalse(builder.IntegratedSecurity);
            Assert.AreEqual("sql01", builder.DataSource);
            Assert.AreEqual(SqlConnectionEncryptOption.Mandatory, builder.Encrypt);
            Assert.AreEqual("DBA Dash Live Plan", builder.ApplicationName);
        }

        [TestMethod]
        public void Connection_Windows()
        {
            var connection = DirectLivePlanSource.Connection.From(Read(password: null, connectionString: "Data Source=sql01;Integrated Security=True"));

            Assert.IsFalse(connection.HasCredential);
            Assert.IsTrue(new SqlConnectionStringBuilder(connection.ConnectionString).IntegratedSecurity);
        }

        [TestMethod]
        public void Connection_TakesOnlyWhereToConnectAndHowToSecureIt()
        {
            var connection = DirectLivePlanSource.Connection.From(Read(password: null,
                connectionString: @"Data Source=sql01;Integrated Security=True;AttachDBFilename=\\evil\share\x.mdf;Pooling=False;Application Name=Something;Password=leak"));
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);

            Assert.AreEqual(string.Empty, builder.AttachDBFilename);
            Assert.IsTrue(builder.Pooling);
            Assert.AreEqual(string.Empty, builder.Password);
            Assert.AreEqual("DBA Dash Live Plan", builder.ApplicationName);
        }

        [TestMethod]
        public void Connection_KeepsReadOnlyIntent()
        {
            var connection = DirectLivePlanSource.Connection.From(Read(password: null,
                connectionString: "Data Source=aglistener;Integrated Security=True;ApplicationIntent=ReadOnly;MultiSubnetFailover=True"));
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);

            Assert.AreEqual(ApplicationIntent.ReadOnly, builder.ApplicationIntent);
            Assert.IsTrue(builder.MultiSubnetFailover);
        }

        [TestMethod]
        public void Connection_RefusesEntraAuthentication()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => DirectLivePlanSource.Connection.From(Read(password: null,
                connectionString: "Data Source=sql01;Authentication=ActiveDirectoryInteractive;User ID=someone@example.com")));
        }

        [TestMethod]
        public void Connection_RefusesAPasswordWithWindowsAuthentication()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => DirectLivePlanSource.Connection.From(Read(
                connectionString: "Data Source=sql01;Integrated Security=True")));
        }

        [TestMethod]
        public void Connection_RefusesSqlLoginWithoutPassword()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => DirectLivePlanSource.Connection.From(Read(password: null)));
        }
    }
}
