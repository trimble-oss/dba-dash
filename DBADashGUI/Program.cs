global using DBADashSharedGUI;
using DBADashGUI.Theme;
using CommandLine;
using DBADashGUI.Deadlocks;
using DBADashGUI.QueryPlans;
using DBADashGUI.ShellIntegration;
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
            // Default export file names use the time zone the user has chosen in the app.
            CommonShared.AppNow = () => DateHelper.AppNow;
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
               // Anything that isn't a file is more likely a mistyped switch than a graph or a plan - say
               // how to use the command line rather than report it as a file that couldn't be read.
               var missing = files.Where(f => !File.Exists(f)).ToList();
               if (missing.Count > 0)
               {
                   MessageBox.Show(
                       $"File not found:\n{string.Join("\n", missing)}\n\n{CommandLine.Text.HelpText.AutoBuild(result, h => h, e => e)}",
                       "DBA Dash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }

               RunViewers(files.Except(missing));
               return;
           }

           FileAssociation.UpdateRegistrations();
           Application.Run(new Main(o));
       });
        }

        /// <summary>
        /// A file passed on the command line - typically from Explorer via a file association - opens just the
        /// viewer for it.  Neither a deadlock graph nor a query plan needs a repository connection, so there is no
        /// reason to make the user sit through the full GUI starting up to read one.  The process ends when the
        /// last window is closed.
        /// </summary>
        private static void RunViewers(IEnumerable<string> files)
        {
            DeadlockViewerForm.IsStandalone = true;
            QueryPlanViewerForm.IsStandalone = true;

            // Full paths: another copy opening them resolves a relative path against its own folder.
            var paths = files.Select(Path.GetFullPath).ToList();

            // Explorer starts a copy per file.  The first opens them all - plans on tabs of one window -
            // and the rest hand their files to it and exit.  See ViewerInstance.
            using var instance = ViewerInstance.Claim();
            if (!instance.IsPrimary && instance.Forward(paths)) return;

            var closing = false;
            instance.Listen(forwarded =>
            {
                // Once the last window has closed the process is on its way out, and a file opened now
                // would vanish with it - the sender opens it instead.
                if (closing) return false;

                OpenFiles(forwarded);
                return true;
            });

            OpenFiles(paths);

            // Nothing opened - each failure has already been reported.
            if (!AnyVisibleForms()) return;

            // Exit once every window is closed - not just the viewers, as a viewer can open other windows that
            // would otherwise be closed out from under the user.  Idle runs once the close has been processed.
            Application.Idle += (_, _) =>
            {
                if (AnyVisibleForms()) return;

                closing = true;
                Application.ExitThread();
            };
            Application.Run();
        }

        private static void OpenFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                // Which viewer by extension, falling back to the deadlock viewer for the .xml both file
                // types also get saved as - it reports a file it cannot read, which is a better answer than
                // guessing at the content and being confidently wrong about it.
                if (Path.GetExtension(file).Equals(FileAssociation.QueryPlan.Extension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    Common.ShowQueryPlanFile(file);
                }
                else
                {
                    Common.ShowDeadlockGraphFile(file);
                }
            }
        }

        private static bool AnyVisibleForms() => Application.OpenForms.Cast<Form>().Any(f => f.Visible);

        private static void SetFileAssociation(bool register)
        {
            try
            {
                if (register)
                {
                    FileAssociation.RegisterAll();
                }
                else
                {
                    FileAssociation.UnregisterAll();
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error updating the file associations");
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