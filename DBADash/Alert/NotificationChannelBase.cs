using DBADash;
using DBADash.Alert;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Alert = DBADash.Alert.Alert;

namespace DBADashGUI.DBADashAlerts
{
    public abstract class NotificationChannelBase : IValidatableObject
    {
        private static readonly Guid ClassGuid = new("B5AA3F37-6A53-4852-9364-DF117CC0579A");

        [JsonIgnore]
        [Category("DBA Dash Notification Channel")]
        public abstract NotificationChannelTypes NotificationChannelType { get; }

        public enum NotificationChannelTypes
        {
            Webhook = 1,
            Email = 2,
            Slack = 3
        }

        [JsonIgnore]
        [Browsable(false)]
        public int? ChannelID { get; set; }

        [JsonIgnore]
        [Description("Name of the notification channel for identification purposes in DBA Dash")]
        [DisplayName("Channel Name (Required)")]
        [Category("DBA Dash Notification Channel")]
        public string ChannelName { get; set; }

        [JsonIgnore]
        [DisplayName("Disable From")]
        [Category("Enable/Disable")]
        [Description("Option to disable the notification channel from the specified date.")]
        public DateTime? DisableFrom { get; set; }

        [JsonIgnore]
        [DisplayName("Disable To")]
        [Category("Enable/Disable")]
        [Description("Option to disable the notification channel until specified date.")]
        public DateTime? DisableTo { get; set; }

        [Category("DBA Dash Notification Channel")]
        [JsonIgnore] public List<NotificationChannelSchedule> Schedules { get; set; }

        protected NotificationChannelBase()
        {
            Schedules = new() { new NotificationChannelSchedule() };
        }

        public async Task SendNotificationAsync(Alert alert, string connectionString)
        {
            Log.Information("Sending alert notification to {ChannelName}", ChannelName);
            var errorMessage = string.Empty;
            try
            {
                await InternalSendNotificationAsync(alert, connectionString);
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
            }
            if (string.IsNullOrEmpty(connectionString)) return; // Might not be set for testing
            await UpdateLastNotified(alert, connectionString, errorMessage);
        }

        protected async Task UpdateLastNotified(Alert alert, string connectionString, string errorMessage)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("Alert.AlertNotification_Upd", cn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@AlertID", alert.AlertID);
            cmd.Parameters.AddWithValue("@NotificationChannelID", ChannelID);
            cmd.Parameters.AddWithValue("@NotificationMessage", alert.Message);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                cmd.Parameters.AddWithValue("@ErrorMessage", errorMessage);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        protected abstract Task InternalSendNotificationAsync(Alert alert, string connectionString);

        public void Save(string connectionString)
        {
            Validator.ValidateObject(this, new ValidationContext(this), true);
            if (ChannelID == null)
            {
                AddChannel(connectionString);
            }
            else
            {
                UpdateChannel(connectionString);
            }

            SaveSchedules(connectionString);
        }

        public string GetChannelDetails() => JsonConvert.SerializeObject(this).EncryptString(ClassGuid + ChannelName);

        private void SaveSchedules(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannelSchedule_Upd", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@NotificationChannelID", ChannelID);
            cmd.Parameters.AddWithValue("@Schedules", GetSchedulesDataTable());
            cmd.ExecuteNonQuery();
        }

        private DataTable GetSchedulesDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("AlertNotificationLevel", typeof(short));
            dt.Columns.Add("RetriggerThresholdMins", typeof(int));
            dt.Columns.Add("ApplyToTagID", typeof(int));
            dt.Columns.Add("TimeFrom", typeof(TimeSpan));
            dt.Columns.Add("TimeTo", typeof(TimeSpan));
            dt.Columns.Add("Monday", typeof(bool));
            dt.Columns.Add("Tuesday", typeof(bool));
            dt.Columns.Add("Wednesday", typeof(bool));
            dt.Columns.Add("Thursday", typeof(bool));
            dt.Columns.Add("Friday", typeof(bool));
            dt.Columns.Add("Saturday", typeof(bool));
            dt.Columns.Add("Sunday", typeof(bool));
            dt.Columns.Add("TimeZone", typeof(string));
            foreach (var s in Schedules)
            {
                var r = dt.NewRow();
                r["AlertNotificationLevel"] = (int)s.AlertNotificationLevel;
                r["RetriggerThresholdMins"] = s.RetriggerThresholdMins;
                r["ApplyToTagID"] = s.ApplyToTag?.TagID ?? -1;
                r["TimeFrom"] = s.TimeFrom == null ? DBNull.Value : s.TimeFrom.Value.ToTimeSpan();
                r["TimeTo"] = s.TimeTo == null ? DBNull.Value : s.TimeTo.Value.ToTimeSpan();
                r["Monday"] = s.Monday;
                r["Tuesday"] = s.Tuesday;
                r["Wednesday"] = s.Wednesday;
                r["Thursday"] = s.Thursday;
                r["Friday"] = s.Friday;
                r["Saturday"] = s.Saturday;
                r["Sunday"] = s.Sunday;
                r["TimeZone"] = s.TimeZoneAsString();
                dt.Rows.Add(r);
            }

            return dt;
        }

        public void Delete(string connectionString)
        {
            if (ChannelID == null)
            {
                throw new ArgumentException("Channel ID is NULL");
            }
            Delete(connectionString, (int)ChannelID);
        }

        public static void Delete(string connectionString, int channelId)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannel_Del", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@NotificationChannelID", channelId);
            cmd.ExecuteNonQuery();
        }

        private void UpdateChannel(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannel_Upd", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@NotificationChannelID", ChannelID);
            cmd.Parameters.AddWithValue("@ChannelName", ChannelName);
            cmd.Parameters.AddWithValue("@DisableFrom", DisableFrom);
            cmd.Parameters.AddWithValue("@DisableTo", DisableTo);
            cmd.Parameters.AddWithValue("@NotificationChannelTypeID", NotificationChannelType);
            cmd.Parameters.AddWithValue("@ChannelDetails", GetChannelDetails());
            cmd.ExecuteNonQuery();
        }

        private void AddChannel(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannel_Add", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@ChannelName", ChannelName);
            cmd.Parameters.AddWithValue("@DisableFrom", DisableFrom);
            cmd.Parameters.AddWithValue("@DisableTo", DisableTo);
            cmd.Parameters.AddWithValue("@ChannelDetails", GetChannelDetails());
            cmd.Parameters.AddWithValue("@NotificationChannelTypeID", NotificationChannelType);
            var pChannelID = new SqlParameter("@NotificationChannelID", SqlDbType.Int)
            { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(pChannelID);
            cmd.ExecuteNonQuery();
            ChannelID = (int)pChannelID.Value;
        }

        private const string DataMaskedString = "xxxx";

        public static NotificationChannelBase GetChannel(int channelId, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannel_Get", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@NotificationChannelID", channelId);
            var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                var channelType = (NotificationChannelTypes)Convert.ToInt32(rdr["NotificationChannelTypeID"]);
                var channelName = (string)rdr["ChannelName"];
                var channelDetails = (string)rdr["ChannelDetails"];
                if (channelDetails == DataMaskedString)
                {
                    throw new Exception("Insufficient permission to access notification channel details");
                }
                try
                {
                    channelDetails = channelDetails.DecryptString(ClassGuid + channelName);
                }
                catch
                {
                    //Ignore. Might be plain text
                }
                NotificationChannelBase channel = channelType switch
                {
                    NotificationChannelTypes.Webhook => JsonConvert
                        .DeserializeObject<WebhookNotificationChannel>(channelDetails),
                    NotificationChannelTypes.Email => JsonConvert.DeserializeObject<EmailNotificationChannel>(channelDetails),
                    NotificationChannelTypes.Slack => JsonConvert.DeserializeObject<SlackNotificationChannel>(channelDetails),
                    _ => throw new NotImplementedException($"Channel type {channelType} hasn't been implemented.")
                };

                channel.ChannelID = (int)rdr["NotificationChannelID"];
                channel.ChannelName = (string)rdr["ChannelName"];
                channel.DisableFrom = rdr["DisableFrom"] == DBNull.Value
                    ? null
                    : (DateTime)rdr["DisableFrom"];
                channel.DisableTo = rdr["DisableTo"] == DBNull.Value ? null : (DateTime)rdr["DisableTo"];
                channel.Schedules = GetSchedules(channelId, connectionString);
                return channel;
            }
            else
            {
                throw new ArgumentException($"Notification Channel {channelId} not found");
            }
        }

        public static NotificationChannelBase CreateChannel(NotificationChannelTypes type)
        {
            return type switch
            {
                NotificationChannelTypes.Webhook => new WebhookNotificationChannel(),
                NotificationChannelTypes.Email => new EmailNotificationChannel(),
                NotificationChannelTypes.Slack => new SlackNotificationChannel(),
                _ => throw new NotImplementedException($"Channel type {type} hasn't been implemented.")
            };
        }

        public static NotificationChannelBase GetChannelWithCaching(int notificationChannelId, string connectionString, int expirationSeconds = 600)
        {
            var cacheKey = $"NotificationChannel_{notificationChannelId}" + EncryptText.GetShortHash(connectionString);
            ObjectCache cache = MemoryCache.Default;

            // Try to get the channel from the cache
            if (cache[cacheKey] is NotificationChannelBase cachedChannel)
            {
                return cachedChannel;
            }
            else
            {
                // If the channel is not in the cache, fetch it from the database & add it to the cache with specified expiration (default 10mins)
                var channel = GetChannel(notificationChannelId, connectionString);

                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(expirationSeconds)
                };
                cache.Set(cacheKey, channel, policy);

                return channel;
            }
        }

        public static List<NotificationChannelSchedule> GetSchedules(int channelId, string connectionString)
        {
            var schedules = new List<NotificationChannelSchedule>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannelSchedule_Get", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@NotificationChannelID", channelId);
            var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var schedule = new NotificationChannelSchedule
                {
                    ApplyToTag = new DBADashTag()
                    {
                        TagID = (int)rdr["ApplyToTagID"],
                        TagName = rdr["TagName"] == DBNull.Value ? string.Empty : (string)rdr["TagName"],
                        TagValue = rdr["TagValue"] == DBNull.Value ? string.Empty : (string)rdr["TagValue"]
                    },
                    AlertNotificationLevel = (Alert.Priorities)Convert.ToInt16(rdr["AlertNotificationLevel"]),
                    Monday = (bool)rdr["Monday"],
                    Tuesday = (bool)rdr["Tuesday"],
                    Wednesday = (bool)rdr["Wednesday"],
                    Thursday = (bool)rdr["Thursday"],
                    Friday = (bool)rdr["Friday"],
                    Saturday = (bool)rdr["Saturday"],
                    Sunday = (bool)rdr["Sunday"],
                    TimeFrom = rdr["TimeFrom"] == DBNull.Value ? null : TimeOnly.FromTimeSpan((TimeSpan)rdr["TimeFrom"]),
                    TimeTo = rdr["TimeTo"] == DBNull.Value ? null : TimeOnly.FromTimeSpan((TimeSpan)rdr["TimeTo"]),
                    RetriggerThresholdMins = (int)rdr["RetriggerThresholdMins"],
                    TimeZone = ScheduleBase.TimeZoneFromString(((string)rdr["TimeZone"]))
                };
                schedules.Add(schedule);
            }

            return schedules;
        }

        public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);

        protected virtual IEnumerable<ValidationResult> ValidateBase(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(ChannelName))
            {
                yield return new ValidationResult("Channel Name is required");
            }

            foreach (var result in Schedules.SelectMany(schedule => schedule.ValidateSchedule(validationContext)))
            {
                yield return result;
            }
        }
    }
}