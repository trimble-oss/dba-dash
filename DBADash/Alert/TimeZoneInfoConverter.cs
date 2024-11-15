using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace DBADash.Alert
{
    public class TimeZoneInfoConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Get all system time zones, sorted by their base UTC offset and then by name
            var timeZones = TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(tz => tz.BaseUtcOffset)
                .ThenBy(tz => tz.DisplayName)
                .ToList();

            // Create a list of strings that includes both the display name and the offset for each time zone
            var timeZoneDisplayStrings = timeZones.Select(tz => tz.DisplayName).ToList();

            return new StandardValuesCollection(timeZoneDisplayStrings);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                // Extract the ID from the display string
                var timeZoneId = TimeZoneInfo.GetSystemTimeZones()
                    .FirstOrDefault(tz => tz.DisplayName == stringValue)?.Id;

                if (timeZoneId != null)
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is TimeZoneInfo timeZoneInfo)
            {
                // Convert the TimeZoneInfo object to the display string including its offset
                return timeZoneInfo.DisplayName;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}