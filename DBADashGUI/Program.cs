global using DBADashSharedGUI;
using DBADashGUI.Theme;
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
            try
            {
                // If the user has a saved theme in user settings, apply it immediately
                // so the app starts using the selected theme instead of briefly
                // showing the default (white) theme.
                var saved = Properties.Settings.Default["Theme"] as string;
                if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out ThemeType st))
                {
                    DBADashUser.SetTheme(st);
                }
            }
            catch
            {
                // ignore
            }
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