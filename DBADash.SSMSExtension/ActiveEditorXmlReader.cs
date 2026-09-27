using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Reads a plan or deadlock graph out of a plain XML results tab - what you get from a hyperlinked
    /// XML grid cell (e.g. sp_BlitzLock's deadlock output), rather than SSMS's own graphical plan or
    /// deadlock viewer. That's just a normal text editor showing XML, so unlike ShowPlanXmlReader/
    /// DeadlockXmlReader this needs no reflection - IVsTextManager/IVsTextView is the same public,
    /// stable API every VS-based editor is built on, SSMS's query editor included.
    /// </summary>
    internal static class ActiveEditorXmlReader
    {
        // Enough to reach ShowPlanXML (the root element) or <deadlock (the root, or a few hundred
        // characters into an xml_deadlock_report event wrapping it) without copying the whole buffer.
        private const int SniffLength = 8192;

        /// <summary>
        /// True if the active editor looks like it holds a plan or deadlock graph. Reads only the start
        /// of the buffer, since it runs on every BeforeQueryStatus and the buffer could be a large script.
        /// </summary>
        public static bool IsRelevant()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var lines = GetActiveEditorLines();
            return lines != null && LooksLikePlanOrDeadlock(GetText(lines, SniffLength));
        }

        /// <summary>
        /// Returns the active editor's text if it looks like a plan or deadlock graph, or null if
        /// nothing is focused, it isn't a text editor, or its content is neither. Which one it actually
        /// is gets decided later, by DBADashVisualizer's own file-open content sniffing
        /// (ViewerLauncher.ShowXmlFile) - this only needs to be sure it's worth opening at all.
        /// </summary>
        public static string TryRead()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var lines = GetActiveEditorLines();
            if (lines == null || !LooksLikePlanOrDeadlock(GetText(lines, SniffLength))) return null;

            return GetText(lines, int.MaxValue);
        }

        // The same cheap text checks PlanParser.LooksLikeExecutionPlan/DeadlockParser use in the main
        // app - reproduced here rather than referenced, since this project can't take a project
        // reference to that net10 code from its own net472 runtime. Must also start as XML, so a SQL
        // script that merely mentions <deadlock (e.g. XQuery over system_health) isn't mistaken for one.
        private static bool LooksLikePlanOrDeadlock(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var trimmed = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
            return trimmed.StartsWith("<", StringComparison.Ordinal) &&
                   (trimmed.IndexOf("ShowPlanXML", StringComparison.Ordinal) >= 0 ||
                    trimmed.IndexOf("<deadlock", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IVsTextLines GetActiveEditorLines()
        {
            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager)) is not IVsTextManager textManager)
            {
                return null;
            }

            textManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var activeView);
            if (activeView == null) return null;

            return activeView.GetBuffer(out var textLines) == VSConstants.S_OK ? textLines : null;
        }

        /// <summary>The first <paramref name="maxLength"/> characters of the buffer (line breaks not
        /// counted), or the whole buffer for int.MaxValue.</summary>
        private static string GetText(IVsTextLines lines, int maxLength)
        {
            if (lines.GetLastLineIndex(out var lastLine, out var lastIndex) != VSConstants.S_OK) return null;

            var endLine = lastLine;
            var endIndex = lastIndex;
            if (maxLength != int.MaxValue)
            {
                long length = 0;
                for (var line = 0; line <= lastLine; line++)
                {
                    if (lines.GetLengthOfLine(line, out var lineLength) != VSConstants.S_OK) return null;
                    length += lineLength;
                    if (length < maxLength) continue;

                    // A single long line (sp_BlitzLock's output is often one line) is cut mid-line.
                    endLine = line;
                    endIndex = lineLength - (int)(length - maxLength);
                    break;
                }
            }

            return lines.GetLineText(0, 0, endLine, endIndex, out var text) == VSConstants.S_OK ? text : null;
        }
    }
}
