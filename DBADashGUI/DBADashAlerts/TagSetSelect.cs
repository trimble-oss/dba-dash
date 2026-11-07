using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>PropertyGrid editor for <see cref="DBADashTagSet"/> - opens the multi tag picker</summary>
    public class TagSetSelect : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (editorService == null)
                return value;

            using var picker = new Tagging.MultiTagPicker { SelectedTags = value as DBADashTagSet ?? DBADashTagSet.AllInstances() };
            return editorService.ShowDialog(picker) == DialogResult.OK ? picker.SelectedTags : value;
        }
    }
}
