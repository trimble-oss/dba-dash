global using DBADashSharedGUI;
using CommandLine;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using DBADash.Alert;
using DBADashGUI.DBADashAlerts;

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
            AddEditors();
            Common.IsApplicationRunning = true;
            Parser.Default.ParseArguments<CommandLineOptions>(args)
       .WithParsed(o =>
       {
           Application.EnableVisualStyles();
           Application.SetCompatibleTextRenderingDefault(false);
           Application.Run(new Main(o));
       });
        }

        public static void AddEditors()
        {
            TypeDescriptor.AddAttributes(typeof(DBADashTag),
                new EditorAttribute(typeof(TagSelect), typeof(UITypeEditor)));

            TypeDescriptor.AddAttributes(typeof(JsonString),
                new EditorAttribute(typeof(JsonStringEditor), typeof(UITypeEditor)));

            TypeDescriptor.AddAttributes(typeof(string),
                new EditorAttribute(typeof(MultilineStringEditor), typeof(UITypeEditor)));
        }
    }
}