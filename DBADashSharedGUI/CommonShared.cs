using DBADashGUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace DBADashSharedGUI
{
    public class CommonShared
    {
        public static void OpenURL(string url)
        {
            var psi = new ProcessStartInfo(url) { UseShellExecute = true };
            Process.Start(psi);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(IWin32Window owner,bool StartGUIOnUpgrade)
        {
            using About frm = new()
            {
                DBVersion = new Version(),
                StartGUIOnUpgrade = StartGUIOnUpgrade
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(string connectionString, IWin32Window owner, bool StartGUIOnUpgrade)
        {
            Version DBVersion;
            try
            {
                DBVersion = DBADash.DBValidations.GetDBVersion(connectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting repository version" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DBVersion = new Version();
            }
            using About frm = new()
            {
                DBVersion = DBVersion,
                StartGUIOnUpgrade = StartGUIOnUpgrade
            };
            frm.ShowDialog(owner);
        }

        public static void StyleGrid(ref DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.GetType() == typeof(DataGridViewLinkColumn))
                {
                    var linkCol = (DataGridViewLinkColumn)col;
                    linkCol.LinkColor = DashColors.LinkColor;
                }
            }
        }

    }
}
