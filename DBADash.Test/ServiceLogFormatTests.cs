#nullable disable
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace DBADash.Test
{
    /// <summary>
    /// Tests for <see cref="ServiceLogFormat"/>: switching the service's log file between text and Serilog's compact JSON,
    /// with the choice kept in serilog.local.json so an upgrade - which replaces serilog.json - leaves it in place.
    /// </summary>
    [TestClass]
    public class ServiceLogFormatTests
    {
        private string _folder;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "ServiceLogFormatTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            File.Copy(ShippedConfigPath(), Path.Combine(_folder, ServiceLogFormat.DefaultConfigFileName));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        /// <summary>The serilog.json the service ships with, from the source tree.</summary>
        private static string ShippedConfigPath()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var path = Path.Combine(dir.FullName, "DBADashService", ServiceLogFormat.DefaultConfigFileName);
                if (File.Exists(path)) return path;
            }

            throw new FileNotFoundException("DBADashService/serilog.json not found above " + AppContext.BaseDirectory);
        }

        private string LocalPath => Path.Combine(_folder, ServiceLogFormat.LocalConfigFileName);

        private JObject FileSinkArgs(string fileName) =>
            (JObject)JObject.Parse(File.ReadAllText(Path.Combine(_folder, fileName)))
                .SelectToken("Serilog.WriteTo[0].Args.configure[?(@.Name == 'File')].Args");

        [TestMethod]
        public void ShippedConfig_IsJson()
        {
            Assert.AreEqual(ServiceLogFileFormat.Json, ServiceLogFormat.GetFormat(_folder));
            Assert.AreEqual(ServiceLogFormat.DefaultConfigFileName, ServiceLogFormat.ConfigFileName(_folder));
            var args = FileSinkArgs(ServiceLogFormat.DefaultConfigFileName);
            Assert.AreEqual("Logs/log-.json", (string)args["path"]);
            Assert.AreEqual(ServiceLogFormat.JsonFormatter, (string)args["formatter"]);
        }

        [TestMethod]
        public void SetText_WritesLocalConfig_WithTextSinkAndTxtExtension()
        {
            ServiceLogFormat.SetFormat(_folder, ServiceLogFileFormat.Text);

            Assert.IsTrue(File.Exists(LocalPath));
            Assert.AreEqual(ServiceLogFormat.LocalConfigFileName, ServiceLogFormat.ConfigFileName(_folder));
            Assert.AreEqual(ServiceLogFileFormat.Text, ServiceLogFormat.GetFormat(_folder));

            var args = FileSinkArgs(ServiceLogFormat.LocalConfigFileName);
            Assert.AreEqual("Logs/log-.txt", (string)args["path"]);
            Assert.IsNull(args["formatter"]);
            Assert.AreEqual(ServiceLogFormat.TextOutputTemplate, (string)args["outputTemplate"]);
            // The rest of the sink is as shipped.
            Assert.AreEqual("Hour", (string)args["rollingInterval"]);
            Assert.AreEqual(24, (int)args["retainedFileCountLimit"]);
        }

        [TestMethod]
        public void SetJson_AfterText_RemovesLocalConfig_WhenItMatchesShipped()
        {
            ServiceLogFormat.SetFormat(_folder, ServiceLogFileFormat.Text);
            ServiceLogFormat.SetFormat(_folder, ServiceLogFileFormat.Json);

            Assert.IsFalse(File.Exists(LocalPath), "The release's config is used again, so later releases' changes are picked up");
            Assert.AreEqual(ServiceLogFileFormat.Json, ServiceLogFormat.GetFormat(_folder));
        }

        [TestMethod]
        public void SetJson_KeepsLocalConfig_WithTheUsersOtherEdits()
        {
            ServiceLogFormat.SetFormat(_folder, ServiceLogFileFormat.Text);
            var local = JObject.Parse(File.ReadAllText(LocalPath));
            local["Serilog"]!["MinimumLevel"] = "Debug";
            File.WriteAllText(LocalPath, local.ToString());

            ServiceLogFormat.SetFormat(_folder, ServiceLogFileFormat.Json);

            Assert.IsTrue(File.Exists(LocalPath));
            Assert.AreEqual(ServiceLogFileFormat.Json, ServiceLogFormat.GetFormat(_folder));
            Assert.AreEqual("Debug", (string)JObject.Parse(File.ReadAllText(LocalPath)).SelectToken("Serilog.MinimumLevel"));
            Assert.AreEqual("Logs/log-.json", (string)FileSinkArgs(ServiceLogFormat.LocalConfigFileName)["path"]);
        }

        /// <summary>
        /// An extension the user chose is kept where it suits the new format.  Otherwise it's changed, so the new format
        /// isn't appended to a file in the old one when the service restarts within the same rolling period.
        /// </summary>
        [TestMethod]
        [DataRow("Logs/service-.clef", ServiceLogFileFormat.Json, "Logs/service-.clef")]
        [DataRow("Logs/service-.jsonl", ServiceLogFileFormat.Json, "Logs/service-.jsonl")]
        [DataRow("Logs/service-.log", ServiceLogFileFormat.Text, "Logs/service-.log")]
        [DataRow("Logs/service-.log", ServiceLogFileFormat.Json, "Logs/service-.json")]
        [DataRow("Logs/service-.clef", ServiceLogFileFormat.Text, "Logs/service-.txt")]
        [DataRow("Logs/service-.data", ServiceLogFileFormat.Json, "Logs/service-.json")]
        [DataRow("Logs/service-", ServiceLogFileFormat.Text, "Logs/service-.txt")]
        public void SetFormat_KeepsAnExtensionOnlyWhereItSuitsTheFormat(string path, ServiceLogFileFormat format, string expected)
        {
            var config = JObject.Parse($$"""
                { "Serilog": { "WriteTo": [ { "Name": "File", "Args": { "path": "{{path}}", "outputTemplate": "{Message}" } } ] } }
                """);

            ServiceLogFormat.SetFormat(config, format);

            var args = config.SelectToken("Serilog.WriteTo[0].Args")!;
            Assert.AreEqual(expected, (string)args["path"]);
            if (format == ServiceLogFileFormat.Json)
            {
                Assert.AreEqual(ServiceLogFormat.JsonFormatter, (string)args["formatter"]);
                Assert.IsNull(args["outputTemplate"]);
            }
            else
            {
                Assert.AreEqual(ServiceLogFormat.TextOutputTemplate, (string)args["outputTemplate"]);
                Assert.IsNull(args["formatter"]);
            }
        }

        [TestMethod]
        public void NoFileSink_IsNone_AndCantBeSet()
        {
            var config = JObject.Parse("""{ "Serilog": { "WriteTo": [ { "Name": "Console" } ] } }""");

            Assert.AreEqual(ServiceLogFileFormat.None, ServiceLogFormat.GetFormat(config));
            Assert.Throws<InvalidOperationException>(() => ServiceLogFormat.SetFormat(config, ServiceLogFileFormat.Text));
        }
    }
}
