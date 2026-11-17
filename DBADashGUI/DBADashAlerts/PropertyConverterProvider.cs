using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>A converter, and optionally an editor, for a named property in the PropertyGrid</summary>
    internal sealed record PropertyConverter(string PropertyName, TypeConverter Converter);

    /// <summary>
    /// TypeDescriptionProvider that attaches a TypeConverter (and optionally a UITypeEditor) to named properties so the
    /// PropertyGrid shows a named dropdown (e.g. GroupID, EmailServerID) instead of a raw number.
    /// </summary>
    internal sealed class PropertyConverterProvider : TypeDescriptionProvider
    {
        private readonly IReadOnlyDictionary<string, PropertyConverter> _converters;

        public PropertyConverterProvider(TypeDescriptionProvider parent, params PropertyConverter[] converters) : base(parent)
            => _converters = converters.ToDictionary(c => c.PropertyName, c => c);

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            => new ConverterTypeDescriptor(base.GetTypeDescriptor(objectType, instance), _converters);

        private sealed class ConverterTypeDescriptor : CustomTypeDescriptor
        {
            private readonly IReadOnlyDictionary<string, PropertyConverter> _converters;

            public ConverterTypeDescriptor(ICustomTypeDescriptor parent, IReadOnlyDictionary<string, PropertyConverter> converters) : base(parent)
                => _converters = converters;

            public override PropertyDescriptorCollection GetProperties()
                => Wrap(base.GetProperties());

            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
                => Wrap(base.GetProperties(attributes));

            private PropertyDescriptorCollection Wrap(PropertyDescriptorCollection props)
            {
                var newProps = props.Cast<PropertyDescriptor>()
                    .Select(p => _converters.TryGetValue(p.Name, out var c) ? new ConverterPropertyDescriptor(p, c) : p)
                    .ToArray();
                return new PropertyDescriptorCollection(newProps);
            }
        }

        private sealed class ConverterPropertyDescriptor : PropertyDescriptor
        {
            private readonly PropertyDescriptor _inner;
            private readonly PropertyConverter _propertyConverter;

            public ConverterPropertyDescriptor(PropertyDescriptor inner, PropertyConverter propertyConverter) : base(inner)
            {
                _inner = inner;
                _propertyConverter = propertyConverter;
            }

            public override TypeConverter Converter => _propertyConverter.Converter;
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
