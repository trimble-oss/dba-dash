using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    public class Alert
    {
        public enum Priorities
        {
            Critical = 0,
            High1 = 1,
            High2 = 2,
            High3 = 3,
            High4 = 4,
            High5 = 5,
            High6 = 6,
            High7 = 7,
            High8 = 8,
            High9 = 9,
            High10 = 10,
            Medium1 = 11,
            Medium2 = 12,
            Medium3 = 13,
            Medium4 = 14,
            Medium5 = 15,
            Medium6 = 16,
            Medium7 = 17,
            Medium8 = 18,
            Medium9 = 19,
            Medium10 = 20,
            Low1 = 21,
            Low2 = 22,
            Low3 = 23,
            Low4 = 24,
            Low5 = 25,
            Low6 = 26,
            Low7 = 27,
            Low8 = 28,
            Low9 = 29,
            Low10 = 30,
            Information1 = 31,
            Information2 = 32,
            Information3 = 33,
            Information4 = 34,
            Information5 = 35,
            Information6 = 36,
            Information7 = 37,
            Information8 = 38,
            Information9 = 39,
            Information10 = 40,
            Success = 41
        }

        public long AlertID { get; set; }
        public string ConnectionID { get; set; }

        public Priorities Priority { get; set; }

        public string Message { get; set; }

        public string AlertName { get; set; }

        public DateTime? ResolvedDate { get; set; }

        public bool IsResolved { get; set; }

        public string Status => IsResolved ? "Resolved" : "Active";

        public string Action => IsResolved ? "resolved" : "triggered";

        public DateTime TriggerDate { get; set; }

        public string CustomThreadKey { get; set; }

        public string ThreadKey => string.IsNullOrEmpty(CustomThreadKey) ? DefaultThreadKey : CustomThreadKey;

        public string DefaultThreadKey => $"DBADash_{ConnectionID}_{AlertName}_{TriggerDate.Ticks}";

        public string GetEmoji()
        {
            if (IsResolved)
            {
                return "✅";
            }

            return (short)Priority switch
            {
                41 => "✅",
                < 11 => "‼️",
                < 21 => "⚠️",
                < 31 => "🟡",
                _ => "ℹ️"
            };
        }

        public string GetIcon()
        {
            if (IsResolved)
            {
                return "DBADash_Success.png";
            }

            return (short)Priority switch
            {
                41 => "DBADash_Success.png",
                < 11 => "DBADash_Critical.png",
                < 21 => "DBADash_Warning.png",
                < 31 => "DBADash_WarningLow.png",
                _ => "DBADash_Neutral.png"
            };
        }

        public string GetIconUrl() => "https://dbadash.com/" + GetIcon();

        public static Alert GetTestAlert()
        {
            return new Alert()
            {
                AlertID = -1,
                AlertName = "DBADASH_TEST_ALERT",
                Priority = Priorities.Information1,
                TriggerDate = DateTime.Now,
                Message = "Test Alert",
                ConnectionID = Environment.MachineName
            };
        }

        public async Task SetCustomThreadKey(int channelID, string threadKey, string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("Alert.CustomThreadKey_Add", cn)
            { CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync();
            cmd.Parameters.AddWithValue("@NotificationChannelID", channelID);
            cmd.Parameters.AddWithValue("AlertID", AlertID);
            cmd.Parameters.AddWithValue("@ThreadKey", threadKey);
            await cmd.ExecuteNonQueryAsync();
            CustomThreadKey = threadKey;
        }
    }
}