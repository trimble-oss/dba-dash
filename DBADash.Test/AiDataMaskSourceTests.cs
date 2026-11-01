using System;
using System.IO;
using System.Linq;
using DBADash.Mcp;
using DBADashAI.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Where a DBADashMCP server's masking comes from.  A registered server runs as long as the AI tool's session - days,
    /// sometimes - so it reads the user's setting on each call rather than once when it starts.
    /// </summary>
    [TestClass]
    public class AiDataMaskSourceTests
    {
        private string _folder = null!;

        [TestInitialize]
        public void Setup() => _folder = Directory.CreateTempSubdirectory("dbadash-mask-").FullName;

        [TestCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }

        private string SettingPath => Path.Combine(_folder, "DBADash", "McpDataDetail.json");

        [TestMethod]
        public void NoFile_MasksParametersAndLiterals()
        {
            Assert.AreEqual(AiDataMask.Default, AiDataMaskSource.FromUserSetting(SettingPath).Current);
        }

        [TestMethod]
        public void AChange_IsReadOnTheNextCall()
        {
            var source = AiDataMaskSource.FromUserSetting(SettingPath);
            Assert.IsTrue(source.FollowsUserSetting);

            AiDataMaskSource.UserSetting.Write(SettingPath, "full");
            Assert.AreEqual(AiDataMask.None, source.Current);

            AiDataMaskSource.UserSetting.Write(SettingPath, "parameters");
            Assert.AreEqual(new AiDataMask(true, false), source.Current);

            AiDataMaskSource.UserSetting.Write(SettingPath, "metadataOnly");
            Assert.AreEqual(AiDataMask.Metadata, source.Current);

            File.Delete(SettingPath);
            Assert.AreEqual(AiDataMask.Default, source.Current, "Removing the setting goes back to the default, not to the last value.");
        }

        [TestMethod]
        [DataRow("{\"dataDetail\":\"everything\"}")]
        [DataRow("{\"dataDetail\":7}")]
        [DataRow("{}")]
        [DataRow("[]")]
        public void AWordThatIsntALevel_Masks(string json)
        {
            var source = AiDataMaskSource.FromUserSetting(SettingPath);
            AiDataMaskSource.UserSetting.Write(SettingPath, "full");
            Assert.AreEqual(AiDataMask.None, source.Current);

            File.WriteAllText(SettingPath, json);
            Assert.AreEqual(AiDataMask.Default, source.Current);
        }

        [TestMethod]
        public void AFileThatCantBeReadJustNow_KeepsTheLastValue()
        {
            var source = AiDataMaskSource.FromUserSetting(SettingPath);
            AiDataMaskSource.UserSetting.Write(SettingPath, "literals");
            Assert.AreEqual(new AiDataMask(false, true), source.Current);

            // Half written: not JSON.
            File.WriteAllText(SettingPath, "{\"dataDe");
            Assert.AreEqual(new AiDataMask(false, true), source.Current);

            // Locked by whoever is writing it.
            AiDataMaskSource.UserSetting.Write(SettingPath, "parameters");
            using (new FileStream(SettingPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.AreEqual(new AiDataMask(false, true), source.Current);
            }

            Assert.AreEqual(new AiDataMask(true, false), source.Current);
        }

        [TestMethod]
        public void Write_RefusesAWordThatIsntALevel()
        {
            Assert.ThrowsExactly<ArgumentException>(() => AiDataMaskSource.UserSetting.Write(SettingPath, "everything"));
            Assert.IsFalse(File.Exists(SettingPath));
        }

        [TestMethod]
        public void Fixed_IgnoresTheSetting()
        {
            var source = AiDataMaskSource.Fixed(AiDataMask.None);
            Assert.IsFalse(source.FollowsUserSetting);
            Assert.AreEqual(AiDataMask.None, source.Current);
        }

        [TestMethod]
        public void ARegistration_LeavesTheLevelToTheSetting_AndAOneRequestServerPinsIt()
        {
            const string exe = @"C:\Program Files\DBA Dash\DBADashMCP.exe";
            CollectionAssert.AreEqual(new[] { "--connection", "SQL01" }, McpServerEntry.ForConnection(exe, "SQL01").Args.ToArray());
            CollectionAssert.AreEqual(new[] { "--connection", "SQL01", "--data-detail", "parametersAndLiterals" },
                McpServerEntry.ForConnection(exe, "SQL01", "parametersAndLiterals").Args.ToArray(),
                "The default is pinned too, or a later change to the setting would reach this request's server.");
        }
    }
}
