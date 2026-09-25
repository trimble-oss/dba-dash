using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using DBADashGUI.SchemaCompare;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// A PropertyGrid editor for read-only SQL string properties: the '...' button opens the value in
    /// the shared Code Viewer with SQL syntax highlighting, which reads a statement far better than the
    /// single line the grid gives it.  The value is never changed - the viewer is opened read-only and
    /// the original string is returned unchanged.
    /// </summary>
    public sealed class SqlCodeUITypeEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) =>
            UITypeEditorEditStyle.Modal;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (value is string sql && !string.IsNullOrWhiteSpace(sql))
            {
                using var frm = new CodeEditorForm
                {
                    Code = sql,
                    Syntax = CodeEditor.CodeEditorModes.SQL,
                    EditEnabled = false
                };

                // Shown through the PropertyGrid's editor service so it opens modally over the grid's
                // host rather than potentially behind the main form.
                if (provider?.GetService(typeof(IWindowsFormsEditorService)) is IWindowsFormsEditorService editorService)
                {
                    editorService.ShowDialog(frm);
                }
                else
                {
                    frm.ShowDialog();
                }
            }

            return value;
        }
    }
}
