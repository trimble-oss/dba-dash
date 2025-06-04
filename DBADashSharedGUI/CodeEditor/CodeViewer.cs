using DBADashGUI.Theme;
using System;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;

namespace DBADashGUI
{
    public class CodeViewer : CodeEditorForm
    {
        public CodeViewer()
        {
            EditEnabled = false;
        }

        public CodeEditor.CodeEditorModes Language
        {
            get => Syntax;
            set => Syntax = value;
        }
    }
}