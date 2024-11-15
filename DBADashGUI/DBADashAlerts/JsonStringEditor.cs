using DBADash.Alert;
using DBADashGUI.SchemaCompare;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI.DBADashAlerts
{
    public class JsonStringEditor : UITypeEditor
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
                codeViewer.Syntax = CodeEditor.CodeEditorModes.Json;
                editorService.ShowDialog(codeViewer);

                try
                {
                    // Attempt to parse edited value as JSON to validate
                    var jsonString = new JsonString(codeViewer.Code);
                    return jsonString;
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Invalid JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Return the original value if the JSON is invalid
                    return value;
                }
            }

            return value;
        }
    }
}