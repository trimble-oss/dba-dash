using DBADash.Alert;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DBADashGUI.AgentJobs
{
    /// <summary>What a busy period applies to.  The most specific level with any periods is the one used for an instance.</summary>
    public enum BusyPeriodScope
    {
        ThisInstance,
        Tag,
        AllInstances
    }

    /// <summary>
    /// One recurring busy period (dbo.BusyPeriod) - e.g. weekdays 08:00 to 18:00 - edited in a property grid like an alert
    /// blackout.  The times are in <see cref="ScheduleBase.TimeZone"/>; a To time earlier than the From time crosses midnight.
    /// </summary>
    public class BusyPeriod : ScheduleBase, IValidatableObject
    {
        [Browsable(false)]
        public int? BusyPeriodID { get; set; }

        /// <summary>
        /// The instance <see cref="BusyPeriodScope.ThisInstance"/> means: the one the editor was opened for, or for a period
        /// of another instance listed with Show all, that instance - so editing it doesn't move it
        /// </summary>
        [Browsable(false)]
        public int InstanceID { get; set; }

        [Category("Apply To"), DisplayName("1. Apply To")]
        [Description("This instance, every instance with a tag, or all instances.  An instance uses its own periods if it has any; otherwise those for its tags; otherwise those for all instances - so one instance can be the exception to its tag.")]
        public BusyPeriodScope ApplyTo { get; set; } = BusyPeriodScope.ThisInstance;

        [Category("Apply To"), DisplayName("2. Tag")]
        [Description("The tag the period applies to, when Apply To is Tag")]
        public DBADashTag ApplyToTag { get; set; } = DBADashTag.AllInstancesTag();

        [Category("Description")]
        [Description("What the period is for, e.g. Business hours")]
        public string Description { get; set; }

        public async Task Save()
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.BusyPeriod_Upd", cn) { CommandType = CommandType.StoredProcedure };
            var pId = new SqlParameter("BusyPeriodID", SqlDbType.Int) { Direction = ParameterDirection.InputOutput, Value = BusyPeriodID.HasValue ? BusyPeriodID.Value : DBNull.Value };
            cmd.Parameters.Add(pId);
            cmd.Parameters.AddWithValue("ApplyToInstanceID", ApplyTo == BusyPeriodScope.ThisInstance ? InstanceID : -1);
            cmd.Parameters.AddWithValue("ApplyToTagID", ApplyTo == BusyPeriodScope.Tag ? ApplyToTag.TagID : -1);
            cmd.Parameters.AddWithValue("Monday", Monday);
            cmd.Parameters.AddWithValue("Tuesday", Tuesday);
            cmd.Parameters.AddWithValue("Wednesday", Wednesday);
            cmd.Parameters.AddWithValue("Thursday", Thursday);
            cmd.Parameters.AddWithValue("Friday", Friday);
            cmd.Parameters.AddWithValue("Saturday", Saturday);
            cmd.Parameters.AddWithValue("Sunday", Sunday);
            cmd.Parameters.AddWithValue("TimeFrom", TimeFrom!.Value.ToTimeSpan());
            cmd.Parameters.AddWithValue("TimeTo", TimeTo!.Value.ToTimeSpan());
            cmd.Parameters.AddWithValue("TimeZone", TimeZoneAsString());
            cmd.Parameters.AddWithValue("Description", string.IsNullOrWhiteSpace(Description) ? DBNull.Value : Description);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            BusyPeriodID = (int)pId.Value;
        }

        public static async Task Delete(int busyPeriodId)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.BusyPeriod_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("BusyPeriodID", busyPeriodId);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>A period from the editor's list (the second result of dbo.BusyPeriods_Get)</summary>
        public static BusyPeriod FromRow(DataRow row, int instanceId) => new()
        {
            BusyPeriodID = row.Field<int>("BusyPeriodID"),
            InstanceID = row.Field<int>("ApplyToInstanceID") > 0 ? row.Field<int>("ApplyToInstanceID") : instanceId,
            ApplyTo = row.Field<int>("ApplyToInstanceID") > 0 ? BusyPeriodScope.ThisInstance
                : row.Field<int>("ApplyToTagID") > 0 ? BusyPeriodScope.Tag
                : BusyPeriodScope.AllInstances,
            ApplyToTag = row.Field<int>("ApplyToTagID") > 0
                ? new DBADashTag { TagID = row.Field<int>("ApplyToTagID"), TagName = row["TagName"] as string, TagValue = row["TagValue"] as string }
                : DBADashTag.AllInstancesTag(),
            Monday = row.Field<bool>("Monday"),
            Tuesday = row.Field<bool>("Tuesday"),
            Wednesday = row.Field<bool>("Wednesday"),
            Thursday = row.Field<bool>("Thursday"),
            Friday = row.Field<bool>("Friday"),
            Saturday = row.Field<bool>("Saturday"),
            Sunday = row.Field<bool>("Sunday"),
            TimeFrom = TimeOnly.FromTimeSpan(row.Field<TimeSpan>("TimeFrom")),
            TimeTo = TimeOnly.FromTimeSpan(row.Field<TimeSpan>("TimeTo")),
            TimeZone = TimeZoneFromString(row.Field<string>("TimeZone")),
            Description = row["Description"] as string
        };

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TimeFrom == null || TimeTo == null) yield return new ValidationResult("Time From and Time To are required");
            else if (TimeFrom == TimeTo) yield return new ValidationResult("Time To must be different from Time From.  An earlier Time To crosses midnight, e.g. 22:00 to 06:00.");
            if (!(Monday || Tuesday || Wednesday || Thursday || Friday || Saturday || Sunday)) yield return new ValidationResult("Pick at least one day");
            if (ApplyTo == BusyPeriodScope.Tag && ApplyToTag is not { TagID: > 0 }) yield return new ValidationResult("Pick a tag for the period to apply to");
        }
    }

    /// <summary>Reads busy periods (dbo.BusyPeriods_Get) and copies them between instances</summary>
    internal static class BusyPeriodsData
    {
        /// <summary>The busy intervals that apply to the instance over the range, in UTC, and the periods for the editor to list</summary>
        public static (List<(DateTime Start, DateTime End)> IntervalsUtc, DataTable Periods) Get(int instanceId, DateTime fromUtc, DateTime toUtc)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.BusyPeriods_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.Parameters.AddWithValue("FromDate", fromUtc);
            cmd.Parameters.AddWithValue("ToDate", toUtc);
            using var da = new SqlDataAdapter(cmd);
            var ds = new DataSet();
            da.Fill(ds);
            var intervals = ds.Tables[0].Rows.Cast<DataRow>()
                .Select(r => (DateTime.SpecifyKind(r.Field<DateTime>("StartUtc"), DateTimeKind.Utc), DateTime.SpecifyKind(r.Field<DateTime>("EndUtc"), DateTimeKind.Utc)))
                .ToList();
            return (intervals, ds.Tables[1]);
        }

        /// <summary>Replace <paramref name="toInstanceId"/>'s own busy periods with copies of <paramref name="fromInstanceId"/>'s</summary>
        public static async Task Copy(int fromInstanceId, int toInstanceId)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.BusyPeriods_Copy", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("FromInstanceID", fromInstanceId);
            cmd.Parameters.AddWithValue("ToInstanceID", toInstanceId);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
