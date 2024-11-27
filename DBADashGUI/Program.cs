global using DBADashSharedGUI;
using CommandLine;
using System;
using System.Windows.Forms;

namespace DBADashGUI
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Common.IsApplicationRunning = true;
            Parser.Default.ParseArguments<CommandLineOptions>(args)
       .WithParsed(o =>
       {
           Application.EnableVisualStyles();
           Application.SetCompatibleTextRenderingDefault(false);
           Application.Run(new Main(o));
       });
        }
    }
}