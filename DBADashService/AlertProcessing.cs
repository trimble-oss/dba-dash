using DBADash;
using DBADashGUI.DBADashAlerts;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alert = DBADash.Alert.Alert;

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
                    _agentId = DBADashAgent.GetCurrent().GetDBADashAgentID(ConnectionString);
                    return _agentId.Value;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting DBA Dash Agent ID");
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
                    await LogErrorAsync(ex, "Error processing alerts");
                }
                await Task.Delay(NotificationProcessingFrequencySeconds * 1000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private async Task LogErrorAsync(Exception exception, string message)
        {
            try
            {
                Log.Error(exception, message);
                var dtErrors = DBCollector.GetErrorDataTableSchema();
                DBCollector.AddErrorRow(ref dtErrors, "Alert", exception.ToString(), "AlertProcessing");
                await DBImporter.InsertErrorsAsync(ConnectionString, null, DateTime.UtcNow, dtErrors, default);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running logging alert error to the database");
            }
        }

        private async Task<Dictionary<int, List<Alert>>> GetGroupedAlerts()
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("Alert.Notifications_Get", cn) { CommandType = CommandType.StoredProcedure };
            var rdr = await cmd.ExecuteReaderAsync();
            var groupedAlerts = new Dictionary<int, List<Alert>>();

            while (await rdr.ReadAsync())
            {
                var alert = new Alert
                {
                    AlertID = (long)rdr["AlertID"],
                    ConnectionID = (string)rdr["ConnectionID"],
                    InstanceDisplayName = (string)rdr["InstanceDisplayName"],
                    Priority = (Alert.Priorities)Convert.ToInt16(rdr["Priority"]),
                    Message = (string)rdr["AlertDetails"],
                    AlertName = (string)rdr["AlertKey"],
                    ResolvedDate = rdr["ResolvedDate"] == DBNull.Value ? null : (DateTime?)rdr["ResolvedDate"],
                    IsResolved = (bool)rdr["IsResolved"],
                    TriggerDate = (DateTime)rdr["TriggerDate"],
                    CustomThreadKey = rdr["CustomThreadKey"] == DBNull.Value ? null : (string)rdr["CustomThreadKey"],
                    NotificationCount = (int)rdr["NotificationCount"],
                    MaxNotifications = (int)rdr["AlertMaxNotificationCount"],
                    AlertType = (string)rdr["AlertType"],
                    IsAcknowledged = (bool)rdr["IsAcknowledged"]
                };

                var notificationChannelId = (int)rdr["NotificationChannelID"];

                if (!groupedAlerts.ContainsKey(notificationChannelId))
                {
                    groupedAlerts.Add(notificationChannelId, (new List<Alert> { alert }));
                }
                else
                {
                    groupedAlerts[notificationChannelId].Add(alert);
                }
            }

            return groupedAlerts;
        }

        public async Task ProcessNotifications()
        {
            var groupedAlerts = await GetGroupedAlerts();

            foreach (var (notificationChannelId, alerts) in groupedAlerts)
            {
                var channel = NotificationChannelBase.GetChannelWithCaching(notificationChannelId, ConnectionString);
                if (alerts.Count >= (channel.AlertConsolidationThreshold ?? NotificationChannelBase.DefaultAlertConsolidationThreshold))
                {
                    await SendConsolidatedAlertNotification(channel, alerts);
                }
                else
                {
                    await SendIndividualAlertNotification(channel, alerts);
                }
            }
        }

        private async Task SendIndividualAlertNotification(NotificationChannelBase channel, List<Alert> alerts)
        {
            foreach (var alert in alerts)
            {
                var notificationCount = alert.NotificationCount + 1; // Notification count after this notification is sent
                if (channel.IncludeNotificationCountInMessage)
                {
                    if (notificationCount >= alert.MaxNotifications)
                    {
                        alert.Message +=
                            $"\n\nAlert has reached the maximum notification count {notificationCount}.  Further notifications are supressed until the alert is closed.";
                    }
                    else
                    {
                        alert.Message +=
                            $"\n\nAlert has been sent {notificationCount} times of a maximum of {alert.MaxNotifications}";
                    }
                }

                try
                {
                    await channel.SendNotificationAsync(alert, ConnectionString);
                }
                catch (Exception ex)
                {
                    await LogErrorAsync(ex, $"Error sending notification to channel {channel.ChannelName}");
                }
            }
        }

        private async Task SendConsolidatedAlertNotification(NotificationChannelBase channel, List<Alert> alerts)
        {
            var distinctConnectionCount = alerts.Select(a => a.ConnectionID).Distinct().Count();
            var consolidatedAlert = new Alert()
            {
                AlertName = $"{alerts.Count} alert notifications for your attention",
                Priority = alerts.Min(a => a.Priority),
                IsResolved = alerts.All(a => a.IsResolved),
                InstanceDisplayName = distinctConnectionCount == 1 ? alerts.First().InstanceDisplayName : distinctConnectionCount + " Instances",
                ConnectionID = distinctConnectionCount == 1 ? alerts.First().ConnectionID : distinctConnectionCount + " Instances",
                TriggerDate = DateTime.UtcNow
            };
            var sb = new StringBuilder();
            foreach (var group in alerts.GroupBy(a => a.InstanceDisplayName).OrderBy(a => a.Key))
            {
                sb.AppendLine(group.Key);
                foreach (var alert in group.OrderBy(a => a.Priority))
                {
                    sb.AppendLine(alert.GetEmoji() + "[" + alert.Status + "] " + alert.AlertName + "\n" + alert.Message + "\n");
                }

                sb.AppendLine();
            }

            consolidatedAlert.Message = sb.ToString();

            var errorMessage = string.Empty;
            try
            {
                await channel.SendNotificationAsync(consolidatedAlert, ConnectionString);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            foreach (var alert in alerts)
            {
                await channel.UpdateLastNotified(alert, ConnectionString, errorMessage);
            }
        }

        public async Task AlertsUpdate()
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("Alert.Alerts_UPD", cn) { CommandType = CommandType.StoredProcedure };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}