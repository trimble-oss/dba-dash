using DBADash.Messaging;
using DBADashGUI.Messaging;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DBADashGUI
{
    /// <summary>
    /// Audit log of changes users make from the GUI (dbo.UserActionLog) - who did what, to which instance, and the outcome.
    /// Shown by the User Actions report.  Action specific details are stored as JSON.
    /// </summary>
    internal static class UserActionLog
    {
        public const string ActionSchedule = "Schedule";
        public const string ActionTemporarySchedule = "Temporary Schedule";
        public const string ActionEndTemporarySchedule = "End Temporary Schedule";
        public const string ActionPause = "Pause Collections";
        public const string ActionResume = "Resume Collections";

        public const string StatusSucceeded = "Succeeded";

        private static readonly JsonSerializerSettings DetailsSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        /// <summary>
        /// Sends a message to a service and logs it: the request is recorded before it's sent, so nothing is sent if it
        /// can't be logged, then updated with the outcome when the service replies.  The log and the service's own
        /// logging share the message group ID.
        /// </summary>
        /// <param name="instanceIDs">Instances the action applies to - empty for a change to the service itself</param>
        /// <param name="details">Serialized to JSON - e.g. an anonymous object with what was changed</param>
        public static async Task<MessagingHelper.MessageOutcome> SendLoggedAsync(MessageBase message, int importAgentID,
            int collectAgentID, IEnumerable<int> instanceIDs, string actionType, object details, MessagingHelper.SetStatusDelegate setStatus)
        {
            var messageGroup = Guid.NewGuid();
            await AddAsync(actionType, instanceIDs, details, collectAgentID: collectAgentID, messageGroup: messageGroup);
            var outcome = await MessagingHelper.SendAndWaitAsync(message, importAgentID, setStatus, messageGroup);
            var warnings = outcome.Warnings;
            var status = !outcome.Succeeded ? "Failed: " + outcome.Error
                : warnings.Count > 0 ? "Succeeded with warnings: " + string.Join("  ", warnings)
                : StatusSucceeded;
            await UpdateAsync(messageGroup, status);
            return outcome;
        }

        /// <summary>Details logged for a schedule change - each collection changed, and when a temporary change ends.</summary>
        public static object ScheduleChangeDetails(CollectionScheduleUpdateMessage message) => new
        {
            Scope = message.Scope.ToString(),
            Changes = message.Changes.Select(c => new
            {
                Collection = c.Key.ToString(),
                Schedule = c.Value?.Schedule ?? (message.Temporary ? "End" : "Inherit"),
                c.Value?.RunOnServiceStart
            }),
            message.TemporaryUntilUtc
        };

        /// <summary>Logs an action made directly in the repository (no message to a service), with its outcome.</summary>
        public static Task LogAsync(string actionType, IEnumerable<int> instanceIDs, object details, string status = StatusSucceeded,
            string databaseName = null) =>
            AddAsync(actionType, instanceIDs, details, status: status, databaseName: databaseName);

        private static async Task AddAsync(string actionType, IEnumerable<int> instanceIDs, object details, int? collectAgentID = null,
            Guid? messageGroup = null, string status = null, string databaseName = null)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.UserActionLog_Add", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@user_name", Environment.UserName);
            cmd.Parameters.AddWithValue("@action_type", actionType);
            cmd.Parameters.AddWithValue("@InstanceIDs", (instanceIDs ?? Enumerable.Empty<int>()).Distinct().AsDataTable());
            cmd.Parameters.AddWithValue("@CollectAgentID", (object)collectAgentID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@database_name", (object)databaseName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@details", details == null ? DBNull.Value : JsonConvert.SerializeObject(details, DetailsSerializerSettings));
            cmd.Parameters.AddWithValue("@status", (object)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MessageGroupID", (object)messageGroup ?? DBNull.Value);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Update the log with the outcome of a request sent to a service.</summary>
        private static async Task UpdateAsync(Guid messageGroup, string status)
        {
            try
            {
                await using var cn = new SqlConnection(Common.ConnectionString);
                await using var cmd = new SqlCommand("dbo.UserActionLog_Upd", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
                cmd.Parameters.AddWithValue("@Status", status);
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // The action has already happened - a failure to record the outcome shouldn't surface as an error.
                Serilog.Log.Warning(ex, "Failed to update UserActionLog for message group {messageGroup}", messageGroup);
            }
        }
    }
}
