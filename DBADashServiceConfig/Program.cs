global using DBADashSharedGUI;
using System;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new ServiceConfig());
        }
    }
}