using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADash;
using DBADash.Alert;
using DBADashGUI.DBADashAlerts;
using MailKit.Search;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DBADashService
{
    internal class AlertProcessing
    {

        public int NotificationProcessingFrequencySeconds { get; set; } = CollectionConfig.DefaultAlertProcessingFrequencySeconds;
        private readonly DBADashConnection Connection;
        private string ConnectionString => Connection.ConnectionString;
        private int? _agentId;

        public int NotificationProcessingStartupDelaySeconds { get; set; } = CollectionConfig.DefaultAlertProcessingStartupDelaySeconds;

        private int DBADashAgentID
        {
            get
            {
                if (_agentId.HasValue) return _agentId.Value;
                try
                {
                    _agentId= DBADashAgent.GetCurrent().GetDBADashAgentID(ConnectionString);
                    return _agentId.Value;
                }
                catch (Exception ex)
                {
                    Log.Error(ex,"Error getting DBA Dash Agent ID");
                    throw;
                }
            }
        }

        public AlertProcessing(DBADashConnection _connection)
        {
            Connection = _connection;
        }

        public async Task<bool> AcquireLock()
        {
            await using var cn = new SqlConnection(ConnectionString);
            var cmd = new SqlCommand("Alert.CurrentAgent_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("DBADashAgentID", DBADashAgentID);
            await cn.OpenAsync();
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task ProcessAlerts()
        {
            await Task.Delay(NotificationProcessingStartupDelaySeconds * 1000);
            Log.Information("Alert processing started");
            while (true)
            {
                try
                {
                    if (await AcquireLock()) // Ensure only a single instance of DBA Dash service is processing alerts
                    {
                        await AlertsUpdate();
                        await ProcessNotifications();
                    }
                    else
                    {
                        Log.Debug("Lock for alert processing not acquired");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error processing alerts");
                }
                await Task.Delay(NotificationProcessingFrequencySeconds * 1000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void LogError(Exception exception,string message)
        {
            try
            {
                Log.Error(exception,message);
                var dtErrors = DBCollector.GetErrorDataTableSchema();
                DBCollector.AddErrorRow(ref dtErrors,"Alert",exception.ToString(),"AlertProcessing");
                DBImporter.InsertErrors(ConnectionString,null,DateTime.UtcNow, dtErrors,default);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running logging alert error to the database");
            }
        }

        public async Task ProcessNotifications()
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("Alert.Notifications_Get", cn) { CommandType = CommandType.StoredProcedure };
            var rdr= await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var alert = new Alert
                {
                    AlertID = (long)rdr["AlertID"],
                    ConnectionID = (string)rdr["ConnectionID"],
                    Priority = (Alert.Priorities)Convert.ToInt16(rdr["Priority"]),
                    Message = (string)rdr["AlertDetails"],
                    AlertName = (string)rdr["AlertKey"],
                    ResolvedDate = rdr["ResolvedDate"] == DBNull.Value ? null : (DateTime?)rdr["ResolvedDate"],
                    IsResolved = (bool)rdr["IsResolved"],
                    TriggerDate = (DateTime)rdr["TriggerDate"],
                    CustomThreadKey = rdr["CustomThreadKey"] == DBNull.Value ? null : (string)rdr["CustomThreadKey"]
                };
                var notificationChannelId = (int)rdr["NotificationChannelID"];
                var channel = NotificationChannelBase.GetChannelWithCaching(notificationChannelId, ConnectionString);
                var notificationCount = (int)rdr["NotificationCount"];
                var maxNotifications = (int)rdr["AlertMaxNotificationCount"];
                notificationCount++;

                if (notificationCount >= maxNotifications)
                {
                    alert.Message += $"\n\nAlert has reached the maximum notification count {notificationCount}.  Further notifications are supressed until the alert is closed.";
                }
                else
                {
                    alert.Message +=$"\n\nAlert has been sent {notificationCount} times of a maximum of {maxNotifications}";
                }

                try
                {
                    await channel.SendNotificationAsync(alert, ConnectionString);
                }
                catch (Exception ex)
                {
                    LogError(ex,$"Error sending notification to channel {channel.ChannelName}");
                }
            }
        }

        public async Task AlertsUpdate()
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("Alert.Alerts_UPD",cn) {CommandType = CommandType.StoredProcedure };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
