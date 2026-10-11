using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Telling an install for all users from one for the current user, by where it is.</summary>
    [TestClass]
    public class InstallScopeTests
    {
        private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        [TestMethod]
        public void UnderLocalAppDataIsForTheUser() =>
            Assert.IsFalse(InstallScope.IsForAllUsers(Path.Combine(LocalAppData, "DBADash_Visualizer", "current")));

        [TestMethod]
        public void CaseDoesNotMatter() =>
            Assert.IsFalse(InstallScope.IsForAllUsers(Path.Combine(LocalAppData.ToUpperInvariant(), "DBADash_GUI", "current")));

        [TestMethod]
        public void ProgramFilesIsForAllUsers() =>
            Assert.IsTrue(InstallScope.IsForAllUsers(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DBADash_Visualizer", "current")));

        [TestMethod]
        public void AFolderThatOnlyStartsWithTheSameNameIsNotInIt() =>
            Assert.IsTrue(InstallScope.IsForAllUsers(LocalAppData + "Other\\DBADash_GUI\\current"));

        [TestMethod]
        public void NoPathIsNotForAllUsers() => Assert.IsFalse(InstallScope.IsForAllUsers(null));
    }
}
