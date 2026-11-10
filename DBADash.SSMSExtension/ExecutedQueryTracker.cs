using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Keeps the text each query window last executed - its selection, if any, as SSMS runs only that, or else all of it
    /// - taken as Query.Execute (F5, Ctrl+E, the Execute button) starts.  What's in the editor when the results are
    /// opened may not be what ran: the selection may have moved, or the text been edited since.  The app needs what ran
    /// to tell whether it was a single statement, whose own time is the total.
    ///
    /// Other ways of executing - the current statement on its own, where SSMS has that - run text this can't tell, so
    /// they forget the window's text rather than leave an earlier execution's standing for theirs.
    /// </summary>
    internal static class ExecutedQueryTracker
    {
        private const string ExecuteCommand = "Query.Execute";

        /// <summary>Commands that execute something other than the selection or the whole window.</summary>
        private static readonly string[] OtherExecuteCommands = ["Query.ExecuteCurrentStatement", "Query.ExecuteStatement"];

        // By document: its full name, which an unsaved query window has too.
        private static readonly Dictionary<string, string> Executed = new(StringComparer.OrdinalIgnoreCase);

        // Held so the COM event sources aren't collected, which would silently unhook the handlers.
        private static readonly List<object> EventSources = new();

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            if (await package.GetServiceAsync(typeof(EnvDTE.DTE)) is not EnvDTE.DTE dte) return;

            Hook(dte, ExecuteCommand, Record);
            foreach (var name in OtherExecuteCommands) Hook(dte, name, Forget);

            try
            {
                var documents = dte.Events.DocumentEvents[null];
                documents.DocumentClosing += document =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    Executed.Remove(Key(document) ?? string.Empty);
                };
                EventSources.Add(documents);
            }
            catch
            {
                // Only tidies up - a closed window's text is just never asked for again.
            }
        }

        /// <summary>
        /// Calls <paramref name="handler"/> with the active document as the named command starts.  Nothing, where this
        /// version of SSMS has no such command.
        /// </summary>
        private static void Hook(EnvDTE.DTE dte, string commandName, Action<EnvDTE.Document> handler)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var command = dte.Commands.Item(commandName);
                var events = dte.Events.CommandEvents[command.Guid, command.ID];
                events.BeforeExecute += (string _, int _, object _, object _, ref bool _) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    try
                    {
                        handler(dte.ActiveDocument);
                    }
                    catch
                    {
                        // Never stand in the way of the query running.
                    }
                };
                EventSources.Add(events);
            }
            catch
            {
                // No such command in this SSMS.
            }
        }

        private static void Record(EnvDTE.Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Key(document) is not { } key) return;

            var text = EditorText(document);
            if (text == null) Executed.Remove(key);
            else Executed[key] = text;
        }

        private static void Forget(EnvDTE.Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Key(document) is { } key) Executed.Remove(key);
        }

        /// <summary>The text <paramref name="document"/> last executed, or null if it isn't known.</summary>
        public static string ExecutedText(EnvDTE.Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Key(document) is { } key && Executed.TryGetValue(key, out var text) ? text : null;
        }

        /// <summary>
        /// What executing <paramref name="document"/> now would run: its selection if any, or else all of it.  Null if it
        /// can't be read, or is too long to be worth sending.
        /// </summary>
        public static string EditorText(EnvDTE.Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (document?.Object("TextDocument") is not EnvDTE.TextDocument textDocument) return null;

            var selected = textDocument.Selection?.Text;
            var text = !string.IsNullOrWhiteSpace(selected)
                ? selected
                : textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            return string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength ? null : text;
        }

        private const int MaxTextLength = 1_000_000;

        private static string Key(EnvDTE.Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                return document?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }
}
