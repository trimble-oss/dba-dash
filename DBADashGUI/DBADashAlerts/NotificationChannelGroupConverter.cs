using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// TypeConverter that presents notification channel groups as a named dropdown in the PropertyGrid.
    /// </summary>
    public class NotificationChannelGroupConverter : TypeConverter
    {
        private static List<NotificationChannelGroup> _groups;

        /// <summary>Called by AlertConfig after groups may have changed to force a fresh load next time.</summary>
        public static void Invalidate() => _groups = null;

        private static List<NotificationChannelGroup> Groups
            => _groups ??= NotificationChannelGroup.GetGroups(Common.ConnectionString);

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => new(Groups.Select(g => g.GroupID).ToArray());

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is int id)
                return Groups.FirstOrDefault(g => g.GroupID == id)?.GroupName ?? "(Default)";
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string name)
            {
                var group = Groups.FirstOrDefault(g => g.GroupName == name);
                return group?.GroupID ?? 0;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
}