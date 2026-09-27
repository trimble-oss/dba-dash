using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Locates a control of a given type somewhere in the same top-level window as whatever currently
    /// has keyboard focus in SSMS. Both the execution plan and the deadlock graph tabs are WinForms
    /// controls hosted in the same document window as everything else the user might have focused
    /// (the diagram canvas, a search box, a legend) - walking up to the window root and back down
    /// finds the tab's control regardless of which of its children actually has focus.
    /// </summary>
    internal static class FocusedControlFinder
    {
        /// <summary>
        /// Finds the first control of the exact runtime type <paramref name="fullTypeName"/> in the
        /// window that currently has focus. Returns null if nothing is focused, or the window holds no
        /// control of that type - e.g. because the command was invoked from an unrelated tab.
        /// </summary>
        public static Control Find(string fullTypeName)
        {
            var focused = Control.FromHandle(GetFocus());
            if (focused == null) return null;

            var root = focused;
            while (root.Parent != null) root = root.Parent;

            return FindDescendant(root, fullTypeName);
        }

        private static Control FindDescendant(Control control, string fullTypeName)
        {
            if (string.Equals(control.GetType().FullName, fullTypeName, StringComparison.Ordinal))
            {
                return control;
            }

            foreach (Control child in control.Controls)
            {
                var match = FindDescendant(child, fullTypeName);
                if (match != null) return match;
            }

            return null;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
    }
}
