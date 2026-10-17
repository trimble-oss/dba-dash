using DBADash;
using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADash.Messaging.CollectionScheduleUpdateMessage;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// Opens the full schedule dialog (<see cref="ScheduleConfig"/>, shared with the service config tool) for an instance or
    /// for the service that collects it, using the schedule as the service holds it.  Unlike Collection Dates this lists
    /// every collection, including those that are disabled and so have never run.  Instance changes can be applied
    /// temporarily - e.g. to switch a collection on while investigating an issue - and revert by themselves.  For the
    /// service schedule it shows which of the service's instances a change would affect.
    /// </summary>
    internal static class ConfigureSchedule
    {
        private static readonly TimeSpan DefaultTemporaryDuration = TimeSpan.FromHours(1);

        /// <summary>An instance, and the service that collects it, to get or change the schedule for.</summary>
        private sealed record ScheduleTarget(int InstanceID, string ConnectionID, string InstanceName, int CollectAgentID, int ImportAgentID,
            DBADashAgent CollectAgent, DBADashAgent ImportAgent)
        {
            public string ServiceName => $"{CollectAgent.AgentHostName}\\{CollectAgent.AgentServiceName}";
        }

        /// <summary>The instance to work with, or null (with the reason shown) if the user or its service doesn't allow schedule changes.</summary>
        private static ScheduleTarget GetTarget(IWin32Window owner, int instanceID, string caption)
        {
            if (!DBADashUser.AllowScheduleChanges)
            {
                MessageBox.Show(owner, "You don't have permission to change collection schedules.  Membership of the AllowScheduleChanges role is required.",
                    caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var instance = PauseCollections.GetInstances(new[] { instanceID }).FirstOrDefault();
            if (instance == null || !instance.Field<bool>("ScheduleChangesEnabled"))
            {
                MessageBox.Show(owner, "The schedule can't be changed for this instance.\n\nSchedule changes must be allowed in the service configuration tool for the service that collects the instance.",
                    caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            var importAgentID = instance.Field<int>("ImportAgentID");
            var collectAgentID = instance.Field<int>("CollectAgentID");
            return new ScheduleTarget(instanceID, instance.Field<string>("ConnectionID"), instance.Field<string>("InstanceDisplayName"),
                collectAgentID, importAgentID,
                DBADashAgent.GetDBADashAgent(Common.ConnectionString, collectAgentID),
                DBADashAgent.GetDBADashAgent(Common.ConnectionString, importAgentID));
        }

        /// <summary>The schedule as the service holds it (see <see cref="CollectionScheduleGetMessage"/>), or the error.</summary>
        private static async Task<(DataSet Data, string Error)> GetScheduleDataAsync(ScheduleTarget target, MessagingHelper.SetStatusDelegate setStatus)
        {
            setStatus("Getting schedule from the service...", null, DashColors.Information);
            var outcome = await MessagingHelper.SendAndWaitAsync(
                new CollectionScheduleGetMessage { ConnectionID = target.ConnectionID, CollectAgent = target.CollectAgent, ImportAgent = target.ImportAgent, Lifetime = 60 },
                target.ImportAgentID, setStatus);
            if (outcome.Succeeded && outcome.Data != null)
            {
                setStatus(string.Empty, null, DashColors.Information);
                return (outcome.Data, null);
            }
            return (null, outcome.Error ?? "no data returned");
        }

        /// <summary>Lists the instances collected by the service that collects <paramref name="instanceID"/>, with any schedules of their own.</summary>
        public static async Task<ScheduleActionResult> ShowServiceInstancesAsync(IWin32Window owner, int instanceID,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            var target = GetTarget(owner, instanceID, "Service Instances");
            if (target == null) return null;
            var (scheduleData, error) = await GetScheduleDataAsync(target, setStatus);
            if (scheduleData == null)
            {
                return new ScheduleActionResult(false, $"Unable to get the schedule from the service: {error}");
            }
            var instances = CollectionScheduleGetMessage.ReadServiceInstances(scheduleData);
            if (instances == null)
            {
                return new ScheduleActionResult(false, "The service didn't return its instances.  It may need to be upgraded.");
            }
            ServiceInstancesDialog.Show(owner, target.ServiceName, instances);
            return null;
        }

        /// <summary>
        /// Shows the schedule dialog for <paramref name="instanceID"/> (or its service when <paramref name="serviceLevel"/>)
        /// and sends any changes.  Returns null if nothing was sent, otherwise the outcome to show the user.
        /// </summary>
        public static async Task<ScheduleActionResult> ConfigureAsync(IWin32Window owner, int instanceID, bool serviceLevel,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            var target = GetTarget(owner, instanceID, "Configure Schedule");
            if (target == null) return null;
            var (scheduleData, error) = await GetScheduleDataAsync(target, setStatus);
            if (scheduleData == null)
            {
                return new ScheduleActionResult(false, $"Unable to get the schedule from the service: {error}");
            }

            var serviceSchedules = CollectionScheduleGetMessage.ReadSchedules(scheduleData, CollectionScheduleGetMessage.LevelService);
            var instanceSchedules = CollectionScheduleGetMessage.ReadSchedules(scheduleData, CollectionScheduleGetMessage.LevelInstance);
            var temporarySchedules = CollectionScheduleGetMessage.ReadTemporarySchedules(scheduleData);
            var isInConfig = (bool)scheduleData.Tables["Instance"]!.Rows[0]["IsInConfig"];

            var baseSchedule = serviceLevel
                ? CollectionSchedules.DefaultSchedules
                : CollectionSchedules.Combine(CollectionSchedules.DefaultSchedules, serviceSchedules);
            var before = serviceLevel ? serviceSchedules : instanceSchedules;

            using var frm = new ScheduleConfig
            {
                Text = serviceLevel ? $"Service Schedule - {target.ServiceName}" : $"Instance Schedule - {target.InstanceName}",
                BaseSchedule = baseSchedule,
                ConfiguredSchedule = before,
                AllowTemporary = !serviceLevel,
                TemporarySchedulesText = serviceLevel ? null : DescribeTemporary(temporarySchedules),
                TemporaryUntil = DateHelper.AppNow.Add(DefaultTemporaryDuration)
            };
            var serviceInstances = serviceLevel ? CollectionScheduleGetMessage.ReadServiceInstances(scheduleData) : null;
            if (serviceInstances != null)
            {
                // Which instances a change to each row would affect - those without their own schedule for the collection
                frm.InstanceCount = serviceInstances.Count;
                frm.InstancesFollowing = baseSchedule.Keys.ToDictionary(type => type, type => serviceInstances.Count(i => i.FollowsServiceSchedule(type)));
                frm.ShowInstances = (dialogOwner, type) => ServiceInstancesDialog.Show(dialogOwner, target.ServiceName, serviceInstances, type);
            }
            if (frm.ShowDialog(owner) != DialogResult.OK) return null;

            var message = new CollectionScheduleUpdateMessage
            {
                Scope = serviceLevel ? ScheduleScopes.Service : ScheduleScopes.Instance,
                ConnectionID = target.ConnectionID,
                CollectAgent = target.CollectAgent,
                ImportAgent = target.ImportAgent
            };
            string done;
            if (frm.EndTemporarySchedules)
            {
                message.Temporary = true;
                message.Changes = temporarySchedules.Keys.ToDictionary(type => type, _ => (CollectionSchedule)null);
                done = $"Temporary schedules ended for {target.InstanceName}";
            }
            else if (frm.ApplyTemporarily)
            {
                if (!frm.TemporaryUntil.TryAppTimeZoneToUtc(out var temporaryUntilUtc))
                {
                    return new ScheduleActionResult(false, $"{frm.TemporaryUntil:yyyy-MM-dd HH:mm} doesn't exist in the {DateHelper.AppTimeZone.DisplayName} time zone - it's skipped when the clocks go forward.  Choose another time.");
                }
                message.Temporary = true;
                message.TemporaryUntilUtc = temporaryUntilUtc;
                message.Changes = GetChanges(baseSchedule, before, frm.ConfiguredSchedule, temporary: true);
                done = $"Temporary schedule applied to {target.InstanceName} until {PauseCollections.FormatPausedUntil(temporaryUntilUtc)}";
            }
            else
            {
                if (!serviceLevel && !isInConfig)
                {
                    MessageBox.Show(owner, $"{target.InstanceName} was discovered by the service rather than defined in its config (e.g. an Azure DB found via the master connection), so its schedule can only be changed temporarily.  Change the service schedule to change it permanently.",
                        "Configure Schedule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
                message.Changes = GetChanges(baseSchedule, before, frm.ConfiguredSchedule, temporary: false);
                done = serviceLevel ? $"Service schedule updated for {target.ServiceName}" : $"Schedule updated for {target.InstanceName}";
            }

            if (message.Changes.Count == 0)
            {
                return new ScheduleActionResult(true, "No changes to the schedule");
            }

            var action = frm.EndTemporarySchedules ? UserActionLog.ActionEndTemporarySchedule
                : frm.ApplyTemporarily ? UserActionLog.ActionTemporarySchedule
                : UserActionLog.ActionSchedule;
            var outcome = await UserActionLog.SendLoggedAsync(message, target.ImportAgentID, target.CollectAgentID,
                serviceLevel ? Array.Empty<int>() : new[] { target.InstanceID }, action, UserActionLog.ScheduleChangeDetails(message), setStatus);
            if (!outcome.Succeeded)
            {
                return new ScheduleActionResult(false, $"Failed to update the schedule: {outcome.Error}");
            }
            var warnings = outcome.Warnings;
            return warnings.Count > 0
                ? new ScheduleActionResult(true, $"{done}.  Warning: {string.Join("  ", warnings)}", IsWarning: true)
                : new ScheduleActionResult(true, done);
        }

        private static string DescribeTemporary(Dictionary<CollectionType, TemporarySchedule> temporarySchedules)
        {
            if (temporarySchedules.Count == 0) return null;
            var items = temporarySchedules.OrderBy(s => s.Value.UntilUtc).Select(s =>
                $"{s.Key} ({CronParser.GetScheduleDescriptionSafe(s.Value.Schedule, runOnServiceStart: s.Value.RunOnServiceStart)}) until {PauseCollections.FormatPausedUntil(s.Value.UntilUtc)}");
            return "Temporary schedules in effect: " + string.Join(", ", items);
        }
    }
}
