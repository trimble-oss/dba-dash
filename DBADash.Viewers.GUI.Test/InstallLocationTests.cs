using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Whether a copy of the app was installed by winget or extracted from the zip.</summary>
    [TestClass]
    public class InstallLocationTests
    {
        [TestMethod]
        [DataRow(@"C:\Users\someone\AppData\Local\Microsoft\WinGet\Packages\Trimble.DBADashVisualizer_Microsoft.Winget.Source_8wekyb3d8bbwe\")]
        [DataRow(@"C:\Users\someone\AppData\Local\Microsoft\WinGet\Packages\Trimble.DBADashVisualizer_Microsoft.Winget.Source_8wekyb3d8bbwe")]
        [DataRow(@"C:\Program Files\WinGet\Packages\Trimble.DBADashVisualizer_Microsoft.Winget.Source_8wekyb3d8bbwe\")]
        [DataRow(@"C:\Users\someone\AppData\Local\Microsoft\WinGet\Links\")]
        [DataRow("C:/Users/someone/AppData/Local/Microsoft/WinGet/Packages/Trimble.DBADashVisualizer_x/")]
        [DataRow(@"c:\users\someone\appdata\local\microsoft\winget\packages\trimble.dbadashvisualizer_x\")]
        public void AFolderUnderWinGetIsAWingetInstall(string folder)
        {
            Assert.AreEqual(InstallSource.Winget, InstallLocation.Detect(folder));
        }

        [TestMethod]
        [DataRow(@"C:\Tools\DBADashVisualizer\")]
        [DataRow(@"C:\Users\someone\Downloads\DBADash_Visualizer_4.19.0")]
        [DataRow(@"D:\DBADash\")]
        // Named like winget's folder, but not under one.
        [DataRow(@"C:\WinGetPackages\DBADashVisualizer\")]
        [DataRow(@"C:\Tools\MyWinGet\DBADashVisualizer\")]
        public void AnyOtherFolderIsAZipInstall(string folder)
        {
            Assert.AreEqual(InstallSource.Zip, InstallLocation.Detect(folder));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void NoFolderIsAZipInstall(string folder)
        {
            Assert.AreEqual(InstallSource.Zip, InstallLocation.Detect(folder));
        }
    }
}
