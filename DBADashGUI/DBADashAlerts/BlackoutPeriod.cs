using DBADash.Alert;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DBADashGUI.DBADashAlerts
{
    public class BlackoutPeriod : ScheduleBase, IValidatableObject
    {
        [Browsable(false)]
        public int? BlackoutPeriodID { get; set; }

        [Category("Filters")]
        [Browsable(false)]
        public int? ApplyToInstanceID { get; set; }

        private string _applyToInstance;

        [JsonIgnore]
        [Category("Filters"), DisplayName("Apply To (Instance)")]
        [Description("Option to apply blackout period to a specific instance.  Can't be used in combination with Apply To Tag.")]
        public string ApplyToInstance
        {
            get => _applyToInstance;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ApplyToInstanceID = CommonData.Instances.Rows.Cast<DataRow>().Where(r => string.Equals(r.Field<string>("ConnectionID"), value, StringComparison.InvariantCultureIgnoreCase)).Select(r => r.Field<int?>("InstanceID")).FirstOrDefault() ??
                                        CommonData.Instances.Rows.Cast<DataRow>().Where(r => string.Equals(r.Field<string>("InstanceDisplayName"), value, StringComparison.InvariantCultureIgnoreCase)).Select(r => r.Field<int?>("InstanceID")).FirstOrDefault();

                    if (ApplyToInstanceID == null)
                    {
                        throw new ArgumentException("InstanceID not found", nameof(ApplyToInstance));
                    }

                    ApplyToTag = DBADashTag.AllInstancesTag();
                }
                else
                {
                    ApplyToInstanceID = null;
                }

                _applyToInstance = value;
            }
        }

        [Category("Filters"), DisplayName("Apply To (Tag)")]
        public DBADashTag ApplyToTag { get; set; } = DBADashTag.AllInstancesTag();

        [Category("Filters")]
        [DisplayName("Alert Key")]
        [Description("Enter an value to filter blackout period by alert key (LIKE syntax supported). Set to % to apply to all alerts.")]
        public string AlertKey { get; set; } = "%";

        [Category("Effective Period")]
        [DisplayName("1. Start Date"),Description("Option to start blackout period at a future date/time.  To start now, set to current date or leave blank.")]
        public DateTime? StartDate { get; set; }

        [Category("Effective Period")]
        [DisplayName("2. End Date"), Description("Date/Time the blackout period should end.  For recurring blackouts, leave blank or set to a date in the distant future.")]
        public DateTime? EndDate { get; set; }

        [Description("Add notes. Markdown is supported")]
        [Editor(typeof(MarkdownEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Notes { get; set; }

        public void AdjustDatesToUtc()
        {
            StartDate = StartDate?.AppTimeZoneToUtc();
            EndDate = EndDate?.AppTimeZoneToUtc();
        }

        public void AdjustDatesToAppTimeZone()
        {
            StartDate = StartDate?.ToAppTimeZone();
            EndDate = EndDate?.ToAppTimeZone();
        }

        public async Task Save()
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("Alert.BlackoutPeriod_Add", cn)
            { CommandType = CommandType.StoredProcedure };
            if (ApplyToInstanceID > 0)
            {
                ApplyToTag = DBADashTag.AllInstancesTag();
            }
            await cn.OpenAsync();
            var pBlackoutPeriodID = new SqlParameter("BlackoutPeriodID", SqlDbType.Int)
            { Direction = ParameterDirection.InputOutput, Value = BlackoutPeriodID.HasValue ? BlackoutPeriodID : DBNull.Value };
            cmd.Parameters.Add(pBlackoutPeriodID);
            cmd.Parameters.AddWithValue("ApplyToInstanceID", ApplyToInstanceID);
            cmd.Parameters.AddWithValue("ApplyToTagID", ApplyToTag.TagID);
            cmd.Parameters.AddWithValue("AlertKey", string.IsNullOrEmpty(AlertKey) ? "%" : AlertKey);
            cmd.Parameters.AddWithValue("StartDate", StartDate.HasValue ? StartDate : DBNull.Value);
            cmd.Parameters.AddWithValue("EndDate", EndDate.HasValue ? EndDate : DBNull.Value);
            cmd.Parameters.AddWithValue("Monday", Monday);
            cmd.Parameters.AddWithValue("Tuesday", Tuesday);
            cmd.Parameters.AddWithValue("Wednesday", Wednesday);
            cmd.Parameters.AddWithValue("Thursday", Thursday);
            cmd.Parameters.AddWithValue("Friday", Friday);
            cmd.Parameters.AddWithValue("Saturday", Saturday);
            cmd.Parameters.AddWithValue("Sunday", Sunday);
            cmd.Parameters.AddWithValue("TimeFrom", TimeFrom);
            cmd.Parameters.AddWithValue("TimeTo", TimeTo);
            cmd.Parameters.AddWithValue("TimeZone", TimeZoneAsString());
            cmd.Parameters.AddWithValue("Notes", Notes);

            await cmd.ExecuteNonQueryAsync();
            BlackoutPeriodID = (int)pBlackoutPeriodID.Value;
        }

        public static async Task Delete(int blackoutPeriodId)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("Alert.BlackoutPeriod_Del", cn)
            { CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync();
            cmd.Parameters.AddWithValue("BlackoutPeriodID", blackoutPeriodId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Delete()
        {
            if (!BlackoutPeriodID.HasValue) return;
            await Delete(BlackoutPeriodID.Value);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult("End Date must be greater than Start Date");
            }
            foreach (var result in ValidateSchedule(validationContext))
            {
                yield return result;
            }
        }
    }
}