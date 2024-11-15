using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI.DBADashAlerts
{
    public class TagSelect : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            // This will show the ellipsis button (...) next to the property value in the PropertyGrid
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            // Use the IWindowsFormsEditorService to show your dialog
            var editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

            if (editorService == null)
                return value; // Return the original value if the user cancels or closes the dialog

            using var tagPicker = new Tagging.TagPicker() { SelectedTag = (DBADashTag)value };
            return editorService.ShowDialog(tagPicker) == DialogResult.OK ? tagPicker.SelectedTag : value;
        }
    }
}