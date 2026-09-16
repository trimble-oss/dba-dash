global using DBADashSharedGUI;
using DBADashGUI.Theme;
using CommandLine;
using DBADashGUI.Deadlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using DBADash.Alert;
using DBADashGUI.DBADashAlerts;
using Serilog;

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
            ConfigureLogging();
            try
            {
                Run(args);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>Folder the GUI writes its log to - per user, as the GUI can run from a folder the user can't write to.</summary>
        private static string LogFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash", "Logs");

        /// <summary>
        /// Without this the Serilog calls made from the GUI go nowhere.  Kept to a small rolling file: the GUI has no
        /// console, and the log is only needed to diagnose something that failed without telling the user.
        /// </summary>
        private static void ConfigureLogging()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(Path.Combine(LogFolder, "DBADashGUI-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        // Standalone deadlock viewers run as separate processes alongside the main GUI
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {NewLine}{Exception}")
                    .CreateLogger();
            }
            catch
            {
                // Logging is a diagnostic aid - never a reason not to start
            }
        }

        private static void Run(string[] args)
        {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            result.WithParsed(o =>
       {
           try
           {
               // If the user has a saved theme in user settings, apply it immediately
               // so the app starts using the selected theme instead of briefly
               // showing the default (white) theme.
               var saved = Properties.Settings.Default.Theme as string;
               if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out ThemeType st))
               {
                   DBADashUser.SetTheme(st);
               }
           }
           catch
           {
               // ignore
           }
           if (o.RegisterFileAssociation || o.UnregisterFileAssociation)
           {
               SetFileAssociation(o.RegisterFileAssociation);
               return;
           }

           var files = o.Files?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
           if (files is { Count: > 0 })
           {
               // Anything that isn't a file is more likely a mistyped switch than a deadlock graph - say how to
               // use the command line rather than report it as a graph that couldn't be read.
               var missing = files.Where(f => !File.Exists(f)).ToList();
               if (missing.Count > 0)
               {
                   MessageBox.Show(
                       $"File not found:\n{string.Join("\n", missing)}\n\n{CommandLine.Text.HelpText.AutoBuild(result, h => h, e => e)}",
                       "DBA Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }

               RunDeadlockViewer(files.Except(missing));
               return;
           }

           DeadlockFileAssociation.UpdateRegistration();
           Application.Run(new Main(o));
       });
        }

        /// <summary>
        /// A file passed on the command line - typically from Explorer via the .xdl association - opens just the
        /// deadlock viewer.  The graph needs no repository connection, so there is no reason to make the user sit
        /// through the full GUI starting up to read one.  The process ends when the last window is closed.
        /// </summary>
        private static void RunDeadlockViewer(IEnumerable<string> files)
        {
            DeadlockViewerForm.IsStandalone = true;
            foreach (var file in files)
            {
                Common.ShowDeadlockGraphFile(file);
            }

            // Nothing opened - each failure has already been reported.
            if (!AnyVisibleForms()) return;

            // Exit once every window is closed - not just the viewers, as a viewer can open other windows that
            // would otherwise be closed out from under the user.  Idle runs once the close has been processed.
            Application.Idle += (_, _) =>
            {
                if (!AnyVisibleForms()) Application.ExitThread();
            };
            Application.Run();
        }

        private static bool AnyVisibleForms() => Application.OpenForms.Cast<Form>().Any(f => f.Visible);

        private static void SetFileAssociation(bool register)
        {
            try
            {
                if (register)
                {
                    DeadlockFileAssociation.Register();
                }
                else
                {
                    DeadlockFileAssociation.Unregister();
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error updating the .xdl file association");
            }
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