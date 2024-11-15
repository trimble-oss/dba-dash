using DBADashGUI.SchemaCompare;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI.DBADashAlerts
{
    public class MarkdownEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            // This will open the modal editor when the property is clicked
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService = null;

            if (provider != null)
            {
                editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            }

            if (editorService != null)
            {
                using var codeViewer = new CodeEditorForm();
                codeViewer.Code = value?.ToString();
                codeViewer.Syntax = CodeEditor.CodeEditorModes.Markdown;
                return editorService.ShowDialog(codeViewer) != DialogResult.OK ? value : codeViewer.Code;
            }

            return value;
        }
    }
}