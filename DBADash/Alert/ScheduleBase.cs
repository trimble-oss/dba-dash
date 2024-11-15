using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DBADash.Alert
{
    public class ScheduleBase
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (IsEveryDay)
            {
                sb.Append("Every day");
            }
            else
            {
                sb.Append("On ").Append(string.Join(", ", GetDaysOfWeek()));
            }
            if (TimeFrom != null || TimeTo != null)
            {
                sb.Append($", from {TimeFrom ?? TimeOnly.MinValue:t} to {TimeTo ?? TimeOnly.MinValue:t}");
            }
            return sb.ToString();
        }

        public IEnumerable<ValidationResult> ValidateSchedule(ValidationContext validationContext)
        {
            if (TimeFrom > TimeTo)
            {
                yield return new ValidationResult("TimeTo should be greater than TimeFrom");
            }
        }

        [Browsable(false)]
        public bool IsEveryDay => Monday && Tuesday && Wednesday && Thursday && Friday && Saturday && Sunday;

        [TypeConverter(typeof(TimeZoneInfoConverter)), Category("Schedule")]
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;

        public static TimeZoneInfo TimeZoneFromString(string tz) => TimeZoneInfo.FindSystemTimeZoneById(tz);

        public string TimeZoneAsString() => TimeZone.Id;

        [Category("Schedule"), DisplayName("Time From")]
        public TimeOnly? TimeFrom { get; set; }

        [Category("Schedule"), DisplayName("Time To")]
        public TimeOnly? TimeTo { get; set; }

        [Category("Schedule - Days"), Description("Indicates if Monday is included."), DisplayName("1. Monday")]
        public bool Monday { get; set; } = true;

        [Category("Schedule - Days"), Description("Indicates if Tuesday is included."), DisplayName("2. Tuesday")]
        public bool Tuesday { get; set; } = true;

        [Category("Schedule - Days"), Description("Indicates if Wednesday is included."), DisplayName("3. Wednesday")]
        public bool Wednesday { get; set; } = true;

        [Category("Schedule - Days"), Description("Indicates if Thursday is included."), DisplayName("4. Thursday")]
        public bool Thursday { get; set; } = true;

        [Category("Schedule - Days"), Description("Indicates if Friday is included."), DisplayName("5. Friday")]
        public bool Friday { get; set; } = true;

        [Category("Schedule - Days"), Description("Indicates if Saturday is included."), DisplayName("6. Saturday")]
        public bool Saturday { get; set; } = true;

        [Category("Schedule - Days"), Description("Indicates if Sunday is included."), DisplayName("7. Sunday")]
        public bool Sunday { get; set; } = true;

        public HashSet<DayOfWeek> GetDaysOfWeek()
        {
            HashSet<DayOfWeek> includedDays = new();

            if (Monday) includedDays.Add(DayOfWeek.Monday);
            if (Tuesday) includedDays.Add(DayOfWeek.Tuesday);
            if (Wednesday) includedDays.Add(DayOfWeek.Wednesday);
            if (Thursday) includedDays.Add(DayOfWeek.Thursday);
            if (Friday) includedDays.Add(DayOfWeek.Friday);
            if (Saturday) includedDays.Add(DayOfWeek.Saturday);
            if (Sunday) includedDays.Add(DayOfWeek.Sunday);

            return includedDays;
        }
    }
}