using System;
using System.ComponentModel;
using System.Linq;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// TypeDescriptionProvider that attaches <see cref="NotificationChannelGroupConverter"/>
    /// to the <c>GroupID</c> property so the PropertyGrid shows a named dropdown.
    /// </summary>
    internal sealed class GroupIdConverterProvider : TypeDescriptionProvider
    {
        public GroupIdConverterProvider(TypeDescriptionProvider parent) : base(parent) { }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            => new GroupIdTypeDescriptor(base.GetTypeDescriptor(objectType, instance));

        private sealed class GroupIdTypeDescriptor : CustomTypeDescriptor
        {
            public GroupIdTypeDescriptor(ICustomTypeDescriptor parent) : base(parent) { }

            public override PropertyDescriptorCollection GetProperties()
                => Wrap(base.GetProperties());

            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
                => Wrap(base.GetProperties(attributes));

            private static PropertyDescriptorCollection Wrap(PropertyDescriptorCollection props)
            {
                var newProps = props.Cast<PropertyDescriptor>()
                    .Select(p => p.Name == "GroupID" ? new ConverterPropertyDescriptor(p) : p)
                    .ToArray();
                return new PropertyDescriptorCollection(newProps);
            }
        }

        private sealed class ConverterPropertyDescriptor : PropertyDescriptor
        {
            private readonly PropertyDescriptor _inner;
            private static readonly NotificationChannelGroupConverter ConverterInstance = new();

            public ConverterPropertyDescriptor(PropertyDescriptor inner) : base(inner) => _inner = inner;

            public override TypeConverter Converter => ConverterInstance;
            public override Type ComponentType => _inner.ComponentType;
            public override bool IsReadOnly => _inner.IsReadOnly;
            public override Type PropertyType => _inner.PropertyType;
            public override bool CanResetValue(object component) => _inner.CanResetValue(component);
            public override object GetValue(object component) => _inner.GetValue(component);
            public override void ResetValue(object component) => _inner.ResetValue(component);
            public override void SetValue(object component, object value) => _inner.SetValue(component, value);
            public override bool ShouldSerializeValue(object component) => _inner.ShouldSerializeValue(component);
        }
    }
}
