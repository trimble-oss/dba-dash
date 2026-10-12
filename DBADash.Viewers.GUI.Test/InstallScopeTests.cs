using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Telling an install for all users from one for the current user, by the folder the install for all users recorded.</summary>
    [TestClass]
    public class InstallScopeTests
    {
        private const string MachineInstall = @"D:\Apps\DBADash_Visualizer";

        [TestMethod]
        public void InTheInstallForAllUsersIsForAllUsers() =>
            Assert.IsTrue(InstallScope.IsIn(@"D:\Apps\DBADash_Visualizer\current", MachineInstall));

        [TestMethod]
        public void CaseDoesNotMatter() =>
            Assert.IsTrue(InstallScope.IsIn(@"d:\apps\dbadash_visualizer\current", MachineInstall + "\\"));

        [TestMethod]
        public void APerUserInstallOnAnotherDriveIsNotForAllUsers() =>
            Assert.IsFalse(InstallScope.IsIn(@"E:\Mine\DBADash_Visualizer\current", MachineInstall));

        [TestMethod]
        public void AFolderThatOnlyStartsWithTheSameNameIsNotInIt() =>
            Assert.IsFalse(InstallScope.IsIn(@"D:\Apps\DBADash_VisualizerOther\current", MachineInstall));

        [TestMethod]
        public void NoInstallForAllUsersIsNotForAllUsers() =>
            Assert.IsFalse(InstallScope.IsIn(@"C:\Program Files\DBADash_Visualizer\current", null));

        [TestMethod]
        public void NoPathIsNotForAllUsers() => Assert.IsFalse(InstallScope.IsIn(null, MachineInstall));
    }
}
