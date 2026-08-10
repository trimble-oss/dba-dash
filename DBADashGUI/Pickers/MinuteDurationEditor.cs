using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace DBADashGUI.Pickers
{
    /// <summary>
    /// A PropertyGrid drop-down editor for a duration that is stored as a number of minutes.
    /// It presents separate day / hour / minute entry boxes (e.g. [7] days [4] hrs [1] min).
    /// Reusable on any <c>decimal?</c> / <c>int?</c> property whose value represents minutes.
    /// Use <see cref="NullableMinuteDurationEditor"/> for properties where an unset (null) value
    /// is a meaningful state that the user should be able to choose.
    /// </summary>
    public class MinuteDurationEditor : UITypeEditor
    {
        private sealed class DropDownCloser : DurationDropDown.IWindowsFormsEditorServiceCloser
        {
            private readonly IWindowsFormsEditorService service;

            public DropDownCloser(IWindowsFormsEditorService service) => this.service = service;

            public void CloseDropDown() => service.CloseDropDown();
        }

        /// <summary>When true, the drop-down offers a "Not set" checkbox so the user can clear the value.</summary>
        protected virtual bool AllowNull => false;

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            => UITypeEditorEditStyle.DropDown;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService editorService)
            {
                return value;
            }

            using var control = new DurationDropDown(new DropDownCloser(editorService), AllowNull)
            {
                Value = value == null ? null : Convert.ToDecimal(value)
            };

            editorService.DropDownControl(control);

            return FromValue(control.Value, context);
        }

        /// <summary>Converts the edited minutes back to the property's underlying type (int or decimal), or null.</summary>
        private static object FromValue(decimal? minutes, ITypeDescriptorContext context)
        {
            if (minutes == null)
            {
                return null;
            }

            var propertyType = context?.PropertyDescriptor?.PropertyType;
            var underlyingType = propertyType == null ? null : Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            return underlyingType == typeof(int)
                ? (int)Math.Round(minutes.Value, MidpointRounding.AwayFromZero)
                : (object)minutes.Value;
        }
    }

    /// <summary>
    /// A <see cref="MinuteDurationEditor"/> variant that lets the user explicitly clear the value
    /// (via a "Not set" checkbox), for properties where null is a meaningful state.
    /// </summary>
    public sealed class NullableMinuteDurationEditor : MinuteDurationEditor
    {
        protected override bool AllowNull => true;
    }
}
