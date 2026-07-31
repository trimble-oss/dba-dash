using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// PropertyGrid editor that shows the <see cref="CustomSqlProcPicker"/> modal list of UserAlert procs
    /// for the custom SQL alert rule's ProcName property.
    /// </summary>
    public class CustomSqlProcSelect : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            => UITypeEditorEditStyle.Modal;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var editorService = (IWindowsFormsEditorService)provider?.GetService(typeof(IWindowsFormsEditorService));
            if (editorService == null)
                return value;

            using var picker = new CustomSqlProcPicker { SelectedProcName = value as string };
            return editorService.ShowDialog(picker) == DialogResult.OK ? picker.SelectedProcName : value;
        }
    }
}
